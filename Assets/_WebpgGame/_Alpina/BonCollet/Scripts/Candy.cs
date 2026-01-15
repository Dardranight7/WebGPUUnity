using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class Candy : MonoBehaviour
{
    [Header("Configuración de la gomita")] public int points = 2; // puntos que da (puede ser negativo)
    public float lifeTime = 5f; // para prevenir acumulación, se destruye después de cierto tiempo
    public float spawnCooldown = 0f; //Cooldown mínimo en segundos entre apariciones de ESTE TIPO de candy
    [SerializeField] private GameObject[] candyVisuals;
    public float
        spawnIntervalOverride =
            0f; //Si > 0, el spawner usará este tiempo (en segundos) como espera después de instanciar esta candy. Si es 0, se usará el spawnInterval global del spawner.

    public PowerUp _powerUp;

    public float fallSpeed = 3f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ChangePrivateVisuals();
        // asegurar que detecte triggers hijos (si usas trigger de recogida)
        // Si la prefab tiene un collider principal que no es trigger y un collider hijo trigger para pickup,
        // OnTriggerEnter se ejecutará si la gomita tiene Rigidbody.
        if (rb != null) rb.useGravity = false;
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * fallSpeed;
        }
    }

    void ChangePrivateVisuals()
    {
        if (candyVisuals.Length > 1)
        {
            int randomVisual = Random.Range(0,candyVisuals.Length);
            candyVisuals[randomVisual].SetActive(true);
        }
    }
    // Si decides que la gomita se recoja al tocar al player/bot, puedes usar OnTriggerEnter.
    // void OnTriggerEnter(Collider other)
    // {
    //     // El other puede ser una parte del jugador; buscamos el Collector en los padres.
    //     var collector = other.GetComponentInParent<Collector>();
    //     if (collector != null)
    //     {
    //         collector.Collect(this);
    //     }
    // }

    // Alternativa: podrías usar OnCollisionEnter si prefieres colisiones en vez de trigger.
    private void OnCollisionEnter(Collision other)
    {
        var collector = other.gameObject.GetComponentInParent<Collector>();
        if (collector != null)
        {
            collector.Collect(this);
        }
    }
}

public enum PowerUp
    {
        Speed,
        Slowness,
        None
    }
