using System;
using UnityEditor;
using UnityEngine;

public static class TeleportEvents
{
    public static event Action<SceneAsset> OnSceneSeleted;
    public static event Action OnPostalActive;

    public static void SelectScene(SceneAsset nameScene)
    {
        OnSceneSeleted?.Invoke(nameScene);
    }

    public static void SelectScene()
    {
        OnPostalActive?.Invoke();
    }
}
