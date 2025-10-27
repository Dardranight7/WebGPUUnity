using UnityEngine;

public class SimpleOpenURL : MonoBehaviour
{
    public string URL;
    public void OpenURL()
    {
        Application.OpenURL(URL);
    }
}
