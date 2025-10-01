using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool isPaused = false;
    public int targetFrameRate = 60;

    [Header("Player & Bots UI")] 
    public Text playerNameText;
    public Text playerScoreText;
    public List<Text> botNameTexts;
    public List<Text> botScoreTexts;
    
    private string[] botNames = { "Mochi", "Luna", "Dani", "Ziggy", "Nova", "Bolt", "Jenn", "Andy", "Pepe", "Mari", "Pau", "Juan", "Diego", "Fer" };
    private List<float> botScores = new List<float>();
    
    [Header("UI References")]
    public Text scoreText;
    public Text highScoreText;
    public GameObject gameOverPanel;
    
    [Header("References")]
    public PlayerController player;
    public PlatformGenerator platformGenerator;
    
    [Header("Scoring")]
    public float scoreMultiplier = 2f;
    public int jumpBonus = 5;
    
    private float score = 0f;
    private float highScore = 0f;
    private float startHeight = 0f;
    private bool gameStarted = false;
    private int jumpsCount = 0;
    public GameObject WinPanel;
    
    private List<string> podium = new List<string>();
    public int totalPlayers = 4; 
    public bool gameStared = false;
    
    void Start()
    {
       
        Application.targetFrameRate = targetFrameRate;
        highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        
       
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        if (platformGenerator == null)
            platformGenerator = FindFirstObjectByType<PlatformGenerator>();
        if (player != null)
        {
            startHeight = player.transform.position.y;
            player.OnDie.AddListener(GameOver);
            player.OnJump.AddListener(OnPlayerJump);
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        
        // Pide el nombre de Usuario
        string playerName = PlayerPrefs.GetString("PlayerName", "P1");
        if (playerNameText != null)
            playerNameText.text = playerName;
        
        
        //Nombre aleatorio para los Bots
        System.Random rnd = new System.Random();
        List<int> usedIndexes = new List<int>();
        botScores.Clear();
        for (int i = 0; i < botNameTexts.Count; i++)
        {
            int idx;
            do
            {
                idx = rnd.Next(botNames.Length);
            } while (usedIndexes.Contains(idx));
            usedIndexes.Add(idx);
            if (botNameTexts[i] != null)
                botNameTexts[i].text = botNames[idx];
            botScores.Add(0f);
        }
        UpdateUI();
        Debug.Log("GameManager Inicializado");
        
    }

    public void StartBots()
    {
        var bots = FindObjectsOfType<BotController>();
        foreach (var bot in bots )
            bot.StartBot();
    }
    
    void Update()
    {
        
        // Score player principal
        if (player != null && player.IsAlive)
        {
            float currentHeight = player.transform.position.y - startHeight;
            float newScore = (currentHeight * scoreMultiplier) + (jumpsCount * jumpBonus);
            score = Mathf.Max(score, newScore);
        }
        
        //Score Bots
        for (int i = 0; i < botScores.Count; i++)
        {
            botScores[i] += Random.Range(0f, 0.5f);
        }
        
        UpdateUI();
        
    }
    
    void OnPlayerJump()
    {
        jumpsCount++;
    }
    
    void UpdateUI()
    {
        //Score del player 
        if (scoreText != null)
            scoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        if (playerScoreText != null)
            playerScoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        
        if (highScoreText != null)
            highScoreText.text = "Best: " + Mathf.FloorToInt(highScore).ToString();
        
        //Score de los bots
        for (int i = 0; i < botScoreTexts.Count; i++)
        {
            if(botScoreTexts != null && i < botScoreTexts.Count && botScoreTexts[i] != null)
            {
                
                botScoreTexts[i].text = "Score: " + Mathf.FloorToInt(botScores[i]).ToString();
            }
        }
    }
    

    public void WinGame()
    {
        if (WinPanel != null)
            WinPanel.SetActive(true);
        Debug.Log(" You Win! Final Score: ");
    }
    
    public void GameOver()
    {
        Debug.Log("Game Over! Final Score: " + Mathf.FloorToInt(score));
        
        
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetFloat("HighScore", highScore);
            PlayerPrefs.Save();
            Debug.Log(" New High Score: " + Mathf.FloorToInt(highScore));
        }
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        
        
    }
    
    public void RestartGame()
    {
        score = 0f;
        jumpsCount = 0;
        
        if (player != null)
        {
            player.Restart();
            startHeight = player.transform.position.y;
        }
        
        if (platformGenerator != null)
        {
            
            if (platformGenerator.GetComponent<PlatformGenerator>())
            {
                // You can add a reset method to PlatformGenerator if needed
            }
        }
        
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        //reinicia los Scores de los bots
        for (int i = 0; i < botScores.Count; i++)
            botScores[i] = 0f;
        
        UpdateUI();
        Debug.Log("Game Restarted");
    }
    
    public void RegisterFinish(string name)
    {
        if (!podium.Contains(name))
            podium.Add(name);
        
        if (podium.Count >= totalPlayers)
            ShowPodium();
    }
    
    public void OnPlayerMoveDown(int rows)
    {
        float puntosPorFila = 2f * scoreMultiplier;
        score = Mathf.Max(0, score - (rows * puntosPorFila));
        UpdateUI();
    }
    
    public void OnBotMoveDown(string botName, int rows)
    {
        int idx = -1;
        for (int i = 0; i < botNameTexts.Count; i++)
        {
            if (botNameTexts[i] != null && botNameTexts[i].text == botName)
            {
                idx = i;
                break;
            }
        }

        if (idx >= 0 && idx < botScores.Count)
        {
            float puntosPorFila = 2f * scoreMultiplier;
            botScores[idx] = Mathf.Max(0, botScores[idx] - (rows * puntosPorFila));
            UpdateUI();
        }
    }

    void ShowPodium()
    {
        string result = "Posiciones finales:\n";
        for (int i = 0; i < podium.Count; i++)
            result += $"{i + 1}. {podium[i]}\n";
        Debug.Log(result);
    }
}