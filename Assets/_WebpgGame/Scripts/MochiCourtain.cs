using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MochiCourtain : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] public Canvas canvas;
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI percentValue;
    public static MochiCourtain Singleton;

    [SerializeField] string coreSceneName = "Core"; // Escena base que nunca se descarga
    string lastLoadedScene = null;
    float disableTime = 1f;

    private void Awake()
    {
        SceneManager.activeSceneChanged += ChangeTargetCanvas;
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.SetResolution(1280, 720, false);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= ChangeTargetCanvas;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log($"Memoria usada: {Profiler.GetTotalAllocatedMemoryLong() / 1048576f} MB");
        }
    }

    public IEnumerator ShowCourtain(float time)
    {
        disableTime = time;
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < time)
        {
            canvasGroup.alpha = elapsed / time;
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public IEnumerator HideCourtain(float time)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f;
        while (elapsed < time)
        {
            canvasGroup.alpha = 1f - (elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    public void ChangeTargetCanvas(Scene current, Scene next)
    {
        canvas.worldCamera = Camera.main;
        StartCoroutine(HideCourtain(disableTime));
    }

    public void LoadSceneWithCourtain(string sceneName, float time)
    {
        StartCoroutine(AwaitCourtain(sceneName, time));
    }

    private IEnumerator AwaitCourtain(string sceneName, float time)
    {
        yield return StartCoroutine(ShowCourtain(time));
        yield return StartCoroutine(LoadSceneAdditiveCoroutine(sceneName));
        yield return StartCoroutine(HideCourtain(time));
    }

    private IEnumerator LoadSceneAdditiveCoroutine(string sceneName)
    {
        // 3️⃣ Descargar la escena anterior (si no es la core)
        if (!string.IsNullOrEmpty(lastLoadedScene) && lastLoadedScene != coreSceneName)
        {
            Debug.Log($"Descargando escena anterior: {lastLoadedScene}");
            yield return SceneManager.UnloadSceneAsync(lastLoadedScene);
            // Libera objetos inactivos de la escena actual
            Resources.UnloadUnusedAssets();
            System.GC.Collect(); // Fuerza el GC de C#
        }

        if (sceneName != coreSceneName)
        {
            // 1️⃣ Cargar la nueva escena de forma aditiva
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                    percentValue.text = $"{progress * 100f}%";
                }
                Debug.Log($"Cargando '{sceneName}' {progress * 100f}%");

                if (asyncLoad.progress >= 0.9f)
                {
                    // Espera un momento para efectos visuales si quieres
                    // yield return new WaitForSeconds(0.2f);
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }
        }

        // 2️⃣ Activar la nueva escena como principal
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
            canvas.worldCamera = Camera.main;
        }
        
        // 4️⃣ Guardar referencia a la nueva escena
        lastLoadedScene = sceneName;
    }
}
