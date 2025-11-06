using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManagerMochiPush : MonoBehaviour
{
    [Header("Arena")]
    public Transform arenaCenter;
    public float arenaRadius = 12f;

    [Header("Participantes")]
    [Tooltip("Arrastra aquí el CollisionMochiPush del jugador principal.")]
    public CollisionMochiPush playerCombatant;

    [Tooltip("Se puede llenar por Inspector o se autollenará en Start.")]
    public List<CollisionMochiPush> combatants = new List<CollisionMochiPush>();

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

    bool _finished;
    float _nextCheck;
    
    [Header("Audios")]
    public AudioSource audioSource;
    public AudioClip victoryClip;
    public AudioClip loseClip;
    
    void Start()
    {
        if (resultadosUI) resultadosUI.SetActive(false);
    }

    void Update()
    {
        if (_finished) return;

        if (Time.time >= _nextCheck)
        {
            CheckWinCondition();
            _nextCheck = Time.time + checkEvery;
        }
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
    }

    IEnumerator EndSequence(CollisionMochiPush winner)
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

        if (audioSource != null)
        {
            audioSource.Stop();
            if (playerGano && victoryClip != null)
                audioSource.PlayOneShot(victoryClip);
            else if (!playerGano && loseClip != null)
                audioSource.PlayOneShot(loseClip);
            
        }
        

        
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
    
    
}