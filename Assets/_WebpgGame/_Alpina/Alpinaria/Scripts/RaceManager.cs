using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using TMPro;



public class RaceManager : MonoBehaviour
{

    [Header("Referencias")] [SerializeField]
    public PlayerSurfaceInput playerInput;

    [SerializeField] public List<BotAlpinaria> bots = new List<BotAlpinaria>();


    [Header("UI")] public TextMeshProUGUI countDownText;
    [SerializeField] private string go = "¡YA!";

    [Header("Cuenta regresiva")] public float timeCountdown = 3f;

    [Header("configuración UI")] public GameObject resultsUI;
    public GameObject panelWin;
    public GameObject panelLose;

    [Header("Paneo cámara final")] public Camera mainCamera;
    public GameObject finishPoint;
    public float panDuration = 3f;
    public Vector3 panOffset = new Vector3(0f, 0f, 0f);
    public float holdAfterPan = 0.5f;





    // Estado de la carrera
    private bool _initialRace = false;
    private bool _finishRace = false;
    private float _timeRace = 0f;

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
        if (!_initialRace || _finishRace) return;

        _timeRace += Time.deltaTime;
        // Aquí puedes agregar verificación de fin de carrera si lo necesitas
        // Ejemplo:
        // if (playerInput != null && playerInput.Finished()) EndRace();
        // foreach (var bot in bots) if (bot != null && bot.End()) EndRace();
        VerifyFinishRace();
    }

    public void StartRace()
    {
        _initialRace = true;
        _finishRace = false;
        _timeRace = 0f;

        if (playerInput != null)
            playerInput.OnRaceStart();

        if (bots != null && bots.Count > 0)
        {
            foreach (BotAlpinaria bot in bots)
            {
                if (bot == null) continue;
                bot.OnRaceStart();
            }

        }

        Debug.Log("¡Carrera iniciada!");
    }


    // prueba de paneo sustituimos cambiamos un poco verifyFinishRace y creamos unas coroutines para el paneo
    void VerifyFinishRace()
    {
        // ¿Ganó el jugador?
        if (playerInput != null && playerInput.Finished() && !_finishRace)
        {
            _finishRace = true; // evita reentradas
            StartCoroutine(FinishSequence(playerInput.transform, true));
            return;
        }

        // ¿Llegó algún bot antes?
        if (bots != null && !_finishRace)
        {
            for (int i = 0; i < bots.Count; i++)
            {
                var bot = bots[i];
                if (bot != null && bot.End())
                {
                    _finishRace = true; // evita reentradas
                    StartCoroutine(FinishSequence(bot.transform, false));
                    return;
                }
            }
        }
    }

    //agregamos esta coroutine que congela y teleporta al ganador, panea cámara y muestra UI
    IEnumerator FinishSequence(Transform winner, bool playerWon)
    {
        StopAllContestants();
        
        TeleportWinnerToFinishPoint(winner);
        
        yield return StartCoroutine(PanCameraWin(winner));
        
        ShowResult(playerWon);
        
        Debug.Log(playerWon ? "¡Has ganado la carrera!" : "Has perdido la carrera.");
        
    }


/*void VerifyFinishRace()
{
    // Verificar si el jugador terminó
    if (playerInput != null && playerInput.Finished() && !_finishRace)
    {
        if (resultsUI != null)
        {
            resultsUI.SetActive(true);
            if (panelWin != null)
                panelWin.SetActive(true);
        }
        EndRace();
    }

    // Verificar si algún bot terminó
    for (int i = 0; i < bots.Count; i++)
    {
        if (bots[i] != null && bots[i].End() && !_finishRace)
        {
            if (resultsUI != null)
            {
                resultsUI.SetActive(true);
                if (panelLose != null)
                    panelLose.SetActive(true);
            }
            EndRace();
            break;  // Termina al primer bot que llegue (ajusta si quieres todos)
        }
    }
}*/

    void EndRace()
    {
        if (_finishRace) return;
        _finishRace = true;

        StopAllContestants();
        Debug.Log("CARRERA FINALIZADA ");
    }

    IEnumerator PanCameraWin(Transform winner)
    {
        if (mainCamera == null || finishPoint == null || winner == null) yield break;

        Behaviour cineBrain = mainCamera.GetComponent("CinemachineBrain") as Behaviour; 
        bool restoreCine = false;
        if (cineBrain != null && cineBrain.enabled)
        {
            restoreCine = true;
            cineBrain.enabled = false;
        }

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        // Offset local respecto al ganador (ya en meta)
        Vector3 usedOffset = panOffset == Vector3.zero ? new Vector3(0f, 2.5f, -6f) : panOffset;
        Vector3 targetPos = winner.TransformPoint(usedOffset);

        // Mirar al ganador (ligero offset hacia arriba para encuadre)
        Vector3 lookTarget = winner.position + Vector3.up * 1.0f;
        Quaternion targetRot = Quaternion.LookRotation((lookTarget - targetPos).normalized, Vector3.up);

        float t = 0f;
        while (t < panDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / panDuration);
            float e = Mathf.SmoothStep(0f, 1f, k);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, e);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, e);
            yield return null;
        }

        if (holdAfterPan > 0f)
            yield return new WaitForSeconds(holdAfterPan);

        // Restaurar controlador de cámara si se desactivó
        if (restoreCine && cineBrain != null)
            cineBrain.enabled = true;
    }

    private void StopAllContestants()
    {
        if (playerInput != null) playerInput.OnRaceStop();

        if (bots != null)
        {
            foreach (var bot in bots)
            {
                if (bot == null) continue;
                bot.OnRaceStop();
            }
        }
    }

    private void TeleportWinnerToFinishPoint(Transform winner)
    {
        if (winner == null || finishPoint == null) return;

        var rb = winner.GetComponent<Rigidbody>();
        bool hadRB = rb != null;
        bool prevKinematic = false;

        if (hadRB)
        {
            prevKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        winner.position = finishPoint.transform.position;
        winner.rotation = finishPoint.transform.rotation;

        if (hadRB)
            rb.isKinematic = prevKinematic;
    }

    private void ShowResult(bool playerWon)
    {
        if (resultsUI != null) resultsUI.SetActive(true);
        if (panelWin != null) panelWin.SetActive(playerWon);
        if (panelLose != null) panelLose.SetActive(!playerWon);
    }
}
        
    
