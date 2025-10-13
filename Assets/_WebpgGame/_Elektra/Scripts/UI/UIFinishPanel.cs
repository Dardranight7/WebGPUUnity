using UnityEngine;

public class UIFinishPanel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ElektraManager.Instance.ValidateAllProgress(out string result)) 
        {
            Debug.Log(result);
        }
        else if (!string.IsNullOrEmpty(result))
        {
            Debug.Log(result);
        }
    }
    
}
