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

    public AccessoryType accessoryType; 
    public Image typeIconImage;        // La imagen del icono de tipo (cabeza, pie, etc.)
    public GameObject selectionCheck;  // El GameObject del check de selección (el icono superior derecho)
    
    // Setup con el typeIcon en reemplazo de Update Visual
    public void SetupSlot(Sprite bg, int itemIndex, AccessoryType type, Sprite typeIcon)
    {
        this.index = itemIndex; 
        this.accessoryType = type; 
        BG.sprite = bg;

        // Actualiza el icono de tipo
        typeIconImage.sprite = typeIcon;
        typeIconImage.gameObject.SetActive(true); 
    
        // Obtener datos de ModelView
        ModelView itemData = collection.Find(m => m.index == itemIndex);
    
        // Lógica de compra (sin cambios)
        bool isBought = Backend.singleton.playerProfile.userInventory.Contains(itemIndex);
        cost.gameObject.SetActive(!isBought);
        bought.gameObject.SetActive(isBought);
    
        if(itemData != null)
        {
            costText.text = itemData.cost.ToString();
            itemData.model.gameObject.SetActive(true);
        }
    
        // Establecer el estado inicial de la selección
        UpdateSelectionVisual();
    }
    // Función llamada para actualizar el check de selección
    public void UpdateSelectionVisual()
    {
        if (mochisaurView.equippedAccessoryIDs.TryGetValue(accessoryType, out int equippedID))
        {
            // El ítem está equipado si su ID coincide con el ID equipado para su tipo
            bool isEquipped = (equippedID == index);
            selectionCheck.SetActive(isEquipped); // Usa SetActive en el GameObject
        }
        else
        {
            // Si no hay entrada para este tipo, asumimos que no está equipado
            selectionCheck.SetActive(false);
        }
    }
    // Llamado cuando el usuario hace clic en el slot de UI
    public void SelectAccessory()
    {
        // 1. Verificar si está comprado
        if (!Backend.singleton.playerProfile.userInventory.Contains(index))
        {
            TryToBuy(); // Llama a la lógica de compra existente
            return;
        }

        // 2. Si ya está comprado, equiparlo (o desequiparlo si ya estaba seleccionado)
        mochisaurView.EquipAccessory(index, accessoryType);
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
