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
            }

            BotAlpinaria bot = other.GetComponent<BotAlpinaria>();
            if (bot != null)
            {
                bot.HitObstacle(0.5f);
            }
        }
    }
}
