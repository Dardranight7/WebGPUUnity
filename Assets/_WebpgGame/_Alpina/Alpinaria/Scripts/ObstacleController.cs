using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    [SerializeField] private float forwardSlowdownFactor = 0.6f; // Factor de reduccion de movimiento.
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Racer"))
        {
            PlayerSurfaceInput playerInput = other.GetComponent<PlayerSurfaceInput>();

            if (playerInput != null)
            {
                // Llamar al nuevo método de aturdimiento
                playerInput.HitObstacle(forwardSlowdownFactor);
                
                // TODO
                // Desactivar colision? hacer efecto en jugador del choque?
            }
        }
    }
}
