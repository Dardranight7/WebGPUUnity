using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using Random = UnityEngine.Random;


public class LoadingScreenManager : MonoBehaviour
{
    // [SerializeField]private Slider progressBar;
    // [SerializeField]private TMP_Text progressText;
    [SerializeField,Range(2,15)]private float minLoadTime = 2f;
    
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private GameObject rotationGameObject;
    [SerializeField] private RectTransform rectComponent;

    [Header("List of tips showen to players")] 
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private string[] tipsLists;
    [SerializeField] private float tipDisplayTime;
    private int currentIndex = 0;
    private List<int> shuffledIndices = new List<int>();
    
    private AsyncOperation operation;
    
    void Start()
    {
        InitializeShuffledTips();
        StartCoroutine(LoadAsyncOperation(SceneData.nextSceneId));
    }

    private void FixedUpdate()
    {
        rectComponent.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    IEnumerator LoadAsyncOperation(string sceneName)
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
            
            // float progress = Mathf.Clamp01(operation.progress / 0.9f);
            // float fakeProgress = Mathf.Clamp01(timer / minLoadTime);
            // progressBar.value = Mathf.Max(progress, fakeProgress);
            // progressText.text = (progressBar.value * 100).ToString("F0") + "%";
            rotationGameObject.SetActive(true);
            if (tipTimer >= tipDisplayTime)
            {
                UpdateTextTips();
                tipTimer = 0f;
            }
    
            if (operation.progress >= 0.9f && timer >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private void UpdateTextTips()
    {
        int nextTip = GetNextUniqueTipIndex();
        tipText.text = tipsLists[nextTip];
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
