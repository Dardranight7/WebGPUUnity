using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoadingScreenManager : MonoBehaviour
{
    // [SerializeField]private Slider progressBar;
    // [SerializeField]private TMP_Text progressText;
    [SerializeField,Range(2,15)]private float minLoadTime = 2f;
    
    [SerializeField]private float rotationSpeed = 200f;
    [SerializeField]private GameObject rotationGameObject;
    [SerializeField]private RectTransform rectComponent;
    
    private AsyncOperation operation;
    
    void Start()
    {
        StartCoroutine(LoadAsyncOperation(SceneData.nextSceneId));
    }

    private void FixedUpdate()
    {
        rectComponent.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    IEnumerator LoadAsyncOperation(string sceneName)
    {
        float timer = 0f;
    
        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
    
        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            
            // float progress = Mathf.Clamp01(operation.progress / 0.9f);
            // float fakeProgress = Mathf.Clamp01(timer / minLoadTime);
            // progressBar.value = Mathf.Max(progress, fakeProgress);
            // progressText.text = (progressBar.value * 100).ToString("F0") + "%";
            rotationGameObject.SetActive(true);
    
            if (operation.progress >= 0.9f && timer >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
