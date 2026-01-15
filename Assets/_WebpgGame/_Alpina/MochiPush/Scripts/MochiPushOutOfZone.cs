using UnityEngine;

public class MochiPushOutOfZone : MonoBehaviour
{
    [SerializeField] private Collider bondaryCollider;
    /// <summary>
    /// If a player or bot goes out the area, we send a message for them to stop moving and fall
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Bot"))
        {
            EventBus<PlayerExitZoneSignal>.Publish(new PlayerExitZoneSignal
            {
                player = other.gameObject
            });
        }
    }
}
