using System.Collections.Generic;
using UnityEngine;

public class PlayerMochi : MonoBehaviour
{
    public List<GameObject> MochisList = new List<GameObject>();
    public string mochiIndexPref;
    public bool isPlayerMochi;
    public bool forceRandomizeMochi;

    public bool autoUpdateVisual = true;
    public SwimmingMinigameController swimmingMinigameController;
    private void OnEnable()
    {
        if (autoUpdateVisual)
        {
            UpdateVisual();
        }

        Backend.OnPlayerProfileUpdate += UpdateVisual;
    }

    private void OnDestroy()
    {
        Backend.OnPlayerProfileUpdate -= UpdateVisual;
    }

    public void UpdateVisual()
    {
        if (forceRandomizeMochi)
        {
            int randomIndex = Random.Range(0, MochisList.Count);
            PlayerPrefs.SetInt(mochiIndexPref, randomIndex);
        }

        foreach (var mochi in MochisList)
        {
            mochi.SetActive(false);
        }
        if (isPlayerMochi)
        {
            int playerMochiIndex;
            if (Backend.singleton == null)
            {
                playerMochiIndex = 0;
            }
            else
            {
                playerMochiIndex = Backend.singleton.playerProfile.activeMochiIndex;
            }
            MochisList[playerMochiIndex].gameObject.SetActive(true);
            if (swimmingMinigameController != null)
                swimmingMinigameController.SwimmingPlayerUI.ChangeImage(Backend.singleton.MochiDB[playerMochiIndex].image);
        }
        else
        {
            if (mochiIndexPref != "" && !string.IsNullOrEmpty(mochiIndexPref))
            {
                int index = PlayerPrefs.GetInt(mochiIndexPref, 0);
                MochisList[index].gameObject.SetActive(true);
                if (swimmingMinigameController != null)
                    swimmingMinigameController.SwimmingPlayerUI.ChangeImage(Backend.singleton.MochiDB[index].image);
            }

            List<MochiProp> mochiProps = new List<MochiProp>(GetComponentsInChildren<MochiProp>(true));
            foreach (var prop in mochiProps)
            {
                prop.gameObject.SetActive(false);
            }
        }
    }
}
