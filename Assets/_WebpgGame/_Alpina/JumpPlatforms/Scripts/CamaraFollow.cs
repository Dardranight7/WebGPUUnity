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
        
        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
            gm.gameStared = false;

        if (showIntro)
        {
            isIntroPlaying = true;
            StartCoroutine(IntroCameraPan());
        }
        else
        {
            ResetCamera();
            
            if (gm != null)
            {
                gm.gameStared = true;
                gm.StartBots();
            }
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
        cameraHeight_internal = introHeight;
        cameraDistance_internal = introDistance;
        offset = new Vector3(0, cameraHeight_internal, cameraDistance_internal);
        
        float duration = Mathf.Max(0.1f, introDuration);
        float elapsed = 0f;
        float radius =  Mathf.Abs(introDistance) + introRadiusExtra;
        Vector3 center = (target != null) ? (target.position + Vector3.up * (cameraHeight_internal + 2f)) : new Vector3(0, 15, 0);

        if (target != null)
        {
            Vector3 startPos = center + new Vector3(radius, 0f, 0f);
            startPos.y = center.y;
            transform.position = startPos;
        }
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float angle = Mathf.Lerp(0, 180, t);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            pos.y = center.y + Mathf.Sin(rad * 0.5f) * 2f;
            transform.position = pos;
            
            if (target != null)
            {
                Vector3 lookPoint = target.position + Vector3.up * (cameraHeight_internal * 0.5f);
                Vector3 dir = lookPoint - transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * introRotationLerp);
                }

            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.rect = new Rect(0f, 0.5f, 0.5f, 0.5f);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = true;
        }

        cameraHeight_internal = gameplayHeight;
        cameraDistance_internal = gameplayDistance;
        offset = new Vector3(0, cameraHeight_internal, cameraDistance_internal);

        if (target != null)
        {
            Vector3 endPos = target.position + offset;
            endPos.y = Mathf.Max(endPos.y, minY);
            transform.position = endPos;
            
            Vector3 lookPoint = target.position + Vector3.up * (cameraHeight_internal * 0.5f);
            Vector3 dir = lookPoint - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        
        highestY = transform.position.y;
        isIntroPlaying = false;
         //conteo regrsivo
         yield return StartCoroutine(DoCountDown());
        
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.gameStared = true;
            gm.StartBots();
        }
    }

    IEnumerator DoCountDown()
    {
        if (countdownTime != null)
            countdownText.gameObject.SetActive(true);
        
        float remaining = countdownTime;
        while (remaining > 0)
        {
            if (countdownTime != null)
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