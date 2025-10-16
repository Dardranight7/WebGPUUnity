
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
     public void LoadSceneWithString(string toScene)
     {
          SceneManager.LoadScene(toScene);
     }
}
