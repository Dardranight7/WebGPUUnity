using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OutroResume : MonoBehaviour
{
    void Start()
    {
        List<int> indexes = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("WinnerYogoNado"));
        if (indexes.Count > 1)
        {
            //Draw
        }
        else
        {
            //Victory of any player
            int index = indexes[0];
        }
    }

    public void ReturnToLobby()
    {
        MochiCourtain.Singleton.LoadSceneWithCourtain("0", 1);
    }
}
