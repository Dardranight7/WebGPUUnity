using System;
using UnityEngine;

public class ButtonTeleport : MonoBehaviour
{
    public Texture texture;
    public string toScene;

    public void OnTriggerEnter(Collider other)
    {
        TeleportEvents.SelectScene(toScene);
        TeleportEvents.ActivePortal(texture);

    }
}
