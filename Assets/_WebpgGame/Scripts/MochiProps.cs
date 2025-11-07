using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class MochiProps : MonoBehaviour
{
    public static MochiProps instance;
    public System.Action OnPropsUpdated;

    private void Awake()
    {
        instance = this;
        Backend.OnPlayerProfileUpdate += UpdatePropsVisual;
    }

    private void OnEnable()
    {
        UpdatePropsVisual();
    }

    private void OnDestroy()
    {
        Backend.OnPlayerProfileUpdate -= UpdatePropsVisual;
    }

    public void UpdatePropsVisual() 
    {
        if (Backend.singleton == null)
        {
            return;
        }
        Inventory = Backend.singleton.playerProfile.userEquip;
        OnPropsUpdated?.Invoke();
    }

    public List<int> Inventory = new List<int>();
}
