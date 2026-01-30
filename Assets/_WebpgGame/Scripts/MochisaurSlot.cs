using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MochisaurSlot : MonoBehaviour
{
    public Button Btn;
    public MochisaurView mochisaurView;
    public TextMeshProUGUI MochiName;
    public Image Image;
    public TextMeshProUGUI costText;
    public Transform cost, bought;
    public int index = 0;
    
    public void UpdateVisual(Sprite sprite, string name, int index, bool isUnlocked)
    {
        this.index = index;
        Image.sprite = sprite;
        MochiName.text = name;
        Btn.onClick.RemoveAllListeners();
        bought.gameObject.SetActive(isUnlocked);
        cost.gameObject.SetActive(!isUnlocked);
        
        costText.gameObject.SetActive(!isUnlocked);
        costText.text = "50";
        if (isUnlocked)
            Btn.onClick.AddListener(()=>ChangeSelected(index));
        else
            Btn.onClick.AddListener(()=> TryToUnlock(index));
    }

    public void UnlockMochi()
    {
        cost.gameObject.SetActive(false);
        costText.gameObject.SetActive(false);
        bought.gameObject.SetActive(true);
    }

    public void ChangeSelected(int index)
    {
        Backend.singleton.UpdateData(new Backend.UpdateActiveMochiDTO()
        {
            serial = Backend.singleton.playerProfile.serial,
            activeMochiIndex = index
        }, (response) =>
        {
            if (response.code == 0)
            {
                Backend.singleton.playerProfile = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(response.data);
                Backend.OnPlayerProfileUpdate?.Invoke();
                Debug.Log("Selected Mochi updated to index: " + index);
                Debug.Log("Updated data");
            }
            else
            {
                Debug.LogError("Failed to update selected Mochi: " + response.message);
            }
        });
    }

    private void TryToUnlock(int id)
    {
        if (Backend.singleton.playerProfile.gems / 12 >= 50)
        {
            mochisaurView.selectedMochiSlot = this;
            mochisaurView.popupBuyMochi.gameObject.SetActive(true);
        }
        else
        {
            mochisaurView.popupNoFounds.gameObject.SetActive(true);
        }
    }
}
