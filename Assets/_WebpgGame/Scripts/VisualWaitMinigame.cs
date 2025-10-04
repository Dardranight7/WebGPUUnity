using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class VisualWaitMinigame : MonoBehaviour
{
    [System.Serializable]
    public class Minigames
    {
        public string name;
        public MinigameID minigameID;
        [TextArea]
        public string _TittleMinigame, _LoadingScene, _DinamicText, _WinText;
    }

    [SerializeField] private TextMeshProUGUI TittleMinigame, LoadingScene, DinamicText, WinText;

    public Minigames[] options;
    [SerializeField] private int currentIndex = 0;



    public void ChangeVisualMinigame(MinigameID minigameID)
    {
        switch(minigameID)
        {
            case MinigameID.Yogorush:
                AssignText();
                break;
            case MinigameID.Boggyjump:
                AssignText();
                break;
            case MinigameID.ALPISLIDE:
                AssignText();
                break;
            case MinigameID.BONCOLLECT:
                AssignText();
                break;
            default:
                AssignText();
                break;
        }
    }

     void AssignText()
    {
        var opt = options[currentIndex];

        TittleMinigame.text = opt._TittleMinigame;
        LoadingScene.text = opt._LoadingScene;
        DinamicText.text = opt._DinamicText;
        WinText.text = opt._WinText;
    }
}


