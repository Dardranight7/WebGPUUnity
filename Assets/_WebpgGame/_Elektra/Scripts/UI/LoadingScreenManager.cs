using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
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

    private AsyncOperationHandle<SceneInstance> operation;

    void Start()
    {
        InitializeShuffledTips();
        StartCoroutine(LoadScene(SceneData.nextSceneId));
    }

    IEnumerator LoadScene(string sceneName)
    {
        bool isLobbyScene = sceneName == "Lobby" || sceneName == "0";

        if (isLobbyScene)
        {
            yield return LoadSceneNormal(sceneName);
        }
        else
        {
            yield return LoadAsyncOperation(sceneName);
        }
    }

    IEnumerator LoadSceneNormal(string sceneName)
    {
        float timer = 0f;
        float tipTimer = 0f;
        UpdateTextTips();

        Debug.Log($"[LoadingScreen] Cargando escena tradicional: {sceneName}");

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
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
            {
                Debug.Log("[LoadingScreen] Activando escena tradicional...");
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void FixedUpdate()
    {
        rectComponent.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    IEnumerator LoadAsyncOperation(string sceneName)
    {
        float timer = 0f;
        float tipTimer = 0f;
        bool sceneActivated = false;
        UpdateTextTips();

        Debug.Log($"[LoadingScreen] Iniciando carga de escena: {sceneName}");

        // Cargar la escena usando Addressables
        operation = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single, false);

        while (!sceneActivated)
        {
            timer += Time.deltaTime;
            tipTimer += Time.deltaTime;

            float progress = operation.PercentComplete;

            //if (Mathf.FloorToInt(timer) != Mathf.FloorToInt(timer - Time.deltaTime))
            //{
            //    Debug.Log($"[LoadingScreen] Progress: {progress:F2}, Timer: {timer:F1}s, Status: {operation.Status}, IsDone: {operation.IsDone}");
            //}

            rotationGameObject.SetActive(true);

            if (tipTimer >= tipDisplayTime)
            {
                UpdateTextTips();
                tipTimer = 0f;
            }

            // Verificar si la operación está completa
            if (operation.IsDone)
            {
                Debug.Log($"[LoadingScreen] Operación completada. Status final: {operation.Status}");

                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    // Esperar el tiempo mínimo si es necesario
                    if (timer < minLoadTime)
                    {
                        Debug.Log($"[LoadingScreen] Esperando tiempo mínimo. Restante: {minLoadTime - timer:F1}s");
                        yield return new WaitForSeconds(minLoadTime - timer);
                    }

                    Debug.Log("[LoadingScreen] Activando escena...");
                    // Activar la escena
                    AsyncOperation activationOp = operation.Result.ActivateAsync();
                    yield return activationOp;
                    sceneActivated = true;
                    Debug.Log("[LoadingScreen] Escena activada exitosamente");
                }
                else
                {
                    Debug.LogError($"[LoadingScreen] Error al cargar la escena. Status: {operation.Status}");
                    if (operation.OperationException != null)
                    {
                        Debug.LogError($"[LoadingScreen] Exception: {operation.OperationException}");
                    }
                    yield break;
                }
            }

            yield return null;
        }
    }

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

    private void OnDestroy()
    {
        // Importante: Liberar el handle cuando se destruya el objeto
        if (operation.IsValid())
        {
            Addressables.Release(operation);
        }
    }
}