using UnityEngine;

public class GoToLoginAgain : MonoBehaviour
{
    public void GoToLobby()
    {
        MochiCourtain.Singleton.LoadSceneWithCourtain("Core",1);
        AuthManager.OnNeedToShowLogin?.Invoke();
    }
}
