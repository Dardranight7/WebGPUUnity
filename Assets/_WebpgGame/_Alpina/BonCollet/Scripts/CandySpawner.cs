using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandySpawner : MonoBehaviour
{
    [Header("Prefabs de gomitas (asignar prefabs)")]
    public GameObject[] candyPrefabs; // diferentes tipos de gomitas (configura sus puntos en el prefab)

    [Header("Spawn")]
    public float spawnInterval = 0.8f; // intervalo entre aparecimientos
    public float spawnHeight = 8f; // altura desde la que caen las gomitas
    public float spawnRadius = 4f; // radio alrededor del centro (transform.position)
    public int maxActive = 30; // máximo de gomitas activas en escena

    private bool spawning = false;
    private List<GameObject> activeCandies = new List<GameObject>();

    public void StartSpawning()
    {
        if (!spawning)
        {
            spawning = true;
            StartCoroutine(SpawnLoop());
        }
    }

    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
    }

    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            // controlar numero máximo de gomitas activas
            activeCandies.RemoveAll(item => item == null);
            if (activeCandies.Count < maxActive && candyPrefabs.Length > 0)
            {
                SpawnRandomCandy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnRandomCandy()
    {
        // elegir tipo aleatorio
        var prefab = candyPrefabs[Random.Range(0, candyPrefabs.Length)];

        // pos aleatoria dentro de un círculo XZ
        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + new Vector3(r.x, spawnHeight, r.y);

        var go = Instantiate(prefab, pos, Quaternion.identity);
        activeCandies.Add(go);
    }
}