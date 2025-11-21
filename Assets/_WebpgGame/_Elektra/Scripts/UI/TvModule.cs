using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class TvModule : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage _rawImage;
    [SerializeField] private GameObject resumeBtn;
    [SerializeField] private GameObject playBtn;
    [SerializeField] private GameObject pauseBtn;
    
    [Header("Background Audio Component")]
    [SerializeField] private AudioSource musicaDeFondo;
    [SerializeField] private float volumenBajo = 0f;
    [SerializeField] private float volumenNormal = 0.2f;
    

    private void OnEnable()
    {
        if (musicaDeFondo == null)
            musicaDeFondo = UIAudioManager.Instance.audioSource;
        _videoPlayer.prepareCompleted += OnPrepareCompleted;
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        _videoPlayer.prepareCompleted -= OnPrepareCompleted;
        _videoPlayer.loopPointReached += OnVideoFinished;
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
        SubirVolumen();
    }

    public void PlayVideo()
    {
        _videoPlayer.Prepare();
        HideallBtns();
        pauseBtn.SetActive(true);
    }
    public void ResumeVideo()
    {
        if (!_videoPlayer.isPlaying && _videoPlayer.isPrepared) 
        {
            // 1. Intenta reanudar normalmente (debe funcionar)
            _videoPlayer.Play();
            if(!_videoPlayer.isPlaying)
                _videoPlayer.frame++; // Descomentar si la línea anterior no funciona.
        
            BajarVolumen();
            _rawImage.color = Color.white;
            HideallBtns();
            pauseBtn.SetActive(true);
        } 
        else 
        {
            Debug.LogWarning("VideoPlayer no está en un estado reanudable (Prepared: " + _videoPlayer.isPrepared + ", Playing: " + _videoPlayer.isPlaying + ").");
        }
    }
    public void PauseVideo()
    {
        SubirVolumen();
        _videoPlayer.Pause();
        HideallBtns();
        resumeBtn.SetActive(true);
    }
    void OnPrepareCompleted(VideoPlayer vp)
    {
        vp.Play();
        _rawImage.color = Color.white; 
        BajarVolumen();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        playBtn.SetActive(true);
        vp.Stop();
    }

    void HideallBtns()
    {
        resumeBtn.SetActive(false);
        playBtn.SetActive(false);
        pauseBtn.SetActive(false);
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
