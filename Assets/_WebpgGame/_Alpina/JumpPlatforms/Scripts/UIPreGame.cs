using UnityEngine.UI;
using UnityEngine;

public class UIPreGame: MonoBehaviour
{
    [Header("UI")]
    public GameObject menuRoot;                // root del menu (Canvas o Panel que engloba todo)
    public Button playButton;                  // arrastra el botón Play aquí

    [Header("References")]
    public CameraFollow cameraFollow;          // arrastra el objeto con CameraFollow
    public bool useCameraIntro = true;         // true -> llamamos BeginIntro; false -> llamamos StartGameProperly directamente

    [Header("Audio (optional)")]
    public AudioClip menuMusicClip;            // si quieres reproducir música de menú
    [Range(0f,1f)]
    public float menuMusicVolume = 0.8f;

    private void Awake()
    {
        // seguridad: si no asignaste menuRoot lo buscamos en el mismo GameObject
        if (menuRoot == null) menuRoot = this.gameObject;
    }

    private void Start()
    {
        // Asegurar que el boton tenga el listener
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayPressed);
        }

        // Reproducir música de menú si quieres (opcional)
        if (menuMusicClip != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(menuMusicClip, 0.5f);
            }
            else
            {
                // fallback simple
                var go = new GameObject("MenuMusic");
                var src = go.AddComponent<AudioSource>();
                src.clip = menuMusicClip;
                src.loop = true;
                src.spatialBlend = 0f;
                src.volume = menuMusicVolume;
                src.Play();
                DontDestroyOnLoad(go);
            }
        }

        // Mostrar menu al inicio (asegúrate menuRoot activo)
        if (menuRoot != null) menuRoot.SetActive(true);
    }

    public void OnPlayPressed()
    {
        // Desactivar UI
        if (menuRoot != null) menuRoot.SetActive(false);

        // si usas MusicManager y quieres que la musica de juego cambie, puedes detener la de menu aquí
        // if (MusicManager.Instance != null) MusicManager.Instance.StopMusic(0.3f);

        // Lanzar intro / iniciar juego
        if (useCameraIntro && cameraFollow != null)
        {
            cameraFollow.BeginIntro();
        }
        else
        {
            // Si no quieres intro, busca GameManager y llama StartGameProperly
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.StartGameProperly();
            else Debug.LogWarning("MenuUIManager: no se encontró GameManager para iniciar el juego.");
        }
    }
}
