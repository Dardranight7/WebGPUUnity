using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SFXBonCollet : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip pickupPositiveClip;
    public AudioClip pickupNegativeClip;
    public AudioClip genericClip;

    [Header("Audio Settings")]
    public AudioMixerGroup sfxMixerGroup; // opcional: asignar AudioMixer
    [Tooltip("Volumen global (0..1)")]
    [Range(0f, 1f)] public float volume = 1f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // configuración por defecto
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D (ajusta según tu necesidad)
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
        audioSource.volume = volume;
    }

    // Reproduce un clip no posicional (en el audioSource del gestor)
    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volumeScale);
    }

    // Reproduce pickup según tipo positivo/negativo
    public void PlayPickup(bool positive)
    {
        if (positive)
            PlayOneShot(pickupPositiveClip);
        else
            PlayOneShot(pickupNegativeClip);
    }

    // Reproduce un clip posicional en world (crea temporalmente un AudioSource en esa posición)
    public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // Ajustar volumen en tiempo de ejecución
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (audioSource != null) audioSource.volume = volume;
    }
}