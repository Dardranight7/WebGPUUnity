using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [Header("Configuracion")]
    public float health = 3f;
    public float forceThreshold = 5f;
    public GameObject destructionEffect;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            // Calculamos la fuerza del impacto
            float impactForce = collision.relativeVelocity.magnitude;

            if (impactForce >= forceThreshold)
            {
                TakeDamage(1);
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
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }
        
        // Destruimos el objeto
        Destroy(gameObject);
    }
}
