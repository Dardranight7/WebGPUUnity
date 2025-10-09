using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool isPaused = false;
    public int targetFrameRate = 60;
    private bool gameEnded = false; // Controlar si el juego terminó

    [Header("Player & Bots UI")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerScoreText;
    public List<TextMeshProUGUI> botNameTexts;
    public List<TextMeshProUGUI> botScoreTexts;
    private string[] botNames = { "Mochi", "Luna", "Dani", "Ziggy", "Nova", "Bolt", "Jenn", "Andy", "Pepe", "Mari", "Pau", "Juan", "Diego", "Fer" };
    private List<float> botScores = new List<float>();

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;  // Panel de Game Over
    public GameObject WinPanel;       // Panel de Victoria
    public TextMeshProUGUI winnerNameText;

    [Header("Finish / Victory Cinematic")]
    public Transform finishPoint;                     // Asignar en Inspector: transform de la meta
    public Vector3 victoryCameraOffset = new Vector3(0f, 3f, -6f);
    public float victoryPanDuration = 1.0f;
    public float victoryHoldTime = 0.8f;
    public bool teleportPlayerWhenBotsLose = true;
    public GameObject resultadosUIPanel;

    [Header("References")]
    public PlayerController player;
    public PlatformGenerator platformGenerator;
    public CameraFollow cameraFollow;

    [Header("Scoring")]
    public float scoreMultiplier = 2f;
    public int jumpBonus = 5;

    private float score = 0f;
    private float startHeight = 0f;
    private bool gameStarted = false;
    private int jumpsCount = 0;

    private List<string> podium = new List<string>();
    public int totalPlayers = 4;
    public bool gameStared = false;

    // Guardar estado cámara para restaurar
    private bool cameraFollowWasEnabled = true;
    private Vector3 cameraOriginalPosition;
    private Quaternion cameraOriginalRotation;

    void Start()
    {
        Application.targetFrameRate = targetFrameRate;

        // Asegurar que los paneles estén OCULTOS al inicio
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(false);
        if (resultadosUIPanel != null) resultadosUIPanel.SetActive(false);

        // Inicializar referencias
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        if (platformGenerator == null)
            platformGenerator = FindFirstObjectByType<PlatformGenerator>();

        if (player != null)
        {
            startHeight = player.transform.position.y;
            player.OnDie.AddListener(GameOver);
            player.OnJump.AddListener(OnPlayerJump);
        }

        // Configurar nombre del jugador
        string playerName = PlayerPrefs.GetString("PlayerName", "P1");
        if (playerNameText != null)
            playerNameText.text = playerName;

        // Configurar nombres de bots aleatorios
        System.Random rnd = new System.Random();
        List<int> usedIndexes = new List<int>();
        botScores.Clear();
        for (int i = 0; i < botNameTexts.Count; i++)
        {
            int idx;
            do
            {
                idx = rnd.Next(botNames.Length);
            } while (usedIndexes.Contains(idx));
            usedIndexes.Add(idx);

            if (botNameTexts[i] != null)
                botNameTexts[i].text = botNames[idx];
            botScores.Add(0f);
        }

        UpdateUI();
        gameEnded = false;
    }

    public void StartBots()
    {
        var bots = FindObjectsOfType<BotController>();
        foreach (var bot in bots)
            bot.StartBot();
    }

    void Update()
    {
        // No actualizar scores si el juego terminó
        if (gameEnded) return;

        // Score del jugador principal
        if (player != null && player.IsAlive)
        {
            float currentHeight = player.transform.position.y - startHeight;
            float newScore = (currentHeight * scoreMultiplier) + (jumpsCount * jumpBonus);
            score = Mathf.Max(score, newScore);
        }

        // Score de Bots (SOLO si el juego NO ha terminado)
        if (!gameStarted)
        {
            for (int i = 0; i < botScores.Count; i++)
            {
                botScores[i] += Random.Range(0f, 0.5f);
            }
        }
        UpdateUI();
    }

    void OnPlayerJump()
    {
        jumpsCount++;
    }

    void UpdateUI()
    {
        // Score del player 
        if (scoreText != null)
            scoreText.text = Mathf.FloorToInt(score).ToString();
        if (playerScoreText != null)
            playerScoreText.text = Mathf.FloorToInt(score).ToString();

        // Score de los bots
        for (int i = 0; i < botScoreTexts.Count; i++)
        {
            if (botScoreTexts != null && i < botScoreTexts.Count && botScoreTexts[i] != null)
            {
                botScoreTexts[i].text = Mathf.FloorToInt(botScores[i]).ToString();
            }
        }
    }

    /*public void RegisterFinish(string name)
    {
        if (gameEnded) return;

        if (!podium.Contains(name))
        {
            podium.Add(name);
            Debug.Log($"🏅 {name} ha terminado en posición {podium.Count}");
        }

        if (podium.Count == 1)
        {
            // Todos han terminado, finalizar el juego

            // --- Cambios para paneo y teleport al ganador ---
            Transform winnerTransform = null;
            CameraFollow winnerCamFollow = null;
            string localPlayerName = PlayerPrefs.GetString("PlayerName", "P1");

            if (player != null && name == localPlayerName)
            {
                winnerTransform = player.transform;
                winnerCamFollow = winnerTransform.GetComponent<CameraFollow>();
            }
            else
            {
                var bots = FindObjectsOfType<BotController>();
                foreach (var bot in bots)
                {
                    if (bot.botName == name)
                    {
                        winnerTransform = bot.transform;
                        winnerCamFollow = bot.GetComponentInChildren<CameraFollow>();
                        break;
                    }
                }
            }
            // Si tienes varias cámaras, aquí puedes usar Camera[] y seleccionar la del ganador
            // Camera winnerCamera = winnerTransform.GetComponentInChildren<Camera>();
            StartCoroutine(HandleVictorySequence(name, winnerTransform, cameraFollow));
        }
    }*/
    
    //prueba de RegisterFinish
    
    public void RegisterFinish(string name)
    {
        if (gameEnded) return;
        if (!podium.Contains(name))
        {
            podium.Add(name);
            Debug.Log($"🏅 {name} ha terminado en posición {podium.Count}");
        }

        if (podium.Count == 1)
        {
            string localPlayerName = PlayerPrefs.GetString("PlayerName", "P1");
            bool winnerIsPlayer = name == localPlayerName;
            
            Transform winnerTransform = null;
            Camera winnerCamera = null;
            CameraFollow winnerCamFollow = null;
            

            // Si el jugador local ganó
            if (winnerIsPlayer)
            {
                winnerTransform = player.transform;
                winnerCamFollow = cameraFollow;
                winnerCamera = Camera.main;
            }
            else
            {
                // Buscar el bot ganador
                var bots = FindObjectsOfType<BotController>();
                foreach (var bot in bots)
                {
                    if (bot.botName == name)
                    {
                        winnerTransform = bot.transform;
                        winnerCamera = bot.GetComponentInChildren<Camera>();
                        winnerCamFollow = bot.GetComponentInChildren<CameraFollow>();
                    }
                }
            }

            if (winnerTransform == null)
            {
                Debug.LogError($"RegisterFinish: No se pudo encontrar transform del ganador '{name}'.");
                return;
            }

            Debug.Log($"RegisterFinish -> ganador='{name}', winnerIsPlayer={winnerIsPlayer}, transform={winnerTransform.name}, camera={(winnerCamera!=null?winnerCamera.name:"null")}");
            StartCoroutine(HandleVictorySequence(name, winnerTransform, winnerCamera, winnerCamFollow, winnerIsPlayer));
        } 
    }


    /*IEnumerator HandleVictorySequence(string winnerName, Transform winnerTransform, CameraFollow winnerCamFollow)
    {
        gameEnded = true; // Evitar más registros

        // Detener bots
        StopAllBots();

        // determinar si el ganador es el jugador
        string localPlayerName = PlayerPrefs.GetString("PlayerName", "P1");
        bool winnerIsPlayer = (winnerName == localPlayerName) || (player != null && winnerName == player.gameObject.name);

        // Desactivar inputs del player mientras hacemos cinematica
        if (player != null)
            player.enabled = false;

        if (winnerIsPlayer)
        {
            StartCoroutine(WaitTeleport());
        }
        else
        {
            if (teleportPlayerWhenBotsLose)
                StartCoroutine(WaitTeleport());
        }

        SetPlayerIdle();
        yield return StartCoroutine(DoVictoryCameraPan());
        yield return new WaitForSeconds(victoryHoldTime);

        ShowWinPanel(winnerName, winnerIsPlayer);

        Debug.Log($"Secuencia de victoria completada para {winnerName}");
    }*/
    
    //prueba HandleVictorySequence
    IEnumerator HandleVictorySequence(string winnerName, Transform winnerTransform, Camera winnerCamera, CameraFollow winnerCamFollow, bool winnerIsPlayer)
    {
        //
        gameEnded = true;
        StopAllBots();

        Debug.Log($"HandleVictorySequence: ganador={winnerName}, winnerIsPlayer={winnerIsPlayer}, transform={winnerTransform?.name}");

        // Desactivar inputs del player (si existe)
        if (player != null)
            player.enabled = false;

        // Teleportar SOLO al ganador (sea bot o player)
        TeleportTransformToFinish(winnerTransform);
        

        SetPlayerIdle();

        // Paneo usando la cámara del ganador (si existe)
        yield return StartCoroutine(DoVictoryCameraPan(winnerCamera, winnerCamFollow, winnerTransform));
        yield return new WaitForSeconds(victoryHoldTime);

        

        ShowWinPanel(winnerName, winnerIsPlayer);
        StartCoroutine(ShowResultadosUIDelayed(2f));
        Debug.Log($"Secuencia de victoria completada para {winnerName}");
    }
    

    /*void ShowWinPanel(string winnerName, bool winnerIsPlayer)
    {
        if (WinPanel != null)
            WinPanel.SetActive(true);

        if (winnerNameText != null)
        {
            if (winnerIsPlayer)
                winnerNameText.text = "¡Ganaste!";
            else
                winnerNameText.text = $"Perdiste";
        }

        Debug.Log("✅ WinPanel activado por secuencia de victoria");
    }*/
    
    //pruebaShowInPanle
    void ShowWinPanel(string winnerName, bool winnerIsPlayer)
    {
        if (winnerIsPlayer)
        {
            // Gana el jugador, muestra panel de victoria
            if (WinPanel != null)
                WinPanel.SetActive(true);
            if (winnerNameText != null)
                winnerNameText.text = "¡Ganaste!";
            Debug.Log("✅ WinPanel activado. Resultado: Ganó el jugador");
        }
        else
        {
            // Gana el bot, muestra panel de game over
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            if (winnerNameText != null)
                winnerNameText.text = "Perdiste";
            Debug.Log("❌ GameOverPanel activado. Resultado: Ganó un bot");
        }
    }

    /*IEnumerator DoVictoryCameraPan()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            yield break;

        // Guardar estado original
        cameraFollowWasEnabled = (cameraFollow != null) ? cameraFollow.enabled : false;
        cameraOriginalPosition = mainCam.transform.position;

        // Desactivar CameraFollow para controlar la cámara manualmente
        if (cameraFollow != null)
            cameraFollow.enabled = false;

        // Definir destino de la cámara: centrado en el jugador + offset
        Vector3 targetCenter = finishPoint.position;
        Vector3 targetCamPos = targetCenter + victoryCameraOffset;

        float elapsed = 0f;
        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        // Mirar hacia el targetCenter
        Quaternion targetRot = Quaternion.LookRotation(targetCenter - targetCamPos, Vector3.up);

        while (elapsed < victoryPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / victoryPanDuration);
            mainCam.transform.position = Vector3.Lerp(startPos, targetCamPos, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        // Asegurar la posición final
        mainCam.transform.position = targetCamPos;
        mainCam.transform.rotation = targetRot;
        yield break;
    }*/
    
    //pruebaa DoVictoryCameraPan
    
    IEnumerator DoVictoryCameraPan(Camera cam, CameraFollow camFollow, Transform winnerTransform)
    {
        if (cam == null)
        {
            Debug.LogWarning("DoVictoryCameraPan: cámara del ganador es null. Se aborta paneo.");
            yield break;
        }

        // verificación de winner transform en camera 
        PlayerController player = winnerTransform.gameObject.GetComponent<PlayerController>();
        BotController bot = null;
        Camera winnerCam = null;

        if (player == null)
        {
            //ganador es bot
            bot = winnerTransform.gameObject.GetComponent<BotController>();
            winnerCam = bot.botCamera;
        }
        else
        {
            //ganador es player
            winnerCam = player.playerCamera;
        }
        
        player.playerCamera.rect = new Rect(0, 0, 1, 1);
        bot.botCamera.rect = new Rect(0, 0, 1, 1);

        // Desactivar CameraFollow temporalmente si existe
        bool followWasEnabled = false;
        if (camFollow != null)
        {
            followWasEnabled = camFollow.enabled;
            camFollow.enabled = false;
        }

        Vector3 targetCenter = (finishPoint != null) ? finishPoint.position : (winnerTransform != null ? winnerTransform.position : Vector3.zero);
        Vector3 targetCamPos = targetCenter + victoryCameraOffset;

        float elapsed = 0f;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(targetCenter - targetCamPos, Vector3.up);

        while (elapsed < victoryPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / victoryPanDuration);
            cam.transform.position = Vector3.Lerp(startPos, targetCamPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.position = targetCamPos;
        cam.transform.rotation = targetRot;

        // Si quieres restaurar el seguimiento después del paneo, descomenta la siguiente línea
        // if (camFollow != null) camFollow.enabled = followWasEnabled;

        yield break;
    }
/*
    [ContextMenu("Teleport Player To Finish")]
    void TeleportPlayerToFinish()
    {
        if (player == null || finishPoint == null) return;

        Vector3 targetPos = finishPoint.position;
        player.transform.position = targetPos;
        player.transform.eulerAngles = new Vector3(0f, 180f, 0f);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"🚀 Jugador teletransportado a la meta en {targetPos}");
    }*/

//prueba TeleportPlayerToFinish
    void TeleportTransformToFinish(Transform target)
    {
        if (target == null || finishPoint == null)
        {
            Debug.LogWarning("TeleportTransformToFinish: target o finishPoint es null.");
            return;
        }
        
        Debug.Log($"TeleportTransformToFinish: teleportando '{target.name}' a {finishPoint.position}");
        
        
        Rigidbody rb = target.GetComponent<Rigidbody>();
        PlayerController playerController = target.GetComponent<PlayerController>();
        BotController botController = target.GetComponent<BotController>();
        
        Debug.Log("pos antes" + target.position + "pos Finish" + finishPoint.position);
        Debug.Log ("Pos despues" + target.position);


        if (playerController != null)
        {
            playerController.StopAllCoroutines();
            playerController.isJumping = false;
            playerController.enabled = true;
        }

        if (botController != null)
        {
            botController.StopAllCoroutines();
            botController.canMove = false;
            botController.enabled = true;
        }
        
        // Detener física momentáneamente
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;         // <-- propiedad correcta
            rb.angularVelocity = Vector3.zero;  // <-- propiedad correcta
        }
        
        // teletransportar
        target.position = finishPoint.position;
        target.rotation = Quaternion.Euler(0f, 180f, 0f);
        Debug.Log($"Nueva posición confirmada : {target.position}");
        
        
        Debug.Log($"Nueva posición confirmada : {target.position}");
    }

    /*IEnumerator WaitTeleport()
    {
        yield return new WaitForSeconds(1f);
        TeleportPlayerToFinish();
    }*/

    void SetPlayerIdle()
    {
        if (player == null) return;

        Animator anim = player.GetComponent<Animator>();
        if (anim == null) return;

        // Buscar si existe un Trigger "Idle"
        bool hasIdleTrigger = false;
        bool hasIsJumpingBool = false;

        foreach (var p in anim.parameters)
        {
            if (p.name == "Idle" && p.type == AnimatorControllerParameterType.Bool)
                hasIdleTrigger = true;
            if (p.name == "Jump" && p.type == AnimatorControllerParameterType.Trigger)
                hasIsJumpingBool = true;
        }

        if (hasIdleTrigger)
        {
            anim.SetTrigger("Jump");
            Debug.Log("Animator: disparado trigger 'Idle'");
        }
        else if (hasIsJumpingBool)
        {
            anim.SetBool("Idle", false);
            Debug.Log("Animator: seteado isJumping = false");
        }
        else
        {
            Debug.Log("Animator: no se encontró 'Idle' ni 'isJumping'. No se aplicó cambio en Animator.");
        }
    }

    public void WinGame(string winnerName = "")
    {
        if (gameEnded) return; // Evitar llamadas múltiples
        gameEnded = true; // Detener el juego

        Debug.Log($"¡{winnerName} ha GANADO! Score final: {Mathf.FloorToInt(score)}");

        // Mostrar panel de victoria
        if (WinPanel != null)
        {
            WinPanel.SetActive(true);
            Debug.Log("✅ WinPanel activado");

            // Mostrar nombre del ganador
            if (winnerNameText != null)
            {
                if (string.IsNullOrEmpty(winnerName))
                    winnerName = PlayerPrefs.GetString("PlayerName", "P1");

                winnerNameText.text = $"¡Ganador: {winnerName}!";
                Debug.Log($"✅ Nombre del ganador mostrado: {winnerName}");
            }
        }
        else
        {
            Debug.LogError("WinPanel es NULL - verifica el Inspector");
        }

        StopAllBots();
    }

    public void GameOver()
    {
        if (gameEnded) return; // Evitar llamadas múltiples
        gameEnded = true; // Detener el juego

        Debug.Log($"Game Over! Score final: {Mathf.FloorToInt(score)}");

        // Mostrar panel de Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("GameOverPanel activado");
        }
        else
        {
            Debug.LogError("gameOverPanel es NULL - verifica el Inspector");
        }

        StopAllBots();
    }

    void StopAllBots()
    {
        var bots = FindObjectsOfType<BotController>();
        foreach (var bot in bots)
        {
            bot.StopBot();
        }
        Debug.Log($"{bots.Length} bots detenidos");
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Reiniciando el juego...");
        Time.timeScale = 1;
        MochiCourtain.Singleton.LoadSceneWithCourtain(SceneManager.GetActiveScene().name, 1);
    }

    void DelayedStart()
    {
        gameStared = true;
        StartBots();
    }

    public void OnPlayerMoveDown(int rows)
    {
        float puntosPorFila = 2f * scoreMultiplier;
        score = Mathf.Max(0, score - (rows * puntosPorFila));
        UpdateUI();
    }

    public void OnBotMoveDown(string botName, int rows)
    {
        int idx = -1;
        for (int i = 0; i < botNameTexts.Count; i++)
        {
            if (botNameTexts[i] != null && botNameTexts[i].text == botName)
            {
                idx = i;
                break;
            }
        }

        if (idx >= 0 && idx < botScores.Count)
        {
            float puntosPorFila = 2f * scoreMultiplier;
            botScores[idx] = Mathf.Max(0, botScores[idx] - (rows * puntosPorFila));
            UpdateUI();
        }
    }

    public void GoToMainMenu()
    {
        Debug.Log("🏠 Volviendo al menú principal...");
        MochiCourtain.Singleton.LoadSceneWithCourtain("0", 1);
    }

    public void QuitGame()
    {
        Debug.Log("👋 Saliendo del juego...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    IEnumerator ShowResultadosUIDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultadosUIPanel != null)
            resultadosUIPanel.SetActive(true);
    }

    public void CheckBotsStatus()
    {
        var bots = FindObjectsOfType<BotController>();
        bool anyBotAlive = false;
        foreach (var b in bots)
        {
            if (b != null && b.isAlive)
            {
                anyBotAlive = true;
                break;
            }
        }

        if (!anyBotAlive)
        {
            // Todos los bots han muerto
            Debug.Log("[GameManager] Todos los bots han muerto. El jugador gana.");
            // si el player sigue vivo y no llegó a la meta. darle la victoria
            if (player != null && player.IsAlive && !player.arrived)
            {
                Debug.Log("[GameManager] Se otorgará la victoria al player automáticamente.");
                RegisterFinish(PlayerPrefs.GetString("PlayerName", "P1"));
            }
        }
    }
}