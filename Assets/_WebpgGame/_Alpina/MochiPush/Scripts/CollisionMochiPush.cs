using UnityEngine;

// “Combatant” de tu juego. Decide cuándo está FUERA de la plataforma y notifica al GameManager.
[RequireComponent(typeof(Rigidbody))]
public class CollisionMochiPush : MonoBehaviour
{
    [Header("Referencias")]
    public GameManagerMochiPush gameManager;  // Asigna por Inspector
    public Transform arenaCenter;             // Si no se asigna, toma del GameManager
    public float arenaRadius = 12f;           // Si no se asigna, toma del GameManager

    [Header("Jugador o Bot")]
    public bool isHuman = false;              // Marca true solo en el Player

    [Header("Reglas de eliminación")]
    public float extraRadiusMargin = 0.4f;    // Tolerancia por colisionadores
    public float outOfBoundsY = -5f;          // Caída por altura
    public bool disableOnEliminate = true;    // Desactivar GameObject al eliminar

    public bool Eliminated { get; private set; }

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Registrar
        if (gameManager) gameManager.Register(this);
    }

    void OnDestroy()
    {
        if (gameManager) gameManager.Unregister(this);
    }

    void Update()
    {
        if (Eliminated) return;

        // 1) Caída por Y
        if (transform.position.y < outOfBoundsY)
        {
            Eliminate();
            return;
        }

        // 2) Salirse del radio de la plataforma (proyección XZ)
        if (arenaCenter && arenaRadius > 0f)
        {
            Vector3 p = transform.position;
            Vector3 c = arenaCenter.position;
            float distXZ = Mathf.Sqrt((p.x - c.x) * (p.x - c.x) + (p.z - c.z) * (p.z - c.z));

            if (distXZ > (arenaRadius + extraRadiusMargin))
            {
                Eliminate();
                return;
            }
        }
    }

    public void Eliminate()
    {
        if (Eliminated) return;
        Eliminated = true;

        // Notificar y “sacarlo del juego”
        if (gameManager) gameManager.NotifyEliminated(this);

        if (disableOnEliminate)
            gameObject.SetActive(false);
    }

    // KillZone opcional debajo de la isla
    void OnTriggerEnter(Collider other)
    {
        if (Eliminated) return;

        if (other.CompareTag("KillZone"))
            Eliminate();
    }
}