using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMochi : MonoBehaviour
{
    public List<GameObject> MochisList = new List<GameObject>();
    public string mochiIndexPref;
    public bool isPlayerMochi;
    public bool forceRandomizeMochi;

    public bool autoUpdateVisual = true;
    public SwimmingMinigameController swimmingMinigameController;
    [SerializeField] private Image mochiSprite;
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
        List<MochiProp> mochiProps = new List<MochiProp>(GetComponentsInChildren<MochiProp>(true));
        List<int> propMochiIndex = new List<int>();
        
        if (forceRandomizeMochi)
        {
            int randomIndex = Random.Range(0, MochisList.Count);
            propMochiIndex.Add(Random.Range(0,mochiProps.Count));
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
                propMochiIndex = Backend.singleton.playerProfile.userEquip;
            }
            MochisList[playerMochiIndex].gameObject.SetActive(true);
            if (swimmingMinigameController != null)
                swimmingMinigameController.SwimmingPlayerUI.ChangeImage(Backend.singleton.MochiDB[playerMochiIndex].image);
            if (mochiSprite != null)
            {
                mochiSprite.sprite = Backend.singleton.MochiDB[playerMochiIndex].image;
            }
        }
        else
        {
            if (mochiIndexPref != "" && !string.IsNullOrEmpty(mochiIndexPref))
            {
                int index = PlayerPrefs.GetInt(mochiIndexPref, 0);
                MochisList[index].gameObject.SetActive(true);
                if (swimmingMinigameController != null)
                    swimmingMinigameController.SwimmingPlayerUI.ChangeImage(Backend.singleton.MochiDB[index].image);
                if (mochiSprite != null)
                {
                    mochiSprite.sprite = Backend.singleton.MochiDB[index].image;
                }
            }
        }
        foreach (var prop in mochiProps)
        {
            int currentPropIndex = prop.GetComponent<MochiProp>().index;
            if(propMochiIndex.Contains(currentPropIndex))
                prop.gameObject.SetActive(true);
            else    
                prop.gameObject.SetActive(false);
        }
        
    }
}
