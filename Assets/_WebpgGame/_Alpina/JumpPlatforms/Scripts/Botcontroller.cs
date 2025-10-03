using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BotController : MonoBehaviour
{
    public float jumpInterval = 1f;
    public LayerMask PlatformLayer;
    public bool isAlive = true;
    public bool canMove = false;
    public GameManager gameManager;
    public PlatformGenerator platformGenerator;
    public float victoryHeight;
    public string botName = "Bot";
    public bool arrived;
    private Animator animator;
    private Rigidbody rb;
    private float lastJumpTime = 0f;
    private bool isJumping = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (platformGenerator == null) platformGenerator = FindObjectOfType<PlatformGenerator>();
        if (platformGenerator != null) victoryHeight = platformGenerator.platformSpacing * (platformGenerator.maxFilas - 1);

        // Si el GameManager ya indicó inicio, activar bots; también escalonar el primer salto
        canMove = (gameManager != null && gameManager.gameStared);
        lastJumpTime = Time.time - Random.Range(0f, jumpInterval);
    }

    void Update()
    {
        // Auto-start bots cuando GameManager cambie a started
        if (arrived || !isAlive || !canMove) return;
        
        if (!canMove && gameManager != null && gameManager.gameStared)
            StartBot();

        if (gameManager != null && !gameManager.gameStared) return;
        if (!canMove || !isAlive) return;

        if (Time.time - lastJumpTime >= jumpInterval)
        {
            lastJumpTime = Time.time;
            Transform nextPlatform = FindNextPlatform();
            if (nextPlatform != null && !isJumping)
            {
                StartCoroutine(JumpToPlatform(nextPlatform.position));
            }
        }

        if (!arrived && transform.position.y >= victoryHeight)
        {
            arrived = true;
            canMove = false;
            if (gameManager != null) gameManager.RegisterFinish(botName);
        }
        Debug.Log($"{botName} ha llegado a la meta");
    }

    Transform FindNextPlatform()
    {
        if (platformGenerator == null) return null;

        float rowY = transform.position.y + platformGenerator.platformSpacing;
        float tolerance = Mathf.Max(0.6f, platformGenerator.platformSpacing * 0.5f);
        GameObject[] plats = GameObject.FindGameObjectsWithTag("Platform");
        List<Transform> candidates = new List<Transform>();

        // Primera pasada: plataformas exactamente en la siguiente fila (dentro de tolerancia)
        foreach (var p in plats)
        {
            if (Mathf.Abs(p.transform.position.y - rowY) <= tolerance)
                candidates.Add(p.transform);
        }

        // Si no hay candidatos, buscar plataformas ligeramente por encima (ventana mayor)
        if (candidates.Count == 0)
        {
            float maxSearchY = transform.position.y + platformGenerator.platformSpacing * 1.5f;
            float minSearchY = transform.position.y + 0.2f;
            foreach (var p in plats)
            {
                if (p.transform.position.y > minSearchY && p.transform.position.y <= maxSearchY)
                    candidates.Add(p.transform);
            }
        }

        if (candidates.Count == 0) return null;

        // Elegir la plataforma con menor distancia absoluta en X (primera por X)
        Transform best = null;
        float bestDx = float.MaxValue;
        float myX = transform.position.x;
        foreach (var c in candidates)
        {
            float dx = Mathf.Abs(c.position.x - myX);
            if (dx < bestDx)
            {
                bestDx = dx;
                best = c;
            }
        }

        return best;
    }

    IEnumerator JumpToPlatform(Vector3 targetPos)
    {
        isJumping = true;
        Vector3 startPos = transform.position;
        float jumpTime = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < jumpTime)
        {
            float t = elapsedTime / jumpTime;
            float height = Mathf.Sin(t * Mathf.PI) * 1.0f;
            Vector3 current = Vector3.Lerp(startPos, targetPos, t);
            current.y += height;
            transform.position = current;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        isJumping = false;
    }

    public void Die()
    {
        isAlive = false;
        canMove = false;
        //avisar al gamemanager para que compruebe el auto-win
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null) gameManager.CheckBotsStatus();
    }

    public void StartBot()
    {
        canMove = true;
    }

    public void StopBot()
    {
        canMove = false;
    }
}