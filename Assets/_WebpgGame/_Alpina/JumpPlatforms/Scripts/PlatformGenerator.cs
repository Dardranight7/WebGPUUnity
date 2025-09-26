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
    public float startHeight = 0f;
    private float currentHeight = 0f;
    public float platformSpacing = 0.1f;
    [Header("Plataformas por fila")]
    public int plataformasPorFila = 6;
    public float separacionEntrePlataformas = 1.0f;

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        currentHeight = 0f;
        GenerateInitialPlatforms();
    }

    private void GenerateInitialPlatforms()
    {
        float startY = startHeight;
        int filas = 40;
        float platformWidth = separacionEntrePlataformas;
        // Si hay prefabs, usa el ancho real del primero
        if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
        {
            var rend = normalPlatformPrefabs[0].GetComponent<Renderer>();
            if (rend != null)
                platformWidth = rend.bounds.size.x + separacionEntrePlataformas;
        }
        for (int fila = 0; fila < filas; fila++)
        {
            float y = startY + fila * platformSpacing;
            float totalWidth = (plataformasPorFila - 1) * platformWidth;
            // Zigzag: desplaza toda la fila medio ancho a la derecha en filas impares
            float zigzagOffset = (fila % 2 == 0) ? 0 : platformWidth / 2;
            float startX = -totalWidth / 2 + zigzagOffset;
            for (int col = 0; col < plataformasPorFila; col++)
            {
                float x = startX + col * platformWidth;
                if (Mathf.Abs(y) < 0.01f) continue;
                PlatformType tipo;
                if (fila < 3)
                {
                    tipo = (Random.value < 0.5f) ? PlatformType.Static : PlatformType.Normal;
                }
                else
                {
                    float r = Random.value;
                    if (r < 0.7f)
                        tipo = PlatformType.Normal;
                    else if (r < 0.8f)
                        tipo = PlatformType.Static;
                    else if (r < 0.9f)
                        tipo = PlatformType.Fake;
                    else
                        tipo = PlatformType.Breakable;
                }
                GameObject prefab = GetPlatformPrefab(tipo);
                if (prefab != null)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    GameObject platform = Instantiate(prefab, pos, Quaternion.identity, transform);
                    platform.SetActive(true);
                    activePlatforms.Add(platform);
                }
            }
        }
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

    public void GenerateNextPlatform()
    {
        float y = currentHeight;
        float platformWidth = separacionEntrePlataformas;
        if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
        {
            var rend = normalPlatformPrefabs[0].GetComponent<Renderer>();
            if (rend != null)
                platformWidth = rend.bounds.size.x + separacionEntrePlataformas;
        }
        float totalWidth = (plataformasPorFila - 1) * platformWidth;
        float playerX = playerTransform != null ? playerTransform.position.x : 0f;
        float startX = playerX - (totalWidth / 2);
        float zigzagOffset = ((int)(currentHeight/platformSpacing) % 2 == 0) ? 0 : platformWidth / 2;
        float startXZigzag = startX + zigzagOffset;
        for (int i = 0; i < plataformasPorFila; i++)
        {
            float x = startXZigzag + i * platformWidth;
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

