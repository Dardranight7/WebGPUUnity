using UnityEngine;

public class ZonePortalExit : MonoBehaviour
{
    public void OnTriggerExit(Collider other)
    {
        TeleportEvents.DesactivePostal();

    }
}
