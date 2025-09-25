using UnityEngine;
using System.Collections.Generic;


public class PlatformGenerator : MonoBehaviour
{
    public GameObject[] normalPlatformPrefabs;
    public GameObject[] breakablePlatformPrefabs;
    public GameObject[] fakePlatformPrefabs;
    public GameObject[] spikePlatformPrefabs;

    private List<GameObject> activePlatforms = new List<GameObject>();
    private Transform playerTransform;
    private float currentHeight = 0f;
    public float platformSpacing = 0.02f;

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        currentHeight = playerTransform != null ? playerTransform.position.y - 1.5f : 0f;
        GenerateInitialPlatforms();
    }

    // Genera las primeras plataformas con patrón Happy Hop
    private void GenerateInitialPlatforms()
    {
    float startY = currentHeight;
    int filas = 40;
    int plataformasPorFila = 20;
    float minX = -3.0f;
    float maxX = 3.0f;
    float minDist = 1.2f;

    for (int fila = 0; fila < filas; fila++)
    {
        float y = startY + fila * platformSpacing;
        float platformWidth = 1.0f; // Ajusta según el ancho real del prefab
        float totalWidth = plataformasPorFila * platformWidth + (plataformasPorFila - 1) * platformWidth;
        float startX = -(totalWidth / 2) + platformWidth / 2;
        float zigzagOffset = (fila % 2 == 0) ? 0 : platformWidth;
        for (int col = 0; col < plataformasPorFila; col++)
        {
            float x = startX + col * platformWidth * 2 + zigzagOffset;
            // No crear plataformas en Y=0 (suelo) para evitar colisión inicial
            if (fila == 0) continue;
            if (fila < 3) {
                // Primeras 3 filas: solo plataformas estáticas y normales, muchas para facilitar el inicio
                if (Random.value < 0.8f) {
                    PlatformType tipo = (Random.value < 0.5f) ? PlatformType.Static : PlatformType.Normal;
                    GameObject prefab = GetPlatformPrefab(tipo);
                    if (prefab != null) {
                        Vector3 pos = new Vector3(x, y, 0);
                        GameObject platform = Instantiate(prefab, pos, Quaternion.identity, transform);
                        platform.SetActive(true);
                        activePlatforms.Add(platform);
                    }
                }
            } else {
                // Resto de filas: patrón aleatorio y zigzag
                if (Random.value < 0.6f) {
                    PlatformType tipo;
                    float r = Random.value;
                    if (r < 0.3f)
                        tipo = PlatformType.Static;
                    else if (r < 0.6f)
                        tipo = PlatformType.Normal;
                    else if (r < 0.8f)
                        tipo = PlatformType.Fake;
                    else
                        tipo = PlatformType.Breakable;
                    GameObject prefab = GetPlatformPrefab(tipo);
                    if (prefab != null) {
                        Vector3 pos = new Vector3(x, y, 0);
                        GameObject platform = Instantiate(prefab, pos, Quaternion.identity, transform);
                        platform.SetActive(true);
                        activePlatforms.Add(platform);
                    }
                }
            }
        }
    }
    currentHeight = startY + filas * platformSpacing;

    }

    void Update()
    {
        if (playerTransform == null) return;

        float playerY = playerTransform.position.y;
        float generateToY = playerY + 20f;
        float cleanupBelowY = playerY - 30f;

        // Generar plataformas por encima del jugador
        while (currentHeight < generateToY)
        {
            GenerateNextPlatform();
        }

        // Eliminar plataformas que quedan muy abajo
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject platform = activePlatforms[i];
            if (platform != null && platform.transform.position.y < cleanupBelowY)
            {
                Destroy(platform);
                activePlatforms.RemoveAt(i);
            }
        }
    }

    private void GenerateNextPlatform()
    {
        float minX = -3.0f, maxX = 3.0f;
    float minDist = 1.2f;
        int platformsThisRow = 8; // Muchas plataformas por fila
        float y = currentHeight;
        List<float> usedPositions = new List<float>();
        int attempts = 0;
        int maxAttempts = platformsThisRow * 10;
        float platformWidth = 1.0f; // Ajusta según el ancho real del prefab
        float totalWidth = platformsThisRow * platformWidth;
        float playerX = playerTransform != null ? playerTransform.position.x : 0f;
        float startX = playerX - (totalWidth / 2) + (platformWidth / 2);
        for (int i = 0; i < platformsThisRow; i++)
        {
            float x = startX + i * platformWidth;
            PlatformType type;
            float r = Random.value;
            if (r < 0.3f)
                type = PlatformType.Static;
            else if (r < 0.6f)
                type = PlatformType.Normal;
            else if (r < 0.8f)
                type = PlatformType.Fake;
            else
                type = PlatformType.Breakable;

            GameObject prefab = GetPlatformPrefab(type);
            if (prefab != null)
            {
                Vector3 pos = new Vector3(x, y, 0);
                GameObject platform = Instantiate(prefab, pos, Quaternion.identity, transform);
                platform.SetActive(true);
                activePlatforms.Add(platform);
            }
        }
    currentHeight += platformSpacing;
    }

    private GameObject GetPlatformPrefab(PlatformType type)
    {
        switch (type)
        {
            case PlatformType.Static:
            case PlatformType.Normal:
                if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
                    return normalPlatformPrefabs[Random.Range(0, normalPlatformPrefabs.Length)];
                break;
            case PlatformType.Breakable:
                if (breakablePlatformPrefabs != null && breakablePlatformPrefabs.Length > 0)
                    return breakablePlatformPrefabs[Random.Range(0, breakablePlatformPrefabs.Length)];
                break;
            case PlatformType.Fake:
                if (fakePlatformPrefabs != null && fakePlatformPrefabs.Length > 0)
                    return fakePlatformPrefabs[Random.Range(0, fakePlatformPrefabs.Length)];
                break;
        }
        return null;
    }
}
    