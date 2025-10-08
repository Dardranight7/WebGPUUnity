using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f; // tiempo entre actualizaciones

    private int frames = 0;
    private float timePassed = 0f;
    private float fps = 0f;

    void Update()
    {
        frames++;
        timePassed += Time.unscaledDeltaTime;

        if (timePassed >= updateInterval)
        {
            fps = frames / timePassed;
            fpsText.text = $"{fps:0} fps";

            // Reinicia contadores
            frames = 0;
            timePassed = 0f;
        }
    }
}
