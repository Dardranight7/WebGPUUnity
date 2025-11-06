using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    //Referencia para mostrar el mensaje de error/progreso
    public string nextScene;
    [Header("UI Feedback (Opcional)")]
    public GameObject popUpReturn;
    public GameObject popUpConfirm;
    public LoadScene confirmLoadScene;
    public bool itsConfirmPopup = false;

    //Tiempo que se muestra el mensaje de error
    [SerializeField] private float feedbackDisplayTime = 3f;
    /// <summary>
    /// Intenta cargar una nueva escena, validando el progreso de la escena actual primero.
    /// </summary>
    /// <param name="toScene">El ID de la escena a la que se desea ir.</param>
    public void LoadSceneWithString(string toScene)
    {
        if (ElektraManager.Instance == null)
        {
            Debug.LogError("ElektraManager no encontrado. No se puede validar el progreso.");
            return;
        }
        
        if (itsConfirmPopup)
        {
            ElektraManager.Instance.LoadLevelAsync(nextScene);
            return;
        }
        if (toScene == "Lobby")
        {
            ElektraManager.Instance.LoadLevelAsync("Lobby");
            return;
        }
        // Obtener el ID de la escena actual para la validación
        string currentSceneId = SceneManager.GetActiveScene().name;

        // VALIDAR EL PROGRESO**
        string validationMessage;
        bool progressComplete = ElektraManager.Instance.ValidateProgress(out validationMessage, currentSceneId);
        if (progressComplete)
        {
            // Inicia la carga a través del manager.
            Debug.Log($"Progreso completado en {currentSceneId}. Cargando {toScene}.");
            ElektraManager.Instance.LoadLevelAsync(toScene);
        }
        else
        {
            //Muestra el mensaje de advertencia.
            Debug.LogWarning($"Progreso incompleto en {currentSceneId}: {validationMessage}");
        
            if (popUpReturn != null && popUpConfirm!= null)
            {
                popUpReturn.SetActive(true);
                popUpConfirm.SetActive(true);
                confirmLoadScene.nextScene = toScene;
            }
        }
    }
}
