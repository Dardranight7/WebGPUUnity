using System;
using UnityEngine;

public class ValideteScene : MonoBehaviour
{
    public string idscene;

    public void Start()
    {
        ElektraManager.Instance.RegisterSceneVisited(idscene);
    }
}
