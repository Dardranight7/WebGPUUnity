using UnityEngine;
using UnityEngine.UI;

public class Competitor : MonoBehaviour
{
    public Image avatar;
    public float portalIndex;
    public float sqrDistanceToOwnPortal;
    public RacePortal ownPortal;

    private void Update()
    {
        if (ownPortal != null)
            sqrDistanceToOwnPortal = (transform.position - ownPortal.transform.position).sqrMagnitude;
    }
}
