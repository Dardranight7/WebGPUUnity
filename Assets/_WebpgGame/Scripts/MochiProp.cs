using UnityEngine;

public class MochiProp : MonoBehaviour
{
    public int index;

    private void Awake()
    {
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
        MochiProps.instance.OnPropsUpdated -= DisableIfNotOwned;
    }

    public void DisableIfNotOwned()
    {
        if (MochiProps.instance.Inventory.Contains(index))
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
