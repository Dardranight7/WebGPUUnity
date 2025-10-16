using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameResume : MonoBehaviour
{
    public PlayerMochi playerMochi;
    public TextMeshProUGUI pointsText, gemsText, bigGemsText;
    public Transform tournamentParent, tournamentWinnerParent ,winnerParent, looseParent;
    List<int> Indexes = new List<int>();
    private void OnEnable()
    {
        Indexes = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("WinnerYogoNado"));
        if (PlayerPrefs.GetInt("Tournament", 0) > 0)
        {
            string pointsString = PlayerPrefs.GetString("TournamentPoints");
            TournamentPoints tournamentPoints;
            if (!string.IsNullOrEmpty(pointsString) && !(PlayerPrefs.GetInt("TournamentRonda", 1) == 1))
            {
                tournamentPoints = JsonConvert.DeserializeObject<TournamentPoints>(pointsString);
            }
            else
            {
                tournamentPoints = new TournamentPoints();
                for (int i = 0; i < 4; i++)
                {
                    int charIndex = i;
                    tournamentPoints.characters.Add(new TournamentCharacter()
                    {
                        index = charIndex,
                        points = 0
                    });
                }
            }
            
            //Give points
            foreach (var character in tournamentPoints.characters)
            {
                int value = 0;
                gemsText.text = "0";
                //winner
                if (character.index == Indexes[3])
                {
                    character.points += 10;
                    value = 10;
                }
                else if (character.index == Indexes[2])
                {
                    character.points += 6;
                    value = 6;
                }
                else if (character.index == Indexes[1])
                {
                    character.points += 3;
                    value = 3;
                }
                else if (character.index == Indexes[0])
                {
                    character.points += 0;
                    value = 0;
                }
                if (character.index == 0)
                {
                    pointsText.text = value.ToString();
                }
            }

            
            PlayerPrefs.SetString("TournamentPoints", JsonConvert.SerializeObject(tournamentPoints));
            //is tournament
            tournamentParent.gameObject.SetActive(true);
            if (Indexes[3] == 0)
            {
                tournamentWinnerParent.gameObject.SetActive(true);    
            }
            else
            {
                tournamentWinnerParent.gameObject.SetActive(false);
            }

            //Gems reward at last game of tournament
            if (PlayerPrefs.GetInt("TournamentRonda", 1) == 2)
            {
                int position = tournamentPoints.characters.OrderByDescending(b => b.points).ToList().IndexOf(tournamentPoints.characters[0]);
                int mochiPoints = 0;
                int points = 0;
                bool givePoints = false;
                if (position == 0)
                {
                    points = 12;
                    givePoints = true;
                    gemsText.text = "12";
                    mochiPoints = 20;
                }
                else if (position == 1)
                {
                    points = 5;
                    givePoints = true;
                    gemsText.text = "5";
                    mochiPoints = 10;
                }
                else if (position == 2)
                {
                    points = 2;
                    givePoints = true;
                    gemsText.text = "2";
                    mochiPoints = 0;
                }
                else
                {
                    gemsText.text = "0";
                    mochiPoints = -10;
                }
                if (givePoints)
                {
                    Backend.singleton.UpdateData(new UpdateGems { serial = Backend.singleton.playerProfile.serial, gems = Backend.singleton.playerProfile.gems + points, mochiPoints = Mathf.Clamp(Backend.singleton.playerProfile.mochiPoints + mochiPoints,0,int.MaxValue) }, (a) =>
                    {
                        Debug.Log("Gems updated");
                        if (a.code == 0)
                        {
                            Backend.singleton.playerProfile.gems = Backend.singleton.playerProfile.gems + 6;
                        }
                    });
                }
            }
        }
        else
        {
            tournamentParent.gameObject.SetActive(false);
            if (Indexes[0] == 0)
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
        if (Indexes[0] == 0)
        {
            //Player
            playerMochi.isPlayerMochi = true;
        }
        else
        {
            //Bots
            playerMochi.isPlayerMochi = false;
            playerMochi.mochiIndexPref = "mochiBot" + Indexes[0].ToString(); 
        }
        playerMochi.UpdateVisual();
    }

    class UpdateGems
    {
        public string serial;
        public int gems;
        public int mochiPoints;
    }

    [System.Serializable]
    class TournamentPoints
    {
        public List<TournamentCharacter> characters = new List<TournamentCharacter>();
    }

    [System.Serializable]
    class TournamentCharacter
    {
        public int index;
        public int points;
    }
}
