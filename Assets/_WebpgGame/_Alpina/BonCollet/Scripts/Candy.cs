using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Candy : MonoBehaviour
{
    [Header("Configuración de la gomita")]
    public int points = 2;       // puntos que da (puede ser negativo)
    public float lifeTime = 3f; // para prevenir acumulación, se destruye después de cierto tiempo

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // asegurar que detecte triggers hijos (si usas trigger de recogida)
        // Si la prefab tiene un collider principal que no es trigger y un collider hijo trigger para pickup,
        // OnTriggerEnter se ejecutará si la gomita tiene Rigidbody.
        Destroy(gameObject, lifeTime);
    }

    // Si decides que la gomita se recoja al tocar al player/bot, puedes usar OnTriggerEnter.
    void OnTriggerEnter(Collider other)
    {
        // El other puede ser una parte del jugador; buscamos el Collector en los padres.
        var collector = other.GetComponentInParent<Collector>();
        if (collector != null)
        {
            collector.Collect(this);
        }
    }

    // Alternativa: podrías usar OnCollisionEnter si prefieres colisiones en vez de trigger.
}