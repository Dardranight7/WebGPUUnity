using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManagerBonCollet : MonoBehaviour
{
    [Header("Modo de juego")]
    [Tooltip("Cantidad de puntos que debe alcanzar un jugador para ganar")]
    public int pointsToWin = 20;
    [Tooltip("Duración en segundos")]
    public float gameDuration = 60f;
    [Tooltip("Text UI que muestra el tiempo restante de juego")]
    public Text timerText;
    float timeRemaining;
    [SerializeField] private GameObject tutorialUIGO;
    [Header("Paneo / Cámara")]
    [SerializeField] private CinemachineSplineDolly introCameraDolly; // asignar si quieres paneo y focus al ganador
    [SerializeField] private GameObject miniGameBaseCamera;
    [SerializeField] private CinemachineCamera finalGameBaseCamera;
    [SerializeField] private float timeToStartIntro = 5f;
    
    [Tooltip("Duración del paneo inicial antes de iniciar el juego")]
    public float startPanDuration = 4f;

    [Header("Spawning")]
    public CandySpawner spawner;               // referencia al spawner de gomitas
    public bool stopSpawningOnWin = true;

    [Header("Jugadores y UI")]
    [Tooltip("Lista de jugadores (Player/Bots) y las UI que representan su barra")]
    public List<PlayerSlot> players = new List<PlayerSlot>();

    [Header("UI smoothing")]
    [Tooltip("Velocidad de interpolación al rellenar la barra UI (fillAmount)")]
    public float uiFillLerpSpeed = 8f;
    
    [Header("Resultados UI")]
    public GameObject resultsPanel;
    public GameObject panelWin; //panel que se usa cuando el player principal gana
    public GameObject panelLose; //panel que se usa cuando el player principal pierde
    public Transform pedestalSpot; //lugar donde se muestra el player ganador
    [SerializeField] private TournamentManager tournamentManager;
    
    [Header("Audios")]
    private AudioSource audioSource;
    private AudioManager audioManager;
    
    public bool playMusicOnStart = false;
    public AudioClip backgroundMusic;
    public AudioClip MusicVictoryClip;
    public AudioClip MusicLoseClip;

    public GameObject UIControls;
    bool isPreUIActive = true;
    
    [Header("EndGame Outro Components")]
    //secuencia de victoria para dinamica de juego por tiempo 
    public float winPanDuration = 3f;
    [SerializeField] private GameObject podiumGameObject;
    [SerializeField] private PlayerMochi mochiwinnerView;
    private string winnerMochiId;

    //public GameObject PlayerPrefab;

    // Estado interno
    bool gameRunning = false;
    private void Awake()
    {
        // Buscar AudioManager (como en tu GameManager)
        if (audioManager == null) audioManager = AudioManager.Instance;
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
    }

    void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            if (audioManager != null) audioManager.PlayMusic(backgroundMusic, 1f);
        }
        if (UIControls != null)
            UIControls.SetActive(false);
        
        // Inicializar slots (scores, UI)
        foreach (var p in players)
        {
            if (p.collector != null) p.collector.ResetScore();
            p.currentFill = 0f;
            UpdatePlayerUIImmediate(p);
        }
        
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        foreach (var p in players)
        {
            if (p != null) p.EnableCollector(false);
        }

        if (introCameraDolly != null)
        {
            // iniciar juego luego del paneo
            StartCoroutine(WaitFortutorialTime());
        }
    }
    IEnumerator WaitFortutorialTime()
    {
        yield return new WaitForSeconds(timeToStartIntro);
        
        introCameraDolly.enabled = true;
        if(tutorialUIGO !=null)
            tutorialUIGO.SetActive(false);
        
        yield return new WaitForSeconds(startPanDuration);
        if (UIControls != null) UIControls.SetActive(true);
        miniGameBaseCamera.SetActive(true);
        StartGame();
    }
     
    public void StartGame()
    {
        if (gameRunning) return;

        gameRunning = true;
        timeRemaining = Mathf.Max(0.1f, gameDuration);
        

        // resetear scores y UI
        foreach (var p in players)
        {
            p.score = 0;
            p.currentFill = 0f;
            UpdatePlayerUIImmediate(p);
            p.EnableCollector(true);
        }
        if (spawner != null)
            spawner.StartSpawning();
    }

    // Called by Collector when it collects a candy
    public void AddScore(Collector collector, int points)
    {
        if (!gameRunning) return;

        var slot = players.Find(s => s.collector == collector);
        if (slot == null)
        {
            Debug.LogWarning("GameManagerBonCollet: Collector no registrado en players list: " + (collector != null ? collector.name : "null"));
            return;
        }

        slot.score += points;

        if (slot.score < 0) slot.score = 0;

        // actualizar UI objetivo (no instantáneamente, lo interpolamos en Update para suavidad)
        slot.targetFill = (pointsToWin <= 0) ? 1f : Mathf.Clamp01((float)slot.score / (float)pointsToWin);

        // actualizar texto inmediato (si tiene)
        if (slot.scoreText != null)
            slot.scoreText.text = slot.score.ToString();
        UpdatePosition();
        // verificar ganador en dinamica por puntos
        //if (slot.score >= pointsToWin)
        //{
        // OnPlayerWin(slot);
        //}
    }

    private void UpdatePosition()
    {
        // Clonar la lista y ordenar por score (mayor a menor)
        List<PlayerSlot> rankedPlayers = new List<PlayerSlot>(players);
        rankedPlayers.Sort((a, b) => b.score.CompareTo(a.score));

        int position = 1;
        // Inicializamos con un valor alto para asegurar que el primer jugador sea siempre la posición 1
        int lastScore = int.MaxValue; 

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            PlayerSlot p = rankedPlayers[i];
            
            // Si la puntuación actual es menor que la del jugador anterior, avanzamos de posición.
            // Si la puntuación es igual, mantiene la misma posición (empate).
            if (p.score < lastScore)
            {
                position = i + 1;
            }
            
            // 2. Actualizar el texto de POSICIÓN (usando positionText)
            if (p.positionText != null)
            {
                p.positionText.text = position.ToString();
            }

            // Guardar la puntuación actual para la siguiente iteración
            lastScore = p.score;
        }
    }
    
    // void OnPlayerWin(PlayerSlot winner)
    // {
    //     if (!gameRunning) return;
    //     gameRunning = false;
    //
    //     if (spawner != null && stopSpawningOnWin)
    //         spawner.StopSpawning();
    //
    //     // detener movimiento de todos
    //     foreach (var p in players)
    //         p.EnableCollector(false);
    //     
    //     // cámara hacia ganador si existe
    //     if (introCameraDolly != null && winner != null && winner.collector != null)
    //     {
    //         introCameraDolly.FocusOnWinner(winner.collector.transform);
    //     }
    //     
    //     
    //     Debug.Log("GameManagerBonCollet - Ganador: " + (winner != null ? winner.GetName() + " (" + winner.score + " pts)" : "Nadie"));
    //
    //     // Aquí puedes invocar UI final (panel de victoria), reproducir efectos, etc.
    //     if (winner != null)
    //     {
    //         AudioManager.Instance.StopMusic();
    //         AudioManager.Instance.PlaySFX(MusicVictoryClip);
    //         resultsPanel.SetActive(true);
    //         
    //         if (winner != null && winner.collector != null && !winner.collector.isBot)
    //         {
    //         
    //             // el jugador principal ha ganado
    //             if (panelWin != null)
    //                 panelWin.SetActive(true);
    //             if (panelLose != null)
    //                 panelLose.SetActive(false);
    //         
    //         }
    //         else
    //         {
    //         
    //             // el jugador principal ha perdido
    //             if (panelWin != null)
    //                 panelWin.SetActive(false);
    //             if (panelLose != null)
    //                 panelLose.SetActive(true);
    //         }
    //         
    //         
    //         if (pedestalSpot != null && winner.collector != null)
    //         {
    //             // mover el ganador al pedestal
    //             winner.collector.transform.position = pedestalSpot.position;
    //             winner.collector.transform.rotation = pedestalSpot.rotation;
    //         }
    //     }
    // }

    void Update()
    {
        //Dinamica de juego por tiempo
        //timer
        if (gameRunning)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI(timeRemaining);
            
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                // tiempo agotado, determinar ganador
                gameRunning = false;
                if (spawner != null && stopSpawningOnWin) spawner.StopSpawning();
                foreach (var p in players) p.EnableCollector(false);
                
                //determinamos el ganador
                PlayerSlot winner = DetermineWinnerByScore();
                //paneo y mostrar panel
                StartCoroutine(WinSequenceCoroutine(winner));
            }
        }
    }
    
    
    void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;
        seconds = Mathf.Max(0f, seconds);
        int s = Mathf.CeilToInt(seconds);
        int mins = s / 60;
        int secs = s % 60;
        timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
    }

    PlayerSlot DetermineWinnerByScore()
    {
        PlayerSlot best = null;

        
        int bestScore = int.MinValue;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (p.score > bestScore)
            {
                bestScore = p.score;
                best = p;
            }
        }
        winnerMochiId = best.collector.mochiId;
        return best;
    }

    IEnumerator WinSequenceCoroutine(PlayerSlot winner)
    {
        // protección
        if (winner == null)
        {
            // si no hay ganador, mostramos panel vacío (si quieres cambiar este comportamiento, hazlo)
            if (resultsPanel != null) resultsPanel.SetActive(true);
            yield break;
        }

        // marcar no en ejecución para evitar seguir sumando
        gameRunning = false;

        // detener spawner
        if (spawner != null && stopSpawningOnWin)
            spawner.StopSpawning();
        
        // cámara hacia ganador si existe (paneo)
        if (introCameraDolly != null && winner.collector != null)
        {
            // introCameraDolly.FocusOnWinner(winner.collector.transform);
            if (!winner.collector.isBot)
                mochiwinnerView.isPlayerMochi = true;
            else
                mochiwinnerView.mochiIndexPref = winner.collector.mochiId;
             
            podiumGameObject.SetActive(true);
            miniGameBaseCamera.gameObject.SetActive(false);
            finalGameBaseCamera.gameObject.SetActive(true);
        }
        // detener movimiento de todos
        foreach (var p in players)
            p.EnableCollector(false);
        // esperar un tiempo para dejar que se vea el paneo
        if (winPanDuration > 0f)
            yield return new WaitForSeconds(winPanDuration);
        
        //Reproducimos música de victoria o derrota
        bool winnerisPlayer = (winner.collector != null && !winner.collector.isBot);

        if (winnerisPlayer)
        {
            if (MusicVictoryClip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopMusic();
                    AudioManager.Instance.PlaySFX(MusicVictoryClip);
                }
            }
        }
        else
        {
            if (MusicLoseClip != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopMusic();
                    AudioManager.Instance.PlaySFX(MusicLoseClip);
                }
            }
        }

        // mostrar panel de resultados
        if (resultsPanel != null)
            ShowResult(winnerisPlayer);

        // determinar si el ganador es el player principal (no es bot) para mostrar win/lose
        if (winner.collector != null && !winner.collector.isBot)
        {
            if (panelWin != null) panelWin.SetActive(true);
            if (panelLose != null) panelLose.SetActive(false);
        }
        else
        {
            if (panelWin != null) panelWin.SetActive(false);
            if (panelLose != null) panelLose.SetActive(true);
        }

        // mover el ganador al pedestal (si asignado)
        if (pedestalSpot != null && winner.collector != null)
        {
            winner.collector.transform.position = pedestalSpot.position;
            winner.collector.transform.rotation = pedestalSpot.rotation;
        }

        yield return new WaitForSeconds(5f);
        tournamentManager.LoadNextGameUsingCourtain();
    }
    private void ShowResult(bool playerWon)
    {
        List<int> Indexes = new List<int>();
        if (!playerWon)
        {
            Indexes = new List<int>() {0,1,2,3};
        }
        else
        {

            Indexes = new List<int>() { 3, 2, 1, 0 };
        }
        PlayerPrefs.SetString("WinnerYogoNado", JsonConvert.SerializeObject(Indexes));
        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (panelWin != null) panelWin.SetActive(playerWon);
        if (panelLose != null) panelLose.SetActive(!playerWon);
    }

    // Actualizar UI sin interpolación (uso al inicio o reseteo)
    void UpdatePlayerUIImmediate(PlayerSlot slot)
    {
        if (slot.fillImage != null)
        {
            slot.targetFill = (pointsToWin <= 0) ? 1f : Mathf.Clamp01((float)slot.score / (float)pointsToWin);
            slot.currentFill = slot.targetFill;
            slot.fillImage.fillAmount = slot.currentFill;
        }
        if (slot.scoreText != null)
            slot.scoreText.text = slot.score.ToString();
    }
    
    

    // Para debug / recuperar info desde otros scripts
    public PlayerSlot GetPlayerSlot(int index)
    {
        if (index < 0 || index >= players.Count) return null;
        return players[index];
    }

    [System.Serializable]
    public class PlayerSlot
    {
        [Tooltip("Collector (Player o Bot) asociado a este slot")]
        public Collector collector;

        [Tooltip("Image (UI) tipo Filled que representa la barra de puntos")]
        public Image fillImage;

        public TMP_Text positionText;
        [Tooltip("Texto opcional que muestra los puntos")]
        public Text scoreText;

        [HideInInspector] public int score = 0;
        [HideInInspector] public float targetFill = 0f; // objetivo 0..1
        [HideInInspector] public float currentFill = 0f; // valor actual para interpolación

        public void EnableCollector(bool enable)
        {
            if (collector == null) return;
            collector.EnableCollector(enable);
        }

        public string GetName()
        {
            if (collector != null) return collector.playerName;
            return "Unknown";
        }

        public void UpdatePosition(string position)
        {
            scoreText.text = position;
        }
    }
}