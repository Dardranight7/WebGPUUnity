using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.Networking;   // <--- necesario para WebGL bundles
using Random = UnityEngine.Random;

[Serializable]
public struct TipText
{
    public string tittle;
    public string text;
}

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField, Range(2, 15)] private float minLoadTime = 2f;

    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private GameObject rotationGameObject;
    [SerializeField] private RectTransform rectComponent;

    [Header("List of tips shown to players")]
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text tipTitleText;
    [SerializeField] private TipText[] tipsLists;
    [SerializeField] private float tipDisplayTime;
    private int currentIndex = 0;
    private List<int> shuffledIndices = new List<int>();

    private AsyncOperation operation;

    void Start()
    {
        InitializeShuffledTips();

        string nextScene = SceneData.nextSceneId;

        // Decide si cargar desde bundle o desde build settings
        if (nextScene == "Lobby" || nextScene == "0")
        {
            StartCoroutine(LoadFromBuildSettings(nextScene));
        }
        else
        {
            StartCoroutine(LoadFromBundle(nextScene));
        }
    }

    private void FixedUpdate()
    {
        rectComponent.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    // ----------------------------------------------------------
    //  A) Cargar escenas que SÍ están en Build Settings
    // ----------------------------------------------------------
    IEnumerator LoadFromBuildSettings(string sceneName)
    {
        float timer = 0f;
        float tipTimer = 0f;

        UpdateTextTips();

        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            tipTimer += Time.deltaTime;
            rotationGameObject.SetActive(true);

            if (tipTimer >= tipDisplayTime)
            {
                UpdateTextTips();
                tipTimer = 0f;
            }

            if (operation.progress >= 0.9f && timer >= minLoadTime)
                operation.allowSceneActivation = true;

            yield return null;
        }
    }

    // ----------------------------------------------------------
    //  B) Cargar escenas desde AssetBundles (WebGL compatible)
    // ----------------------------------------------------------
    IEnumerator LoadFromBundle(string sceneName)
    {
        float timer = 0f;
        float tipTimer = 0f;

        UpdateTextTips();

        string bundlePath = System.IO.Path.Combine(Application.streamingAssetsPath, sceneName);

        // 1. VERIFICACIÓN: Revisamos si el bundle ya está cargado en memoria
        bool isBundleLoaded = false;

        // Buscamos en los bundles cargados actualmente
        var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        foreach (var b in loadedBundles)
        {
            if (b.name == sceneName)
            {
                isBundleLoaded = true;
                Debug.Log($"El bundle '{sceneName}' ya estaba cargado. Usando versión en cache de memoria.");
                break;
            }
        }

        // 2. DESCARGA: Solo descargamos si NO está cargado
        if (!isBundleLoaded)
        {
            Debug.Log("Loading bundle from: " + bundlePath);
            UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error downloading AssetBundle: " + req.error);
                yield break;
            }

            // Obtenemos el contenido para instanciarlo en memoria
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(req);

            if (bundle == null)
            {
                Debug.LogError("AssetBundle is NULL");
                yield break;
            }
        }

        // 3. CARGA DE ESCENA: Ahora que sabemos que el bundle está en memoria (ya sea recién bajado o antiguo), cargamos la escena
        operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError($"No se pudo cargar la escena '{sceneName}'. Verifica que el nombre sea correcto y esté dentro del bundle.");
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            tipTimer += Time.deltaTime;
            rotationGameObject.SetActive(true);

            if (tipTimer >= tipDisplayTime)
            {
                UpdateTextTips();
                tipTimer = 0f;
            }

            if (operation.progress >= 0.9f && timer >= minLoadTime)
                operation.allowSceneActivation = true;

            yield return null;
        }
    }

    // ----------------------------------------------------------
    //  Tips existentes (sin cambiar)
    // ----------------------------------------------------------
    private void UpdateTextTips()
    {
        int nextTip = GetNextUniqueTipIndex();
        TipText nextHint = tipsLists[nextTip];
        tipTitleText.text = nextHint.tittle;
        tipText.text = nextHint.text;
    }
    private void InitializeShuffledTips()
    {
        shuffledIndices = Enumerable.Range(0, tipsLists.Length).ToList();
        ShuffleList(shuffledIndices);
    }
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    public int GetNextUniqueTipIndex()
    {
        if (currentIndex >= shuffledIndices.Count)
        {
            ShuffleList(shuffledIndices);
            currentIndex = 0;
        }

        int uniqueIndex = shuffledIndices[currentIndex];
        currentIndex++;

        return uniqueIndex;
    }
}

