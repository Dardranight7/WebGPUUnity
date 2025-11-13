using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoTvImageController : MonoBehaviour
{
    [SerializeField] private TvModule[] tvsModules;
    [Header("Background Audio Component")]
    [SerializeField] private AudioSource musicaDeFondo;
    [SerializeField] private float volumenBajo = 0.3f;
    [SerializeField] private float volumenNormal = 1.0f;
    
    private TvModule selectedModule;
    void Start()
    {
        GetSelectedTvModule(SceneData.nextSceneId);
    }
    public void SetVideoClipURL(VideoClip videoClip)
    {
        selectedModule.SetClip(videoClip);
    }
    public void SetVideoClipURL(string videoClipURL)
    {
        selectedModule.SetClip(ElektraManager.InteractionUrl + videoClipURL);
    }
    private void GetSelectedTvModule(string sceneName)
    {
        switch (sceneName)
        {
            case "50s-60s":
                selectedModule =  tvsModules[1];
                break;
            case "70-80s":
                selectedModule =  tvsModules[2];
                break;
            case "90-2000s":
                selectedModule =  tvsModules[3];
                break;
            case "2010-2025s":
                selectedModule =  tvsModules[4];
                break;
            default:
                selectedModule =  tvsModules[0]; 
                break;
        }
    }
    public void ShowTv()
    {
        selectedModule.ShowTv();
        BajarVolumen();
    }
    public void HideTv()
    {
        selectedModule.HideTv();
        SubirVolumen();
    }

    #region BackgroundAudio Functions

    public void BajarVolumen()
    {
        if (musicaDeFondo != null)
        {
            musicaDeFondo.volume = volumenBajo;
        }
    }
    public void SubirVolumen()
    {
        if (musicaDeFondo != null)
        {
            musicaDeFondo.volume = volumenNormal;
        }
    }

    #endregion BackgroundAudio Functions
}
