using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool isPaused = false;
    public int targetFrameRate = 60;
    private bool gameEnded = false; // ✅ NUEVO: controlar si el juego terminó

    [Header("Player & Bots UI")] 
    public Text playerNameText;
    public Text playerScoreText;
    public List<Text> botNameTexts;
    public List<Text> botScoreTexts;
    
    private string[] botNames = { "Mochi", "Luna", "Dani", "Ziggy", "Nova", "Bolt", "Jenn", "Andy", "Pepe", "Mari", "Pau", "Juan", "Diego", "Fer" };
    private List<float> botScores = new List<float>();
    
    [Header("UI References")]
    public Text scoreText;
    public GameObject gameOverPanel;  // ✅ Panel de Game Over
    public GameObject WinPanel;       // ✅ Panel de Victoria
    public Text winnerNameText;  
    
    [Header("Finish / Victory Cinematic")]
    public Transform finishPoint;                     // Asignar en Inspector: transform de la meta
    public Vector3 victoryCameraOffset = new Vector3(0f, 3f, -6f);
    public float victoryPanDuration = 1.0f;
    public float victoryHoldTime = 0.8f;              // tiempo que se mantiene la cámara antes de mostrar el panel
    public bool teleportPlayerWhenBotsLose = true;     
    
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
        
        // ✅ Asegurar que los paneles estén OCULTOS al inicio
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(false);
        
        
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
        // ✅ NO actualizar scores si el juego terminó
        if (gameEnded) return;
        
        // Score del jugador principal
        if (player != null && player.IsAlive)
        {
            float currentHeight = player.transform.position.y - startHeight;
            float newScore = (currentHeight * scoreMultiplier) + (jumpsCount * jumpBonus);
            score = Mathf.Max(score, newScore);
        }
        
        // ✅ Score de Bots (SOLO si el juego NO ha terminado)
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
            scoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        if (playerScoreText != null)
            playerScoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        
        // Score de los bots
        for (int i = 0; i < botScoreTexts.Count; i++)
        {
            if (botScoreTexts != null && i < botScoreTexts.Count && botScoreTexts[i] != null)
            {
                botScoreTexts[i].text = "Score: " + Mathf.FloorToInt(botScores[i]).ToString();
            }
        }
    }
    
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
            // Todos han terminado, finalizar el juego
            StartCoroutine(HandleVictorySequence(podium[0]));
        }
    }
    
    IEnumerator HandleVictorySequence(string winnerName)
    {
        gameEnded = true; // Evitar más registros
        
        // Detener bots
        StopAllBots();
        
        // determinar si el ganador es el jugador
        string localPlayerName = PlayerPrefs.GetString("PlayerName", "P1");
        bool winnerIsPlayer = (winnerName == localPlayerName) || (player != null &&  winnerName == player.gameObject.name);
        
        //Desactivar inputs del player mientras hacemos cinematica
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
    }
    void ShowWinPanel(string winnerName, bool winnerIsPlayer)
    {
        if (WinPanel != null)
            WinPanel.SetActive(true);

        if (winnerNameText != null)
        {
            if (winnerIsPlayer)
                winnerNameText.text = "¡Ganaste!";
            else
                winnerNameText.text = $"¡Ganador: {winnerName}!";
        }

        Debug.Log("✅ WinPanel activado por secuencia de victoria");
    }

    public float rotationYCam = 180f;
    
    IEnumerator DoVictoryCameraPan()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            yield break;

        // Guardar estado original
        cameraFollowWasEnabled = (cameraFollow != null) ? cameraFollow.enabled : false;
        cameraOriginalPosition = mainCam.transform.position;
        cameraOriginalRotation = mainCam.transform.rotation.y == rotationYCam ? mainCam.transform.rotation : Quaternion.Euler(0f, rotationYCam, 0f);

        // Desactivar CameraFollow para controlar la cámara manualmente
        if (cameraFollow != null)
            cameraFollow.enabled = false;

        // Definir destino de la cámara: centrado en el jugador + offset
        Vector3 targetCenter;
        
        targetCenter = finishPoint.position;

        Vector3 targetCamPos = targetCenter + victoryCameraOffset;

        float elapsed = 0f;
        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        // Opcional: mirar hacia el targetCenter
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
    }

    
    [ContextMenu("Teleport Player To Finish")]
    void TeleportPlayerToFinish()
    {
        if (player == null || finishPoint == null) return;
        
        Vector3 targetPos = finishPoint.position;
        

        player.transform.position = targetPos;
        player.transform.rotation = finishPoint.rotation;
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        
        
        Debug.Log($"🚀 Jugador teletransportado a la meta en {targetPos}");
    }

    IEnumerator WaitTeleport()
    {
        yield return new WaitForSeconds(1f);
        TeleportPlayerToFinish();
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
            if (p.name == "Idle" && p.type == AnimatorControllerParameterType.Trigger)
                hasIdleTrigger = true;
            if (p.name == "isJumping" && p.type == AnimatorControllerParameterType.Bool)
                hasIsJumpingBool = true;
        }

        if (hasIdleTrigger)
        {
            anim.SetTrigger("Idle");
            Debug.Log("Animator: disparado trigger 'Idle'");
        }
        else if (hasIsJumpingBool)
        {
            anim.SetBool("isJumping", false);
            Debug.Log("Animator: seteado isJumping = false");
        }
        else
        {
            // Si no hay parámetros esperados, solo desactivar el controlador de movimiento (por seguridad)
            // Puedes desactivar componentes de control del personaje aquí si lo necesitas.
            Debug.Log("Animator: no se encontró 'Idle' ni 'isJumping'. No se aplicó cambio en Animator.");
        }
    }
    
    // ✅ MÉTODO MEJORADO: WinGame con nombre del ganador
    public void WinGame(string winnerName = "")
    {
        if (gameEnded) return; // Evitar llamadas múltiples
        
        gameEnded = true; // ✅ Detener el juego
        
        Debug.Log($"🏆 ¡{winnerName} ha GANADO! Score final: {Mathf.FloorToInt(score)}");
        
        // ✅ Mostrar panel de victoria
        if (WinPanel != null)
        {
            WinPanel.SetActive(true);
            Debug.Log("✅ WinPanel activado");
            
            // ✅ Mostrar nombre del ganador
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
            Debug.LogError("❌ WinPanel es NULL - verifica el Inspector");
        }
        
        // ✅ Detener bots
        StopAllBots();
    }
    
    
    // ✅ MÉTODO MEJORADO: GameOver
    public void GameOver()
    {
        
        if (gameEnded) return; // Evitar llamadas múltiples
        
        gameEnded = true; // ✅ Detener el juego
        
        Debug.Log($"💀 Game Over! Score final: {Mathf.FloorToInt(score)}");
        
        // ✅ Mostrar panel de Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("✅ GameOverPanel activado");
        }
        else
        {
            Debug.LogError("❌ gameOverPanel es NULL - verifica el Inspector");
        }
        
        // ✅ Detener bots
        StopAllBots();
    }
    
    void StopAllBots() 
    { 
        var bots = FindObjectsOfType<BotController>();
        foreach (var bot in bots)
        {
            bot.StopBot(); 
            
        } 
        
        Debug.Log($"🛑 {bots.Length} bots detenidos");
        
    }
    
    // ✅ NUEVO: Detener todos los bots
    
    
    // ✅ MÉTODO MEJORADO: RestartGame
    public void RestartGame()
    {
        Debug.Log("🔄 Reiniciando el juego...");
        
        Time.timeScale = 1;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void DelayedStart()
    {
        gameStared = true;
        StartBots();
    }
    

    
    
    // ✅ NUEVO: Método para registrar finalización
    
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
    
    // ✅ NUEVO: Método para ir al menú principal
    public void GoToMainMenu()
    {
        Debug.Log("🏠 Volviendo al menú principal...");
        SceneManager.LoadScene("MainMenu"); // Cambia "MainMenu" por el nombre de tu escena
    }
    
    // ✅ NUEVO: Método para salir del juego
    public void QuitGame()
    {
        Debug.Log("👋 Saliendo del juego...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public void CheckBotsStatus()
    {
        var bots = FindObjectsOfType<BotController>();
        bool anyBotAlive = false;
        foreach (var b in bots)
        {
            if (b != null && b.isAlive) { anyBotAlive = true; break;}
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