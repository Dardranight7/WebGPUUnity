using System.Collections;
using UnityEngine;

public class CameraFollowMochiPush : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;                   // Asigna el jugador principal
    public bool followEnabled = true;          // Se desactiva temporalmente durante el paneo
    public Vector3 offset = new Vector3(0f, 6f, -8f);
    public float followLerp = 6f;
    public float lookLerp = 10f;
    public bool alwaysLookAtTarget = true;

    void LateUpdate()
    {
        if (!followEnabled || !target) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);

        if (alwaysLookAtTarget)
        {
            Vector3 lookDir = (target.position - transform.position);
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, lookLerp * Time.deltaTime);
            }
        }
    }

    
    public Coroutine PanToTargetCoroutine(Transform newTarget, float distance, float height, float arcDegrees, float duration, bool restoreFollow = true)
    {
        return StartCoroutine(CoPan(newTarget, distance, height, arcDegrees, duration, restoreFollow));
    }

    IEnumerator CoPan(Transform newTarget, float distance, float height, float arcDegrees, float duration, bool restoreFollow)
    {
        if (!newTarget) yield break;

        bool prevFollow = followEnabled;
        followEnabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Punto de pivote a la altura deseada sobre el objetivo
        Vector3 pivot = newTarget.position + Vector3.up * height;

        // Vector inicial desde el pivote a la cámara en plano XZ, normalizado a "distance"
        Vector3 flat = new Vector3(startPos.x - pivot.x, 0f, startPos.z - pivot.z);
        if (flat.sqrMagnitude < 0.0001f)
            flat = -newTarget.forward * distance;
        flat = flat.normalized * Mathf.Max(0.1f, distance);

        // Vector final: rota el offset en arco sobre Y
        Quaternion arcRot = Quaternion.AngleAxis(Mathf.Clamp(arcDegrees, -120f, 120f), Vector3.up);
        Vector3 toFlat = arcRot * flat;

        Vector3 endPos = pivot + toFlat;

        float t = 0f;
        duration = Mathf.Max(0.05f, duration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // para que funcione incluso si pausas el juego
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            Vector3 pos = Vector3.Lerp(startPos, endPos, k);
            transform.position = pos;

            Vector3 lookDir = (newTarget.position - transform.position);
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(startRot, look, k);
            }

            yield return null;
        }

        // Opcionalmente actualiza el objetivo y offset, útil si quieres que al volver al follow se quede en la nueva composición
        target = newTarget;
        offset = transform.position - target.position;

        if (restoreFollow)
            followEnabled = prevFollow;
    }
}