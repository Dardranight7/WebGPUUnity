using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MochiCourtain : MonoBehaviour
{
    [SerializeField] public Transform bubblesParent;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] public Canvas canvas;
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

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= ChangeTargetCanvas;
    }

    public IEnumerator ShowCourtain(float time)
    {
        disableTime = time;
        float elapsed = 0f;
        while (elapsed < time)
        {
            canvasGroup.alpha = elapsed / time;
            elapsed += Time.deltaTime;
            yield return null;
        }
        bubblesParent.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    public IEnumerator HideCourtain(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            canvasGroup.alpha = 1f - (elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bubblesParent.gameObject.SetActive(false);
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
        
        // 1️⃣ Cargar la nueva escena de forma aditiva
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Cargando '{sceneName}' {progress * 100f}%");

            if (asyncLoad.progress >= 0.9f)
            {
                // Espera un momento para efectos visuales si quieres
                yield return new WaitForSeconds(0.2f);
                asyncLoad.allowSceneActivation = true;
            }
            yield return null;
        }

        // 2️⃣ Activar la nueva escena como principal
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
            canvas.worldCamera = Camera.main;
        }
        
        // 3️⃣ Descargar la escena anterior (si no es la core)
        if (!string.IsNullOrEmpty(lastLoadedScene) && lastLoadedScene != coreSceneName)
        {
            Debug.Log($"Descargando escena anterior: {lastLoadedScene}");
            yield return SceneManager.UnloadSceneAsync(lastLoadedScene);
        }

        // 4️⃣ Guardar referencia a la nueva escena
        lastLoadedScene = sceneName;
    }
}
