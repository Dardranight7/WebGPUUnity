using System;
using UnityEngine;

public class UITutorialScript : MonoBehaviour
{
    private ElektraManager _manager;
    private void Awake()
    {
        _manager = ElektraManager.Instance;
        if (_manager != null)
        {
            if (!_manager.IsTutorialComplete)
                gameObject.SetActive(true);
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("ElektraManager.Instance no encontrado.");
            gameObject.SetActive(false); // Desactivar si no hay manager
        }
    }

    public void CompletTutorial()
    {
        if (_manager != null)
        {
            _manager.CompleteTutorial();
        }
    }
}
