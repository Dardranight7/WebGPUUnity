using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public bool enableKeyboard = true;
    public bool enableTouch = true;
    public bool enableJoysticks = true;
    
    [Header("References")]
    public PlayerController playerController;
    
    [Header("Touch Settings")]
    public float touchDeadZone = 50f;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    private Vector2 inputVector = Vector2.zero;
    private bool jumpPressed = false;
    
    void Start()
    {
        // Find player if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("❌ InputManager: No PlayerController found!");
        }
        else
        {
            Debug.Log("✅ InputManager initialized");
        }
    }
    
    void Update()
    {
        // ACTIVADO SOLO PARA JOYSTICKS!
        // El PlayerController maneja su propio input de teclado
        // Este InputManager solo procesa joysticks virtuales
        
        // Los joysticks llaman directamente a OnJoystickLeft/Right
        // No hacemos nada aquí para evitar interferencia
        
        // Dejamos esto vacío a propósito
    }
    
    void ProcessInput()
    {
        inputVector = Vector2.zero;
        jumpPressed = false;
        
        // Keyboard Input
        if (enableKeyboard)
        {
            ProcessKeyboardInput();
        }
        
        // Touch Input
        if (enableTouch)
        {
            ProcessTouchInput();
        }
        
        // Debug
        if (showDebug && (inputVector.magnitude > 0.1f || jumpPressed))
        {
            Debug.Log($"Input: {inputVector}, Jump: {jumpPressed}");
        }
    }
    
    void ProcessKeyboardInput()
    {
        float horizontal = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        
        inputVector.x = horizontal;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            if (showDebug) Debug.Log("🚀 Keyboard jump pressed!");
        }
    }
    
    void ProcessTouchInput()
    {
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        Vector2 touchPos = touch.position;
        float screenCenter = Screen.width * 0.5f;
        
        // Simple left/right detection
        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
        {
            if (touchPos.x > screenCenter + touchDeadZone)
            {
                inputVector.x = 1f;
                if (showDebug) Debug.Log($"👉 Touch right: {touchPos}");
            }
            else if (touchPos.x < screenCenter - touchDeadZone)
            {
                inputVector.x = -1f;
                if (showDebug) Debug.Log($"👈 Touch left: {touchPos}");
            }
        }
        
        // Jump on touch begin
        if (touch.phase == TouchPhase.Began)
        {
            jumpPressed = true;
            if (showDebug) Debug.Log("🚀 Touch jump triggered!");
        }
    }
    
    void ApplyInput()
    {
        // Apply jump
        if (jumpPressed)
        {
            if (inputVector.x > 0.1f)
            {
                playerController.JumpRight();
            }
            else if (inputVector.x < -0.1f)
            {
                playerController.JumpLeft();
            }
            else
            {
                playerController.Jump();
            }
        }
        
        // Don't interfere with PlayerController's own movement handling
        // Let PlayerController handle its own movement logic
    }
    
    // Public methods for joysticks to call
    public void OnJoystickLeft()
    {
        if (playerController != null && !playerController.isJumping)
        {
            playerController.JumpLeft();
            if (showDebug) Debug.Log("🕹️ Joystick Left triggered!");
        }
    }

    public void OnJoystickRight()
    {
        if (playerController != null && !playerController.isJumping)
        {
            playerController.JumpRight();
            if (showDebug) Debug.Log("🕹️ Joystick Right triggered!");
        }
    }
}