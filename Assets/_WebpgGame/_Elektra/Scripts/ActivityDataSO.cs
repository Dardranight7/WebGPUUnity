using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElektraActivityData", menuName = "Elektra/Activity System/Activity Data", order = 1)]
public class ActivityDataSO : ScriptableObject
{
    // Una lista de IDs de actividad y su estado de finalización
    // La clave es el ID de la actividad (string), el valor es si está completada (bool)
    public Dictionary<string, bool> activitiesStatus = new Dictionary<string, bool>();

    // Evento para notificar a la UI u otros sistemas cuando se completa una actividad
    // Se usa System.Action para evitar la dependencia de UnityEvent
    public event System.Action<string> OnActivityCompleted;

    // Evento para notificar cuando todas las actividades de la escena están completadas
    public event System.Action OnAllActivitiesCompleted;

    // Propiedad para saber cuántas actividades hay en total
    public int TotalActivities => activitiesStatus.Count;

    // Propiedad para saber cuántas actividades se han completado
    public int CompletedActivitiesCount
    {
        get
        {
            int count = 0;
            foreach (var status in activitiesStatus.Values)
            {
                if (status)
                {
                    count++;
                }
            }
            return count;
        }
    }

    // Propiedad para verificar si todas las actividades han sido completadas
    public bool AllActivitiesCompleted => TotalActivities > 0 && CompletedActivitiesCount == TotalActivities;

    /// <summary>
    /// Marca una actividad como completada y verifica si todas están hechas.
    /// </summary>
    /// <param name="activityID">El ID único de la actividad.</param>
    public void CompleteActivity(string activityID)
    {
        if (activitiesStatus.ContainsKey(activityID) && !activitiesStatus[activityID])
        {
            activitiesStatus[activityID] = true;
            Debug.Log($"Actividad completada: {activityID}");

            // Notifica que esta actividad ha sido completada
            OnActivityCompleted?.Invoke(activityID);

            // Verifica si se completaron todas las actividades de la escena
            if (AllActivitiesCompleted)
            {
                Debug.Log("¡Todas las actividades de la escena completadas! Se entrega premio.");
                OnAllActivitiesCompleted?.Invoke();
                // Aquí podrías agregar la lógica de entrega de premio en el Manager
            }
        }
    }

    /// <summary>
    /// Inicializa la lista de actividades para la escena actual (llamar al cargar la escena).
    /// </summary>
    /// <param name="initialActivities">Lista de IDs de las actividades requeridas en la escena.</param>
    public void InitializeActivities(List<string> initialActivities)
    {
        activitiesStatus.Clear();
        foreach (var id in initialActivities)
        {
            // Inicializa todas las actividades como no completadas
            activitiesStatus.Add(id, false);
        }
        Debug.Log($"Actividades inicializadas. Total: {TotalActivities}");
    }
}
