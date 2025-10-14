using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    public AudioSource audioSource;           // asignar en inspector (AudioSource del objeto)
    public AudioMixer mixer;                 // asignar GameAudioMixer
    public string musicVolumeParam = "MusicVolume"; // nombre expuesto en Mixer

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Reproducir una pista con cross-fade
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeToNewTrack(clip, fadeTime));
    }

    IEnumerator FadeToNewTrack(AudioClip clip, float fadeTime)
    {
        // Obtener volumen actual en linear (de dB)
        float startDb;
        mixer.GetFloat(musicVolumeParam, out startDb);
        float startLinear = Mathf.Pow(10f, startDb / 20f);

        // Fade out
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float linear = Mathf.Lerp(startLinear, 0f, t / fadeTime);
            mixer.SetFloat(musicVolumeParam, LinearToDb(linear));
            yield return null;
        }

        // Cambiar clip
        audioSource.clip = clip;
        audioSource.Play();

        // Fade in
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float linear = Mathf.Lerp(0f, 1f, t / fadeTime);
            mixer.SetFloat(musicVolumeParam, LinearToDb(linear));
            yield return null;
        }
        mixer.SetFloat(musicVolumeParam, 0f); // 0 dB = full
    }

    public void StopMusic(float fadeTime = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndStop(fadeTime));
    }

    IEnumerator FadeOutAndStop(float fadeTime)
    {
        float startDb;
        mixer.GetFloat(musicVolumeParam, out startDb);
        float startLinear = Mathf.Pow(10f, startDb / 20f);

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float linear = Mathf.Lerp(startLinear, 0f, t / fadeTime);
            mixer.SetFloat(musicVolumeParam, LinearToDb(linear));
            yield return null;
        }
        audioSource.Stop();
    }

    // Convierte linear (0..1) a dB para AudioMixer
    float LinearToDb(float linear)
    {
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        return 20f * Mathf.Log10(linear);
    }

    // Llamar desde UI slider (valor 0..1)
    public void SetMusicVolume(float linear)
    {
        mixer.SetFloat(musicVolumeParam, LinearToDb(linear));
    }
}

