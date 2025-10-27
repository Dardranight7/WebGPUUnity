using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public List<string> keys = new List<string>();
    private void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void NeedToShow(string value, TutorialPart tutorialPart)
    {
        if (keys.Contains(value))
        {
            tutorialPart.gameObject.SetActive(false);
        }
        else
        {
            keys.Add(value);
        }
    }

}
