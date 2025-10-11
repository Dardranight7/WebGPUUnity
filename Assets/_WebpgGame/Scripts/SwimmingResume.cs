using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SwimmingResume : MonoBehaviour
{
    public PlayerMochi playerMochi;
    public TextMeshProUGUI pointsText, gemsText, bigGemsText;
    public Transform tournamentParent, tournamentWinnerParent ,winnerParent, looseParent;
    List<int> winnerIndexes = new List<int>();
    private void OnEnable()
    {
        winnerIndexes = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("WinnerYogoNado"));
        if (PlayerPrefs.GetInt("Tournament", 0) > 0)
        {
            //is tournament
            tournamentParent.gameObject.SetActive(true);
            if (winnerIndexes[0] == 0)
            {
                tournamentWinnerParent.gameObject.SetActive(true);
                
                Backend.singleton.UpdateData(new UpdateGems { serial = Backend.singleton.playerProfile.serial ,gems = Backend.singleton.playerProfile.gems + 6 }, (a) =>
                {
                    Debug.Log("Gems updated");
                    if (a.code == 0)
                    {
                        Backend.singleton.playerProfile.gems = Backend.singleton.playerProfile.gems + 6;
                    }
                });
            }
            else
            {
                tournamentWinnerParent.gameObject.SetActive(false);
            }
            //Show points
        }
        else
        {
            tournamentParent.gameObject.SetActive(false);
            if (winnerIndexes[0] == 0)
            {
                winnerParent.gameObject.SetActive(true);
                looseParent.gameObject.SetActive(false);
            }
            else
            {
                looseParent.gameObject.SetActive(true);
                winnerParent.gameObject.SetActive(false);
            }
            //not is tournament
        }
        if (winnerIndexes[0] == 0)
        {
            //Player
            playerMochi.isPlayerMochi = true;
        }
        else
        {
            //Bots
            playerMochi.isPlayerMochi = false;
            playerMochi.mochiIndexPref = "mochiBot" + winnerIndexes[0].ToString(); 
        }
        playerMochi.UpdateVisual();
    }

    class UpdateGems
    {
        public string serial;
        public int gems;
    }
}
