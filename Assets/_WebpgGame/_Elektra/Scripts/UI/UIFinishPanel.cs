using UnityEngine;

public class UIFinishPanel : MonoBehaviour
{
    public string idScene;

    public GameObject panel;
    public GameObject popUpIncomple;
    public GameObject popUpExit;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ValidateProgressAges()
    {
        panel.SetActive(true);
        if (ElektraManager.Instance.ValidateProgress(out string result, idScene)) 
        {
            popUpExit.SetActive(true);
            popUpIncomple.SetActive(false);
            Debug.Log(result);
        }
        else if (!string.IsNullOrEmpty(result))
        {
            popUpExit.SetActive(false);
            popUpIncomple.SetActive(true);
            Debug.Log(result);
        }
    }
    
    
}
