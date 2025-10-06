using UnityEngine;
using UnityEngine.Events;

public class SimpleOnTrigger : MonoBehaviour
{
    public LayerMask layerMask;
    public UnityEvent OnTriggerEnterCallback, OnTriggerExitCallback;
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & layerMask) != 0)
        {
            OnTriggerEnterCallback?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & layerMask) != 0)
        {
            OnTriggerExitCallback?.Invoke();
        }
    }
}
