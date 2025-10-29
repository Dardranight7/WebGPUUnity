using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// [RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{

    public static UIAudioManager Instance;
    [SerializeField] private AudioSource audioSource;
    
    [Header("UI Sound Effects")]
    public AudioClip clickSound;
    public AudioClip selectSound;
    public AudioClip openPanelSound;
    public AudioClip closePanelSound;
    public AudioClip teportSound;
    public AudioClip loadSoundSound;
    public AudioClip appearanceSound;

    public float fadeTime = 1f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlayClick() => audioSource.PlayOneShot(clickSound);
    public void PlaySelect() => audioSource.PlayOneShot(selectSound);
    public void PlayTeportSound() => audioSource.PlayOneShot(teportSound);
    public void StoptTeportSound() => StartCoroutine(FadeOutSmooth(fadeTime  ));
    public void PlayOpenPanel() => audioSource.PlayOneShot(openPanelSound);
    public void PlayLoadSound() => audioSource.PlayOneShot(loadSoundSound);
    public void PlayAppearanceSound()=> audioSource.PlayOneShot(appearanceSound);
    public void PlayClosePanel() => audioSource.PlayOneShot(closePanelSound);
    public void PlaySequence(AudioSource audioSource,params AudioClip[] clips) => StartCoroutine(PlayAudiosSequence(audioSource,clips));

    public void StopAudio() => audioSource.Stop();
    private IEnumerator PlayAudiosSequence(AudioSource audioSourceTemp,AudioClip[] clips)
    {
        foreach (var clip in clips)
        {
            if (clip != null)
            {
                audioSourceTemp.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
            }
        }
    }
    
    private IEnumerator FadeOutSmooth(float fadeTime)
    {
        float starVolumen = audioSource.volume;
        float elapseTime = 0f;

        while (elapseTime < fadeTime)
        {
            elapseTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(starVolumen, 0f, elapseTime / fadeTime);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = starVolumen;
    }

    //public void PlaySequence 
}
