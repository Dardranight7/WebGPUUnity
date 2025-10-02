using System;
using UnityEngine;

public static class TeleportEvents
{
    public static event Action<string> OnSceneSeleted;
    public static event Action<Texture> OnPostalActive;
    public static event Action OnPostalDesactive;
    
    

    public static void SelectScene(string nameScene)
    {
        OnSceneSeleted?.Invoke(nameScene);
    }

    public static void ActivePortal(Texture texture)
    {
        OnPostalActive?.Invoke(texture);
    }
    public static void DesactivePostal()
    {
        OnPostalDesactive?.Invoke();
    }
}
