using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public bool enableKeyboard = true;
    public bool enableTouch = true;
    public bool enableJoysticks = true;
    
    [Header("References")]
    public PlayerController playerController;
    public GameManager gameManager;
    
    [Header("Touch Settings")]
    public float touchDeadZone = 50f;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    private Vector2 inputVector = Vector2.zero;
    private bool jumpPressed = false;
    
    void Start()
    {
        
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
            Debug.LogError(" InputManager: No PlayerController found!");
        else if(showDebug)
            Debug.Log(" InputManager initialized");
        
    }
    
    void Update()
    {
        ProcessInput();
        ApplyInput();
    }
    
    void ProcessInput()
    {
        inputVector = Vector2.zero;
        jumpPressed = false;
        
        // Keyboard Input
        if (enableKeyboard) ProcessKeyboardInput();
        // Touch Input
        if (enableTouch) ProcessTouchInput();
        
        // Debug
        if (showDebug && (inputVector.magnitude > 0.1f || jumpPressed))
            Debug.Log($"Input: {inputVector}, Jump: {jumpPressed}");
        
    }
    
    void ProcessKeyboardInput()
    {
        float horizontal = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        inputVector.x = horizontal;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            if (showDebug) Debug.Log(" Keyboard jump pressed!");
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
            if (showDebug) Debug.Log(" Touch jump triggered!");
        }
    }
    
    void ApplyInput()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null && !gameManager.gameStared) return;
        
        // Apply jump
        if (jumpPressed && playerController != null)
        {
            if (inputVector.x > 0.1f) playerController.JumpRight();
            else if (inputVector.x < -0.1f) playerController.JumpLeft();
            else playerController.Jump();
        }
        
    }
    
    
    public void OnJoystickLeft()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null && !gameManager.gameStared) return;
        if (playerController != null && !playerController.isJumping) playerController.JumpLeft();
    }

    public void OnJoystickRight()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null && !gameManager.gameStared) return;
        if (playerController != null && !playerController.isJumping) playerController.JumpRight();
            
        
    }
}