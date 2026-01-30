using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject powerUpPrefab;
    public float spawnRate = 10f;
    public float arenaRadius = 2f;

    void Start() {
        InvokeRepeating(nameof(SpawnPowerUp), spawnRate, spawnRate);
    }

    public void StartCoroutine()
    {
        InvokeRepeating(nameof(SpawnPowerUp), spawnRate, spawnRate);
    }
    private void SpawnPowerUp() {
        Vector2 randomPos = Random.insideUnitCircle * arenaRadius;
        Vector3 spawnPos = new Vector3(randomPos.x, 0.09f, randomPos.y);
        Instantiate(powerUpPrefab, spawnPos, Quaternion.identity);
    }
}
