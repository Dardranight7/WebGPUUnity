using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GemsView : MonoBehaviour
{
    [SerializeField] List<TextMeshProUGUI> gems, littleGems;
    void Start()
    {
        Backend.OnPlayerProfileUpdate += UpdateVisual;
    }

    private void OnEnable()
    {
        UpdateVisual();
    }

    private void OnDestroy()
    {
        Backend.OnPlayerProfileUpdate -= UpdateVisual;
    }

    public void UpdateVisual()
    {
        int totalGems = Backend.singleton.playerProfile.gems;

        int fullPacks = totalGems / 12;
        int remainder = totalGems % 12;

        float littleGemsValue = 0;
        float gemsValue = 0;

        gemsValue = fullPacks;
        littleGemsValue = remainder;


        foreach (var userN in gems)
        {
            userN.text = gemsValue.ToString();
        }
        foreach (var userN in littleGems)
        {
            userN.text = littleGemsValue.ToString();
        }
    }
}
