using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick UI")]
    public RectTransform background;
    public RectTransform handle;
    public float handleLimit = 50f;
    
    [Header("Settings")]
    public bool dynamicPosition = true;
    public float deadZone = 0.2f;
    public bool showOnlyWhenUsed = true;
    
    [Header("Visual Feedback")]
    public Image backgroundImage;
    public Image handleImage;
    [Range(0.1f, 1f)] public float alphaWhenActive = 0.8f;
    [Range(0f, 0.5f)] public float alphaWhenInactive = 0.3f;
    
    private Vector2 inputVector = Vector2.zero;
    private Vector2 originalBackgroundPos;
    private bool isDragging = false;
    private Canvas parentCanvas;
    private Camera uiCamera;
    
    void Start()
    {
        // Setup references
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCamera = parentCanvas.worldCamera;
        
        originalBackgroundPos = background.anchoredPosition;
        
        // Setup inicial
        if (showOnlyWhenUsed && dynamicPosition)
        {
            SetVisibility(false);
        }
        else
        {
            SetAlpha(alphaWhenInactive);
        }
        
        Debug.Log(" Touch Joystick initialized!");
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (dynamicPosition)
        {
            // Mover joystick a posición de toque
            Vector2 touchPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                uiCamera,
                out touchPos
            );
            
            background.anchoredPosition = touchPos;
            
            if (showOnlyWhenUsed)
            {
                SetVisibility(true);
            }
        }
        
        SetAlpha(alphaWhenActive);
        isDragging = true;
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector2 direction;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out direction
        );
        
        // Normalizar dentro del límite
        direction = direction / handleLimit;
        
        // Aplicar dead zone
        if (direction.magnitude < deadZone)
        {
            direction = Vector2.zero;
        }
        else
        {
            // Clamp a círculo unitario
            direction = direction.magnitude > 1f ? direction.normalized : direction;
        }
        
        inputVector = direction;
        
        // Mover handle visual
        handle.anchoredPosition = inputVector * handleLimit;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        
        if (dynamicPosition)
        {
            if (showOnlyWhenUsed)
            {
                SetVisibility(false);
            }
            background.anchoredPosition = originalBackgroundPos;
        }
        
        SetAlpha(alphaWhenInactive);
    }
    
    void SetVisibility(bool visible)
    {
        background.gameObject.SetActive(visible);
    }
    
    void SetAlpha(float alpha)
    {
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = alpha;
            backgroundImage.color = bgColor;
        }
        
        if (handleImage != null)
        {
            Color handleColor = handleImage.color;
            handleColor.a = alpha;
            handleImage.color = handleColor;
        }
    }
    
    // Métodos públicos para Player Controller
    public float GetHorizontalInput()
    {
        return inputVector.x;
    }
    
    public Vector2 GetInputVector()
    {
        return inputVector;
    }
    
    public bool IsActive()
    {
        return isDragging;
    }
    
    public bool HasInput()
    {
        return inputVector.magnitude > deadZone;
    }
}