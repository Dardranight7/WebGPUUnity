using UnityEngine;
using System.Collections.Generic;

public class PlatformMovementDebugger : MonoBehaviour
{
    [Header("Debugging Settings")]
    public bool logMovement = true;
    public bool preventPlatformMovement = true;
    public bool disablePlatformEffects = true;
    
    private Dictionary<Transform, Vector3> lastPlatformPositions = new Dictionary<Transform, Vector3>();
    
    void Start()
    {
        Debug.Log("🔍 Platform Movement Debugger Started");
        
        if (disablePlatformEffects)
        {
            DisableAllPlatformEffects();
        }
    }
    
    void Update()
    {
        if (logMovement)
        {
            TrackPlatformMovement();
        }
        
        if (preventPlatformMovement)
        {
            PreventUnwantedMovement();
        }
    }
    
    void TrackPlatformMovement()
    {
        // Find all platforms
        Platform[] platforms = FindObjectsOfType<Platform>();
        
        foreach (Platform platform in platforms)
        {
            Transform platformTransform = platform.transform;
            Vector3 currentPosition = platformTransform.position;
            
            if (lastPlatformPositions.ContainsKey(platformTransform))
            {
                Vector3 lastPosition = lastPlatformPositions[platformTransform];
                Vector3 movement = currentPosition - lastPosition;
                
                // If platform moved significantly upward
                if (movement.y > 0.01f)
                {
                    Debug.LogWarning($"🚨 Platform {platform.name} moved UP: {movement.y:F3} units!");
                    Debug.LogWarning($"   From: {lastPosition} To: {currentPosition}");
                    
                    // Stop the unwanted movement
                    if (preventPlatformMovement)
                    {
                        platformTransform.position = lastPosition;
                        Debug.Log($"✋ Prevented upward movement of {platform.name}");
                    }
                }
                else if (movement.magnitude > 0.01f)
                {
                    Debug.Log($"📍 Platform {platform.name} moved: {movement}");
                }
            }
            
            lastPlatformPositions[platformTransform] = currentPosition;
        }
    }
    
    void PreventUnwantedMovement()
    {
        // Freeze all platform positions except for intended downward breaking effects
        Platform[] platforms = FindObjectsOfType<Platform>();
        
        foreach (Platform platform in platforms)
        {
            // Only allow movement if it's a breaking platform moving down
            if (platform.hasBeenUsed && platform.transform.position.y > lastPlatformPositions.GetValueOrDefault(platform.transform, platform.transform.position).y)
            {
                // Platform is breaking but moving up - this is wrong!
                if (lastPlatformPositions.ContainsKey(platform.transform))
                {
                    platform.transform.position = lastPlatformPositions[platform.transform];
                }
            }
        }
    }
    
    void DisableAllPlatformEffects()
    {
        Debug.Log("🚫 Disabling all platform visual effects...");
        
        Platform[] platforms = FindObjectsOfType<Platform>();
        int disabledCount = 0;
        
        foreach (Platform platform in platforms)
        {
            platform.enableVisualEffects = false;
            disabledCount++;
        }
        
        Debug.Log($"✅ Disabled visual effects on {disabledCount} platforms");
    }
    
    [ContextMenu("Stop All Platform Movement")]
    public void StopAllPlatformMovement()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        
        foreach (Platform platform in platforms)
        {
            // Stop any coroutines that might be moving platforms
            platform.StopAllCoroutines();
            
            // Disable visual effects
            platform.enableVisualEffects = false;
            
            Debug.Log($"🛑 Stopped all movement for {platform.name}");
        }
        
        Debug.Log("✅ All platform movement stopped!");
    }
    
    [ContextMenu("Reset All Platforms")]
    public void ResetAllPlatforms()
    {
        Platform[] platforms = FindObjectsOfType<Platform>();
        
        foreach (Platform platform in platforms)
        {
            platform.ResetPlatform();
        }
        
        Debug.Log("🔄 All platforms reset!");
    }
}