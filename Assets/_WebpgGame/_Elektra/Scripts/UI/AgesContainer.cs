using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgesContainer : MonoBehaviour
{
    public string sceneAge;
    
    public int numberActivity;
    private int _maxActivitiesExpected;
    
    public Image background;
    public GameObject signAlert;
    public StatusScene statusScene;
    public Sprite desactive;
    public TMP_Text text;
    public Sprite alert;
    public Sprite select;
    public Sprite finish;
    public GameObject completeAge;
    private void Awake()
    {
        if (ElektraManager.Instance == null) return;
        
        // OBTENER EL MÁXIMO AUTOMÁTICAMENTE
        _maxActivitiesExpected = ElektraManager.Instance.GetMaxActivitiesForScene(sceneAge);
        // Si no se encuentra configuración, asume 0 o un valor predeterminado (ej. 25 si es un caso de fallback)
        if (_maxActivitiesExpected == 0)
        {
            Debug.LogWarning($"No se encontró configuración para {sceneAge}. Usando 0 actividades máximas.");
        }
        
        // 1. Obtener el progreso actual
        numberActivity = ElektraManager.Instance.GetActivityCompleted(sceneAge);
        
        if (SceneManager.GetActiveScene().name != sceneAge)
        {
            // 2. Usar el valor dinámico (_maxActivitiesExpected)
            if (numberActivity == 0)
            {
                ImageStatus(statusScene = StatusScene.DESACTIVE);
                return;
            }
            // Condición de alerta: Actividades > 0 Y < Máximo esperado
            if (numberActivity > 0 && numberActivity < _maxActivitiesExpected) 
            {
                ImageStatus(statusScene = StatusScene.ALERT);
                return;
            }

            // Condición de finalización: Actividades igual al Máximo esperado
            if (numberActivity >= _maxActivitiesExpected) // Usar >= por seguridad
            {
                ImageStatus(statusScene = StatusScene.FINISH);
                return;
            }
        }
        statusScene = StatusScene.SELECT;
        ImageStatus(statusScene);
        UIAudioManager.Instance?.StopAudio();
        UIAudioManager.Instance?.PlayAppearanceSound();
    }
    
    private void OnEnable()
    {
        ElektraManager.Instance.OnCountCurrent += UpdateProgress;
    }
    private void OnDisable()
    {
        ElektraManager.Instance.OnCountCurrent -= UpdateProgress;
    }

    private void UpdateProgress(string scene, int progress)
    {
        if (scene == sceneAge)
        {
            Debug.Log($"Updated Scene {sceneAge} {scene}");
            numberActivity = progress;

            // Condición de finalización dinámica
            if (numberActivity >= _maxActivitiesExpected) 
            {
                ImageStatus(statusScene = StatusScene.FINISH);
                completeAge.SetActive(true);
                Invoke( nameof(DesactiveCompleteAge), 6f);
                Invoke(nameof(LoadNextScene), 5f);
            }
        }
    }

    private void DesactiveCompleteAge()
    {
        completeAge.SetActive(false);
    }

    private void LoadNextScene()
    {
        string nextScene = "Lobby";
        switch (SceneManager.GetActiveScene().name)
        {
            case "50s-60s":
                nextScene = "70-80s";
                break;
            case "70-80s":
                nextScene = "90-2000s";
                break;
            case "90-2000s":
                nextScene = "2010-2025s";
                break;
            default:
                nextScene = "Lobby";
                break;
        }
        ElektraManager.Instance.LoadLevelAsync(nextScene);
    }

    private void ImageStatus(StatusScene status)
    {
        switch (status)
        {
            case StatusScene.SELECT:
                background.sprite = select;
                text.color = new Color(1f, 0f, 0f);
                signAlert.SetActive(false);
                break;
            case StatusScene.DESACTIVE:
                background.sprite = desactive;
                text.color = new Color(0.49f, 0.49f, 0.49f);
                signAlert.SetActive(false);
                break;
            case StatusScene.ALERT:
                background.sprite = alert;
                text.color = new Color(0.9f, 0.8f, 0.2f);
                signAlert.SetActive(true);
                break;
            case StatusScene.FINISH:
                signAlert.SetActive(false);
                text.color = new Color(0.6f, 0.13f, 0.11f);
                background.sprite = finish;
                break;
        }
    }

}

public enum StatusScene
{
    DESACTIVE,
    ALERT,
    SELECT,
    FINISH,
}
