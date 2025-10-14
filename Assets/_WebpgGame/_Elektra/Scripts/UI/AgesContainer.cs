using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgesContainer : MonoBehaviour
{
    public string sceneAge;
    public int numberActivity;
    public Image background;
    public GameObject signAlert;
    public StatusScene statusScene;
    public Sprite desactive;
    public TMP_Text text;
    public Sprite alert;
    public Sprite select;
    public Sprite finish;
    public GameObject completeAge;
    private void Awake()
    {
        numberActivity = ElektraManager.Instance.GetActivityCompleted(sceneAge);
        if (SceneManager.GetActiveScene().name != sceneAge)
        {
            if (numberActivity == 0)
            {
                ImageStatus(statusScene = StatusScene.DESACTIVE);
                return;
            }
            if (numberActivity > 0 && numberActivity < 25 )
            {
                ImageStatus(statusScene = StatusScene.ALERT);
                return;
            }

            if (numberActivity == 25)
            {
                ImageStatus(statusScene = StatusScene.FINISH);
                return;
            }

        }
        statusScene = StatusScene.SELECT;
        ImageStatus(statusScene);
    }
    
    private void OnEnable()
    {
        ElektraManager.Instance.OnCountCurrent += UpdateProgress;
    }
    private void OnDisable()
    {
        ElektraManager.Instance.OnCountCurrent -= UpdateProgress;
    }

    private void UpdateProgress(string scene, int progress)
    {
        Debug.Log($"{sceneAge} {scene}");
        numberActivity = ElektraManager.Instance.GetActivityCompleted(scene);
        if (numberActivity == 25 && scene == sceneAge)
        {
            ImageStatus(statusScene = StatusScene.FINISH);
            ImageStatus(statusScene);
            completeAge.SetActive(true);
            Invoke( nameof(DesactiveCompleteAge), 5f);
            Invoke("LoadLobby", 2f);
            
        }
    }

    private void DesactiveCompleteAge()
    {
        completeAge.SetActive(false);
        
    }
    
    void LoadLobby()
    {
        SceneManager.LoadScene("Lobby");
    }


    private void ImageStatus(StatusScene status)
    {
        switch (status)
        {
            case StatusScene.SELECT:
                background.sprite = select;
                text.color = new Color(1f, 0f, 0f);
                signAlert.SetActive(false);
                break;
            case StatusScene.DESACTIVE:
                background.sprite = desactive;
                text.color = new Color(0.49f, 0.49f, 0.49f);
                signAlert.SetActive(false);
                break;
            case StatusScene.ALERT:
                background.sprite = alert;
                text.color = new Color(0.9f, 0.8f, 0.2f);
                signAlert.SetActive(true);
                break;
            case StatusScene.FINISH:
                signAlert.SetActive(false);
                text.color = new Color(0.6f, 0.13f, 0.11f);
                background.sprite = finish;
                break;

        }
    }

}

public enum StatusScene
{
    DESACTIVE,
    ALERT,
    SELECT,
    FINISH,
}
