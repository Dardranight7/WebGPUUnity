using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinUI : MonoBehaviour
{
    [Header("UI References")] public GameManager gameManager;
    public GameObject winPanel;


    [Header("UI Elements")] public Text winnerNameText;
    public Text finalScoreText;
    public Button retryButton;
    public Button quitButton;

    [Header("Settings")] public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (winPanel != null)
            winPanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }


    public void ShowWinPanel(string winnerName)
    {
        if (winPanel == null) return;

        if (winnerNameText != null)
            winnerNameText.text = "¡" + winnerName + " ganó!";

        if (finalScoreText != null && gameManager != null)
        {
            finalScoreText.text = "Puntuación: ";
        }


        winPanel.SetActive(true);
        Time.timeScale = 0f; // Pausar el juego
    }

    void OnRetry()
    {
        Time.timeScale = 1f;
        
        if (gameManager != null)
        {
            winPanel.SetActive(false);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void OnQuit()
    {
        Time.timeScale = 1f;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Application.Quit();
    }


}
