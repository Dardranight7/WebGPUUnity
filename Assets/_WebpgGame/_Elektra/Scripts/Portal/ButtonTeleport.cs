using System;
using UnityEditor;
using UnityEngine;

public class ButtonTeleport : MonoBehaviour
{
    public Texture texture;
    public SceneAsset toScene;

    public void OnTriggerEnter(Collider other)
    {
        TeleportEvents.SelectScene(toScene);
        TeleportEvents.ActivePortal(texture);

    }
}
