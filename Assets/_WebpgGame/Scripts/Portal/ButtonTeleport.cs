using System;
using UnityEditor;
using UnityEngine;

public class ButtonTeleport : MonoBehaviour
{
    public SceneAsset toScene;

    public void OnTriggerEnter(Collider other)
    {
        TeleportEvents.SelectScene(toScene);
        TeleportEvents.SelectScene();

    }
}
