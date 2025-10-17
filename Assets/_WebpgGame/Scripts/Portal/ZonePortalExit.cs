using UnityEngine;

public class ZonePortalExit : MonoBehaviour
{
    public void OnTriggerExit(Collider other)
    {
        TeleportEvents.DesactivePostal();

    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        UIAudioManager.Instance?.StoptTeportSound();

    }
}
