using System.Collections;
using UnityEngine;

// “Combatant” de tu juego. Decide cuándo está FUERA de la plataforma y notifica al GameManager.
[RequireComponent(typeof(Rigidbody))]
public class CollisionMochiPush : MonoBehaviour
{
    [Header("Referencias")]
    public GameManagerMochiPush gameManager;  // Asigna por Inspector
    public Transform arenaCenter;             // Si no se asigna, toma del GameManager
    public float arenaRadius = 2f;         // Si no se asigna, toma del GameManager
    [SerializeField] private GameObject KOUIGo;
    [SerializeField] private GameObject DeadCamera;
    [SerializeField] private float coldownToNextPush = 0.2f;
     
    [Header("Jugador o Bot")]
    public bool isHuman = false;              // Marca true solo en el Player
    public bool canMove;
    
    [Header("Física de Empuje")]
    public float pushForce = 5f;        // Fuerza base del impacto
    public float recoilForce = 2f;      // Fuerza que recibe el que choca
    private bool canBePushed = true;    // Para evitar rebotes infinitos
    
    [Header("Reglas de eliminación")]
    public float extraRadiusMargin = 0.4f;    // Tolerancia por colisionadores
    public float outOfBoundsY = -5f;          // Caída por altura
    public bool disableOnEliminate = true;    // Desactivar GameObject al eliminar

    public bool Eliminated { get; private set; }

    Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();
    void Start()
    {
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
        if (transform.position.y < outOfBoundsY) { Eliminate(); return; }

        // 2) Salirse del radio de la plataforma (proyección XZ)
        if (arenaCenter)
        {
            Vector3 offset = transform.position - arenaCenter.position;
            float distSq = (offset.x * offset.x) + (offset.z * offset.z);
            float limit = arenaRadius + extraRadiusMargin;

            if (distSq > (limit * limit)) Eliminate();
        }
    }

    public void Eliminate()
    {
        if (Eliminated) 
            return;
        if (isHuman && DeadCamera) 
            DeadCamera.SetActive(true);
        
        Eliminated = true;
        if (KOUIGo) 
            KOUIGo.SetActive(true);
        
        // Notificar y “sacarlo del juego”
        if (gameManager) 
            gameManager.NotifyEliminated(this);

        if (disableOnEliminate)
            gameObject.SetActive(false);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (Eliminated || !canBePushed) return;

        // Buscamos si el otro objeto también es un Mochi
        CollisionMochiPush other = collision.gameObject.GetComponent<CollisionMochiPush>();
        // if (!isHuman)//Un Comment To Debug Bot Collisions
        //     Debug.Log($"Mochi bot {gameObject.name} is colliding with {collision.gameObject.name}");
        if (other != null && !other.Eliminated)
        {
            Vector3 direction = (collision.transform.position - transform.position).normalized;
            direction.y = 0; // Empuje horizontal

            // Aplicamos fuerza al otro (Empuje)
            Rigidbody otherRb = collision.rigidbody;
            if (otherRb)
                otherRb.AddForce(direction * pushForce, ForceMode.Impulse);

            if (!isHuman)
            {
                BotMochiPush botScript = GetComponent<BotMochiPush>();
                float finalForce = botScript.GetCalculatedPushForce(this.pushForce);
                otherRb.AddForce(direction * finalForce, ForceMode.Impulse);
                botScript.NotifyHitSuccessful();
            }else
                _rb.AddForce(-direction * recoilForce, ForceMode.Impulse);
            
            // Pequeña pausa para no procesar 100 choques por segundo
            StartCoroutine(PushCooldown());
        }
    }
    IEnumerator PushCooldown()
    {
        canBePushed = false;
        yield return new WaitForSeconds(coldownToNextPush);
        canBePushed = true;
    }
    // KillZone opcional debajo de la isla
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("MochiPushBoundary"))
            return;
        if (Eliminated) return;
            
        if (other.CompareTag("KillZone"))
            Eliminate();
    }
}