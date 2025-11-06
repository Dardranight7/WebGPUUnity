using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class SceneActivityInitializer : MonoBehaviour
{
    void Start()
    {
        if (ElektraManager.Instance == null)
        {
            Debug.LogError("El ElektraManager no está activo. El sistema de actividades no funcionará.");
            return;
        }

        string currentSceneId = SceneData.nextSceneId;
        
        int totalActivitiesForThisScene = ElektraManager.Instance.GetMaxActivitiesForScene(currentSceneId);

        if (totalActivitiesForThisScene == 0)
        {
            Debug.LogWarning($"[Initializer] No se encontró configuración de actividades para la escena {currentSceneId} o la lista está vacía.");
        }
        
        // 2. Decirle al Manager cuántas actividades debe esperar la escena actual
        ElektraManager.Instance.SetCurrentSceneActivitiesCount(totalActivitiesForThisScene);
        
        // 3. Registrar la escena como visitada
        ElektraManager.Instance.RegisterSceneVisited(currentSceneId);
        
        Debug.Log($"[Initializer] Escena {currentSceneId} inicializada. Total de actividades esperadas (desde SO): {totalActivitiesForThisScene}.");
    }
}
