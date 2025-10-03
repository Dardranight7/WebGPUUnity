using UnityEngine;
using UnityEngine.UI;

public class RetryButton : MonoBehaviour
{
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnRetryClicked);
        Debug.Log("✅ RetryButton inicializado");
    }
    
    void OnRetryClicked()
    {
        Debug.Log("🔘 Botón Retry presionado!");
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        
        if (gameManager != null)
        {
            Debug.Log("✅ GameManager encontrado, llamando RestartGame()");
            gameManager.RestartGame();
        }
        else
        {
            Debug.LogError("❌ No se encontró GameManager!");
        }
    }
}
