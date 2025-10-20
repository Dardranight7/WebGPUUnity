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

    public List<AccesoryDatabase> accesoryDatabase = new List<AccesoryDatabase>();

    public Transform popupNoFounds, popupBuy;

    public AccesorySlot selectedSlot;

    [System.Serializable]
    public class AccesoryDatabase
    {
        public int index;
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
        foreach (var item in instancedMochisaurSlots)
        {
            item.gameObject.SetActive(false);
        }
        for (int i = 0; i < accesoryDatabase.Count; i++)
        {
            AccesorySlot selectedSlot;
            if (i < instancedAccesorySlots.Count)
            {
                selectedSlot = instancedAccesorySlots[i];
                instancedAccesorySlots[i].gameObject.SetActive(true);
            }
            else
            {
                AccesorySlot instance = Instantiate(accesoryPrefab, prefabParent).GetComponent<AccesorySlot>();
                instance.mochisaurView = this;
                instancedAccesorySlots.Add(instance);
                selectedSlot = instance;
            }
            selectedSlot.UpdateVisual(Backend.singleton.playerProfile.userInventory.Contains(accesoryDatabase[i].index) ? haveAccesory : dontHave, accesoryDatabase[i].index);
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
