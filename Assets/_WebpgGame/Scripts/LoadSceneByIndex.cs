using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneByIndex : MonoBehaviour
{
    public int index;
    
    public void LoadScene()
    {
        SceneManager.LoadScene(index);
    }
}
