using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class TournamentRankin : MonoBehaviour
{
    [Header("UI References")]
    public Transform slotParent;
    public GameObject slotPrefb;
    public int numberOfRanks;

    // 🔹 Lista para mantener los slots instanciados (pool)
    private readonly List<RankingSlot> slotPool = new();

    private void OnEnable()
    {
        UpdateRankingAroundPlayer();
    }

    public void UpdateRankingAroundPlayer()
    {
        Backend.singleton.GetRanking(new GetLocalRankingObject
        {
            serial = Backend.singleton.playerProfile.serial,
            numberOfRanks = numberOfRanks
        }, (a) =>
        {
            FillRanking(a.data);
        });
    }

    public void UpdateGlobalRanking()
    {
        Backend.singleton.GetRanking(new GetRankingObject
        {
            numberOfRanks = numberOfRanks
        }, (a) =>
        {
            FillRanking(a.data);
        });
    }

    public void FillRanking(string dataJson)
    {
        if (string.IsNullOrEmpty(dataJson))
        {
            Debug.LogWarning("⚠️ FillRanking: dataJson vacío o nulo");
            return;
        }

        List<RankingData> rankingData;
        try
        {
            rankingData = JsonConvert.DeserializeObject<List<RankingData>>(dataJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error deserializando ranking JSON: " + e);
            return;
        }

        // 1️⃣ Desactivar todos los slots actuales
        foreach (var slot in slotPool)
            slot.gameObject.SetActive(false);

        // 2️⃣ Asegurar que haya suficientes slots en el pool
        for (int i = slotPool.Count; i < rankingData.Count; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefb, slotParent);
            RankingSlot newSlot = newSlotObj.GetComponent<RankingSlot>();
            slotPool.Add(newSlot);
        }

        // 3️⃣ Rellenar con los datos
        for (int i = 0; i < rankingData.Count; i++)
        {
            RankingSlot slot = slotPool[i];
            RankingData data = rankingData[i];

            slot.SetValues(
                data.userName.ToString(),
                data.mochiPoints.ToString(),
                data.position.ToString()
            );

            slot.gameObject.SetActive(true);
        }

        // (opcional) ajustar tamaño del contenedor o scroll si es necesario
    }

    // ----------- Clases auxiliares -----------
    [System.Serializable]
    public class GetLocalRankingObject
    {
        public string serial;
        public int numberOfRanks;
    }

    [System.Serializable]
    public class GetRankingObject
    {
        public int numberOfRanks;
    }

    [System.Serializable]
    public class RankingData
    {
        public string serial;
        public string nombre;
        public string apellido;
        public string userName;
        public int mochiPoints;
        public int position;
    }
}
