using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

public class RaceManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayableDirector raceTimeline;
    [SerializeField] private GameObject cinematicGO;
    [SerializeField] private CinemachineCamera playerCam;
    public PlayerSurfaceInput playerInput;

    [SerializeField] public List<BotAlpinaria> bots = new List<BotAlpinaria>();

    [Header("UI")] 
    public TextMeshProUGUI countDownText;
    [SerializeField] private string go = "¡YA!";

    [Header("Cuenta regresiva")] 
    public float timeCountdown = 3f;

    [Header("configuración UI")] 
    public GameObject resultsUI;
    public GameObject panelWin;
    public GameObject panelLose;

    [Header("Paneo cámara final")] 
    public Camera mainCamera;
    public Transform finishPoint;
    [SerializeField] private GameObject endCinemachineCam;
    public float closeHold = 0.5f; //plano cerrado antes de inicar el zoom out
    public float toCloseDuration = 0.35f; //Duración para moverse suavemente desde la posición actual de la cámara al plano cerrado
    public Vector3 wideOffsetLocal = new Vector3(0.0f, 3.5f, 7.5f); //Plano abierto
    public float lookAtOffsetY = 1.2f; //Offset vertical para el punto de enfoque
    public float fovClose = 28f; //FOV plano cerrado
    public float fovWide = 55f; //FOV plano abierto
    public float panDuration = 3f; //Duración del paneo
    public Vector3 panOffset = new Vector3(0.6f, 1.6f, 1.2f); //Offset local respecto al ganador
    public float holdAfterPan = 0.5f;  //Pausa final tras completar el zoom out antes de mostrar la UI.


    
    // Estado de la carrera
    private bool _initialRace = false;
    private bool _finishRace = false;
    private float _timeRace = 0f;
    
    [Header("Audios")]
    //private AudioManager audioManager;
    private AudioSource audioSource;
    private AudioManager audioManager;
    public AudioClip audioWin;
    public AudioClip audioLose;
    public AudioClip audioCountdown; 
    [Range(0f, 1f)] public float sfxVolume = 1f;
    
    public bool playMusicOnStart = false;
    public bool stopMusicOnFinish = false;
    public AudioClip backgroundMusic;
    
    
    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Buscar AudioManager (como en tu GameManager)
        if (audioManager == null) audioManager = AudioManager.Instance;
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();

        // Crear/asegurar un AudioSource 2D de respaldo para SFX si hiciera falta
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D
        audioSource.volume = 1f;
    }
    

    void Start()
    {
        
        if (countDownText != null) countDownText.gameObject.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);

        // Música de fondo (opcional y simple)
        if (playMusicOnStart && backgroundMusic != null)
        {
            if (audioManager != null) audioManager.PlayMusic(backgroundMusic, 1f);
            else
            {
                // respaldo: reproducir en el mismo sfxFallback en loop (no ideal, pero funcional)
                audioSource.Stop();
                audioSource.clip = backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
            
        if (raceTimeline != null)
        {
            StartCoroutine(StartTimelineWatchdog());
        }
        else
        {
            Debug.LogWarning("RaceManager: No PlayableDirector asignado. Iniciando cuenta regresiva directamente.");
            PrepareContestants(); // Ya se hizo en Awake, pero para el fallback.
            StartCoroutine(InitialCountDown());
        }
    }
    
    /// <summary>
    /// Espera la duración de la Timeline para iniciar la carrera.
    /// </summary>
    IEnumerator StartTimelineWatchdog()
    {
        if (raceTimeline == null)
        {
            StartCoroutine(InitialCountDown());
            yield break;
        }
    
        double duration = raceTimeline.duration;
        
        if (raceTimeline.state != PlayState.Playing)
        {
            raceTimeline.Play();
        }
    
        yield return new WaitForSeconds((float)duration + 0.1f);
    
        if (raceTimeline.state == PlayState.Playing)
        {
            raceTimeline.Stop();
            cinematicGO.SetActive(false);
            playerCam.gameObject.SetActive(true);
            Debug.Log("RaceManager: La Timeline no se detuvo sola; se forzó Stop().");
        }
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
        {
            countDownText.gameObject.SetActive(true);
            countDownText.text = Mathf.CeilToInt(timeCountdown).ToString();
        }

        yield return null;
        
        // Reproducir SFX de cuenta regresiva
        
        if (audioCountdown != null)
        {
            if (audioManager != null) audioManager.PlaySFX(audioCountdown, sfxVolume);
            else audioSource.PlayOneShot(audioCountdown, sfxVolume);
        }
        
        float remainingTime = timeCountdown;
        
        while (remainingTime > 0)
        {
            if (countDownText != null)
                countDownText.text = Mathf.CeilToInt(remainingTime).ToString();

            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        if (countDownText != null)
        {
            countDownText.text = go;
            yield return new WaitForSeconds(0.5f);
            countDownText.gameObject.SetActive(false);
        }
            

        yield return new WaitForSeconds(1f);

        StartRace();


        if (countDownText != null)
            countDownText.gameObject.SetActive(false);
        
        
    }

    void Update()
    {
        if (!_initialRace || _finishRace) return;

        _timeRace += Time.deltaTime;
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
        if (playerInput != null && playerInput.IsFinished && !_finishRace)
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
                if (bot != null && bot.HasFinished())
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

        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        endCinemachineCam.SetActive(true);
        yield return StartCoroutine(PanCameraWin(GetMoveRoot(winner)));
        
        ShowResult(playerWon);
        
        // Detener música si lo configuraste
        audioSource.Stop();
        audioManager.StopMusic();

        // Reproducir SFX de resultado una sola vez
        if (playerWon && audioWin != null)
        {
            if (audioManager != null) audioManager.PlaySFX(audioWin, sfxVolume);
            else audioSource.PlayOneShot(audioWin, sfxVolume);
        }
        else if (!playerWon && audioLose != null)
        {
            if (audioManager != null) audioManager.PlaySFX(audioLose, sfxVolume);
            else audioSource.PlayOneShot(audioLose, sfxVolume);
        }
        
        if (playerWon && audioWin != null)  audioSource.PlayOneShot(audioWin);
        else if (!playerWon && audioLose != null) audioSource.PlayOneShot(audioLose);
        
        Debug.Log(playerWon ? "¡Has ganado la carrera!" : "Has perdido la carrera.");
        
    }
    

    void EndRace()
    {
        if (_finishRace) return;
        _finishRace = true;

        StopAllContestants();
        Debug.Log("CARRERA FINALIZADA ");
    }

    IEnumerator PanCameraWin(Transform winner)
    {
        if (mainCamera == null || finishPoint == null || winner == null)
        {
            Debug.LogWarning("[RaceManager] Faltan referencias para el paneo final.");
            yield break;
        }
        // Prepara objetivos de cámara
        Vector3 lookTarget = winner.position + Vector3.up * lookAtOffsetY;

        // 3.1 Mover desde la posición ACTUAL de la cámara al plano CERRADO (suave)
        Vector3 closePos = winner.TransformPoint(panOffset);
        Quaternion closeRot = Quaternion.LookRotation((lookTarget - closePos).normalized, Vector3.up);

        Vector3 camStartPos = mainCamera.transform.position;
        Quaternion camStartRot = mainCamera.transform.rotation;
        float camStartFov = mainCamera.fieldOfView;

        float tClose = 0f;
        float durClose = Mathf.Max(0.01f, toCloseDuration);
        while (tClose < durClose)
        {
            tClose += Time.deltaTime;
            float k = Mathf.Clamp01(tClose / durClose);
            float e = Mathf.SmoothStep(0f, 1f, k);

            mainCamera.transform.position = Vector3.Lerp(camStartPos, closePos, e);
            mainCamera.transform.rotation = Quaternion.Slerp(camStartRot, closeRot, e);
            mainCamera.fieldOfView = Mathf.Lerp(camStartFov, fovClose, e);
            yield return null;
        }

        // Mantener un momento el plano cerrado
        if (closeHold > 0f)
            yield return new WaitForSeconds(closeHold);

        // 3.2 Interpolar al plano ABIERTO (zoom out)
        Vector3 widePos = winner.TransformPoint(wideOffsetLocal);
        Quaternion wideRot = Quaternion.LookRotation((lookTarget - widePos).normalized, Vector3.up);

        Vector3 fromPos = mainCamera.transform.position;
        Quaternion fromRot = mainCamera.transform.rotation;
        float fromFov = mainCamera.fieldOfView;

        float t = 0f;
        float dur = Mathf.Max(0.01f, panDuration);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = Mathf.SmoothStep(0f, 1f, k); // ease in/out

            mainCamera.transform.position = Vector3.Lerp(fromPos, widePos, e);
            mainCamera.transform.rotation = Quaternion.Slerp(fromRot, wideRot, e);
            mainCamera.fieldOfView = Mathf.Lerp(fromFov, fovWide, e);
            yield return null;
        }

        if (holdAfterPan > 0f)
            yield return new WaitForSeconds(holdAfterPan);
    }

    private Transform GetMoveRoot(Transform t)
    {
        var rb = t.GetComponentInParent<Rigidbody>();
        if (rb != null) return rb.transform;

        return t.root != null ? t.root : t;
    }

    private void StopAllContestants()
    {
        if (playerInput != null)
        {
            playerInput.OnRaceStop();
            playerInput.enabled = false;

            var prb = playerInput.GetComponentInParent<Rigidbody>();
            if (prb)
            {
                prb.isKinematic = true;
                prb.linearVelocity = Vector3.zero;
                prb.angularVelocity = Vector3.zero;
            }

            var pAnim = playerInput.GetComponentInChildren<Animator>();
            if (pAnim) { pAnim.applyRootMotion = false; pAnim.enabled = false; }
        }

        if (bots != null)
        {
            foreach (var bot in bots)
            {
                if (bot == null) continue;
                bot.OnRaceStop();
                bot.enabled = false;
                
                var brb = bot.GetComponentInParent<Rigidbody>();
                if (brb)
                {
                    brb.linearVelocity = Vector3.zero;
                    brb.angularVelocity = Vector3.zero;
                    brb.isKinematic = true;
                }
                
                var bAnim = bot.GetComponentInChildren<Animator>();
                if (bAnim) { bAnim.applyRootMotion = false; bAnim.enabled = false; }
            }
        }
    }

    [SerializeField] private float finishLift = 0.05f;

    private void TeleportWinnerToFinishPoint(Transform winner)
    {
        if (winner == null || finishPoint == null) 
        {
            Debug.LogWarning("[RaceManager] No se puede teletransportar al ganador. Faltan referencias.");
            return;
        }
        
        Transform moveRoot = GetMoveRoot(winner);
        Vector3 targetPos = finishPoint.position + Vector3.up * finishLift;
        Quaternion targetRot = finishPoint.rotation;
        
        var rb = moveRoot.GetComponent<Rigidbody>();
        
        Debug.Log($"raceManger teleport start  winner={winner.name}, moveRoot={moveRoot.name}, pos={targetPos}, rot={targetRot.eulerAngles}");

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // evitar fisicas durante el teletransporte
            
            
            rb.position = targetPos;
            rb.rotation = targetRot;
            moveRoot.SetPositionAndRotation(targetPos, targetRot);
        }
        else
        {
            moveRoot.SetPositionAndRotation(targetPos, targetRot);
        }
        
        Physics.SyncTransforms();
        
        Debug.Log("teleport end");
    }

    private void ShowResult(bool playerWon)
    {
        List<int> Indexes = new List<int>();
        if (!playerWon)
        {
            Indexes = new List<int>() {0,1,2,3};
        }
        else
        {

            Indexes = new List<int>() { 3, 2, 1, 0 };
        }
        PlayerPrefs.SetString("WinnerYogoNado", JsonConvert.SerializeObject(Indexes));
        if (resultsUI != null) resultsUI.SetActive(true);
        if (panelWin != null) panelWin.SetActive(playerWon);
        if (panelLose != null) panelLose.SetActive(!playerWon);
    }
    
}
        
    
