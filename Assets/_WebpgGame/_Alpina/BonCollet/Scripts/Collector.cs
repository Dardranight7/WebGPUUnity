using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Collector : MonoBehaviour
{
    public GameManagerBonCollet gameManagerBonCollet; // referencia al GameManager para sumar puntos
    
    [Header("Identidad")]
    public string playerName = "Player";
    public bool isBot = false;

    [HideInInspector]
    public int score = 0;

    [Header("Opciones")]
    public Animator animator; // opcional: para poner en idle al final

    // Movimiento: si quieres desactivar el movimiento al final del juego, estos scripts deberían reaccionar a EnableCollector(false)
    private MonoBehaviour[] movementScripts;

    void Awake()
    {
        movementScripts = GetComponents<MonoBehaviour>(); // buscaremos los scripts que controlan movimiento y los deshabilitaremos
    }

    public void Collect(Candy candy)
    {
        
        if (candy == null) return;

        // sumar puntos a través de GameManager
        if (gameManagerBonCollet != null)
        {
            gameManagerBonCollet.AddScore(this, candy.points);
        }
        else
        {
            // fallback
            score += candy.points;
        }

        // reproducir animación de recoger si hay animator
        if (animator != null)
        {
            animator.SetTrigger("Collect"); // debes tener ese trigger en el Animator si quieres
        }

        // destruir la gomita
        Destroy(candy.gameObject);
    }

    public void ResetScore()
    {
        score = 0;
    }

    public void EnableCollector(bool enable)
    {
        // habilita/deshabilita scripts de movimiento (se asume que los scripts de movimiento son MonoBehaviour en el mismo GameObject)
        // Si tienes scripts específicos, puedes referenciarlos directamente (por ejemplo PlayerControllerRB y BotController).
        foreach (var s in movementScripts)
        {
            if (s == this) continue;
            // como heurística, deshabilitar scripts que parezcan controladores (nombres comunes)
            if (s.GetType().Name.Contains("Controller") || s.GetType().Name.Contains("Movement") || s.GetType().Name.Contains("Bot"))
            {
                s.enabled = enable;
            }
        }

        // poner animator en idle
        if (animator != null && !enable)
        {
            animator.Play("Idle", 0); // nombre de clip debe existir
        }
    }
}