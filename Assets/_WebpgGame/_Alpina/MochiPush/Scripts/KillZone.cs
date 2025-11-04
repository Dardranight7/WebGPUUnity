using UnityEngine;

// Colócalo en un collider grande con IsTrigger debajo de la isla y etiqueta "KillZone".
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CollisionMochiPush>(out var c))
            c.Eliminate();
    }
}