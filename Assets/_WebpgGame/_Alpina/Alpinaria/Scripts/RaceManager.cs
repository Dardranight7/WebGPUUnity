using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Serialization;


public class RaceManager : MonoBehaviour
{
    
    [Header("Referencias")]
    [SerializeField] public PlayerSurfaceInput playerInput;
    [SerializeField] public List<BotAlpinaria> bots = new List<BotAlpinaria>();
    
    
    
    public TextMeshProUGUI countDownText;
    private string go = "¡YA!";
    
    public float timeCountdown = 3f;

    public bool raceStarted = false;
    
    
    
    
    // Estado de la carrera
    private bool initialRace = false;
    private bool finishRace = false;
    private float timeRace = 0f;

    void Start()
    {
        if (countDownText != null)
            countDownText.gameObject.SetActive(false);
        
        StartCoroutine(InitialCountDown());
    }

    private IEnumerator InitialCountDown()
    {
        if (countDownText != null)
            countDownText.gameObject.SetActive(true);
        
        //countDown 3 2 1 ya!
        for (int i = 3; i > 0; i--)
        {
            if (countDownText != null)
                countDownText.text = i.ToString();
            
            yield return new WaitForSeconds(1f);
        }

        if (countDownText != null)
            countDownText.text = go;
        
        yield return new WaitForSeconds(1f);
        
        StartRace();
        
        
        if (countDownText != null)
            countDownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!initialRace || finishRace) return;
        
        timeRace += Time.deltaTime;
        VerifyFinishRace();
    }

    public void StartRace()
    {
        initialRace = true;

        if ( bots != null && bots.Count > 0)
        {   
            foreach (BotAlpinaria bot in bots)
            {
                bot.ResetBot();
            }
            
        }
        
        finishRace = false;
        timeRace = 0f;
       
        
        Debug.Log("¡Carrera iniciada!");
    }
    
    void VerifyFinishRace() 
    {
        // Verificar si el jugador terminó
        if (playerInput != null && playerInput.Finished() && !finishRace)
        {
            EndRace();
        }
        
        // Verificar si algún bot terminó
        for (int i = 0; i < bots.Count; i++)
        {
            if (bots[i] != null && bots[i].End() && !finishRace)
            {
                EndRace();
                break;  // Termina al primer bot que llegue (ajusta si quieres todos)
            }
        }
    }

    void EndRace()
    {
        finishRace = true;
        raceStarted = false;
        Debug.Log("=== CARRERA FINALIZADA ===");
    }
}