using System;
using UnityEngine;

public class UITutorialScript : MonoBehaviour
{
    private ElektraManager _manager;
    private void Awake()
    {
        _manager = GameObject.FindGameObjectWithTag("manager").GetComponent<ElektraManager>();
        if (!_manager.isCompleteTutorial)
            gameObject.SetActive(true);
        else
        {
            gameObject.SetActive(false);
            
        }
    }

    public void CompletTutorial()
    {
        _manager.isCompleteTutorial = true;
    }
}
