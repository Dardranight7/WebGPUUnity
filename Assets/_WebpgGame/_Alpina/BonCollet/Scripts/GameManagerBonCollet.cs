
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManagerBonCollet : MonoBehaviour
{

    [Header("Tiempo de juego")]
    public float startPanDuration = 5f;   // duración del paneo inicial antes de empezar
    public float gameDuration = 60f;      // duración del juego (configurable)
    public bool autoStart = true;

    [Header("Referencias")]
    public CandySpawner spawner;
    public CameraBonCollet cameraController; // controla paneo inicial y enfoque al ganador
    public List<Collector> players = new List<Collector>(); // asigna Player y Bots aquí (o se buscan automáticamente)

    [Header("Opciones")]
    public bool stopSpawningOnEnd = true;

    private bool gameRunning = false;
    private float timeLeft;
    

    void Start()
    {
        // si no asignaste players en inspector, los buscamos por componente Collector
        if (players.Count == 0)
            players = FindObjectsOfType<Collector>().ToList();

        timeLeft = gameDuration;

        if (autoStart)
        {
            StartCoroutine(RunGameFlow());
        }
    }

    IEnumerator RunGameFlow()
    {
        // Paneo inicial: el cameraController debe llamar a StartGame cuando termine, pero aquí también la iniciamos
        if (cameraController != null)
        {
            cameraController.PlayPan(); // comienza paneo, no bloqueante
            yield return new WaitForSeconds(startPanDuration);
        }

        StartGame();
        // tiempo de juego
        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }
        EndGame();
    }

    public void StartGame()
    {
        // reset scores y activar spawner y controllers
        timeLeft = gameDuration;
        gameRunning = true;

        foreach (var p in players)
        {
            p.ResetScore();
            p.EnableCollector(true);
        }

        if (spawner != null)
            spawner.StartSpawning();
    }

    public void EndGame()
    {
        if (!gameRunning) return;
        gameRunning = false;

        // detener spawner
        if (spawner != null && stopSpawningOnEnd)
            spawner.StopSpawning();

        // Determinar ganador por score (si empate, se elige el primero con ese score)
        var winner = players.OrderByDescending(p => p.score).FirstOrDefault();

        // desactivar movimiento / setear estado idle
        foreach (var p in players)
        {
            p.EnableCollector(false); // esto puede parar el movement script
        }

        // Si hay cameraController y winner, hacer zoom hacia winner
        if (cameraController != null && winner != null)
        {
            cameraController.FocusOnWinner(winner.transform);
        }

        // Aquí puedes notificar UI para mostrar resultados (llamar un evento, etc.)
        Debug.Log("Juego terminado. Ganador: " + (winner != null ? winner.playerName + " (" + winner.score + ")" : "Nadie"));
    }

    // Llamado por Collector cuando recoge una gomita
    public void AddScore(Collector collector, int points)
    {
        if (!gameRunning)
        {
            // si no se corre el juego, ignorar o permitir recolección según quieras
            return;
        }

        collector.score += points;
        // Notificar UI o sistema de puntuación aquí
        Debug.Log(collector.playerName + " obtuvo " + points + " puntos. Total: " + collector.score);
    }

    // utilidad para obtener tiempo restante (puedes exponer para UI)
    public float GetTimeLeft() => timeLeft;
}