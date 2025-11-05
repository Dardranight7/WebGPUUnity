using System.Collections.Generic;
using UnityEngine;

public class TournamentManager : MonoBehaviour
{
    public int maxTournamentGames = 2;
    public int tournamentIndex;
    public int currentTournamentRonda;
    public List<Tournament> tournaments = new List<Tournament>();
    bool isTournament;

    private void OnEnable()
    {
        if (PlayerPrefs.GetInt("Tournament", 0) == 0)
        {
            // no es un torneo
            isTournament = false;
        }
        else
        {
            // es un torneo
            isTournament = true;
            tournamentIndex = PlayerPrefs.GetInt("TournamentIndex", 0);
            currentTournamentRonda = PlayerPrefs.GetInt("TournamentRonda", 0);
        }
    }

    public void StartTournament()
    {
        Backend.singleton.UpdateData(new UpdateTournamentCount
        {
            serial = Backend.singleton.playerProfile.serial,
            tournamentPlayCount = Backend.singleton.playerProfile.tournamentPlayCount + 1
        }, (a) => 
        {
            Backend.singleton.playerProfile.tournamentPlayCount++;
        });
        PlayerPrefs.SetInt("Tournament", 1);
        int randomTournament = Random.Range(0, tournaments.Count);
        PlayerPrefs.SetInt("TournamentIndex", randomTournament);
        PlayerPrefs.SetInt("TournamentRonda", 1);
        MochiCourtain.Singleton.LoadSceneWithCourtain(tournaments[randomTournament].sceneNames[0],1);
    }

    public class UpdateTournamentCount
    {
        public string serial;
        public int tournamentPlayCount;
    }

    public void LoadNextGameUsingCourtain()
    {
        if (isTournament)
        {
            if (currentTournamentRonda >= tournaments[tournamentIndex].sceneNames.Count)
            {
                // torneo terminado
                PlayerPrefs.SetInt("Tournament", 0);
                PlayerPrefs.SetInt("TournamentIndex", 0);
                PlayerPrefs.SetInt("TournamentRonda", 0);
                MochiCourtain.Singleton.LoadSceneWithCourtain("0",1);
                return;
            }
            MochiCourtain.Singleton.LoadSceneWithCourtain(tournaments[tournamentIndex].sceneNames[currentTournamentRonda],1);
            PlayerPrefs.SetInt("TournamentRonda", currentTournamentRonda+1);
        }
        else
        {
            MochiCourtain.Singleton.LoadSceneWithCourtain("0",1);
        }
    }

    [System.Serializable]
    public class Tournament
    {
        public string tournamentName;
        public List<string> sceneNames = new List<string>();
    }
}
