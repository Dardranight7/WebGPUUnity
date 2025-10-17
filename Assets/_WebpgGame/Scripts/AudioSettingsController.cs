using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer mixer;                // arrastra tu AudioMixer aquí
    public string musicParam = "MusicVol";  // nombre expuesto en el AudioMixer
    public string sfxParam = "SFXVol";      // nombre expuesto en el AudioMixer

    [Header("UI")]
    public Slider musicSlider;              // arrastra slider de Música
    public Slider sfxSlider;                // arrastra slider de Efectos

    [Header("PlayerPrefs Keys")]
    public string musicPrefKey = "MusicVolume";
    public string sfxPrefKey = "SFXVolume";

    public TextMeshProUGUI musicText, sfxText;

    private void Awake()
    {
        // Suscribir listeners (opcional: puedes hacerlo desde el Inspector)
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolumeNormalized);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolumeNormalized);
    }

    private void Start()
    {
        // Cargar valores guardados o usar 1f por defecto
        float musicVal = PlayerPrefs.GetFloat(musicPrefKey, 1f);
        float sfxVal = PlayerPrefs.GetFloat(sfxPrefKey, 1f);

        // Establecer sliders sin disparar dos veces los listeners (setValue sin eventos)
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musicVal);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfxVal);

        // Aplicar los valores al mixer
        ApplyMusicVolume(musicVal);
        ApplySFXVolume(sfxVal);
    }

    private void OnDestroy()
    {
        // Quitar listeners por seguridad
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolumeNormalized);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolumeNormalized);
    }

    // Called by slider (value from 0..1)
    public void SetMusicVolumeNormalized(float normalized)
    {
        ApplyMusicVolume(normalized);
        PlayerPrefs.SetFloat(musicPrefKey, normalized);
        PlayerPrefs.Save();
    }

    public void SetSFXVolumeNormalized(float normalized)
    {
        ApplySFXVolume(normalized);
        PlayerPrefs.SetFloat(sfxPrefKey, normalized);
        PlayerPrefs.Save();
    }

    // Convierte 0..1 a dB y setea en el mixer
    private void ApplyMusicVolume(float normalized)
    {
        float db = LinearToDb(normalized);
        mixer .SetFloat(musicParam, db);
        musicText.text = ((int)(100 * normalized)).ToString() + " %";
    }

    private void ApplySFXVolume(float normalized)
    {
        float db = LinearToDb(normalized);
        mixer.SetFloat(sfxParam, db);
        sfxText.text = ((int)(100 * normalized)).ToString() + " %";
    }

    // Conversión estándar: 1 -> 0dB, 0 -> -80dB (silencio)
    private float LinearToDb(float linear)
    {
        linear = Mathf.Clamp01(linear);
        if (linear <= 0.0001f) return -80f; // valor muy bajo -> mute práctico
        return Mathf.Log10(linear) * 20f;
    }

    // (Opcional) helper para convertir dB -> linear (si necesitas leer del mixer)
    private float DbToLinear(float db)
    {
        if (db <= -79f) return 0f;
        return Mathf.Pow(10f, db / 20f);
    }
}