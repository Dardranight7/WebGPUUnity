using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneByIndex : MonoBehaviour
{
    public string sceneName;
    
    public void LoadScene()
    {
        MochiCourtain.Singleton.LoadSceneWithCourtain(sceneName,1);
    }
}
