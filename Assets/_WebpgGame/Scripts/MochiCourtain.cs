using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MochiCourtain : MonoBehaviour
{
    [SerializeField] public Transform bubblesParent;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] public Canvas canvas;
    public static MochiCourtain Singleton;

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
        //simple fadein to canvas group
        float  elapseTime = 0;
        while (elapseTime < time)
        {
            canvasGroup.alpha = elapseTime / time;
            elapseTime += Time.deltaTime;
            yield return null;
        }
        bubblesParent.gameObject.SetActive(true);
        canvasGroup.alpha = 1;
    }

    public IEnumerator HideCourtain(float time)
    {
        //simple fadeout to canvas group
        float elapseTime = 0;
        while (elapseTime < time)
        {
            canvasGroup.alpha = 1 - (elapseTime / time);
            elapseTime += Time.deltaTime;
            yield return null;
        }
        bubblesParent.gameObject.SetActive(false);
        canvasGroup.alpha = 0;
    }

    float disableTime = 1;

    public void ChangeTargetCanvas(Scene a, Scene b)
    {
        canvas.worldCamera = Camera.main;
        StartCoroutine(HideCourtain(disableTime));
    }

    public void LoadSceneWithCourtain(string sceneName, float time)
    {
        StartCoroutine(AwaitCourtain(sceneName,time));
    }

    public IEnumerator AwaitCourtain(string sceneName, float time)
    {
        StartCoroutine(ShowCourtain(time));
        yield return new WaitForSeconds(time);
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Inicia la carga asincrónica
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Evita que se active de inmediato
        asyncLoad.allowSceneActivation = false;

        // Mientras carga puedes mostrar un loader o animación
        while (!asyncLoad.isDone)
        {
            // El progreso real va de 0 a 0.9, el 0.9 significa "ya está lista para activar"
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log("Progreso de carga: " + (progress * 100f) + "%");

            // Aquí puedes actualizar una barra de carga o animación de cortinilla
            // Ejemplo:
            // loadingBar.fillAmount = progress;

            // Cuando llegue al 90% (0.9f), ya está lista para activar
            if (asyncLoad.progress >= 0.9f)
            {
                // Espera a que termine tu animación de cortinilla
                yield return new WaitForSeconds(1f); // <-- ajusta este tiempo a tu animación

                // Ahora sí activa la escena
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
