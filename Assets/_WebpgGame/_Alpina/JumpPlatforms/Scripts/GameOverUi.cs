using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject gameOverPanel;   // assign the panel GameObject (same as GameManager.gameOverPanel)
    public Text finalScoreText;        // "Puntuación: ..."
    public Text bestScoreText;         // "Mejor: ..."
    public Button retryButton;         // label: Reintentar
    public Button quitButton;          // label: Salir
    public string mainMenuSceneName = "MainMenu"; // optional

    void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);

        // Optional: if GameManager already opens panel, update texts when it does.
    }

    // Call this from GameManager.GameOver() or hook GameManager to call ShowGameOver()
    public void ShowGameOverPanel()
    {
        if (gameOverPanel == null) return;
        UpdateScoreTexts();
        gameOverPanel.SetActive(true);
        // pause time optionally:
        Time.timeScale = 0f;
    }

    void UpdateScoreTexts()
    {
        int finalScore = Mathf.FloorToInt(PlayerPrefs.GetFloat("LastScore", 0f)); // optional source
        
        if (finalScoreText != null)
            finalScoreText.text = "Puntuación: " + finalScore;
    }

    void OnRetry()
    {
        Time.timeScale = 1f;
        
        if (gameManager != null)
        {
            gameOverPanel.SetActive(false);
            // ensure GameManager restarts bots/camera as needed
        }
        else
        {
            // fallback: reload current scene
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
