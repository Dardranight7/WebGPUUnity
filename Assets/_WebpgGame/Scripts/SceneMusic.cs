using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [Tooltip("Clip que esta escena quiere reproducir")]
    public AudioClip sceneMusicClip;
    [Tooltip("Tiempo de fade al pedir reproducción (segundos)")]
    public float fadeTime = 0.5f;
    [Tooltip("Volumen objetivo 0..1")]
    [Range(0f,1f)] public float targetVolume = 1f;
    [Tooltip("Force restart si quieres reiniciar la misma pista")]
    public bool forceRestart = false;

    void Start()
    {
        if (sceneMusicClip == null) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(sceneMusicClip);
        }
    }
}