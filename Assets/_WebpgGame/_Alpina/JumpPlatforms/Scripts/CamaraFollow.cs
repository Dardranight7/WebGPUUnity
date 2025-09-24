using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public float followSpeed = 5f;
    public Vector3 offset = new Vector3(0, 4, -8);  // Cámara más cerca del jugador
    
    [Header("Camera Height Adjustment")]
    [Range(1f, 10f)]
    public float cameraHeight = 4f;  // Altura ajustable desde el inspector
    [Range(-15f, -3f)]
    public float cameraDistance = -8f;  // Distancia ajustable desde el inspector
    
    [Header("Camera Boundaries")]
    public float minY = 1f;  // Altura mínima más baja
    public bool lockX = true;
    public bool lockZ = true;
    public bool onlyFollowUp = true;  // Solo sigue hacia arriba
    
    [Header("Smooth Transition")]
    public bool smoothFollow = true;
    public float smoothTime = 0.15f;
    
    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 2f;
    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private float highestY;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                target = player.transform;
        }
        
        // Set initial position
        if (target != null)
        {
            Vector3 initialPos = target.position + offset;
            initialPos.y = Mathf.Max(initialPos.y, minY);
            transform.position = initialPos;
            highestY = initialPos.y;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Update offset based on inspector values
        offset = new Vector3(0, cameraHeight, cameraDistance);
        
        Vector3 targetPosition = target.position + offset;
        
        // Platform game style: only follow upward movement
        if (onlyFollowUp)
        {
            if (targetPosition.y > highestY)
            {
                highestY = targetPosition.y;
            }
            targetPosition.y = highestY;
        }
        
        // Lock axes if needed
        if (lockX) targetPosition.x = transform.position.x;
        if (lockZ) targetPosition.z = transform.position.z;
        
        // Minimum Y constraint
        targetPosition.y = Mathf.Max(targetPosition.y, minY);
        
        // Apply movement
        if (smoothFollow)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void ResetCamera()
    {
        if (target != null)
        {
            Vector3 resetPos = target.position + offset;
            transform.position = resetPos;
            velocity = Vector3.zero;
            highestY = resetPos.y;
        }
    }
    
    // Métodos para ajustar la cámara fácilmente
    [ContextMenu("Set Close Camera")]
    public void SetCloseCamera()
    {
        cameraHeight = 3f;
        cameraDistance = -6f;
        Debug.Log("📸 Cámara configurada: Vista cercana");
    }
    
    [ContextMenu("Set Medium Camera")]
    public void SetMediumCamera()
    {
        cameraHeight = 4f;
        cameraDistance = -8f;
        Debug.Log("📸 Cámara configurada: Vista media");
    }
    
    [ContextMenu("Set Far Camera")]
    public void SetFarCamera()
    {
        cameraHeight = 6f;
        cameraDistance = -12f;
        Debug.Log("📸 Cámara configurada: Vista lejana");
    }
}