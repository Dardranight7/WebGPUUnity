using System;
using UnityEngine;

public class ElektraActivity : MonoBehaviour
{
    public string idScene;
    public string idActivity;

    public void Start()
    {
        string key = $"{idScene}_{idActivity}";

        if (ElektraManager.Instance.ThisActivityCompleted(key))
        {
            ShowMeCompleted();
        }
    }

    void ShowMeCompleted()
    {
        Debug.Log($"completed this {idScene}_{idActivity}");
    }

    public void OnButtonCompletedActivity()
    {
        ElektraManager.Instance.ActivityCompleted(idScene, idActivity);
    }
}
