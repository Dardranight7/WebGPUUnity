using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ElektraSceneConfigData", menuName = "Elektra System/Scene Config Data", order = 2)]
public class SceneConfigSO : ScriptableObject
{
    // Clase serializable para el Inspector
    [System.Serializable]
    public class SceneConfiguration
    {
        public string sceneID;
        [Tooltip("Lista manual de todos los IDs de actividad esperados en esta escena.")]
        public List<string> requiredActivityIDs = new List<string>();
        
        // Propiedad de solo lectura para obtener el conteo
        public int MaxActivitiesCount => requiredActivityIDs.Count; 
    }

    [Tooltip("Configuración de actividades para todas las escenas.")]
    public List<SceneConfiguration> sceneConfigurations = new List<SceneConfiguration>();

    /// <summary>
    /// Devuelve el número máximo de actividades requeridas para una escena específica.
    /// </summary>
    public int GetMaxActivities(string sceneId)
    {
        var config = sceneConfigurations.FirstOrDefault(c => c.sceneID == sceneId);
        // Devuelve el conteo o 0 si no se encuentra la configuración
        return (config != null) ? config.MaxActivitiesCount : 0; 
    }
}
