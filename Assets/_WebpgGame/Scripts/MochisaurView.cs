using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class MochisaurView : MonoBehaviour
{
    public Transform prefabParent;
    public GameObject mochisaurPrefab;
    public List<MochisaurSlot> instancedMochisaurSlots = new List<MochisaurSlot>();

    public GameObject accesoryPrefab;
    public List<AccesorySlot> instancedAccesorySlots = new List<AccesorySlot>();

    public Transform popupNoFounds, popupBuy;

    public AccesorySlot selectedSlot;
    public List<AccesoryDatabase> accessoryDatabase = new List<AccesoryDatabase>();
    public Dictionary<AccessoryType, int> equippedAccessoryIDs = new Dictionary<AccessoryType, int>();
    
    [System.Serializable]
    public class AccesoryDatabase
    {
        public int index; // El ID único que usas en el inventario
        public AccessoryType type; //Para filtrar por pestaña
        // Otros datos que puedas necesitar, como la referencia al prefab/sprite.
        public Sprite typeIcon;
    }
    private void OnEnable()
    {
        //Update mochisaurs using player data
        Reload();
    }

    public void BuySelected()
    {
        selectedSlot.Unlock();
    }

    public void Reload()
    {
        Backend.singleton.GetUserData(Backend.singleton.playerProfile.serial, ShowData);
    }

    public void OpenApp()
    {
        Application.OpenURL("https://tekitechar.8thwall.app/dinomochis");
    }

    [SerializeField] Sprite haveAccesory, dontHave;

    public void ShowAccesories()
    {
        // 1. Ocultar todos los slots instanciados
        foreach (var item in instancedMochisaurSlots)
        {
            item.gameObject.SetActive(false);
        }
        foreach (var item in instancedAccesorySlots)
        {
            item.gameObject.SetActive(false);
        }

        // 2. Mostrar/Instanciar todos los accesorios de la base de datos
        for (int i = 0; i < accessoryDatabase.Count; i++)
        {
            var data = accessoryDatabase[i];
            AccesorySlot selectedSlot;
        
            // Reutilización o instanciación del slot de UI
            if (i < instancedAccesorySlots.Count)
            {
                selectedSlot = instancedAccesorySlots[i];
                selectedSlot.gameObject.SetActive(true);
            }
            else
            {
                selectedSlot = Instantiate(accesoryPrefab, prefabParent).GetComponent<AccesorySlot>();
                selectedSlot.mochisaurView = this;
                instancedAccesorySlots.Add(selectedSlot);
            }
        
            // Determinar si está comprado
            Sprite bgSprite = Backend.singleton.playerProfile.userInventory.Contains(data.index) ? haveAccesory : dontHave;
            
            selectedSlot.SetupSlot(bgSprite, data.index, data.type, data.typeIcon);
        }
    }
    // Función central para equipar un ítem, llamada por AccesorySlot
    public void EquipAccessory(int itemID, AccessoryType type)
    {
        // Opción para desequipar: Si el ítem seleccionado es el que ya está equipado, lo desequipamos.
        if (equippedAccessoryIDs.ContainsKey(type) && equippedAccessoryIDs[type] == itemID)
        {
            equippedAccessoryIDs[type] = -1; // -1 significa "nada equipado"
        }
        else
        {
            // Equipar el nuevo ítem, reemplazando el anterior del mismo tipo
            equippedAccessoryIDs[type] = itemID;
        }
    
        // Aplicar el cambio al modelo del Mochisaurio (Lógica de instanciar/destruir prefabs)
        // UpdateMochisaurVisuals(type, equippedAccessoryIDs[type]); 
    
        // Forzar la actualización visual de TODOS los slots
        UpdateAllAccessorySlotVisuals(); 
        SendEquippedStateToBackend();
    }
    public void SendEquippedStateToBackend()
    {
        // 1. Filtrar los IDs válidos del diccionario
        List<int> currentlyEquippedIDs = new List<int>();
    
        foreach (var kvp in equippedAccessoryIDs)
        {
            // Solo incluimos IDs que NO sean -1 (el valor de "nada equipado")
            if (kvp.Value != -1)
            {
                currentlyEquippedIDs.Add(kvp.Value);
            }
        }
    
        // 2. Crear el DTO con los datos del usuario.
        UpdateEquippedAccessories updateData = new UpdateEquippedAccessories
        {
            serial = Backend.singleton.playerProfile.serial,
            userEquip = currentlyEquippedIDs // Enviamos la lista simple de IDs
        };

        // 3. Enviar al Backend.
        Backend.singleton.UpdateData(updateData, (response) =>
        {
            if (response.code == 0)
            {
                Debug.Log($"Equipped accessories state updated successfully on Backend. Sent {currentlyEquippedIDs.Count} items.");
            }
            else
            {
                Debug.LogError("Error updating equipped accessories on Backend: " + response.data);
            }
        });
    }
    // Llama a la actualización visual en todos los slots instanciados.
    public void UpdateAllAccessorySlotVisuals()
    {
        foreach (var slot in instancedAccesorySlots)
        {
            if(slot.gameObject.activeInHierarchy)
                slot.UpdateSelectionVisual(); 
        }
    }

    public void ShowData(Backend.Response response)
    {
        foreach (var item in instancedAccesorySlots)
        {
            item.gameObject.SetActive(false);
        }
        if (response.code == 0)
        {
            Backend.PlayerProfileDTO datos = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(response.data);
            Backend.singleton.playerProfile = datos;

            foreach (var mochi in instancedMochisaurSlots)
            {
                mochi.gameObject.SetActive(false);
            }

            List<int> indexes = JsonConvert.DeserializeObject<List<int>>(datos.unlockedMochis);

            for (int i = 0; i < indexes.Count; i++)
            {
                int mochiIndex = indexes[i];
                MochisaurSlot selectedSlot;
                if (i < instancedMochisaurSlots.Count)
                {
                    selectedSlot = instancedMochisaurSlots[i];
                    instancedMochisaurSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    MochisaurSlot instance = Instantiate(mochisaurPrefab, prefabParent).GetComponent<MochisaurSlot>();
                    instancedMochisaurSlots.Add(instance);
                    selectedSlot = instance;
                }
                selectedSlot.UpdateVisual(Backend.singleton.MochiDB[mochiIndex].image, Backend.singleton.MochiDB[mochiIndex].name, mochiIndex);
            }
        }
    }
}
public enum AccessoryType {
    Head,
    Body,
    Feet
}