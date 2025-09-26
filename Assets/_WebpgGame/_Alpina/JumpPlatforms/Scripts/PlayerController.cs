using UnityEngine;
using UnityEngine.Events;
using System.Collections;  // Necesario para IEnumerator

public class PlayerController : MonoBehaviour
{
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 2.0f;       // EXTREMADAMENTE REDUCIDO para mini-salto tipo "cuadrícula"
    public float horizontalForce = 1.0f;  // EXTREMADAMENTE REDUCIDO para mini-movimiento tipo "cuadrícula"
    public float maxHorizontalSpeed = 1.5f; // Muy limitado para control preciso
    public float jumpHeight = 0.5f;      // Altura muy pequeña para salto casi imperceptible
    public float jumpDuration = 0.2f;    // Super rápido para inmediatez
    public float gravityScale = 1.0f;    // Gravedad mínima
    public float airControl = 0.1f;      // Control mínimo para movimiento predecible
    
    [Header("Auto Jump Settings")]
    public bool autoJump = false;  // Desactivado por defecto para mejor control manual
    public float autoJumpDelay = 0.1f;
    
    [Header("Input Settings")]
    public bool enableKeyboardInput = true;
    public bool enableTouchInput = true;
    public bool enableJoystickInput = true;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;
    
    [Header("Events")]
    public UnityEvent OnJump = new UnityEvent();
    public UnityEvent OnDie = new UnityEvent();
    public UnityEvent OnLand = new UnityEvent();
    
    private Rigidbody rb;
    public bool isDead = false;
    private bool wasGrounded = false;
    private float landingTime = 0f;
    public bool isJumping = false;
    
    // Public properties for other scripts
    public bool IsAlive => !isDead;
    public bool IsJumping => isJumping;
    public bool IsMoving => Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    public Vector3 CurrentVelocity => rb.linearVelocity;
    public float CurrentHeight => transform.position.y;
    
    void Awake()
    {
        // Asegurar que el PlayerController esté activo y referenciado
        this.enabled = true;
        
    InputManager inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null && inputManager.playerController == null)
        {
            inputManager.playerController = this;
            Debug.Log("🎮 PlayerController se ha auto-asignado al InputManager.");
        }
    }

    void Start()
    {
    rb = GetComponent<Rigidbody>();
    // Aparecer en Y=0 (suelo)
    transform.position = new Vector3(0, 0, 0);
        
        // Auto-create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = Vector3.down * 0.5f;
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        if (isDead) return;
        // Permitir input de teclado para saltar izquierda/derecha
        if (!isJumping && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))
        {
            JumpLeft();
        }
        if (!isJumping && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)))
        {
            JumpRight();
        }
        // Permitir salto con joystick touch si tienes método público
    }
    
    // Métodos públicos para compatibilidad con VirtualJoystick
    public void JumpLeft()
    {
        if (!isJumping && !isDead)
        {
            isJumping = true;
            StartCoroutine(JumpLeftCoroutine());
        }
    }

    public void JumpRight()
    {
        if (!isJumping && !isDead)
        {
            isJumping = true;
            StartCoroutine(JumpRightCoroutine());
        }
    }
    // MÉTODO PERSONALIZADO PARA DERROTA CON MENSAJE
    public void DieWithMessage(string message)
    {
        if (isDead) return;
        isDead = true;
        isJumping = false;
        Debug.Log($"💀 {message}");
        OnDie?.Invoke();
        // Aquí puedes agregar UI para mostrar el mensaje en pantalla
        // Por ahora solo loguea y reinicia
        Invoke("RestartLevel", 2f);
    }

    // Eliminar CheckGrounded()
    // Eliminar cualquier referencia a isGrounded en JumpLeft, JumpRight, Jump

    IEnumerator JumpLeftCoroutine()
    {
    rb.linearVelocity = Vector3.zero;
        float nextY = transform.position.y + 1.5f;
        // Buscar plataforma más cercana en la siguiente fila hacia la izquierda
        GameObject nextPlatform = FindClosestPlatform(transform.position.x - 1.5f, nextY);
        if (nextPlatform != null)
        {
            Vector3 targetPosition = new Vector3(nextPlatform.transform.position.x, nextPlatform.transform.position.y, transform.position.z);
            yield return StartCoroutine(AnimateJump(targetPosition));
            OnJump?.Invoke();
            Debug.Log($"🚀 Perfect Jump Left to position: {targetPosition}");
        }
        else
        {
            // No muere, simplemente no hace nada si no hay plataforma
            Debug.Log("No hay plataforma en la siguiente fila a la izquierda, pero el personaje no muere.");
        }
        isJumping = false;
    }

    IEnumerator JumpRightCoroutine()
    {
    rb.linearVelocity = Vector3.zero;
        float nextY = transform.position.y + 1.5f;
        // Buscar plataforma más cercana en la siguiente fila hacia la derecha
        GameObject nextPlatform = FindClosestPlatform(transform.position.x + 1.5f, nextY);
        if (nextPlatform != null)
        {
            Vector3 targetPosition = new Vector3(nextPlatform.transform.position.x, nextPlatform.transform.position.y, transform.position.z);
            yield return StartCoroutine(AnimateJump(targetPosition));
            OnJump?.Invoke();
            Debug.Log($"🚀 Perfect Jump Right to position: {targetPosition}");
        }
        else
        {
            // No muere, simplemente no hace nada si no hay plataforma
            Debug.Log("No hay plataforma en la siguiente fila a la derecha, pero el personaje no muere.");
        }
        isJumping = false;
    }

    public void Jump()
    {
    // No se permite salto vertical, solo derecha o izquierda
    Debug.Log("Salto vertical no permitido, pero el personaje no muere.");

    }

    // Buscar la plataforma más cercana en la siguiente fila
    GameObject FindClosestPlatform(float targetX, float targetY)
    {
        float minDist = 2.0f;
        GameObject closest = null;
        foreach (var platform in GameObject.FindGameObjectsWithTag("Platform"))
        {
            float dx = Mathf.Abs(platform.transform.position.x - targetX);
            float dy = Mathf.Abs(platform.transform.position.y - targetY);
            if (dy < 0.8f && dx < minDist)
            {
                minDist = dx;
                closest = platform;
            }
        }
        return closest;
    }
    
    // Animación suave de salto para ir exactamente a la posición deseada
    IEnumerator AnimateJump(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float jumpTime = 0.3f; // Duración de salto corta para sensación ágil
        float elapsedTime = 0f;
        
        while (elapsedTime < jumpTime)
        {
            float t = elapsedTime / jumpTime;
            float height = Mathf.Sin(t * Mathf.PI) * 0.5f;
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
            currentPos.y += height;
            transform.position = currentPos;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        isJumping = false; // <-- Reforzado aquí
        // isGrounded eliminado
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
        Debug.Log("💀 Player died!");
        OnDie?.Invoke();
        
        // Add death effects here
        // For now, just restart the level after a delay
        Invoke("RestartLevel", 2f);
    }
    
    void RestartLevel()
    {
        isDead = false;
        // Reset player position or reload scene
        transform.position = Vector3.zero;
    rb.linearVelocity = Vector3.zero;
    }
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}