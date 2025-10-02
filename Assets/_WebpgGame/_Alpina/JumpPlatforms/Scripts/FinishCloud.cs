using UnityEngine;
using System.Collections;

public class FinishCloud : MonoBehaviour
{
    public GameManager gameManager;
    private bool finished = false;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        
        // asegurarnos que el collider sea trigger
        Collider c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (finished) return;

        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            finished = true;
            string playerName = PlayerPrefs.GetString("PlayerName", "P1");
            StartCoroutine(FinishAndWinAfterDelay(playerName));
            return;
        }
        
        var bot = other.GetComponent<BotController>();
        if (bot != null)
        {
            finished = true;
            string botName = !string.IsNullOrEmpty(bot.botName) ? bot.botName : "Bot";
            StartCoroutine(FinishAndWinAfterDelay(botName));
            return;
        }
    }

    private IEnumerator FinishAndWinAfterDelay(string name)
    {
        // Espera 2 segundos antes de ejecutar el registro y la victoria
        yield return new WaitForSeconds(2f);

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null)
        {
            gameManager.RegisterFinish(name);
            gameManager.WinGame();
        }

        Debug.Log("Ganaste!");
    }
}