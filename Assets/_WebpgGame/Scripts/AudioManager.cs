using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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
    [Range(0f,1f)] public float musicVolume = 1f;

    [Header("Fade settings")]
    public float defaultFadeTime = 0.5f;

    private Coroutine musicFadeCoroutine;

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
            if (mixer != null)
            {
                // establecer volúmenes iniciales si los parámetros existen
                SetMusicVolume(musicVolume);
            }
        }
        else
        {
            // Si ya existe un AudioManager, destruir este duplicado
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Opcional: si quieres que AudioManager reaccione automáticamente al cargar escenas,
        // descomenta la linea siguiente y usa SceneMusic en cada escena para pedir reproducción.
        // SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            StartCoroutine(PlayMusicWhenAllowed(backgroundMusic, musicVolume));
        }
    }

    private IEnumerator PlayMusicWhenAllowed(AudioClip clip, float vol)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Espera interacción del usuario (click o touch) en WebGL
            while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0) && Input.touchCount == 0)
            {
                yield return null;
            }
        }
        PlayMusic(clip, defaultFadeTime, true, vol);
    }

    // Reproduce una música gestionada por AudioManager. No se solapan:
    // - Si la pista es la misma y ya está sonando, la deja.
    // - Si es otra pista, hace fade out/in.
    public void PlayMusic(AudioClip musicClip, float fadeTime = -1f, bool loop = true, float targetVolume = 1f, bool forceRestart = false)
    {
        if (musicClip == null) return;
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        targetVolume = Mathf.Clamp01(targetVolume);

        // Si la misma pista ya suena y no forzamos reinicio, no hacemos nada
        if (!forceRestart && musicSource.clip == musicClip && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            musicSource.volume = targetVolume;
            return;
        }

        // Si hay un fade en curso, cancelarlo
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeToNewMusic(musicClip, fadeTime, loop, targetVolume));
    }

    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeTime));
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip, float fadeTime, bool loop, float targetVolume)
    {
        // Fade out current
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / Mathf.Max(0.0001f, fadeTime));
            yield return null;
        }

        // Switch clip
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / Mathf.Max(0.0001f, fadeTime));
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float fadeTime)
    {
        float startVol = musicSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / Mathf.Max(0.0001f, fadeTime));
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip = null;
        musicFadeCoroutine = null;
    }

    // SFX (sin cambios importantes)
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    // SFX posicional (mejor usar AudioSource temporal para mantener MixerGroup)
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
        a.spatialBlend = spatialBlend;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1f;
        a.maxDistance = 30f;
        if (sfxMixerGroup != null) a.outputAudioMixerGroup = sfxMixerGroup;
        a.Play();
        Destroy(go, clip.length + 0.1f);
        yield return null;
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

    // Opcional: si quieres que al cargar una escena el AudioManager cambie la música automáticamente,
    // usa SceneMusic en la escena y descomenta el registro de sceneLoaded en OnEnable.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Este callback no hace nada por defecto. Usar SceneMusic en cada escena permite control explicito.
    }
}     