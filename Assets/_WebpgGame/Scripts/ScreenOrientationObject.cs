using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenOrientationObject : MonoBehaviour
{
    public GameObject OrientationImage;

    void Update()
    {
        CheckScreenRatio();
    }

    void CheckScreenRatio()
    {
        if (Screen.height > Screen.width) 
        {
            OrientationImage.SetActive(true); 
        }
        else 
        {
            OrientationImage.SetActive(false); 
        }
    }
}
