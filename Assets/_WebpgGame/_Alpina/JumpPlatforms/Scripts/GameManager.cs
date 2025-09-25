using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool isPaused = false;
    public int targetFrameRate = 60;
    
    [Header("UI References")]
    public Text scoreText;
    public Text highScoreText;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    
    [Header("References")]
    public PlayerController player;
    public PlatformGenerator platformGenerator;
    
    [Header("Scoring")]
    public float scoreMultiplier = 10f;
    public int jumpBonus = 5;
    
    private float score = 0f;
    private float highScore = 0f;
    private float startHeight = 0f;
    private bool gameStarted = false;
    private int jumpsCount = 0;
    
    void Start()
    {
        // Set target frame rate for consistent performance
        Application.targetFrameRate = targetFrameRate;
        
        // Load high score
        highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        
        // Find components if not assigned
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
        
        if (platformGenerator == null)
        {
            platformGenerator = FindFirstObjectByType<PlatformGenerator>();
        }
        
        if (player != null)
        {
            startHeight = player.transform.position.y;
            
            // Subscribe to player events
            player.OnDie.AddListener(GameOver);
            player.OnJump.AddListener(OnPlayerJump);
        }
        
        // Initialize UI
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
        
        Debug.Log("✅ GameManager initialized");
    }
    
    void Update()
    {
        if (isPaused) return;
        
        // Update score based on height (platform game style)
        if (player != null && player.IsAlive)
        {
            float currentHeight = player.transform.position.y - startHeight;
            float newScore = (currentHeight * scoreMultiplier) + (jumpsCount * jumpBonus);
            score = Mathf.Max(score, newScore);
            
            UpdateUI();
        }
        
        // Pause input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    void OnPlayerJump()
    {
        jumpsCount++;
    }
    
    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        }
        
        if (highScoreText != null)
        {
            highScoreText.text = "Best: " + Mathf.FloorToInt(highScore).ToString();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);
        
        Debug.Log(isPaused ? "⏸️ Game Paused" : "▶️ Game Resumed");
    }
    
    public void GameOver()
    {
        Debug.Log("🎮 Game Over! Final Score: " + Mathf.FloorToInt(score));
        
        // Update high score
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
            PlayerPrefs.Save();
            Debug.Log("🏆 New High Score: " + Mathf.FloorToInt(highScore));
        }
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        score = 0f;
        jumpsCount = 0;
        
        if (player != null)
        {
            player.Restart();
            startHeight = player.transform.position.y;
        }
        
        if (platformGenerator != null)
        {
            // Reset platform generator if it has a reset method
            if (platformGenerator.GetComponent<PlatformGenerator>())
            {
                // You can add a reset method to PlatformGenerator if needed
            }
        }
        
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        UpdateUI();
        Debug.Log("🔄 Game Restarted");
    }
}