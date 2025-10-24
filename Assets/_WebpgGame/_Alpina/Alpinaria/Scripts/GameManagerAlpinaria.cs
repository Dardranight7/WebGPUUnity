using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

// GameManager: controla countdown, timer, posiciones (ranking) y resultado para el mainplayer.
// Integra UIManager + RaceManager en un solo script.
public class GameManagerAlpinaria : MonoBehaviour
{
    [Header("Referencias de jugadores")]
    public Transform mainPlayer;         // asigna el main player (tu propio PlayerController ya existente lo controla)
    public BotControllerAlpinaria[] bots;         // asigna los bots en el inspector (total 3 + main = 4)

    [Header("Meta / Pista")]
    public Transform finishTransform;    // punto de meta (Transform)
    public float finishDistanceThreshold = 1.2f; // distancia para considerar llegada

    [Header("Cámara")]
    public CameraFollowAlpinaria camFollow;       // asegúrate de asignar la instancia en el Inspector
    public float panDuration = 1.6f;

    [Header("UI - Timer / Countdown / Result / Posiciones")]
    public Text timerText;
    public GameObject countdownPanel;
    public Text countdownText;
    public GameObject winnerPanel;
    public GameObject loserPanel;

    // slots verticales a la derecha; 4 imágenes ordenadas de arriba=1ro a abajo=4to
    public Image[] positionSlots;
    public Sprite mainPlayerIcon;
    public Sprite botIcon;

    // estado interno
    private bool raceStarted = false;
    private float raceTime = 0f;
    private List<Transform> finishOrder = new List<Transform>();
    private bool raceEnded = false;

    void Start()
    {
        // Desactivar movimiento de bots hasta countdown
        foreach (var b in bots) if (b) b.canMove = false;

        UpdateTimerUI(0f);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);
        if (loserPanel != null) loserPanel.SetActive(false);

        StartCoroutine(PreRaceCountdown());
    }

    void Update()
    {
        if (raceStarted && !raceEnded)
        {
            raceTime += Time.deltaTime;
            UpdateTimerUI(raceTime);
            UpdatePositionsUI();
            // también revisamos si mainPlayer llegó (por seguridad)
            if (Vector3.Distance(mainPlayer.position, finishTransform.position) <= finishDistanceThreshold && !finishOrder.Contains(mainPlayer))
            {
                OnPlayerFinish(mainPlayer);
            }
        }
    }

    IEnumerator PreRaceCountdown()
    {
        if (countdownPanel != null && countdownText != null)
        {
            countdownPanel.SetActive(true);
            string[] steps = new string[] { "3", "2", "1", "¡YA!" };
            foreach (var s in steps)
            {
                countdownText.text = s;
                yield return new WaitForSeconds(1f);
            }
            countdownPanel.SetActive(false);
        }
        StartRace();
        yield break;
    }

    void StartRace()
    {
        raceStarted = true;
        raceTime = 0f;
        foreach (var b in bots) if (b) b.canMove = true;
    }

    void UpdateTimerUI(float time)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Actualiza columna de posiciones basándose en distancia a finish (menor distancia = adelante)
    void UpdatePositionsUI()
    {
        List<Transform> participants = new List<Transform>();
        participants.Add(mainPlayer);
        foreach (var b in bots) if (b != null) participants.Add(b.transform);

        participants.Sort((a, b) =>
        {
            float da = Vector3.Distance(a.position, finishTransform.position);
            float db = Vector3.Distance(b.position, finishTransform.position);
            return da.CompareTo(db);
        });

        for (int i = 0; i < positionSlots.Length; i++)
        {
            if (i < participants.Count && positionSlots[i] != null)
            {
                var tr = participants[i];
                positionSlots[i].sprite = (tr == mainPlayer) ? mainPlayerIcon : botIcon;
                positionSlots[i].color = Color.white;
            }
            else if (positionSlots[i] != null)
            {
                positionSlots[i].color = new Color(1f, 1f, 1f, 0.15f);
            }
        }
    }

    // Llamar desde un trigger (FinishTrigger) o desde Update si detectas distancia
    public void OnPlayerFinish(Transform player)
    {
        if (finishOrder.Contains(player)) return;
        finishOrder.Add(player);

        // Si el primer en llegar es el mainPlayer, haremos paneo y mostraremos winner panel
        if (finishOrder.Count == 1)
        {
            StartCoroutine(HandleFirstPlace(player));
        }

        // Si todos ya terminaron -> finalizar carrera
        int totalParticipants = 1 + bots.Count(b => b != null);
        if (finishOrder.Count >= totalParticipants)
        {
            raceEnded = true;
            // detener bots (usa su método público StopMovement)
            foreach (var b in bots)
            {
                if (b != null)
                {
                    b.StopMovement();
                }
            }
            // si el main no fue ganador y no ha mostrado panel, mostrar panel de derrota
            bool mainIsWinner = finishOrder.Count > 0 && finishOrder[0] == mainPlayer;
            if (!mainIsWinner)
            {
                ShowResultForMain(false);
            }
        }
    }

    IEnumerator HandleFirstPlace(Transform winner)
    {
        // detener a todos mientras paneamos al ganador
        foreach (var b in bots) if (b) b.canMove = false;
        raceStarted = false;

        // Si tiene Animator ponlo Idle si hace falta (opcional)
        Animator anim = winner.GetComponent<Animator>();
        if (anim) { anim.SetTrigger("Idle"); }

      

        // Mostrar panel dirigido al mainplayer
        bool mainIsWinner = (finishOrder.Count > 0 && finishOrder[0] == mainPlayer);
        ShowResultForMain(mainIsWinner);
        yield return null;
    }

    void ShowResultForMain(bool mainIsWinner)
    {
        if (mainIsWinner)
        {
            if (winnerPanel != null) winnerPanel.SetActive(true);
        }
        else
        {
            if (loserPanel != null) loserPanel.SetActive(true);
        }
    }
}