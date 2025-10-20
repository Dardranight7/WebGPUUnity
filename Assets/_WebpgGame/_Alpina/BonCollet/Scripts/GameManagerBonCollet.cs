using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManagerBonCollet : MonoBehaviour
{
    [Header("Modo de juego")]
    [Tooltip("Cantidad de puntos que debe alcanzar un jugador para ganar")]
    public int pointsToWin = 20;

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

    // Estado interno
    bool gameRunning = false;

    void Start()
    {
        // Inicializar slots (scores, UI)
        foreach (var p in players)
        {
            if (p.collector != null) p.collector.ResetScore();
            p.currentFill = 0f;
            UpdatePlayerUIImmediate(p);
        }

        if (autoStartWithPan && cameraController != null)
        {
            // lanzar paneo inicial y luego iniciar juego
            StartCoroutine(AutoStartWithPanRoutine());
        }
    }

    IEnumerator AutoStartWithPanRoutine()
    {
        cameraController.PlayPan();
        yield return new WaitForSeconds(startPanDuration);
        StartGame();
    }

    // Llamar para iniciar el juego (desde UI button o inspector)
    public void StartGame()
    {
        if (gameRunning) return;
        gameRunning = true;

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

        // verificar ganador
        if (slot.score >= pointsToWin)
        {
            OnPlayerWin(slot);
        }
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
    }

    void Update()
    {
        // interpolar fillAmount para cada slot (suavizado visual)
        foreach (var p in players)
        {
            if (p.fillImage == null) continue;
            // targetFill puede actualizarse en AddScore
            p.currentFill = Mathf.Lerp(p.currentFill, p.targetFill, Mathf.Clamp01(uiFillLerpSpeed * Time.deltaTime));
            p.fillImage.fillAmount = p.currentFill;
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