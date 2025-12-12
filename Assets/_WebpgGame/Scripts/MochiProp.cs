
using System.Collections.Generic;
using UnityEngine;

public class MochiProp : MonoBehaviour
{
    public int index;

    private void Awake()
    {
        if (MochiProps.instance == null)
            return;
        MochiProps.instance.OnPropsUpdated += DisableIfNotOwned;
    }

    private void Start()
    {
        DisableIfNotOwned();
    }

    private void OnEnable()
    {
        DisableIfNotOwned();
    }

    private void OnDestroy()
    {
        if (MochiProps.instance == null)
            return;
        MochiProps.instance.OnPropsUpdated -= DisableIfNotOwned;
    }

    public void DisableIfNotOwned()
    {
        if(MochiProps.instance != null)
            if (MochiProps.instance.Inventory.Contains(index))
            {
                gameObject.SetActive(true);
            }
        else
            gameObject.SetActive(false);
    }
}
