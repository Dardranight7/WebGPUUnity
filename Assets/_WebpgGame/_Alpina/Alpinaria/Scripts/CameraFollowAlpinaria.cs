using UnityEngine;
using System.Collections;

/// <summary>
/// CameraFollow: mantiene la cámara siempre por detrás del target y mira hacia él.
/// - No usa target.rotation * offset (evita giros raros).
/// - Proyecta forward sobre el plano XZ para calcular "detrás" correctamente en pendientes.
/// - Snap inicial opcional para evitar aparecer a un lado al comenzar.
/// </summary>
public class CameraFollowAlpinaria : MonoBehaviour
{
    [Tooltip("Transform del jugador principal")]
    public Transform target;

    [Header("Posición")]
    [Tooltip("Distancia en el eje horizontal (cuánto 'detrás' del jugador)")]
    public float distance = 7f;
    [Tooltip("Altura relativa sobre la posición del jugador")]
    public float height = 4.5f;

    [Header("Ajustes de mirada")]
    [Tooltip("Altura objetivo donde mira la cámara (puede ser torso/head)")]
    public float lookHeight = 1.6f;

    [Header("Suavizado")]
    [Tooltip("Velocidad de suavizado (mayor = más rígida)")]
    public float smoothSpeed = 6f;

    [Tooltip("Si true, la cámara se posiciona instantáneamente al inicio en la posición correcta")]
    public bool snapOnStart = true;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: target no asignado.");
            return;
        }

        // Snap inicial para evitar aparecer a un lado
        if (snapOnStart)
        {
            Vector3 desired = CalculateDesiredPosition();
            transform.position = desired;
            transform.rotation = Quaternion.LookRotation((target.position + Vector3.up * lookHeight) - transform.position, Vector3.up);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = CalculateDesiredPosition();

        // suavizado de posición
        transform.position = Vector3.Lerp(transform.position, desiredPos, Mathf.Clamp01(Time.deltaTime * smoothSpeed));

        // mirar al jugador, forzando up = Vector3.up para evitar tilt/roll
        Quaternion desiredRot = Quaternion.LookRotation((target.position + Vector3.up * lookHeight) - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Mathf.Clamp01(Time.deltaTime * smoothSpeed));
    }

    // calcula la posición deseada detrás del jugador sin usar la rotación completa (evita giros raros)
    Vector3 CalculateDesiredPosition()
    {
        // obtener forward proyectado en XZ (evita componente Y que causa inclinaciones)
        Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = new Vector3(target.forward.x, 0f, target.forward.z).normalized;

        Vector3 desired = target.position - flatForward * distance + Vector3.up * height;
        return desired;
    }
}