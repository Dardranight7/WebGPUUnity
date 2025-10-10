using UnityEngine;
using UnityEngine.Events;

public class SimpleOnEnableCallback : MonoBehaviour
{
    public UnityEvent OnEnableCallback;

    private void OnEnable()
    {
        OnEnableCallback?.Invoke();
    }
}
