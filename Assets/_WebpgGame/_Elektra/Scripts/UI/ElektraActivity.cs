using System;
using TMPro;
using UnityEngine;

public class ElektraActivity : MonoBehaviour
{
    public string idScene;
    public string idActivity;

    public TMP_Text title;
    public TMP_Text year;
    public TMP_Text informatioText;
    
    public WorldUI worldUI;

    public AudioSource AudioSource;

    public void Start()
    {
        string key = $"{idScene}_{idActivity}";

        if (ElektraManager.Instance.ThisActivityCompleted(key))
        {
            ShowMeCompleted();
        }
    }

    void ShowMeCompleted()
    {
        Debug.Log($"completed this {idScene}_{idActivity}");
    }

    public void OnButtonCompletedActivity()
    {
        ElektraManager.Instance?.ActivityCompleted(idScene, idActivity);
        worldUI.button.gameObject.SetActive(false);
        worldUI.buttonPanelInformation.gameObject.SetActive(true);
    }

    public void SetScene(string setIdScene)
    {
        idScene = setIdScene;
    }
    
    public void SetActivity(string  setidActivity){
        idActivity =  setidActivity;
    }

    public void SetInformation(WorldUI tempWorldUI)
    {
        worldUI = tempWorldUI;
        title.text = worldUI.title;
        year.text = worldUI.year;
        informatioText.text = worldUI.text;
    }

    public void PlayClickSound()
    {
        UIAudioManager.Instance?.PlayClick();
    }
    
    public void PlayLoadSound()
    {
        UIAudioManager.Instance?.PlayLoadSound();
    }
    public void PlayAudio(AudioClip clip)
    {
        UIAudioManager.Instance?.PlaySequence(AudioSource, new []
        {
            UIAudioManager.Instance?.loadSoundSound,
            clip
        });
    }
}
