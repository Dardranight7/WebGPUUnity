using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public SceneAsset toScene;
    public MeshRenderer meshRenderer;
    public Material material;

    void Start()
    {
        TeleportEvents.OnSceneSeleted += ChanceScene;
        TeleportEvents.OnPostalActive += ActivePortal;
    }

    private void OnDestroy()
    {
        TeleportEvents.OnSceneSeleted -= ChanceScene;
        TeleportEvents.OnPostalActive -= ActivePortal;
    }

    private void OnTriggerEnter(Collider other)
    {
        SceneManager.LoadScene(toScene.name);
    }

    private void ChanceScene(SceneAsset nextScene)
    {
        toScene = nextScene;
    }
    
    private void ActivePortal()
    {
        meshRenderer.material = material;
    }

}
