using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoTvImageController : MonoBehaviour
{
    [SerializeField] private TvModule[] tvsModules;

    private TvModule selectedModule;
    void Start()
    {
        GetSelectedTvModule(SceneData.nextSceneId);
    }
    public void SetVideoClip(VideoClip videoClip)
    {
        selectedModule.SetClip(videoClip);
    }
    private void GetSelectedTvModule(string sceneName)
    {
        switch (sceneName)
        {
            case "50s-60s":
                selectedModule =  tvsModules[0];
                break;
            case "70-80s":
                selectedModule =  tvsModules[1];
                break;
            case "90-2000s":
                selectedModule =  tvsModules[2];
                break;
            case "2010-2025s":
                selectedModule =  tvsModules[3];
                break;
            default:
                selectedModule =  tvsModules[0]; 
                break;
        }
    }
    public void ShowTv()
    {
        selectedModule.ShowTv();
    }
    public void HideTv()
    {
        selectedModule.HideTv();
    }
}
