using System;
using Unity.VisualScripting;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float health = 3f;
    [SerializeField] private float forceThreshold = 5f;
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private GameObject destructionParticles;
    [SerializeField] private ObstacleType ObsType = ObstacleType.Basic;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            switch (ObsType)
            {
                case ObstacleType.Basic:
                default:
                    //DONOTHING
                case ObstacleType.Destructible:
                    // Calculamos la fuerza del impacto
                    float impactForce = collision.relativeVelocity.magnitude;

                    if (impactForce >= forceThreshold)
                    {
                        TakeDamage(1);
                    }
                    break;
                case ObstacleType.Bouncy:
                    break;
            }
            
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        //TODO: add damage particles
        if (health <= 0)
        {
            DestroyObstacle();
        }
    }

    void DestroyObstacle()
    {
        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }
        
        // Destruimos el objeto
        Destroy(gameObject);
    }
}

[Serializable]
public enum ObstacleType
{
    Basic,
    Destructible,
    Bouncy
}
