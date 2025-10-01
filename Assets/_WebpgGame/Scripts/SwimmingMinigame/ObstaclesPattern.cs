
using System.Collections.Generic;
using UnityEngine;

public class ObstaclesPattern : MonoBehaviour
{
    public int NumberOfObstaclesToBeDisabled;
    List<GameObject> Obstacles = new List<GameObject>();


    private void Awake()
    {
        foreach (Transform child in transform)
        {
            Obstacles.Add(child.gameObject);
        }
    }

    public void OnEnable()
    {
        // disable NumberOfObstaclesToBeDisabled random obstacles
        List<int> disabledIndices = new List<int>();
        while (disabledIndices.Count < NumberOfObstaclesToBeDisabled)
        {
            int randomIndex = Random.Range(0, Obstacles.Count);
            if (!disabledIndices.Contains(randomIndex))
            {
                disabledIndices.Add(randomIndex);
                Obstacles[randomIndex].SetActive(false);
            }
        }
    }
}
