
using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Prefabs (current)")]
    [SerializeField] private GameObject[] platformPrefabs;

    [Header("Compatibility (legacy names used by other scripts)")]
    [Tooltip("Legacy: used by PlayerController/BotController")]
    public GameObject[] normalPlatformPrefabs;
    public GameObject[] breakablePlatformPrefabs;
    public GameObject[] fakePlatformPrefabs;
    public float separacionEntrePlataformas = 8f; // legacy horizontal spacing
    public float platformSpacing = 10f;            // legacy vertical spacing
    public int maxFilas = 8;                      // legacy rows count

    [Header("Grid (current)")]
    [Tooltip("Número de filas (alto)")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int[] columnsPerRow;
    [SerializeField] public float spacingX = 3f;
    [SerializeField] private float spacingY = 2f;

    [Header("Positioning")]
    [Tooltip("Centro X para alinear las filas")]
    [SerializeField] private float centerX = 0f;
    [SerializeField] private Vector3 startPosition = Vector3.zero;
    [SerializeField] private Transform parentForPlatforms;

    [Header("Options")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool clearBeforeGenerate = true;
    
    [SerializeField] private float yOffset;

    [Header("Helpers")]
    [Tooltip("Force spawned GameObjects to be active")]
    [SerializeField] private bool activateSpawned = true;
    [Tooltip("Spawn vertical columns at Player and Bots instead of grid")]
    [SerializeField] private bool spawnColumnsAtCharacters = false;

    private readonly List<GameObject> spawned = new List<GameObject>();
    
    [Header("Spawn probabilities (sum not required)")]
    [Tooltip("Relative chance to spawn a normal platform")]
    [Range(0f, 1f)]
    public float normalChance = 0.7f;
    [Tooltip("Relative chance to spawn a breakable platform")]
    [Range(0f, 1f)]
    public float breakableChance = 0.2f;
    [Tooltip("Relative chance to spawn a fake platform")]
    [Range(0f, 1f)]
    public float fakeChance = 0.2f;

    private void Awake()
    {
        if (parentForPlatforms == null) parentForPlatforms = transform;
        SyncLegacyToCurrent();
        ValidateConfiguration();
        SyncCurrentToLegacy();
        // Do not auto-generate in Awake if you rely on other Start() setups in scene.
        if (generateOnStart) Invoke(nameof(Generate), 0.01f);
    }

    private void OnValidate()
    {
        ValidateConfiguration();
        SyncCurrentToLegacy();
    }

    private void SyncLegacyToCurrent()
    {
        if (separacionEntrePlataformas > 0f)
            spacingX = separacionEntrePlataformas;
        if (platformSpacing > 0f)
            spacingY = platformSpacing;
        if (maxFilas > 0)
            rows = Mathf.Max(1, maxFilas);
        if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
            platformPrefabs = normalPlatformPrefabs;
    }

    private void SyncCurrentToLegacy()
    {
        separacionEntrePlataformas = spacingX;
        platformSpacing = spacingY;
        maxFilas = rows;
        normalPlatformPrefabs = platformPrefabs;
    }

    private void ValidateConfiguration()
    {
        rows = Mathf.Max(1, rows);
        spacingX = Mathf.Max(0.01f, spacingX);
        spacingY = Mathf.Max(0.01f, spacingY);

        if (columnsPerRow == null || columnsPerRow.Length < rows)
        {
            var newCols = new int[rows];
            for (int i = 0; i < rows; i++)
                newCols[i] = (columnsPerRow != null && i < columnsPerRow.Length) ? Mathf.Max(1, columnsPerRow[i]) : 3;
            columnsPerRow = newCols;
        }
        else
        {
            for (int i = 0; i < columnsPerRow.Length; i++)
                columnsPerRow[i] = Mathf.Max(1, columnsPerRow[i]);
        }
    }

    [ContextMenu("Clear Spawned Platforms")]
    public void Clear()
    {
#if UNITY_EDITOR
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] != null) DestroyImmediate(spawned[i]);
#else
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] != null) Destroy(spawned[i]);
#endif
        spawned.Clear();
        Debug.Log("[PlatformGenerator] Cleared spawned platforms.");
    }

    [ContextMenu("Generate Platforms")]
    public void Generate()
    {
        // Fallback to legacy prefabs
        if ((platformPrefabs == null || platformPrefabs.Length == 0) && normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
        {
            platformPrefabs = normalPlatformPrefabs;
            Debug.Log("[PlatformGenerator] Using legacy normalPlatformPrefabs as fallback.");
        }

        if ((platformPrefabs == null || platformPrefabs.Length == 0)
            && (normalPlatformPrefabs == null || normalPlatformPrefabs.Length == 0)
            && (breakablePlatformPrefabs == null || breakablePlatformPrefabs.Length == 0)
            && (fakePlatformPrefabs == null || fakePlatformPrefabs.Length == 0))
        {
            Debug.LogWarning("[PlatformGenerator] No platform prefabs assigned. Assign `platformPrefabs` or `normalPlatformPrefabs` in Inspector.");
            return;
        }

        if (parentForPlatforms == null) parentForPlatforms = transform;
        if (clearBeforeGenerate) Clear();
        

        // Standard grid generation
        for (int row = 0; row < rows; row++)
        {
            int cols = (columnsPerRow != null && row < columnsPerRow.Length) ? columnsPerRow[row] : 3;
            float rowWidth = (cols - 1) * spacingX;
            float startX = centerX - rowWidth * 0.5f + startPosition.x;
            float y = startPosition.y + row * spacingY;
            float z = startPosition.z;

            for (int col = 0; col < cols; col++)
            {
                Vector3 pos = new Vector3(startX + col * spacingX, y + yOffset, z);
                GameObject chosenPrefab;
                PlatformType chosenType;
                if (!TryPickPrefabAndType(out chosenPrefab, out chosenType, row, col))
                    continue;

                GameObject go = Instantiate(chosenPrefab, pos, Quaternion.identity, parentForPlatforms);
                go.name = $"{chosenPrefab.name}_r{row}_c{col}_{chosenType}";

                if (activateSpawned && !go.activeSelf) go.SetActive(true);
                var platformComp = go.GetComponent<Platform>();
                if (platformComp == null)
                    platformComp = go.AddComponent<Platform>();
                platformComp.platformType = chosenType;

                spawned.Add(go);

                try { go.tag = "Platform"; }
                catch (UnityException) { /* Tag may not exist - ignore */ }
            }
        }

        SyncCurrentToLegacy();
        Debug.Log($"[PlatformGenerator] Generated {spawned.Count} platforms (grid).");
    }

    private bool TryPickPrefabAndType(out GameObject prefab, out PlatformType type, int row = 0, int col = 0)
    {
        prefab = null;
        type = PlatformType.Static;

        // Normalize chances
        float total = normalChance + breakableChance + fakeChance;
        if (total <= 0f)
        {
            // fallback: prefer normal arrays or platformPrefabs
            if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0)
            {
                prefab = normalPlatformPrefabs[Random.Range(0, normalPlatformPrefabs.Length)];
                type = PlatformType.Static;
                return prefab != null;
            }
            if (platformPrefabs != null && platformPrefabs.Length > 0)
            {
                prefab = platformPrefabs[(row + col) % platformPrefabs.Length];
                type = PlatformType.Static;
                return prefab != null;
            }
            return false;
        }

        float r = Random.value * total;
        GameObject[] sourceArray = null;

        if (r < normalChance)
        {
            sourceArray = (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0) ? normalPlatformPrefabs : platformPrefabs;
            type = PlatformType.Static;
        }
        else if (r < normalChance + breakableChance)
        {
            sourceArray = (breakablePlatformPrefabs != null && breakablePlatformPrefabs.Length > 0) ? breakablePlatformPrefabs : null;
            type = PlatformType.Breakable;
        }
        else
        {
            sourceArray = (fakePlatformPrefabs != null && fakePlatformPrefabs.Length > 0) ? fakePlatformPrefabs : null;
            type = PlatformType.Fake;
        }

        // If preferred array is empty, fallback to platformPrefabs or other arrays
        if ((sourceArray == null || sourceArray.Length == 0) && platformPrefabs != null && platformPrefabs.Length > 0)
        {
            sourceArray = platformPrefabs;
            type = PlatformType.Static; // fallback resets to normal
        }
        if (sourceArray == null || sourceArray.Length == 0)
        {
            // try any available array as last resort
            if (normalPlatformPrefabs != null && normalPlatformPrefabs.Length > 0) { sourceArray = normalPlatformPrefabs; type = PlatformType.Static; }
            else if (breakablePlatformPrefabs != null && breakablePlatformPrefabs.Length > 0) { sourceArray = breakablePlatformPrefabs; type = PlatformType.Breakable; }
            else if (fakePlatformPrefabs != null && fakePlatformPrefabs.Length > 0) { sourceArray = fakePlatformPrefabs; type = PlatformType.Fake; }
            else return false;
        }

        prefab = sourceArray[Random.Range(0, sourceArray.Length)];
        return prefab != null;
    }
    
   
}