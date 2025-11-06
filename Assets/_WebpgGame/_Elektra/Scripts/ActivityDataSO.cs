using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ElektraActivityData", menuName = "Elektra/Activity System/Activity Data", order = 1)]
public class ActivityDataSO : ScriptableObject
{
    // ID: "SceneID_ActivityID", Valor: true (completada)
    public Dictionary<string, bool> activityCompletadas = new();
    
    // ID: "SceneID", Valor: true (visitada)
    public Dictionary<string, bool> scenesActived = new();
    // Bandera para el tutorial
    public bool isTutorialComplete = false;
    // Propiedad para acceder fácilmente a los datos.
    public IReadOnlyDictionary<string, bool> ActivityCompletadas => activityCompletadas;

    /// <summary>
    /// Marca una actividad como completada.
    /// </summary>
    public void RegisterActivityCompleted(string key)
    {
        if (!activityCompletadas.ContainsKey(key) || !activityCompletadas[key])
        {
            activityCompletadas[key] = true;
        }
    }

    /// <summary>
    /// Verifica si una actividad específica ha sido completada.
    /// </summary>
    public bool IsActivityCompleted(string key)
    {
        return activityCompletadas.ContainsKey(key) && activityCompletadas[key];
    }
    
    /// <summary>
    /// Cuenta las actividades completadas para una escena.
    /// </summary>
    public int GetActivityCompletedCount(string sceneId)
    {
        return activityCompletadas.Count(kvp => kvp.Key.StartsWith(sceneId + "_") && kvp.Value);
    }
    
    /// <summary>
    /// Marca una escena como visitada.
    /// </summary>
    public void RegisterSceneVisited(string sceneId)
    {
        scenesActived[sceneId] = true;
        Debug.Log($"Scene visited registered: {sceneId}");
    }
    
    /// <summary>
    /// Marca el tutorial como completado.
    /// </summary>
    public void CompleteTutorial()
    {
        isTutorialComplete = true;
        Debug.Log("Tutorial completado y registrado en ScriptableObject.");
    }
    
    //Para usar en el editor, resetea todos los datos
    [ContextMenu("Reset All Data")]
    public void ResetAllData()
    {
        activityCompletadas.Clear();
        scenesActived.Clear();
        isTutorialComplete = false;
        Debug.Log("Global Activity Data Reset.");
    }
}
