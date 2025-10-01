using System.Collections.Generic;
using UnityEngine;

public class ModelRandomizer : MonoBehaviour
{
    public List<GameObject> Models;
    public List<ModelRandomizer> ChildRandomizer;

    private void OnEnable()
    {
        UpdateVisual();
    }

    [ContextMenu("UpdateVisual")]
    public void UpdateVisual()
    {
        for (int i = 0; i < Models.Count; i++)
        {
            Models[i].SetActive(false);
        }
        Models[Random.Range(0, Models.Count - 1)].SetActive(true);
        if (ChildRandomizer.Count > 0) 
        {
            foreach (var item in ChildRandomizer)
            {
                item.UpdateVisual();
            }
        }
    }
}
