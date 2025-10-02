using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfiniteRunnerObstacles : MonoBehaviour
{
    [Header("Referencias")]
    public Transform spawnPoint;       // Empty donde aparecen los obstáculos
    public Transform despawnPoint;     // Empty donde desaparecen
    public Transform obstaclesParent;  // Parent que se mueve como banda transportadora
    public GameObject obstaclePrefab;  // Prefab del obstáculo

    [Header("Pooling Dinámico")]
    public int initialPoolSize = 10;   // Tamaño inicial de la pool
    private Queue<GameObject> obstaclePool = new Queue<GameObject>();

    [Header("Spawn Settings")]
    public float minSpawnTime = 1f;
    public float maxSpawnTime = 3f;
    private float spawnTimer;

    [Header("Movimiento del escenario")]
    public float moveSpeed = 5f;

    public System.Action<int> ReportLoose;
    public List<bool> players = new List<bool>() { false, false, false, false };

    void Start()
    {
        ReportLoose += OnReceiveReport;
        // Inicializar la pool con algunos objetos
        for (int i = 0; i < initialPoolSize; i++)
        {
            AddNewObstacleToPool();
        }
        obstaclesParent.gameObject.SetActive(true);
        ResetSpawnTimer();
    }

    private void OnDestroy()
    {
        ReportLoose -= OnReceiveReport;
    }

    public void OnReceiveReport(int index)
    {
        players[index] = true;
        looseIndexes.Add(index);
    }

    [SerializeField] Transform winParent, looseParent;
    bool endgame = false;
    [SerializeField] List<int> looseIndexes = new List<int>();
    public List<int> winnerIndexes = new List<int>();

    void Update()
    {
        if (endgame)
        {
            return;
        }
        List<bool> filteredPlayers = new List<bool>();
        filteredPlayers = players.Where(a => a == false).ToList();
        if (players.Count <= 1)
        {
            if (players.Count <= 0)
            {
                //Empate
                //Indexes of winners
                winnerIndexes.Add(looseIndexes[3]);
                winnerIndexes.Add(looseIndexes[4]);
            }
            else
            {
                //Victory of any player
                winnerIndexes.Add(players.IndexOf(false));
            }
            PlayerPrefs.SetString("WinnerYogoNado", JsonConvert.SerializeObject(winnerIndexes));
                //Endgame
            obstaclesParent.gameObject.SetActive(false);
            endgame = true;
        }

        // Mover el padre como banda transportadora
        obstaclesParent.Translate((despawnPoint.position - spawnPoint.position).normalized * moveSpeed * Time.deltaTime);

        // Manejar spawns
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnObstacle();
            ResetSpawnTimer();
        }

        // Revisar obstáculos activos para desactivar
        CheckDespawn();
    }

    void SpawnObstacle()
    {
        GameObject obstacle;

        // Si hay objetos en la pool, los usamos
        if (obstaclePool.Count > 0)
        {
            obstacle = obstaclePool.Dequeue();
        }
        else
        {
            // Si no hay, creamos uno nuevo y lo usamos
            obstacle = AddNewObstacleToPool();
            obstaclePool.Dequeue(); // lo quitamos porque lo vamos a usar ahora
        }

        obstacle.transform.position = spawnPoint.position;
        obstacle.SetActive(true);
    }

    void CheckDespawn()
    {
        foreach (Transform child in obstaclesParent)
        {
            if (child.gameObject.activeSelf && child.position.y <= despawnPoint.position.y)
            {
                child.gameObject.SetActive(false);
                obstaclePool.Enqueue(child.gameObject);
            }
        }
    }

    GameObject AddNewObstacleToPool()
    {
        GameObject obj = Instantiate(obstaclePrefab, obstaclesParent);
        obj.SetActive(false);
        obstaclePool.Enqueue(obj);
        return obj;
    }

    void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }
}

