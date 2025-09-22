using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Button Settings")]
    public bool isPressed = false;
    public bool oneTimePress = true; // Para salto (no continuo)
    
    [Header("Visual Feedback")]
    public Image buttonImage;
    public Color normalColor = Color.white;
    public Color pressedColor = Color.gray;
    [Range(0.1f, 1f)] public float normalAlpha = 0.6f;
    [Range(0.5f, 1f)] public float pressedAlpha = 1f;
    
    private bool wasPressed = false;
    
    void Start()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
            
        SetVisualState(false);
        Debug.Log("🔘 Touch Button initialized!");
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        wasPressed = true;
        SetVisualState(true);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        SetVisualState(false);
    }
    
    void SetVisualState(bool pressed)
    {
        if (buttonImage == null) return;
        
        Color targetColor = pressed ? pressedColor : normalColor;
        float targetAlpha = pressed ? pressedAlpha : normalAlpha;
        targetColor.a = targetAlpha;
        
        buttonImage.color = targetColor;
    }
    
    // Métodos públicos para PlayerController
    public bool IsPressed()
    {
        if (oneTimePress)
        {
            // Para salto - solo una vez por press
            if (wasPressed && !isPressed)
            {
                wasPressed = false;
                return true;
            }
            return false;
        }
        else
        {
            // Para movimiento continuo
            return isPressed;
        }
    }
    
    public bool IsCurrentlyPressed()
    {
        return isPressed;
    }
    
    public void ResetPress()
    {
        isPressed = false;
        wasPressed = false;
        SetVisualState(false);
    }
}