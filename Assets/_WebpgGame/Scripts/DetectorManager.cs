using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DetectorManager : MonoBehaviour
{
    public List<Detector> detectors = new List<Detector>();
    public UnityEvent onAllCollected;

    private void Awake()
    {
        RefreshList();
    }

    private void Start()
    {
        // Revisar estado de todos los detectores
        foreach (var detector in detectors)
        {
            bool collected = PlayerPrefs.GetInt(detector.uniqueID, 0) == 1;
            detector.gameObject.SetActive(!collected);
        }

        // Revisar si ya estaban todos recolectados
        CheckIfAllCollected();
    }

    public void RefreshList()
    {
        detectors.Clear();
        detectors.AddRange(GetComponentsInChildren<Detector>(true));
    }

    public void OnChildCollected(Detector detector)
    {
        CheckIfAllCollected();
    }

    private void CheckIfAllCollected()
    {
        foreach (var detector in detectors)
        {
            if (PlayerPrefs.GetInt(detector.uniqueID, 0) == 0)
                return; // Aún quedan sin recoger
        }

        // Todos recolectados
        onAllCollected?.Invoke();
    }

    [ContextMenu("Asignar IDs")]
    public void AssignIDs()
    {
        RefreshList();

        for (int i = 0; i < detectors.Count; i++)
        {
            detectors[i].uniqueID = "Coin" + i;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(detectors[i]);
#endif
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("IDs asignados correctamente.");
    }
}

