using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VfxManager : MonoBehaviour
{
    public static VfxManager Instance { get; private set; }

    [Header("VFX Library")]
    // Array thah holds the vfxs.
    public VfxLibrary[] vfxEntries; 
    // Dictionary used to handle vfxs
    private Dictionary<string, GameObject> vfxDictionary;

    [SerializeField] private float vfxDestroyTime = 1.5f;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeVFXDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Converts the Inspector array into a dictionary
    /// </summary>
    private void InitializeVFXDictionary()
    {
        vfxDictionary = new Dictionary<string, GameObject>();
        foreach (VfxLibrary entry in vfxEntries)
        {
            // Check for duplicates or missing prefabs
            if (vfxDictionary.ContainsKey(entry.Identifier))
            {
                Debug.LogWarning($"VFXManager: Duplicate identifier found: {entry.Identifier}.");
                continue;
            }
            if (entry.VfxPrefab == null)
            {
                Debug.LogError($"VFXManager: Entry '{entry.Identifier}' has a null Prefab!");
                continue;
            }

            vfxDictionary.Add(entry.Identifier, entry.VfxPrefab);
        }
    }

    /// <summary>
    /// Spawns a VFX by its unique string identifier.
    /// </summary>
    /// <param name="identifier">Vfx Key to spawn.</param>
    /// <param name="position">The world position to spawn the VFX at.</param>
    /// <param name="rotation">The world rotation of the spawned VFX.</param>
    /// <returns>The newly instantiated GameObject, or null if the identifier is invalid.</returns>
    public GameObject SpawnVFX(string identifier, Vector3 position, Quaternion rotation)
    {
        // Check for the VFX in the dictionary
        if (vfxDictionary.TryGetValue(identifier, out GameObject vfxPrefab))
        {
            GameObject spawnedVFX = Instantiate(vfxPrefab, position, rotation);
            
            //TODO: ObjectPooling
            ParticleSystem ps = spawnedVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Cleans itself up after its duration
                Destroy(spawnedVFX, ps.main.duration + ps.main.startLifetimeMultiplier);
            }
            else
            {
                // If there's no ParticleSystem and clean up after X seconds
                Destroy(spawnedVFX, vfxDestroyTime); 
            }

            return spawnedVFX;
        }
        else // If the VFX does not exist, we return null
        {
            // Log an error if the identifier wasn't found
            Debug.LogError($"VFXManager: Cannot find VFX with identifier: {identifier}");
            return null;
        }
    }
    /// <summary>
    /// Spawns a VFX by its unique string identifier.
    /// </summary>
    /// <param name="identifier">Vfx Key to spawn.</param>
    /// <param name="position">The world position to spawn the VFX at.</param>
    /// <param name="rotation">The world rotation of the spawned VFX.</param>
    /// <returns>The newly instantiated GameObject, or null if the identifier is invalid.</returns>
    public GameObject SpawnVFX(string identifier, Transform parent)
    {
        // Check for the VFX in the dictionary
        if (vfxDictionary.TryGetValue(identifier, out GameObject vfxPrefab))
        {
            GameObject spawnedVFX = Instantiate(vfxPrefab, parent.position, parent.rotation, parent);
            
            //TODO: ObjectPooling
            ParticleSystem ps = spawnedVFX.GetComponent<ParticleSystem>();
            Destroy(spawnedVFX, vfxDestroyTime); 

            return spawnedVFX;
        }
        else // If the VFX does not exist, we return null
        {
            // Log an error if the identifier wasn't found
            Debug.LogError($"VFXManager: Cannot find VFX with identifier: {identifier}");
            return null;
        }
    }
}
