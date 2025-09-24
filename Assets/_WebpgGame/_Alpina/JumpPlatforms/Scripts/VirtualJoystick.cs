using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Configuration")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float handleRange = 100f;
    public float deadZone = 0.3f;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    public InputManager inputManager;
    
    [Header("Joystick Side")]
    public bool isLeftJoystick = true;
    
    [Header("Behavior")]
    public bool jumpOnPress = true;
    public bool restrictToScreenHalf = true;
    
    [Header("Visual Feedback")]
    public Color normalColor = new Color(1, 1, 1, 0.2f);
    public Color pressedColor = new Color(0, 1, 0, 0.5f);
    
    // Private variables
    private Vector2 inputDirection = Vector2.zero;
    private bool isDragging = false;
    private Image backgroundImage;
    private bool hasTriggered = false;
    
    void Start()
    {
        InitializeJoystick();
    }
    
    void InitializeJoystick()
    {
        // Get or add Image component
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
            backgroundImage.color = normalColor;
        }
        
        // CRÍTICO: Asegurar raycast target
        backgroundImage.raycastTarget = true;
        
        // Asegurar que está en un Canvas con GraphicRaycaster
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError($"❌ {gameObject.name} must be a child of a Canvas!");
        }
        else
        {
            GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"⚠️ Canvas missing GraphicRaycaster - adding one");
                parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Auto-assign references if not set
        if (joystickBackground == null)
            joystickBackground = GetComponent<RectTransform>();
            
        if (joystickHandle == null && transform.childCount > 0)
        {
            joystickHandle = transform.GetChild(0).GetComponent<RectTransform>();
        }
        
        // Find player and input manager if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<InputManager>();
        }
        
        // Set initial visual state
        backgroundImage.color = normalColor;
        
        Debug.Log($"✅ VirtualJoystick '{gameObject.name}' initialized");
        Debug.Log($"   Side: {(isLeftJoystick ? "LEFT" : "RIGHT")}");
        Debug.Log($"   Raycast Target: {backgroundImage.raycastTarget}");
        Debug.Log($"   Player Found: {playerController != null}");
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"🕹️ {gameObject.name} TOUCHED!");
        
        // Check screen half restriction
        if (restrictToScreenHalf && !IsInCorrectScreenHalf(eventData.position))
        {
            Debug.Log($"❌ Touch outside allowed half for {gameObject.name}");
            return;
        }
        
        isDragging = true;
        hasTriggered = false;
        
        // Visual feedback
        backgroundImage.color = pressedColor;
        
        // Immediate jump on press
        if (jumpOnPress)
        {
            TriggerJump();
            hasTriggered = true;
        }
        
        // Handle drag for visual feedback
        OnDrag(eventData);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"🕹️ {gameObject.name} RELEASED!");
        
        isDragging = false;
        inputDirection = Vector2.zero;
        
        // Reset visual state
        backgroundImage.color = normalColor;
        
        // Reset handle position
        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
        
        hasTriggered = false;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || joystickBackground == null) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        
        Vector2 direction = localPoint.normalized;
        float distance = Mathf.Clamp(localPoint.magnitude, 0f, handleRange);
        
        // Update handle position
        if (joystickHandle != null)
            joystickHandle.anchoredPosition = direction * distance;
        
        // Update input direction
        inputDirection = direction * (distance / handleRange);
        if (inputDirection.magnitude < deadZone)
            inputDirection = Vector2.zero;
    }
    
    bool IsInCorrectScreenHalf(Vector2 screenPos)
    {
        if (!restrictToScreenHalf) return true;
        
        float screenCenter = Screen.width * 0.5f;
        
        if (isLeftJoystick)
            return screenPos.x <= screenCenter;
        else
            return screenPos.x > screenCenter;
    }
    
    void TriggerJump()
    {
        // Try input manager first
        if (inputManager != null)
        {
            if (isLeftJoystick)
            {
                inputManager.OnJoystickLeft();
                Debug.Log($"🚀 {gameObject.name} -> InputManager Jump LEFT");
            }
            else
            {
                inputManager.OnJoystickRight();
                Debug.Log($"🚀 {gameObject.name} -> InputManager Jump RIGHT");
            }
            return;
        }
        
        // Fallback to direct player control
        if (playerController == null)
        {
            Debug.LogError($"❌ No PlayerController or InputManager assigned to {gameObject.name}!");
            return;
        }
        
        if (isLeftJoystick)
        {
            playerController.JumpLeft();
            Debug.Log($"🚀 {gameObject.name} -> Direct Jump LEFT");
        }
        else
        {
            playerController.JumpRight();
            Debug.Log($"🚀 {gameObject.name} -> Direct Jump RIGHT");
        }
    }
    
    // Public properties
    public Vector2 Direction => inputDirection;
    public bool IsDragging => isDragging;
    
    // Debug methods
    [ContextMenu("Test Jump Left")]
    public void TestJumpLeft()
    {
        if (playerController != null)
        {
            playerController.JumpLeft();
            Debug.Log("🔧 TEST: Jump Left");
        }
    }
    
    [ContextMenu("Test Jump Right")]
    public void TestJumpRight()
    {
        if (playerController != null)
        {
            playerController.JumpRight();
            Debug.Log("🔧 TEST: Jump Right");
        }
    }
}