using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

public class TvModule : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;
    public void SetClip(VideoClip videoClip)
    {
        _videoPlayer.clip = videoClip;
    }
    public void ShowTv()
    {
        gameObject.SetActive(true);
    }
    public void HideTv()
    {
        gameObject.SetActive(false);
    }
}
