using UnityEngine;

[System.Serializable]
public class VfxLibrary
{
    // The unique identifier used to call this VFX (e.g., "explosion_fire")
    public string Identifier;
    
    // The actual Unity Prefab that holds the Particle System/VFX Graph
    public GameObject VfxPrefab;
}
