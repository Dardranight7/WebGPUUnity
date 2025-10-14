using System;
using UnityEngine;
using UnityEngine.UI;

public class IsWebOrMovilController : MonoBehaviour
{

    public GameObject LeftStick;
    public GameObject RightStick;
    
    private void Start()
    {
        
        //if (isWebSearch())
        //{
        //    LeftStick.SetActive(false);
        //    RightStick.SetActive(false);            
            
        //}
        //else
        //{
        //    LeftStick.SetActive(true);
        //    RightStick.SetActive(true);
        //}
    }

    bool isWebSearch()
    {
        return Application.platform == RuntimePlatform.WebGLPlayer;
    }
    
    
}
