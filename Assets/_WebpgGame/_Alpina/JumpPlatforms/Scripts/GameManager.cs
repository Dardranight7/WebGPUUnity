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

    [Header("Audio")] 
    public AudioClip gameMusicClip;
    
    [Header("Victory Audio")]
    public AudioClip victoryMusicClip;
    public float victoryFadeTime = 0.1f;
    public bool useMusicManagerForVictory = true;
    public float localVictoryVolume = 1f; // volumen si se usa AudioSource local
    private AudioSource _victoryAudioSource;
    

    void Awake()
    {
        gameStared = false;
        gameEnded = false;
    }

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
    
    //Métodos para ajustar volumenes desde código
    public void SetMusicVolume(float linear01)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(linear01);
        PlayerPrefs.SetFloat("MusicVolume", linear01);
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
                        winnerCamera = bot.botCamera;
                        winnerCamFollow = bot.GetComponent<CameraFollow>();
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
    
    //prueba DoVictoryCameraPan
    IEnumerator DoVictoryCameraPan(Camera cam, CameraFollow camFollow, Transform winnerTransform)
    {
        if (cam == null)
        {
            Debug.LogWarning("DoVictoryCameraPan: cámara del ganador es null. Se aborta paneo.");
            yield break;
        }

        // 1. Identificar si es player o bot
        PlayerController player = winnerTransform.GetComponent<PlayerController>();
        BotController bot = winnerTransform.GetComponent<BotController>();
        Camera winnercam = (player != null) ? player.playerCamera : bot?.botCamera;

        if (winnercam == null)
        {
            Debug.LogError("No se pudo obtener la cámara del ganador");
            yield break;
        }

        Debug.Log($"DoVictoryCameraPan: preparando paneo para '{winnerTransform.name}'");

        // 2. DESACTIVAR TODO EN EL GANADOR
        if (bot != null)
        {
            bot.enabled = false;
            Rigidbody rb = bot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Animator anim = bot.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }

        if (player != null)
        {
            player.enabled = false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            Animator anim = player.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }

        // 3. DESACTIVAR TODOS LOS CameraFollow en la escena
        CameraFollow[] allCameraFollows = FindObjectsOfType<CameraFollow>();
        foreach (var cf in allCameraFollows)
        {
            cf.enabled = false;
        }
        Debug.Log($"{allCameraFollows.Length} CameraFollow desactivados");

        // 4. DESACTIVAR TODAS LAS CÁMARAS
        Camera[] allCameras = Camera.allCameras;
        foreach (var c in allCameras)
        {
            c.enabled = false;
        }

        // 5. DESACOPLAR LA CÁMARA DEL GANADOR DE SU PADRE (CRÍTICO)
        Transform originalParent = winnercam.transform.parent;
        winnercam.transform.SetParent(null); // <-- Esto desvincula la cámara del bot
        Debug.Log("Cámara desacoplada del ganador");

        // 6. Activar solo la cámara del ganador
        winnercam.rect = new Rect(0, 0, 1, 1);
        winnercam.enabled = true;

        // 7. Hacer el paneo
        Vector3 targetCenter = (finishPoint != null) ? finishPoint.position : winnerTransform.position;
        Vector3 targetCamPos = targetCenter + victoryCameraOffset;

        float elapsed = 0f;
        Vector3 startPos = winnercam.transform.position;
        Quaternion startRot = winnercam.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(targetCenter - targetCamPos, Vector3.up);

        while (elapsed < victoryPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / victoryPanDuration);
            
            // Mover la cámara directamente (ya no está vinculada al bot)
            winnercam.transform.position = Vector3.Lerp(startPos, targetCamPos, t);
            winnercam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            
            yield return null;
        }

        // 8. Fijar posición final
        winnercam.transform.position = targetCamPos;
        winnercam.transform.rotation = targetRot;

        Debug.Log("✅ Paneo completado sin vibraciones");

        // 9. OPCIONAL: Si quieres restaurar el parent después del paneo
        // winnercam.transform.SetParent(originalParent);

    yield break;
}
    
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

        }

        StopAllBots();
        
        //resultados UI despues de perder
        StartCoroutine(ShowResultadosUIDelayed(2f));
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
    
    // de inicio de juegos con bots detenidos
    public void StartGameProperly()
    {
        gameStared = true;
        StartBots();
        Debug.Log("Juego iniciado");
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

    public TournamentManager tournamentManager;
    [SerializeField] GameResume gameResume;

    IEnumerator ShowResultadosUIDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultadosUIPanel != null)
            resultadosUIPanel.SetActive(true);

        gameResume.gameObject.SetActive(true);

        yield return new WaitForSeconds(4);
        PlayVictoryMusic(true);
        tournamentManager.LoadNextGameUsingCourtain();
    }
    
    
    void EnsureVictoryAudioSource()
    {
        if (_victoryAudioSource == null)
        {
            _victoryAudioSource = gameObject.AddComponent<AudioSource>();
            _victoryAudioSource.playOnAwake = false;
            _victoryAudioSource.loop = false;
            // si usas AudioMixer y quieres que vaya al grupo Music o SFX, asigna aquí:
            // _victoryAudioSource.outputAudioMixerGroup = <tuMixerGroup>;
        }
    }
    void PlayVictoryMusic(bool immediateStop = false)
    {
        if (victoryMusicClip == null)
        {
            Debug.Log("PlayVictoryMusic: vicxtorymusicclip no asignado");
            return;
        }

        if (AudioManager.Instance != null)
        {
            if (immediateStop)
            {
                // Detener inmediatamente la música actual y reproducir la de victoria
                AudioManager.Instance.StopMusic(0f); // stop sin fade
                AudioManager.Instance.PlayMusic(victoryMusicClip, 0f); // play sin fade
                Debug.Log("PlayVictoryMusic: detuvo música (inmediato) y reprodujo victoria vía MusicManager.");
            }
            else
            {
                // Cross-fade: MusicManager cambia la pista (esto "detiene" la música de fondo gradualmente)
                AudioManager.Instance.PlayMusic(victoryMusicClip, victoryFadeTime);
                Debug.Log("PlayVictoryMusic: cross-fade a música de victoria vía MusicManager.");
            }
            return;
        }
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