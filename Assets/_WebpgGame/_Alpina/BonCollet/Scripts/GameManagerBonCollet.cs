using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Tooltip("Si true, comenzará el paneo inicial y luego iniciará el juego automáticamente")]
    public bool autoStartWithPan = true;

    [Header("Paneo / Cámara")]
    public CameraBonCollet cameraController; // asignar si quieres paneo y focus al ganador
    [Tooltip("Duración del paneo inicial antes de iniciar el juego (si autoStartWithPan true)")]
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
    
    [Header("Control de inicio")]
    [Tooltip("Si true, el juego sólo comenzará cuando se pulse el botón 'Jugar' (OnPlayButton).")]
    public bool requireButtonToStart = true;
    
    public AudioClip musicBackgroundClip;
    
    public AudioClip MusicVictoryClip;
    public AudioClip MusicLoseClip;

    public GameObject preUI;
    public GameObject UIControls;
    bool isPreUIActive = true;
    
    //secuencia de victoria para dinamica de juego por tiempo 
    public float winPanDuration = 3f;
    

    //public GameObject PlayerPrefab;

    // Estado interno
    bool gameRunning = false;

    void Start()
    {
        
        if (preUI != null)
        {
            preUI.SetActive(true);
            isPreUIActive = true;
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

        if (!requireButtonToStart && autoStartWithPan && cameraController != null)
        {
            // lanzar paneo inicial y luego iniciar juego
            StartCoroutine(AutoStartWithPanRoutine());
        }
    }

    IEnumerator AutoStartWithPanRoutine()
    {
        if (preUI != null) preUI.SetActive(false);
        isPreUIActive = false;
        
        if (UIControls != null) UIControls.SetActive(true);
        
        cameraController.PlayPan();
        yield return new WaitForSeconds(startPanDuration);
        StartGame();
    }

    // Llamar para iniciar el juego (desde UI button)
    public void OnPlayButton()
    {
        Debug.Log("[GM] OnPlayButton pressed: ocultando preUI y arrancando juego.");
        if (preUI != null)
        {
            preUI.SetActive(false);
            isPreUIActive = false;
        }
            
        if (UIControls != null)  UIControls.SetActive(true);
        StartCoroutine(AutoStartWithPanRoutine());
    }
     
    public void StartGame()
    {
        if (gameRunning) return;
        
        if (requireButtonToStart && isPreUIActive)
        {
            Debug.Log("[GM] StartGame llamado pero PreUI está activo y se requiere botón para iniciar. Abortando.");
            return;
        }

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

   

    // Llamar para terminar manualmente
    public void EndGame()
    {
        if (!gameRunning) return;
        gameRunning = false;

        if (spawner != null && stopSpawningOnWin)
            spawner.StopSpawning();

        foreach (var p in players)
            p.EnableCollector(false);
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

        // verificar ganador en dinamica por puntos
        //if (slot.score >= pointsToWin)
        //{
           // OnPlayerWin(slot);
        //}
    }
    
    
    void OnPlayerWin(PlayerSlot winner)
    {
        if (!gameRunning) return;
        gameRunning = false;

        if (spawner != null && stopSpawningOnWin)
            spawner.StopSpawning();

        // detener movimiento de todos
        foreach (var p in players)
            p.EnableCollector(false);
        
        // cámara hacia ganador si existe
        if (cameraController != null && winner != null && winner.collector != null)
        {
            cameraController.FocusOnWinner(winner.collector.transform);
        }
        
        
        Debug.Log("GameManagerBonCollet - Ganador: " + (winner != null ? winner.GetName() + " (" + winner.score + " pts)" : "Nadie"));

        // Aquí puedes invocar UI final (panel de victoria), reproducir efectos, etc.
        if (winner != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlaySFX(MusicVictoryClip);
            resultsPanel.SetActive(true);
            
            if (winner != null && winner.collector != null && !winner.collector.isBot)
            {
            
                // el jugador principal ha ganado
                if (panelWin != null)
                    panelWin.SetActive(true);
                if (panelLose != null)
                    panelLose.SetActive(false);
            
            }
            else
            {
            
                // el jugador principal ha perdido
                if (panelWin != null)
                    panelWin.SetActive(false);
                if (panelLose != null)
                    panelLose.SetActive(true);
            }
            
            
            if (pedestalSpot != null && winner.collector != null)
            {
                // mover el ganador al pedestal
                winner.collector.transform.position = pedestalSpot.position;
                winner.collector.transform.rotation = pedestalSpot.rotation;
            }
        }
    }

    void Update()
    {
        //Esta parte llena la barra de la dinamica por puntos 
        // interpolar fillAmount para cada slot (suavizado visual)
        /*foreach (var p in players)
        {
            if (p.fillImage == null) continue;
            // targetFill puede actualizarse en AddScore
            p.currentFill = Mathf.Lerp(p.currentFill, p.targetFill, Mathf.Clamp01(uiFillLerpSpeed * Time.deltaTime));
            p.fillImage.fillAmount = p.currentFill;
        }*/
        
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

        // detener movimiento de todos
        foreach (var p in players)
            p.EnableCollector(false);
        
        // cámara hacia ganador si existe (paneo)
        if (cameraController != null && winner.collector != null)
        {
            cameraController.FocusOnWinner(winner.collector.transform);
        }

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

        // reproducir música de victoria 
        /*if (MusicVictoryClip != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.PlaySFX(MusicVictoryClip);
            }
            else
            {
                Debug.LogWarning("[GM] AudioManager.Instance no encontrado: MusicVictoryClip no reproducido.");
            }
        }*/

        // mostrar panel de resultados
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

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
    }
}