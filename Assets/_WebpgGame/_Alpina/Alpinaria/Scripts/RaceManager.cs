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
    
    
    [Header("UI")]
    public TextMeshProUGUI countDownText;
    [SerializeField] private string go = "¡YA!";
    
    [Header("Cuenta regresiva")]
    public float timeCountdown = 3f;
    
    
    
    // Estado de la carrera
    private bool initialRace = false;
    private bool finishRace = false;
    private float timeRace = 0f;

    void Start()
    {
        if (countDownText != null)
            countDownText.gameObject.SetActive(false);
        
        //Aseguramos que todos esten en modo espera antes de iniciar el countdown
        PrepareContestants();
        
        StartCoroutine(InitialCountDown());
    }

    private void PrepareContestants()
    {
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerSurfaceInput>();
        
        if (playerInput != null)
            playerInput.OnRacePrepare();
        if (bots != null)
        {
            foreach (var bot in bots)
            {
                if (bot == null) continue;
                bot.OnRacePrepare();
            }
        }
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
        // Aquí puedes agregar verificación de fin de carrera si lo necesitas
        // Ejemplo:
        // if (playerInput != null && playerInput.Finished()) EndRace();
        // foreach (var bot in bots) if (bot != null && bot.End()) EndRace();
        VerifyFinishRace();
    }

    public void StartRace()
    {
        initialRace = true;
        finishRace = false;
        timeRace = 0f;
        
        if (playerInput != null)
            playerInput.OnRaceStart();

        if ( bots != null && bots.Count > 0)
        {   
            foreach (BotAlpinaria bot in bots)
            {
                if (bot == null) continue;
                bot.OnRaceStart();
            }
            
        }
       
        
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
        if (finishRace) return;
        finishRace = true;

        // Si quieres detener a todos al terminar:
        if (playerInput != null) playerInput.OnRaceStop();
        if (bots != null)
        {
            foreach (var bot in bots)
            {
                if (bot == null) continue;
                bot.OnRaceStop();
            }
        }

        Debug.Log("=== CARRERA FINALIZADA ===");
    }
}