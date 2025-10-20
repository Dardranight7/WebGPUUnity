using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccesorySlot : MonoBehaviour
{
    public Image BG;
    public List<ModelView> collection = new List<ModelView>();
    public TextMeshProUGUI costText;
    public int index = 0;
    public Transform cost, bought;
    public MochisaurView mochisaurView;

    public void UpdateVisual(Sprite bg, int index)
    {
        foreach (var item in collection)
        {
            item.model.gameObject.SetActive(item.index == index);
        }
        this.index = index;
        if (Backend.singleton.playerProfile.userInventory.Contains(index))
        {
            cost.gameObject.SetActive(false);
            bought.gameObject.SetActive(true);
        }
        else
        {
            cost.gameObject.SetActive(true);
            bought.gameObject.SetActive(false);
        }
        costText.text = collection[index].cost.ToString();
    }

    public void TryToBuy()
    {
        if (Backend.singleton.playerProfile.gems / 12 >= collection[index].cost)
        {
            //se puede comprar
            mochisaurView.selectedSlot = this;
            mochisaurView.popupBuy.gameObject.SetActive(true);
        }
        else
        {
            mochisaurView.popupNoFounds.gameObject.SetActive(true);
        }
    }

    public void Unlock()
    {
    List<int> inventory = new List<int>();
        
        inventory = Backend.singleton.playerProfile.userInventory;
        //compra
        if (!(Backend.singleton.playerProfile.gems / 12 >= collection[index].cost))
        {
            // No se puede comprar
            return;   
        }

        inventory.Add(index);
        Backend.singleton.UpdateData(new UpdateInventory
        {
            serial = Backend.singleton.playerProfile.serial,
            userInventory = inventory
        }, (a) =>
        {
            if (a.code == 0)
            {
                Backend.singleton.playerProfile.userInventory = inventory;
                Backend.singleton.UpdateData(new UpdateGems
                {
                    serial = Backend.singleton.playerProfile.serial,
                    gems = Backend.singleton.playerProfile.gems - collection[index].cost * 12
                }, (b =>
                {
                    if (b.code == 0)
                    {
                        Backend.singleton.playerProfile.gems = Backend.singleton.playerProfile.gems - collection[index].cost;
                        mochisaurView.popupBuy.gameObject.SetActive(false);
                        Backend.singleton.GetUserData(Backend.singleton.Serial, (c) =>
                        {
                            Backend.PlayerProfileDTO datos = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(c.data);
                            Backend.singleton.playerProfile = datos;
                            Backend.OnPlayerProfileUpdate?.Invoke();
                        });
                    }
                    else
                    {
                        Debug.Log(b);
                    }
                }));
            }
            else
            {
                Debug.Log(a);
            }
        });
    }

    class UpdateInventory
    {
        public string serial;
        public List<int> userInventory;
    }

    class UpdateGems
    {
        public string serial;
        public int gems;
    }

    [System.Serializable]
    public class ModelView
    {
        public int index;
        public Transform model;
        public int cost;
    }
}
