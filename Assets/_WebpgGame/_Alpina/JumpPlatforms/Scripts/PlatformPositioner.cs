using UnityEngine;

/// <summary>
/// Script ultra sencillo que posiciona la primera plataforma justo debajo del jugador
/// Colócalo en cualquier GameObject en tu escena (puede ser un objeto vacío)
/// </summary>
public class PlatformPositioner : MonoBehaviour
{
    void Start()
    {
        // Esperar un frame para asegurar que todo esté inicializado
        Invoke("PositionFirstPlatform", 0.1f);
    }
    
    void PositionFirstPlatform()
    {
        // Buscar al jugador - usando el método actualizado recomendado
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("❌ No se encontró PlayerController en la escena");
            return;
        }
        
        // Buscar todas las plataformas - usando el método actualizado recomendado
        Platform[] platforms = FindObjectsByType<Platform>(FindObjectsSortMode.None);
        if (platforms.Length == 0)
        {
            Debug.LogError("❌ No se encontraron plataformas en la escena");
            return;
        }
        
        // Encontrar la plataforma más cercana al jugador
        Platform closestPlatform = null;
        float closestDistance = float.MaxValue;
        
        foreach (Platform platform in platforms)
        {
            float distance = Vector3.Distance(platform.transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlatform = platform;
            }
        }
        
        if (closestPlatform != null)
        {
            // Mover la plataforma justo debajo del jugador
            Vector3 newPosition = new Vector3(
                player.transform.position.x,
                player.transform.position.y - 0.5f,
                player.transform.position.z
            );
            
            closestPlatform.transform.position = newPosition;
            Debug.Log("✅ Primera plataforma posicionada justo debajo del jugador");
        }
    }
}