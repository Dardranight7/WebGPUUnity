using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class MochiCourtain : MonoBehaviour
{
    [SerializeField] public Transform bubblesParent;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] public Canvas canvas;
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI percentValue;
    public static MochiCourtain Singleton;

    [SerializeField] string coreSceneName = "Core"; // Escena base
    string lastLoadedScene = null;
    AsyncOperationHandle<SceneInstance>? lastHandle = null;
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
        // Si no es la escena base (core), cargar mediante Addressables
        if (sceneName != coreSceneName)
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            lastHandle = handle;

            while (!handle.IsDone)
            {
                float progress = Mathf.Clamp01(handle.PercentComplete);
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                    percentValue.text = $"{(int)(progress * 100f)}%";
                }
                yield return null;
            }

            SceneInstance instance = handle.Result;
            SceneManager.SetActiveScene(instance.Scene);
            canvas.worldCamera = Camera.main;
        }
        else
        {
            // Cargar escena base Core (no addressable)
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                    percentValue.text = $"{(int)(progress * 100f)}%";
                }
                yield return null;
            }
        }

        // Descargar escena anterior si corresponde
        if (!string.IsNullOrEmpty(lastLoadedScene) && lastLoadedScene != coreSceneName)
        {
            if (lastHandle.HasValue)
            {
                yield return Addressables.UnloadSceneAsync(lastHandle.Value);
            }
            else
            {
                yield return SceneManager.UnloadSceneAsync(lastLoadedScene);
            }
        }

        lastLoadedScene = sceneName;
    }
}
