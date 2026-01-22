using System;
using UnityEngine;

public class StunPowerUp : MonoBehaviour
{
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private GameObject pickUpEffect;

    private GameObject currentParent;
    private void Start()
    {
        currentParent = transform.parent.gameObject;
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

            
            Destroy(currentParent);
        }
    }
}

public struct StunSignal {
    public GameObject activator;
    public float duration;
}