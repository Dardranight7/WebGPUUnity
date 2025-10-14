using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer mixer;             // Asignar GameAudioMixer
    public string sfxVolumeParam = "SFXVolume";

    [Header("Pool")]
    public int poolSize = 12;

    [Header("Optional default clips (opcional)")]
    public AudioClip jumpClip;           // Puedes asignar aquí el jumpClip por conveniencia
    public AudioClip cloudBreakClip;
    public AudioClip cloudInvisibleClip;

    private List<AudioSource> pool = new List<AudioSource>();

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        // Crear pool de AudioSources como hijos
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("SFXSrc_" + i);
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // por defecto 3D (puedes cambiar en cada Play)
            // asignar grupo SFX si existe
            var groups = mixer != null ? mixer.FindMatchingGroups("SFX") : null;
            if (groups != null && groups.Length > 0)
                src.outputAudioMixerGroup = groups[0];
            pool.Add(src);
        }
    }

    AudioSource GetFreeSource()
    {
        foreach (var s in pool)
            if (!s.isPlaying) return s;

        // Si todos están ocupados, crear uno extra
        GameObject go = new GameObject("SFXSrc_extra");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        var groups = mixer != null ? mixer.FindMatchingGroups("SFX") : null;
        if (groups != null && groups.Length > 0)
            src.outputAudioMixerGroup = groups[0];
        pool.Add(src);
        return src;
    }

    // Reproducir SFX 2D (UI, efectos del jugador si quieres)
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        var src = GetFreeSource();
        src.spatialBlend = 0f;
        src.volume = volume;
        src.clip = clip;
        src.transform.position = Vector3.zero;
        src.Play();
    }

    // Reproducir SFX posicional (3D) en una posición del mundo
    public void PlaySFXAtPosition(AudioClip clip, Vector3 pos, float volume = 1f, float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 20f)
    {
        if (clip == null) return;
        var src = GetFreeSource();
        src.transform.position = pos;
        src.volume = volume;
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.clip = clip;
        src.Play();
    }

    // Métodos conveniencia si asignaste clips por inspector
    public void PlayJump(Vector3 pos, float volume = 1f)
    {
        PlaySFXAtPosition(jumpClip, pos, volume, 0.2f, 1f, 12f);
    }

    public void PlayCloudBreak(Vector3 pos, float volume = 3f)
    {
        PlaySFXAtPosition(cloudBreakClip, pos, volume, 1f, 1f, 25f);
    }

    public void PlayCloudInvisible(Vector3 pos, float volume = 3f)
    {
        PlaySFXAtPosition(cloudInvisibleClip, pos, volume, 0.8f, 1f, 25f);
    }

    public void SetSFXVolume(float linear)
    {
        if (mixer == null) return;
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        mixer.SetFloat(sfxVolumeParam, 20f * Mathf.Log10(linear));
    }
}
