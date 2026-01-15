using UnityEngine;

public class StunPowerUp : MonoBehaviour
{
    public float stunDuration = 3f;
    public GameObject pickUpEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.TryGetComponent<BotMochiPush>(out _))
        {
            EventBus<StunSignal>.Publish(new StunSignal {
                activator = other.gameObject,
                duration = stunDuration
            });

            if (pickUpEffect != null)
                Instantiate(pickUpEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}

public struct StunSignal {
    public GameObject activator;
    public float duration;
}