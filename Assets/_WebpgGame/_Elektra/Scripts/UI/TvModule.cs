using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class TvModule : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage _rawImage;

    private void OnEnable()
    {
        _videoPlayer.prepareCompleted += OnPrepareCompleted;
    }

    private void OnDisable()
    {
        _videoPlayer.prepareCompleted -= OnPrepareCompleted;
    }

    public void SetClip(VideoClip videoClip)
    {
        _videoPlayer.clip = videoClip;
    }
    public void SetClip(string videoClip)
    {
        _videoPlayer.url = videoClip;
    }
    public void ShowTv()
    {
        _rawImage.color = Color.black;
        gameObject.SetActive(true);
    }
    public void HideTv()
    {
        _videoPlayer.Pause();
        _videoPlayer.clip = null;
        gameObject.SetActive(false);
    }

    public void PlayVideo()
    {
        _videoPlayer.Prepare();
    }
    public void ResumeVideo()
    {
        _videoPlayer.Play();
    }
    public void StopVideo()
    {
        _rawImage.color = Color.black;
        _videoPlayer.Pause();
    }
    void OnPrepareCompleted(VideoPlayer vp)
    {
        vp.Play();
        _rawImage.color = Color.white; 
    }
}
