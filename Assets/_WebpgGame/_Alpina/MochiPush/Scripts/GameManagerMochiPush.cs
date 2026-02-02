using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class GameManagerMochiPush : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private TournamentManager tournamentManager;
    
    [Header("Participantes")]
    [Tooltip("Arrastra aquí el CollisionMochiPush del jugador principal.")]
    public CollisionMochiPush playerCombatant;
    public List<CollisionMochiPush> combatants = new List<CollisionMochiPush>();
    
    [Header("Start Game Components")]
    [SerializeField] private float gameDuration = 180f;
    [SerializeField] private float timeRemaining;
    public TMP_Text timerText;
    
    [SerializeField] private GameObject tutorialUIGO;
    [SerializeField] private GameObject UIControls;
    [SerializeField] private CinemachineSplineDolly introCameraDolly;
    [SerializeField] private GameObject miniGameBaseCamera;
    
    [SerializeField] private float timeToStartIntro = 5f;
    [SerializeField] private float startPanDuration = 4f;
    [SerializeField] private PowerUpSpawner spawner;
    bool gameRunning = false;
    
    [Header("UI Resultados")]
    public GameObject resultadosUI;   
    public GameObject panelGanaste;   
    public GameObject panelPerdiste;  

    [Header("Cámara (paneo final)")]
    public CameraFollowMochiPush cameraPanner;
    public float panDistance = 5.5f;
    public float panHeight = 3.0f;
    public float panArcDeg = 35f;
    public float panDuration = 2.0f;

    [Header("Al finalizar")]
    public bool stopBotsOnEnd = true;  

    [Header("Chequeo de victoria")]
    public float checkEvery = 0.25f;

    [SerializeField] private bool _finished = false;
    float _nextCheck;
    
    [Header("Audios")]
    private AudioManager audioManager;
    
    public AudioClip backgroundMusic;
    public AudioClip victoryClip;
    public AudioClip loseClip;
    
    private void Awake()
    {
        // Buscar AudioManager (como en tu GameManager)
        if (audioManager == null) audioManager = AudioManager.Instance;
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
    }
    void Start()
    {
        if(backgroundMusic!=null)
            if (audioManager != null) 
                audioManager.PlayMusic(backgroundMusic, 1f);
        if (resultadosUI) resultadosUI.SetActive(false);
        if (UIControls != null)
            UIControls.SetActive(false);
        SwitchBotPlayerState(false);
        if (introCameraDolly != null)
        {
            // iniciar juego luego del paneo
            StartCoroutine(WaitFortutorialTime());
        }
        timeRemaining = gameDuration;
        UpdateTimerUI(timeRemaining);
    }

    void FixedUpdate()
    {
        if (_finished || !gameRunning) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerUI(timeRemaining);

        if (timeRemaining <= 0)
        {
            CheckWinCondition();
        }
        if (Time.time >= _nextCheck)
        {
            CheckWinCondition();
            _nextCheck = Time.time + checkEvery;
        }
    }
    void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;
        seconds = Mathf.Max(0f, seconds);
        int s = Mathf.CeilToInt(seconds);
        int mins = s / 60;
        int secs = s % 60;
        timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
    }
    // Llama esto desde CollisionMochiPush.Eliminate()
    public void NotifyEliminated(CollisionMochiPush who)
    {
        if (_finished) return;
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (_finished) return;
        // “En pie en la plataforma” = activo y no Eliminated
        var vivos = combatants
            .Where(c => c != null && c.gameObject.activeInHierarchy && !c.Eliminated)
            .ToList();

        if (vivos.Count <= 1)
        {
            _finished = true;
            CollisionMochiPush winner = vivos.Count == 1 ? vivos[0] : null;
            StartCoroutine(EndSequence(winner));
        }
        else if(timeRemaining <= 0)
        {
            StartCoroutine(EndSequence());
        }
    }

    IEnumerator EndSequence(CollisionMochiPush winner = null)
    {
        if (stopBotsOnEnd)
        {
            foreach (var c in combatants)
            {
                if (!c) continue;
                var bot = c.GetComponent<BotMochiPush>();
                if (bot) bot.enabled = false;

                var rb = c.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        
        if (winner && cameraPanner)
        {
            yield return cameraPanner.PanToTargetCoroutine(
                winner.transform,
                panDistance,
                panHeight,
                panArcDeg,
                panDuration
            );
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        
        if (resultadosUI) resultadosUI.SetActive(true);

        bool playerGano = (winner != null && playerCombatant != null && winner == playerCombatant);
        if (panelGanaste) panelGanaste.SetActive(playerGano);
        if (panelPerdiste) panelPerdiste.SetActive(!playerGano);

        if (audioManager != null)
        {
            if (playerGano && victoryClip != null)
                audioManager.PlayMusic(victoryClip);
            else if (!playerGano && loseClip != null)
                audioManager.PlayMusic(loseClip);
            
        }
        
        yield return new WaitForSeconds(5f);
        tournamentManager.LoadNextGameUsingCourtain();
        
    }

    
    public void Register(CollisionMochiPush c)
    {
        if (!c) return;
        if (!combatants.Contains(c)) combatants.Add(c);
    }

    public void Unregister(CollisionMochiPush c)
    {
        if (!c) return;
        combatants.Remove(c);
    }
    
    IEnumerator WaitFortutorialTime()
    {
        yield return new WaitForSeconds(timeToStartIntro);
        
        introCameraDolly.enabled = true;
        if(tutorialUIGO !=null)
            tutorialUIGO.SetActive(false);
        
        yield return new WaitForSeconds(startPanDuration);
        if (UIControls != null) UIControls.SetActive(true);
        miniGameBaseCamera.SetActive(true);
        StartGame();
    }
    public void StartGame()
    {
        gameRunning = true;

        SwitchBotPlayerState(true);
        
        if (spawner != null)
            spawner.StartCoroutine();
    }

    private void SwitchBotPlayerState(bool state)
    {
        foreach (var p in combatants)
        {
            p.canMove = state;
            p.enabled = state;
        }
        playerCombatant.canMove = state;    
        playerCombatant.enabled = state;
    }
}