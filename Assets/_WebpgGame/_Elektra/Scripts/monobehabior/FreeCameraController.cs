using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad normal de movimiento")]
    public float moveSpeed = 10f;
    
    [Tooltip("Multiplicador de velocidad al mantener Shift")]
    public float speedMultiplier = 2f;
    
    [Tooltip("Multiplicador de velocidad lenta al mantener Ctrl")]
    public float slowMultiplier = 0.5f;

    [Header("Mouse Look Settings")]
    [Tooltip("Sensibilidad del mouse")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Limitar rotación vertical")]
    public bool lockVerticalRotation = true;
    
    [Tooltip("Ángulo máximo de rotación vertical")]
    public float maxVerticalAngle = 90f;

    [Header("Smooth Movement")]
    [Tooltip("Suavizado del movimiento (0 = sin suavizado)")]
    public float smoothing = 5f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 velocity = Vector3.zero;
    private bool cursorLocked = true;

    void Start()
    {
        // Bloquear y ocultar el cursor al inicio
        LockCursor(true);
        
        // Inicializar rotación actual
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    void Update()
    {
        // Toggle cursor lock con tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLocked = !cursorLocked;
            LockCursor(cursorLocked);
        }

        if (cursorLocked)
        {
            HandleMouseLook();
        }

        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Obtener input del mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Calcular rotación
        rotationY += mouseX;
        rotationX -= mouseY;

        // Limitar rotación vertical si está activado
        if (lockVerticalRotation)
        {
            rotationX = Mathf.Clamp(rotationX, -maxVerticalAngle, maxVerticalAngle);
        }

        // Aplicar rotación
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    void HandleMovement()
    {
        // Obtener input de movimiento
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D o flechas
        float vertical = Input.GetAxisRaw("Vertical");     // W/S o flechas
        float upDown = 0f;

        // Movimiento vertical con Q/E
        if (Input.GetKey(KeyCode.E))
            upDown = 1f;
        else if (Input.GetKey(KeyCode.Q))
            upDown = -1f;

        // Calcular dirección de movimiento
        Vector3 moveDirection = new Vector3(horizontal, upDown, vertical).normalized;

        // Determinar velocidad actual
        float currentSpeed = moveSpeed;
        
        if (Input.GetKey(KeyCode.LeftAlt))
            currentSpeed *= speedMultiplier;
        else if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= slowMultiplier;

        // Calcular velocidad objetivo basada en la dirección de la cámara
        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * currentSpeed;

        // Aplicar suavizado si está activado
        if (smoothing > 0)
        {
            velocity = Vector3.Lerp(velocity, targetVelocity, Time.deltaTime * smoothing);
        }
        else
        {
            velocity = targetVelocity;
        }

        // Mover la cámara
        transform.position += velocity * Time.deltaTime;
    }

    void LockCursor(bool lockIt)
    {
        if (lockIt)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Método para resetear la posición (opcional, puedes llamarlo desde otro script)
    public void ResetPosition(Vector3 position, Vector3 rotation)
    {
        transform.position = position;
        rotationX = rotation.x;
        rotationY = rotation.y;
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
