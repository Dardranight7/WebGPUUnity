using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElektraManager : MonoBehaviour
{
    public static ElektraManager Instance;

    public bool isCompleteTutorial;

    [SerializeField] private int totalScenes = 4;
    [SerializeField] private int totalActivities = 100;
    [SerializeField] private int activitiesPerScene = 25;

    
    
    public event Action<string, string> OnActivityCompleted;
    public event Action<string, int> OnCountCurrent;
    
    
    public Dictionary<string, bool> _activityCompletadas = new();
    public Dictionary<string, bool> _scenesActived = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    private void OnEnable()
    {
        OnActivityCompleted += RegisterActivityCompleted;
    }


    private void OnDisable()
    {
        OnActivityCompleted -= RegisterActivityCompleted;
    }
    
    private void RegisterActivityCompleted(string scene, string activity)
    {
        string key = $"{scene}_{activity}";
        _activityCompletadas[key] = true;
        int completed = GetActivityCompleted(scene);
        OnCountCurrent?.Invoke(scene, completed);
    }

    public void ActivityCompleted(string scene, string activity)
    {
        Debug.Log($"{scene}_{activity}");
        OnActivityCompleted?.Invoke(scene,activity);
    }

    public bool ThisActivityCompleted(string key)
    {
        return _activityCompletadas.ContainsKey(key) && _activityCompletadas[key];
    }

    public int GetActivityCompleted(string scene)
    {
        return _activityCompletadas.Count(kvp => kvp.Key.StartsWith(scene + "_") && kvp.Value);
    }

    public void RegisterSceneVisited(string sceneId)
    {
        _scenesActived[sceneId] = true;
        Debug.Log($"User intro the scene {sceneId}");
    }

    public bool ValidateProgress(out string message, string idScene)
    {
        message = "";
        
        string detailedProgress = "";

        int sceneActivities = GetActivityCompleted(idScene);
        detailedProgress += $"\n- {idScene}: {sceneActivities}/{activitiesPerScene} actividades";


        if (sceneActivities >= activitiesPerScene)
        {
            message = $"¡Felicidades! Has completado todas las actividades.{detailedProgress}";
            return true;
        }
        else
        {
            int remaining = activitiesPerScene - sceneActivities;
            message =  $" Te faltan {remaining} actividades.{detailedProgress}";
            return false;
        }
    }
    public void LoadLevelAsync(string targetSceneName)
    { 
        SceneData.nextSceneId = targetSceneName;
        
        StartCoroutine(LoadSceneAsync(SceneData.baseLoadSceneId));
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null; 
        }
    }
}
