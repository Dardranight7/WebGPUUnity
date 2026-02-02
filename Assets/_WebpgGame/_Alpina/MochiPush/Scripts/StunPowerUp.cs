using System;
using UnityEngine;

public class StunPowerUp : MonoBehaviour
{
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private GameObject pickUpEffect;
    [SerializeField] private PowerUpSpawner spawner;

    private GameObject currentParent;
    public void SetupPowerUp(PowerUpSpawner spawner)
    {
        currentParent = transform.parent.gameObject;
        this.spawner = spawner;
    }

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

            spawner.DeletePowerup();
            Destroy(currentParent);
        }
    }
}

public struct StunSignal {
    public GameObject activator;
    public float duration;
}