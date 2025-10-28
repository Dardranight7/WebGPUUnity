using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoadingScreenManager : MonoBehaviour
{
    public Slider progressBar;
    public TMP_Text progressText;
    public float minLoadTime = 2f;
    
    private AsyncOperation operation;
    
    void Start()
    {
        StartCoroutine(LoadAsyncOperation(SceneData.nextSceneId));
    }
    
    IEnumerator LoadAsyncOperation(string sceneName)
    {
        float timer = 0f;
    
        operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
    
        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            float fakeProgress = Mathf.Clamp01(timer / minLoadTime);
            progressBar.value = Mathf.Max(progress, fakeProgress);
            progressText.text = (progressBar.value * 100).ToString("F0") + "%";
    
            if (operation.progress >= 0.9f && timer >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
