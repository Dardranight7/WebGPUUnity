using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MochisaurSlot : MonoBehaviour
{
    public TextMeshProUGUI MochiName;
    public Image Image;
    public Button Btn;

    public void UpdateVisual(Sprite sprite, string name, int index)
    {
        Image.sprite = sprite;
        MochiName.text = name;
        Btn.onClick.RemoveAllListeners();
        Btn.onClick.AddListener(()=>ChangeSelected(index));
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
}
