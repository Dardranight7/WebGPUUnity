using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    public GameObject powerUpPrefab;
    public float spawnRate = 10f;
    public float arenaRadius = 2f;
    public int amountOfPowerUpsOnScene = 2;

    private int spawnedPowerUp;
    void Start() {
        InvokeRepeating(nameof(SpawnPowerUp), spawnRate, spawnRate);
    }

    public void StartCoroutine()
    {
        InvokeRepeating(nameof(SpawnPowerUp), spawnRate, spawnRate);
    }
    private void SpawnPowerUp() {
        if (spawnedPowerUp < amountOfPowerUpsOnScene)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject powerUp = Instantiate(powerUpPrefab, spawnPos, Quaternion.identity);
            SetupPowerUp(powerUp);
            spawnedPowerUp++;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 randomPos = Random.insideUnitCircle * arenaRadius;
        return new Vector3(randomPos.x, 0.09f, randomPos.y);
    }

    private void SetupPowerUp(GameObject powerUp)
    {
        StunPowerUp script = powerUp.GetComponentInChildren<StunPowerUp>();
        script.SetupPowerUp(this);
    }

    public void DeletePowerup()
    {
        spawnedPowerUp--;
    }
}
