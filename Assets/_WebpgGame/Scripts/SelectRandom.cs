using System.Collections.Generic;
using UnityEngine;

public class SelectRandom : MonoBehaviour
{
    public List<GameObject> Models = new List<GameObject>();

    private void OnEnable()
    {
        foreach (var model in Models)
        {
            model.gameObject.SetActive(false);
        }
        Models[Random.Range(0, Models.Count)].gameObject.SetActive(true);
    }
}
