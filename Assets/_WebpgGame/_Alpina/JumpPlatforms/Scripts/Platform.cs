using UnityEngine;
using System.Collections;

public enum PlatformType
{
    Static,
    Breakable,
    Fake,
    Spike,
    Normal,
    Moving,
    Bouncy,
    Ice,
    Collectible
}

public class Platform : MonoBehaviour
{
    [Header("Platform Configuration")]
    public PlatformType platformType = PlatformType.Static;
    
    [Header("Breakable Platform Settings")]
    public float breakDelay = 0.5f;
    public bool hasBeenUsed = false;
    
    [Header("Visual Effects")]
    public bool enableVisualEffects = true;
    public Color originalColor = Color.white;
    
    [Header("Generator Settings")]
    public bool autoDetectPlayerStart = true;
    public Transform playerTransform;
    
    private Renderer platformRenderer;
    private Collider platformCollider;
    private bool isBreaking = false;
    
    void Start()
    {
        InitializePlatform();
    }
    
    void InitializePlatform()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();
        
        if (platformRenderer != null)
        {
            originalColor = platformRenderer.material.color;
        }
        
        // Auto-detect player if enabled
        if (autoDetectPlayerStart && playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        ConfigurePlatformType();
        
        Debug.Log($"✅ Platform {name} initialized as {platformType}");
    }
    
    void ConfigurePlatformType()
    {
        switch (platformType)
        {
            case PlatformType.Static:
            case PlatformType.Normal:
                // Normal platform - no special configuration needed
                break;
                
            case PlatformType.Breakable:
                // Breakable platform setup
                break;
                
            case PlatformType.Fake:
                // Fake platform - should be trigger
                if (platformCollider != null)
                {
                    platformCollider.isTrigger = true;
                }
                
                // Make semi-transparent if possible
                if (platformRenderer != null && enableVisualEffects)
                {
                    Color fakeColor = originalColor;
                    fakeColor.a = 0.6f;
                    platformRenderer.material.color = fakeColor;
                }
                break;
                
            case PlatformType.Spike:
                // Spike platform - should cause damage
                if (platformCollider != null)
                {
                    platformCollider.isTrigger = true;
                }
                break;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;
        // Solo interactuar si el jugador cae desde arriba
        if (collision.contacts.Length > 0)
        {
            Vector3 contactNormal = collision.contacts[0].normal;
            if (Vector3.Dot(contactNormal, Vector3.up) > 0.5f)
            {
                HandlePlatformInteraction(player);
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        
        // Handle fake platforms and spikes
        if (platformType == PlatformType.Fake || platformType == PlatformType.Spike)
        {
            HandlePlatformInteraction(player);
        }
    }
    
    void HandlePlatformInteraction(PlayerController player)
    {
        switch (platformType)
        {
            case PlatformType.Static:
            case PlatformType.Normal:
                Debug.Log("🟢 Player landed on safe platform");
                break;

            case PlatformType.Breakable:
                if (!hasBeenUsed && !isBreaking)
                {
                    StartCoroutine(BreakPlatform());
                }
                // Si la plataforma ya está rota, el jugador cae y pierde SOLO si está encima
                // (no por tocarla desde abajo)
                break;

            case PlatformType.Fake:
                Debug.Log("👻 Has perdido: tocaste una plataforma invisible!");
                if (player != null)
                {
                    player.DieWithMessage("Has perdido: tocaste una plataforma invisible!");
                }
                break;

            case PlatformType.Spike:
                Debug.Log("🔴 Player hit spike platform!");
                if (player != null)
                {
                    player.Die();
                }
                break;
        }
    }
    
    IEnumerator BreakPlatform()
    {
        isBreaking = true;
        hasBeenUsed = true;
        
        Debug.Log("💥 Platform breaking!");
        
        // Visual effect - change color to indicate breaking
        if (platformRenderer != null && enableVisualEffects)
        {
            Color breakingColor = Color.red;
            platformRenderer.material.color = breakingColor;
        }
        
        // Wait for break delay
        yield return new WaitForSeconds(breakDelay);
        
        // Disable collider so player falls through
        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }
        
        // Visual effect - make platform disappear or fall
        if (enableVisualEffects)
        {
            StartCoroutine(DisappearEffect());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    IEnumerator DisappearEffect()
    {
        Vector3 originalPosition = transform.position;
        float fallDuration = 1f;
        float elapsed = 0f;
        
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fallDuration;
            
            // Make platform fall down (ensure it's going DOWN, not UP)
            Vector3 fallPosition = originalPosition - Vector3.up * progress * 10f;
            transform.position = fallPosition;
            
            if (platformRenderer != null)
            {
                Color fadeColor = originalColor;
                fadeColor.a = 1f - progress;
                platformRenderer.material.color = fadeColor;
            }
            
            yield return null;
        }
        
        // Deactivate platform
        gameObject.SetActive(false);
    }
    
    public void ResetPlatform()
    {
        // Reset platform to original state
        hasBeenUsed = false;
        isBreaking = false;
        
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }
        
        if (platformRenderer != null)
        {
            platformRenderer.material.color = originalColor;
        }
        
        Debug.Log($"🔄 Platform {name} reset");
    }
    
    void OnDisable()
    {
        // Reset platform when deactivated (returned to pool)
        ResetPlatform();
    }
}