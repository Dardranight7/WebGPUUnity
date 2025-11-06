using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MapAgeTextScript : MonoBehaviour
{
    public string nameSceneScore;
    public TMP_Text score;
    
    void Start()
    {
        // Verificar el Manager antes de continuar
        if (ElektraManager.Instance == null)
        {
            Debug.LogError("ElektraManager.Instance no encontrado. No se puede actualizar el progreso.");
            gameObject.SetActive(false);
            return;
        }

        // Si el objeto no pertenece a la escena que está activa, se desactiva.
        if (SceneManager.GetActiveScene().name != nameSceneScore)
        {
            gameObject.SetActive(false);
            return;
        }

        // Obtiene el maximo real de la escena
        int maxActivities = ElektraManager.Instance.GetMaxActivitiesForScene(nameSceneScore); 
        int completed = ElektraManager.Instance.GetActivityCompleted(nameSceneScore);

        Debug.Log($"Actividades completadas en {nameSceneScore}: {completed}/{maxActivities}");
        
        score.text = $"{completed}/{maxActivities}";
    }

    private void OnEnable()
    {
        if (ElektraManager.Instance != null)
        {
            ElektraManager.Instance.OnCountCurrent += UpdateProgress;
        }
    }
    private void OnDisable()
    {
        if (ElektraManager.Instance != null)
        {
            ElektraManager.Instance.OnCountCurrent -= UpdateProgress;
        }
    }

    private void UpdateProgress(string scene, int progress)
    {
        // Si el evento es para la escena que estamos mostrando, actualizamos.
        if (scene == nameSceneScore)
        {
            // Obtiene el máximo actualizado
            int maxActivities = ElektraManager.Instance.GetMaxActivitiesForScene(scene); 
            
            score.text = $"{progress}/{maxActivities}";
        }
    }
}
