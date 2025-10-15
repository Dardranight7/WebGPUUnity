using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("General Follow")]
    public float followSpeed = 5f;
    public Vector3 offset = new Vector3(0, 4, -6);
    public bool smoothFollow = true;
    public float smoothTime = 0.15f;

    [Header("Gameplay Camera (final)")]
    public float gameplayHeight = 5f;
    public float gameplayDistance = -12f;

    [Header("Intro Camera (paneo)")]
    public bool showIntro = true;
    public float introDuration = 3f;
    public float introHeight = 8f;
    public float introDistance = -60f;        // usa valor muy negativo para estar muy lejos
    public float introRadiusExtra = 30f;      // radio adicional para el orbit
    public float introRotationLerp = 4f;      // suavizado rotación durante intro

    [Header("Rotation")]
    public float rotationSmoothSpeed = 6f;

    [Header("Bounds")]
    public float minY = 1f;
    public bool lockX = true;
    public bool lockZ = true;
    public bool onlyFollowUp = false;

    private Vector3 velocity = Vector3.zero;
    private float highestY;
    private bool isIntroPlaying = false;
    
    [Header("Countdown")]
    public float countdownTime = 3f; 
    public Text countdownText;

    [Header("Countdown audio")]
    public AudioClip countDownClip;
    [Range(0f, 5f)]
    public float countdownVolume = 1f;
    
    [SerializeField]
    private Camera mainCamera;

    [SerializeField] private Camera[] cameras;
    
    void Start()
    {
        
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null) target = player.transform;
        }

        cameraSetToGamePlay();
        
        // Set initial position
        if (target != null)
        {
            Vector3 initialPos = target.position + offset;
            initialPos.y = Mathf.Max(initialPos.y, minY);
            transform.position = initialPos;
            highestY = initialPos.y;
        }
        
        

        if (showIntro)
        {
            isIntroPlaying = true;
            StartCoroutine(IntroCameraPan());
        }
        
        
    }
    
    void cameraSetToGamePlay()
    {
        cameraHeight_internal = gameplayHeight;
        cameraDistance_internal = gameplayDistance;
        offset = new Vector3(0, cameraHeight_internal, cameraDistance_internal);
        
    }
    
    private float cameraHeight_internal;
    private float cameraDistance_internal;
    
    
    IEnumerator IntroCameraPan()
    {
        isIntroPlaying = true;

        float duration = Mathf.Max(1f, introDuration);
        float elapsed = 0f;

        if (target == null)
        {
            Debug.LogWarning("IntroCameraPan: no hay target asignado.");
            yield break;
        }

        // --- configuración inicial ---
        Vector3 focusPoint = target.position + Vector3.up * 3f;
        float startRadius = Mathf.Abs(introDistance);
        float endRadius = Mathf.Abs(gameplayDistance);
        float startHeight = introHeight;
        float endHeight = gameplayHeight;

        // posición inicial lejos del área
        float angle = 0f;
        transform.position = focusPoint + Quaternion.Euler(0, angle, 0) * new Vector3(0, startHeight, -startRadius);
        transform.LookAt(focusPoint);

        // --- animación principal: acercamiento y orbitado suave ---
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            angle = Mathf.Lerp(0f, 220f, t); // gira un poco más que 180° para dar efecto dinámico
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float height = Mathf.Lerp(startHeight, endHeight, t);

            // orbit + bajada tipo dron
            Vector3 orbitPos = focusPoint + Quaternion.Euler(0, angle, 0) * new Vector3(0, 0, -radius);
            orbitPos.y += height;

            // opcional: pequeñas oscilaciones tipo dron
            float noise = Mathf.PerlinNoise(Time.time * 0.3f, 0f) * 0.4f - 0.2f;
            orbitPos.y += noise;

            // movimiento suave
            transform.position = Vector3.SmoothDamp(transform.position, orbitPos, ref velocity, 0.25f);

            // rotación suave hacia el jugador
            Vector3 dir = (focusPoint - transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * introRotationLerp);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- transición al gameplay ---
        Vector3 gameplayOffset = new Vector3(0, gameplayHeight, gameplayDistance);
        Vector3 finalPos = target.position + gameplayOffset;
        Quaternion finalRot = Quaternion.LookRotation(target.position - finalPos);
        
        float blendTime = 2f;
        elapsed = 0f;
        Vector3 velocitySmooth = Vector3.zero;
        

        // Ajuste final
        transform.position = finalPos;
        transform.rotation = finalRot;

        // --- cuenta regresiva y arranque ---
        yield return StartCoroutine(DoCountDown());

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            
            gm.StartGameProperly();
        }

        // Restaurar cámaras múltiples después del intro
        if (mainCamera != null)
            mainCamera.rect = new Rect(0f, 0.5f, 0.5f, 0.5f);

        if (cameras != null && cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                if (c != null) c.enabled = true;
            }
        }

        isIntroPlaying = false;
    }
    
    IEnumerator DoCountDown()
    {
        // ✅ Mostrar las 4 pantallas justo cuando comienza el conteo
        if (mainCamera != null)
            mainCamera.rect = new Rect(0f, 0.5f, 0.5f, 0.5f);

        if (cameras != null && cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                if (c != null) c.enabled = true;
            }
        }

        // 🔢 Mostrar el texto del conteo
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        if (countDownClip != null)
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(countDownClip,countdownVolume);
            else if (Camera.main != null)
                AudioSource.PlayClipAtPoint(countDownClip, Camera.main.transform.position, countdownVolume);
            else 
                AudioSource.PlayClipAtPoint(countDownClip, transform.position, countdownVolume);
        }

        float remaining = countdownTime;
        while (remaining > 0)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();

            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (countdownText != null)
        {
            countdownText.text = "¡GO!";
            yield return new WaitForSeconds(0.5f);
            countdownText.gameObject.SetActive(false);
        }
    }
    
    void LateUpdate()
    {
        if (isIntroPlaying) return;
        if (target == null) return;
        
        // Update offset based on inspector values
        offset = new Vector3(0, cameraHeight_internal, cameraDistance_internal);
        Vector3 targetPosition = target.position + offset;
        
        // Platform game style: only follow upward movement
        if (onlyFollowUp)
        {
            if (targetPosition.y > highestY) highestY = targetPosition.y;
            targetPosition.y = highestY;
        }
        else
        {
            highestY = targetPosition.y;
        }
        
        // Lock axes if needed
        if (lockX) targetPosition.x = transform.position.x;
        if (lockZ) targetPosition.z = transform.position.z;
        
        // Minimum Y constraint
        targetPosition.y = Mathf.Max(targetPosition.y, minY);
        
        // Apply movement
        if (smoothFollow)
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        else
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        Vector3 lookPoint = target.position + Vector3.up * (cameraHeight_internal * 0.5f);
        Vector3 dir = lookPoint - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot,  Time.deltaTime * rotationSmoothSpeed);
        }
    }
    
   
    
    public void SetTarget(Transform newTarget) => target = newTarget;
    
    
    public void ResetCamera()
    {
        if (target == null) return;
        offset = new Vector3(0, cameraHeight_internal, cameraDistance_internal);
        Vector3 resetPos = target.position + offset;
        transform.position = resetPos;
        velocity = Vector3.zero;
        highestY = resetPos.y;
    }
    
    // Métodos para ajustar la cámara fácilmente
    [ContextMenu("Set Close Camera")]
    public void SetCloseCamera()
    {
        gameplayHeight = 3f;
        gameplayDistance = -6f;
        cameraSetToGamePlay();
        Debug.Log(" Cámara configurada: Vista cercana");
    }
    
    [ContextMenu("Set Medium Camera")]
    public void SetMediumCamera()
    {
        gameplayHeight = 4f;
        gameplayDistance = -8f;
        cameraSetToGamePlay();
        Debug.Log("Cámara configurada: Vista media");
    }

    [ContextMenu("Set Far Camera")]
    public void SetFarCamera()
    {
        gameplayHeight = 6f;
        gameplayDistance = -12f;
        cameraSetToGamePlay();
        Debug.Log("Cámara configurada: Vista lejana");
    }
    
    public IEnumerator PlayCinematicPan(Transform focus, Vector3 offset, float duration)
    {
        isIntroPlaying = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = focus.position + offset;
        Quaternion targetRot = Quaternion.LookRotation(focus.position - targetPos);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        isIntroPlaying = false;
        cameraSetToGamePlay();
    }
}