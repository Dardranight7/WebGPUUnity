using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SimpleOnEnableCallback : MonoBehaviour
{
    public UnityEvent OnEnableCallback;

    public bool useTime = false;
    public float waitTime;

    private void OnEnable()
    {
        if (useTime)
        {
            StartCoroutine(LoadAfterTime());
        }
        else
        {
            OnEnableCallback?.Invoke();
        }
    }

    public IEnumerator LoadAfterTime()
    {
        yield return new WaitForSeconds(waitTime);
        OnEnableCallback?.Invoke();
    }
}
