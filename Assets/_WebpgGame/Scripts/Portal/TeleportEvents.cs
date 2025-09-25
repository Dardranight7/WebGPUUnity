using System;
using UnityEditor;
using UnityEngine;

public static class TeleportEvents
{
    public static event Action<SceneAsset> OnSceneSeleted;
    public static event Action OnPostalActive;
    public static event Action OnPostalDesactive;
    
    

    public static void SelectScene(SceneAsset nameScene)
    {
        OnSceneSeleted?.Invoke(nameScene);
    }

    public static void ActivePortal()
    {
        OnPostalActive?.Invoke();
    }
    public static void DesactivePostal()
    {
        OnPostalDesactive?.Invoke();
    }
}
