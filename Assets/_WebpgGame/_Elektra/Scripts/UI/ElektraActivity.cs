using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class ElektraActivity : MonoBehaviour
{
    public string idScene;
    public string idActivity;

    public TMP_Text title;
    public TMP_Text year;
    public TMP_Text informatioText;
    [SerializeField] private GameObject textContainer;
    
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

    private void OnEnable()
    {
        textContainer?.SetActive(false);
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
        if (!string.IsNullOrEmpty(worldUI.text))
        {
            textContainer.SetActive(true);
            informatioText.text = worldUI.text;    
        }
    }

    public void PlayClickSound()
    {
        UIAudioManager.Instance?.PlayClick();
    }
    
    public void PlayLoadSound()
    {
        UIAudioManager.Instance?.PlayLoadSound();
    }
    public void PlayAudio()
    {
        AudioClip finalClip = AudioSource.clip;
        if (finalClip != null)
        {
            UIAudioManager.Instance?.PlaySequence(AudioSource, new []
            {
                UIAudioManager.Instance?.loadSoundSound,
                finalClip
            });
        }
        else
        {
            Debug.LogWarning("No se pudo reproducir el audio: El clip es nulo y el AudioSource no tiene un clip asignado.");
        }
    }
}
