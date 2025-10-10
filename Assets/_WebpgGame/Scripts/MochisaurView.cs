using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class MochisaurView : MonoBehaviour
{
    public Transform prefabParent;
    public GameObject mochisaurPrefab;
    public List<MochisaurSlot> instancedMochisaurSlots = new List<MochisaurSlot>();

    private void OnEnable()
    {
        Backend.singleton.GetUserData(Backend.singleton.playerProfile.serial, ShowData);
        //Update mochisaurs using player data
    }

    public void ShowData(Backend.Response response)
    {
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
