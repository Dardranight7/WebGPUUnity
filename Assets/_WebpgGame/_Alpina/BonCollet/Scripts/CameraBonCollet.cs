using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBonCollet : MonoBehaviour
{
    [Header("Paneo inicial")]
    public Transform[] panWaypoints; // puntos por los que pasa la cámara en el paneo inicial
    public float panDuration = 5f;

    [Header("Enfoque ganador")]
    public Vector3 winnerOffset = new Vector3(0, 2f, -3f); // offset relativo al ganador
    public float focusDuration = 1.5f;
    public float focusDistance = 2f;

    Camera cam;
    Vector3 startPos;
    Quaternion startRot;

    void Awake()
    {
        cam = Camera.main;
        startPos = cam.transform.position;
        startRot = cam.transform.rotation;
    }

    public void PlayPan()
    {
        StopAllCoroutines();
        StartCoroutine(PanRoutine());
    }

    IEnumerator PanRoutine()
    {
        if (panWaypoints == null || panWaypoints.Length == 0)
            yield break;

        float t = 0f;
        float step = panDuration / Mathf.Max(1, panWaypoints.Length - 1);
        for (int i = 0; i < panWaypoints.Length - 1; i++)
        {
            Transform a = panWaypoints[i];
            Transform b = panWaypoints[i + 1];
            float elapsed = 0f;
            while (elapsed < step)
            {
                float frac = elapsed / step;
                cam.transform.position = Vector3.Lerp(a.position, b.position, frac);
                cam.transform.rotation = Quaternion.Slerp(a.rotation, b.rotation, frac);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        // Al final, mantener posición final (o volver a startPos si prefieres)
    }

    public void FocusOnWinner(Transform winner)
    {
        StopAllCoroutines();
        StartCoroutine(FocusRoutine(winner));
    }

    IEnumerator FocusRoutine(Transform winner)
    {
        if (winner == null) yield break;
        Camera main = Camera.main;
        Vector3 initialPos = main.transform.position;
        Quaternion initialRot = main.transform.rotation;

        Vector3 targetPos = winner.position + winnerOffset;
        Quaternion targetRot = Quaternion.LookRotation(winner.position - targetPos);

        float elapsed = 0f;
        while (elapsed < focusDuration)
        {
            float t = elapsed / focusDuration;
            main.transform.position = Vector3.Lerp(initialPos, targetPos, t);
            main.transform.rotation = Quaternion.Slerp(initialRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // mantener posición final
    }
}