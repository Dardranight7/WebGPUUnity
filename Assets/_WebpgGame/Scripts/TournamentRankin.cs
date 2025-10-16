using UnityEngine;

public class TournamentRankin : MonoBehaviour
{
    public int numberOfRanks;

    public void UpdateRankingAroundPlayer()
    {
        Backend.singleton.GetRanking(new GetLocalRankingObject
        {
            serial = Backend.singleton.playerProfile.serial,
            numberOfRanks = numberOfRanks
        }, (a) =>
        {

        });
    }

    public void UpdateGlobalRanking()
    {
        Backend.singleton.GetRanking(new GetRankingObject
        {
            numberOfRanks = numberOfRanks
        }, (a) =>
        {

        });
    }

    public class GetLocalRankingObject
    {
        public string serial;
        public int numberOfRanks;
    }

    public class GetRankingObject
    {
        public int numberOfRanks;
    }
}
