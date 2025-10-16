using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManagerYogoNado : MonoBehaviour
{
    public static AudioManagerYogoNado Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer mainMixer;
    public string musicVolumeParameter = "MusicVolume"; // debe coincidir con el exposed param del mixer
    public string sfxVolumeParameter = "SFXVolume";

    [Header("Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Sources")]
    public AudioSource musicSource; // para música global (2D)
    public AudioSource sfxSource;   // para SFX no posicional (PlayOneShot)

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultMusicVolume = 1f;
    [Range(0f, 1f)] public float defaultSFXVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Establecer grupos en los AudioSources si no fueron asignados en inspector
        if (musicSource != null && musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
        if (sfxSource != null && sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;

        SetMusicVolume(defaultMusicVolume);
        SetSFXVolume(defaultSFXVolume);
    }

    // Música global
    public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
    {
        if (musicSource == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    // SFX 2D (no posicional)
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // SFX posicional (usa GameObject temporal para asignar MixerGroup y spatialBlend)
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
    {
        if (clip == null) return;

        StartCoroutine(PlaySFXAtPointCoroutine(clip, position, Mathf.Clamp01(volume), Mathf.Clamp01(spatialBlend)));
    }

    private IEnumerator PlaySFXAtPointCoroutine(AudioClip clip, Vector3 position, float volume, float spatialBlend)
    {
        GameObject go = new GameObject("SFX_" + clip.name);
        go.transform.position = position;
        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.spatialBlend = spatialBlend; // 1 = 3D, 0 = 2D
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1f;
        a.maxDistance = 30f;
        if (sfxGroup != null) a.outputAudioMixerGroup = sfxGroup;
        a.Play();
        Destroy(go, clip.length + 0.1f);
        yield return null;
    }

    // Control de volúmenes (0..1)
    public void SetMusicVolume(float linear)
    {
        if (mainMixer == null) return;
        float db = (linear <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        mainMixer.SetFloat(musicVolumeParameter, db);
    }

    public void SetSFXVolume(float linear)
    {
        if (mainMixer == null) return;
        float db = (linear <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;
        mainMixer.SetFloat(sfxVolumeParameter, db);
    }
}