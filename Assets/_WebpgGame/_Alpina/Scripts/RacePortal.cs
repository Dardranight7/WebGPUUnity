using UnityEngine;

public class RacePortal : MonoBehaviour
{
    public int Index;

    private void OnTriggerEnter(Collider other)
    {
        Competitor competitor = other.GetComponent<Competitor>();
        competitor.portalIndex = Index;
        competitor.ownPortal = this;
    }
}
