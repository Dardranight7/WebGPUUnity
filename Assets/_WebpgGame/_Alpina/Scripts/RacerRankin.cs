
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RacerRankin : MonoBehaviour
{
    public List<Competitor> competitors = new List<Competitor>();

    private void Update()
    {
        UpdateRankings();
    }

    public void UpdateRankings()
    {
        List<Competitor> sortByPortal = competitors
        .OrderByDescending(c => c.portalIndex)
        .ThenByDescending(c => c.sqrDistanceToOwnPortal)
        .ToList();

        for (int i = 0; i < sortByPortal.Count; i++)
            sortByPortal[i].avatar.transform.SetSiblingIndex(i);
    }
}
