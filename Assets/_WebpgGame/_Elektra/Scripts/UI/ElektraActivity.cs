using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElektraActivity : MonoBehaviour
{
    public string idScene;
    public string idActivity;

    [SerializeField] private PopUpInformationScript popUpReference;
    
    public TMP_Text title;
    public TMP_Text year;
    public TMP_Text informatioText;
    [SerializeField] private GameObject textContainer;
    [SerializeField] private Image miniatureImage;
    
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
        if(textContainer != null)
            textContainer.SetActive(false);
    }
    private void OnDisable()
    {
        if (popUpReference != null)
        {
            popUpReference.OnUIHidden -= ReportActivityOnClose;
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
        if(string.IsNullOrEmpty(worldUI.year))
            year.gameObject.SetActive(false);
        else
        {
            year.gameObject.SetActive(true);
            year.text = worldUI.year;    
        }
        
        if (!string.IsNullOrEmpty(worldUI.text))
        {
            textContainer.SetActive(true);
            informatioText.text = worldUI.text;
            miniatureImage.sprite = worldUI.miniatureImage;
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
    // Método que se llama cuando el usuario INTERACTÚA (clic en el objeto)
    public void OnInteraction()
    {
        // Usamos el SO para saber si la actividad ya fue completada
        string key = $"{idScene}_{idActivity}";
        if (popUpReference == null)
        {
            Debug.LogError("PopUp Reference is missing or invalid in ElektraActivity " + idActivity);
            return;
        }
        popUpReference.OnUIHidden -= ReportActivityOnClose;
        
        if (ElektraManager.Instance.ThisActivityCompleted(key))
        {
            // Si ya está completa, podemos mostrar el pop-up nuevamente, 
            // pero NO nos suscribiremos para reportar la finalización.
            popUpReference.ShowUI();
            return; 
        }

        // Si NO está completada, mostramos y nos suscribimos para el reporte
        popUpReference.ShowUI();
        // Suscribirse al evento que cuenta la actividad
        popUpReference.OnUIHidden += ReportActivityOnClose;
    }
    
    // Método llamado cuando el PopUp se oculta
    private void ReportActivityOnClose()
    {
        // Limpiar la suscripción inmediatamente
        if (popUpReference != null)
        {
            popUpReference.OnUIHidden -= ReportActivityOnClose; 
        }
        
        // Reportar actividad si no está ya completada
        if (ElektraManager.Instance != null)
        {
            string key = $"{idScene}_{idActivity}";
            if (!ElektraManager.Instance.ThisActivityCompleted(key))
            {
                ElektraManager.Instance.ActivityCompleted(idScene, idActivity);
            }
        }
    }
}
