using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    
    [Header("Input Settings")]
    public bool enableKeyboardInput = true;
    //public bool enableTouchInput = true;
   // public bool enableJoystickInput = true;
    
    [Header("Jump Settings")]
    public float jumpTime = 0.3f; //duración de salto
    public float jumpArcHeight  = 2.5f; //Altura del arco 

    //[Header("Platform Detection")] public float platformSearchTolerance = 0.6f; //tolerancia vertical para buscar plataformas
    
    //[Header("Ground Check")]
   // public Transform groundCheck;
    //public float groundCheckRadius = 0.3f;
    
    [Header("Events")]
    public UnityEvent OnJump = new UnityEvent();
    public UnityEvent OnDie = new UnityEvent();
    //public UnityEvent OnLand = new UnityEvent();
    
    private Rigidbody rb;
    public bool isDead = false;
    private bool wasGrounded = false;
    private float landingTime = 0f;
    public bool isJumping = false;
    public float victoryHeight;
    public bool arrived = false;
    
    
    
    public GameManager gameManager;
    public PlatformGenerator platformGenerator;
    public GameObject finishCloud;
    
    public bool IsAlive => !isDead;
    public bool IsJumping => isJumping;
    public bool IsMoving => Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    public Vector3 CurrentVelocity => rb.linearVelocity;
    public float CurrentHeight => transform.position.y;
    GameObject[] platforms;
    
    [SerializeField]
    public Camera playerCamera;
    
    
    void Awake()
    {
        // Asegurar que el PlayerController esté activo y referenciado
        this.enabled = true;
        InputManager inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null && inputManager.playerController == null)
        {
            inputManager.playerController = this;
            Debug.Log("PlayerController se ha auto-asignado al InputManager.");
        }
    }

    void Start()
    {
        StartCoroutine(WaitAndLand());
        rb = GetComponent<Rigidbody>();
        
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (platformGenerator == null)
            platformGenerator = FindFirstObjectByType<PlatformGenerator>();
        
        victoryHeight = finishCloud.transform.position.y; // Valor por defecto si no hay PlatformGenerator
    }
    
    IEnumerator WaitAndLand() 
    {
        
        yield return new WaitForSeconds(1f);
        platforms = GameObject.FindGameObjectsWithTag("Platform");
        
    }
    
    void Update()
    {
        if (isDead) return;
        
        if (gameManager != null && !gameManager.gameStared) return;
        
        // Permitir input de teclado para saltar izquierda/derecha
        if (!isJumping && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))
        {
            JumpLeft();
        }
        if (!isJumping && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)))
        {
            JumpRight();
        }

        if (!arrived && transform.position.y >= victoryHeight)
        {
            arrived = true;
            if (gameManager != null)
                gameManager.RegisterFinish(PlayerPrefs.GetString("PlayerName", "P1"));
        }
        
    }
    
    public void JumpLeft()
    {
        if (!isJumping && !isDead)
        {
            isJumping = true;
            StartCoroutine(JumpLeftCoroutine());
            Debug.Log("JumpLeft");
        }
    }

    public void JumpRight()
    {
        if (!isJumping && !isDead)
        {
            isJumping = true;
            StartCoroutine(JumpRightCoroutine());
            Debug.Log("Jumpright");
        }
    }
    
    public void DieWithMessage(string message)
    {
        if (isDead) return;
        isDead = true;
        isJumping = false;
        Debug.Log($" {message}");
        OnDie?.Invoke();

        if (gameManager != null) ;
        
        Invoke("RestartLevel", 2f);
        
        
    }
    

    IEnumerator JumpLeftCoroutine()
    {
        rb.linearVelocity = Vector3.zero;

        float  horizontaDistance = platformGenerator.separacionEntrePlataformas;
        
        float nextX = transform.position.x - (horizontaDistance / 2);
        float nextY = transform.position.y + platformGenerator.platformSpacing;
        
        // Buscar plataforma más cercana en la siguiente fila hacia la izquierda
        GameObject nextPlatform = FindClosestPlatform(nextX, nextY, horizontaDistance);
        
        if (nextPlatform != null)
        {
            Vector3 targetPosition = nextPlatform.transform.position;
            yield return StartCoroutine(AnimateJump(targetPosition));
            OnJump?.Invoke();
        }
        isJumping = false;
    }

    IEnumerator JumpRightCoroutine()
    {
        rb.linearVelocity = Vector3.zero;
        
        float horizontalDistance = platformGenerator.separacionEntrePlataformas;
        
        float nextX = transform.position.x + (horizontalDistance / 2);
        float nextY = transform.position.y + platformGenerator.platformSpacing;
        
        GameObject nextPlatform = FindClosestPlatform(nextX, nextY, horizontalDistance);
        
        if (nextPlatform != null)
        {
            Vector3 targetPosition = nextPlatform.transform.position;
            yield return StartCoroutine(AnimateJump(targetPosition));
            OnJump?.Invoke();
        }
        isJumping = false;
    }

    public void Jump()
    {
    
    Debug.Log("Salto vertical no permitido, pero el personaje no muere.");

    }

    public float testJump;
    
    // Buscar la plataforma más cercana en la siguiente fila
    GameObject FindClosestPlatform(float targetX, float targetY, float searchRadius)
    {
        GameObject closest = null;
        float minDist = testJump;
        //GameObject[] platforms = GameObject.FindGameObjectsWithTag("Platform");
        
        foreach (var platform in platforms)
        {
            Vector3 plaPos = platform.transform.position;
            float dx = Mathf.Abs(platform.transform.position.x - targetX);
            float dy = Mathf.Abs(platform.transform.position.y - targetY);
            if (dy < platformGenerator.platformSpacing * 0.6f  && dx < minDist)
            {
                //minDist = dx;
                closest = platform;
                Debug.Log($"Dx {dx}");
            }
        }
        return closest;
    }
    
    
    IEnumerator AnimateJump(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float jumpTime = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpTime)
        {
            float t = elapsedTime / jumpTime;
            float height = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
            currentPos.y += height;
            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        
    }
    
    public void Restart()
    {
        isDead = false;
        isJumping = false;
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isJumping = false;
        Debug.Log(" Player died!");
        OnDie?.Invoke();
        Invoke("RestartLevel", 2f);
    }
    
    void RestartLevel()
    {
        isDead = false;
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }
    
}