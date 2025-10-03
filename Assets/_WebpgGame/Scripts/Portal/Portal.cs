using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string toScene;
    public MeshRenderer meshRenderer;

    public new Collider collider;
    public Material material;

    [SerializeField] bool useCourtain = false;

    void Start()
    {
        TeleportEvents.OnSceneSeleted += ChanceScene;
        TeleportEvents.OnPostalActive += ActivePortal;
        TeleportEvents.OnPostalDesactive += DesactivePortal;
    }

    private void OnDestroy()
    {
        TeleportEvents.OnSceneSeleted -= ChanceScene;
        TeleportEvents.OnPostalActive -= ActivePortal;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (useCourtain)
        {
            MochiCourtain.Singleton.LoadSceneWithCourtain(toScene,1);
        }
        else
            SceneManager.LoadScene(toScene);
    }

    private void ChanceScene(string nextScene)
    {
        toScene = nextScene;
    }
    
    private void ActivePortal(Texture texture)
    {
        material.mainTexture = texture;
        collider.enabled = true;
        meshRenderer.material = material;
    }
    
    public void DesactivePortal()
    {
        collider.enabled = false;
        meshRenderer.material = null;
        meshRenderer.materials = new Material[0];
    }

}
