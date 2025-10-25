using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    public AudioSource musicSource;    // fuente para música (loop)
    public AudioSource sfxSource;      // fuente para efectos (PlayOneShot)

    [Header("Mixer Groups (optional)")]
    public AudioMixerGroup musicMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    [Header("Mixer (optional)")]
    public AudioMixer mixer;           // arrastra tu AudioMixer
    public string musicParam = "MusicVol";
    public string sfxParam = "SFXVol";

    [Header("Music")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true; // si quieres que empiece sola
    public float musicVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Si no asignaste AudioSources en el inspector, los creamos aquí
            if (musicSource == null)
            {
                GameObject m = new GameObject("MusicSource");
                m.transform.parent = transform;
                musicSource = m.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.spatialBlend = 0f; // 2D
            }
            if (sfxSource == null)
            {
                GameObject s = new GameObject("SFXSource");
                s.transform.parent = transform;
                sfxSource = s.AddComponent<AudioSource>();
                sfxSource.spatialBlend = 0f;
            }

            if (musicMixerGroup != null) musicSource.outputAudioMixerGroup = musicMixerGroup;
            if (sfxMixerGroup != null) sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            // En WebGL el autoplay puede estar bloqueado, así que esperamos interacción
            StopAllCoroutines();
            StartCoroutine(PlayMusicWhenAllowed());
        }
    }

    private IEnumerator PlayMusicWhenAllowed()
    {
        
        // Espera interacción del usuario (click o touch) en WebGL
        while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0) && Input.touchCount == 0)
        {
            yield return null;
        }
        
        PlayMusic(backgroundMusic, musicVolume);
    }

    public void PlayMusic(AudioClip musicClip, float volume = 1f)
    {
        if (musicClip == null) return;
        musicSource.Stop();
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // Métodos para control desde UI (conversión a dB)
    public void SetMusicVolume(float normalized)
    {
        if (mixer == null) return;
        float db = (normalized <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(normalized)) * 20f;
        mixer.SetFloat(musicParam, db);
    }

    public void SetSFXVolume(float normalized)
    {
        if (mixer == null) return;
        float db = (normalized <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(normalized)) * 20f;
        mixer.SetFloat(sfxParam, db);
    }
}