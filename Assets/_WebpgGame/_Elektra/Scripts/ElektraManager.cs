using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ElektraManager : MonoBehaviour
{
    public static ElektraManager Instance;

    public bool isCompleteTutorial;

    public event Action<string, string> OnActivityCompleted;
    public event Action<string, int> OnCountCurrent;
    
    
    public Dictionary<string, bool> _activityCompletadas = new();
    public Dictionary<string, bool> _scenesActived = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
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

}
