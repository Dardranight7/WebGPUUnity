using UnityEngine;

public class HappyHopFixer : MonoBehaviour
{
    [Header("🔧 ARREGLAR CONTROLES HAPPY HOP")]
    [Space(10)]
    
    [Header("Referencias (auto-detectadas)")]
    public PlayerController player;
    public InputManager inputManager;
    
    [ContextMenu("🚀 ARREGLAR CONTROLES AHORA")]
    public void FixControls()
    {
        Debug.Log("🔧 ARREGLANDO CONTROLES PARA HAPPY HOP...");
        
        FindComponents();
        FixPlayerController();
        DisableInputManager();
        TestControls();
        
        Debug.Log("✅ CONTROLES ARREGLADOS!");
        Debug.Log("🎮 CONTROLES FINALES:");
        Debug.Log("   👈 Flecha Izquierda (o A) = Salto izquierda");
        Debug.Log("   👉 Flecha Derecha (o D) = Salto derecha");
        Debug.Log("   🕹️ Joysticks virtuales = Automáticos");
    }
    
    void FindComponents()
    {
        if (player == null)
        {
            // Buscar Devilsaur Queen específicamente
            GameObject devilsaur = GameObject.Find("Devilsaur Queen with anim");
            if (devilsaur != null)
            {
                player = devilsaur.GetComponent<PlayerController>();
            }
            
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
            
            Debug.Log(player != null ? $"✅ Jugador encontrado: {player.name}" : "❌ Jugador NO encontrado");
        }
        
        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<InputManager>();
            Debug.Log(inputManager != null ? "✅ InputManager encontrado" : "❌ InputManager NO encontrado");
        }
    }
    
    void FixPlayerController()
    {
        if (player == null) return;
        
        Debug.Log("🎮 Configurando PlayerController para Happy Hop...");
        
        // Configuración perfecta para Happy Hop
        player.jumpForce = 18f;
        player.horizontalForce = 10f;
        player.autoJump = false; // IMPORTANTE: Solo saltos manuales
        player.enableKeyboardInput = true;
        player.enableTouchInput = false; // Solo joysticks virtuales
        player.enableJoystickInput = true;
        
        // Configurar Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearDamping = 0.3f;
            rb.angularDamping = 10f;
            rb.freezeRotation = true;
            rb.mass = 1f;
        }
        
        Debug.Log("✅ PlayerController configurado para Happy Hop");
    }
    
    void DisableInputManager()
    {
        if (inputManager == null) return;
        
        Debug.Log("🚫 Desactivando InputManager para evitar interferencias...");
        
        // Desactivar procesamiento de teclado en InputManager
        inputManager.enableKeyboard = false;
        inputManager.enableTouch = false;
        inputManager.enableJoysticks = true; // Solo joysticks virtuales
        
        Debug.Log("✅ InputManager configurado solo para joysticks");
    }
    
    void TestControls()
    {
        Debug.Log("🧪 PROBANDO CONFIGURACIÓN...");
        
        if (player != null)
        {
            Debug.Log($"✅ Jugador: {player.name}");
            Debug.Log($"   🚀 Jump Force: {player.jumpForce}");
            Debug.Log($"   🏃 Horizontal Force: {player.horizontalForce}");
            Debug.Log($"   🤖 Auto Jump: {player.autoJump} (debe ser FALSE)");
            Debug.Log($"   ⌨️ Keyboard Input: {player.enableKeyboardInput} (debe ser TRUE)");
            Debug.Log($"   📱 Touch Input: {player.enableTouchInput} (debe ser FALSE)");
        }
        
        // Probar joysticks virtuales
        VirtualJoystick[] joysticks = FindObjectsByType<VirtualJoystick>(FindObjectsSortMode.None);
        Debug.Log($"🕹️ Joysticks encontrados: {joysticks.Length}");
        
        foreach (var joystick in joysticks)
        {
            joystick.playerController = player;
            joystick.inputManager = inputManager;
            Debug.Log($"   ✅ {joystick.name} configurado");
        }
        
        Debug.Log("🎮 CONTROLES LISTOS!");
    }
    
    [ContextMenu("🧪 Solo Probar Controles")]
    public void TestControlsOnly()
    {
        FindComponents();
        TestControls();
    }
    
    [ContextMenu("🎯 Solo Arreglar Player")]
    public void FixPlayerOnly()
    {
        FindComponents();
        FixPlayerController();
    }
    
    void Start()
    {
        // Auto-arreglar en 1 segundo
        Invoke(nameof(FixControls), 1f);
    }
}