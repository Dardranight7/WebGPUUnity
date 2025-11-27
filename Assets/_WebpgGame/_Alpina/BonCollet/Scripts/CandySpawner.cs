using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    
    [Header("Velocidad de caida")]
    public float fallSpeed = 3f;

    private float[] lastSpawnTimePerPrefab;

    public void Awake()
    {
        if (candyPrefabs == null) candyPrefabs = new GameObject[0];
        lastSpawnTimePerPrefab = new float[candyPrefabs.Length];
        for (int i = 0; i < lastSpawnTimePerPrefab.Length; i++)
        {
            lastSpawnTimePerPrefab[i] = -Mathf.Infinity;
        }
    }

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
                int chosenIndex = PickAvailablePrefabIndex();
                
                if (chosenIndex != -1)
                {
                    // spawn y usar el override de ese prefab para decidir el siguiente tiempo de espera
                     SpawnPrefabAtIndex(chosenIndex);
                }
                else
                {
                    // Si no hay disponibles (todos en cooldown), intentar elegir entre prefabs "comunes" (spawnCooldown == 0)
                    int commonIndex = PickCommonPrefabIndex();
                    if (commonIndex != -1)
                        SpawnPrefabAtIndex(commonIndex);
                    // si tampoco hay comunes, simplemente no spawneamos en este tick
                }
                //SpawnRandomCandy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    int PickAvailablePrefabIndex()
    {
        int prefabCount = candyPrefabs.Length;
        if (prefabCount == 0) return -1;

        List<int> available = new List<int>(prefabCount);
        for (int i = 0; i < prefabCount; i++)
        {
            var prefab = candyPrefabs[i];
            if (prefab == null) continue;
            var candyComp = prefab.GetComponent<Candy>();
            float cooldown = (candyComp != null) ? candyComp.spawnCooldown : 0f;
            float lastTime = (i < lastSpawnTimePerPrefab.Length) ? lastSpawnTimePerPrefab[i] : -9999f;
            if (Time.time - lastTime >= cooldown)
                available.Add(i);
        }

        if (available.Count == 0) return -1;
        return available[Random.Range(0, available.Count)];
    }
    
    // Elige un índice de prefab con spawnCooldown == 0 (comunes)
    int PickCommonPrefabIndex()
    {
        int prefabCount = candyPrefabs.Length;
        if (prefabCount == 0) return -1;

        List<int> commons = new List<int>(prefabCount);
        for (int i = 0; i < prefabCount; i++)
        {
            var prefab = candyPrefabs[i];
            if (prefab == null) continue;
            var candyComp = prefab.GetComponent<Candy>();
            float cooldown = (candyComp != null) ? candyComp.spawnCooldown : 0f;
            if (cooldown <= 0f)
                commons.Add(i);
        }

        if (commons.Count == 0) return -1;
        return commons[Random.Range(0, commons.Count)];
    }
    
    void SpawnPrefabAtIndex(int chosenIndex)
    {
        if (chosenIndex < 0 || chosenIndex >= candyPrefabs.Length) return;
        var chosenPrefab = candyPrefabs[chosenIndex];
        if (chosenPrefab == null) return;

        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + new Vector3(r.x, spawnHeight, r.y);

        var go = Instantiate(chosenPrefab, pos, Quaternion.identity);
        activeCandies.Add(go);

        var candy = go.GetComponent<Candy>();
        if (candy != null)
        {
            candy.fallSpeed = fallSpeed;
        }

        // registrar la hora de spawn para este prefab (evita re-spawnear antes del cooldown)
        if (chosenIndex >= 0 && chosenIndex < lastSpawnTimePerPrefab.Length)
            lastSpawnTimePerPrefab[chosenIndex] = Time.time;
    }
    
    // método público para ajustar intervalo en tiempo de ejecución
    public void SetSpawnInterval(float seconds)
    {
        spawnInterval = Mathf.Max(0.01f, seconds);
    }

    // método para actualizar la lista de prefabs (por si cambian en runtime)
    public void RefreshPrefabList()
    {
        lastSpawnTimePerPrefab = new float[candyPrefabs.Length];
        for (int i = 0; i < lastSpawnTimePerPrefab.Length; i++) lastSpawnTimePerPrefab[i] = -9999f;
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
        
        //velocidad de caida
        var candy = go.GetComponent<Candy>();
        if (candy != null) candy.fallSpeed = fallSpeed;
    }
}