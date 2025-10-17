using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SwimmingMinigameController : MonoBehaviour
{
    public float moveForce = 5f;
    public float maxSpeed = 5f;
    public float friction = 2f; // 🔹 Factor de fricción (cuanto mayor, más rápido se frena)
    public Vector2 movementLimits = new Vector2(10f, 5f);
    public bool isBot = false;
    public int Lifes = 3;
    public int index = 0;
    bool isDead = false;

    private Rigidbody rb;
    [SerializeField] GameObject modelParent;
    ThirdPerson inputActions;

    private void Awake()
    {
        inputActions = new ThirdPerson();
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    private void OnDestroy()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
    }

    float h = 0;
    float v = 0;

    public void OnMove(InputAction.CallbackContext movement)
    {
        Vector2 vector2 = movement.ReadValue<Vector2>();
        h = vector2.x;
        v = vector2.y;
    }

    void Start()
    {
        if (!isBot)
        {
            SwimmingMinigame.Player = this;
        }
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // No hay gravedad
        zPosition = transform.position.x;
    }

    void Update()
    {
        if (isDead)
        {
            DeadMovement();
            return;
        }
        if (Lifes <= 0)
        {
            isDead = true;
            FindFirstObjectByType<InfiniteRunnerObstacles>()?.ReportLoose?.Invoke(index);
        }
        if (isBot)
        {
            BotMovement();
        }
        else
        {
            PlayerMovement();
        }

        ApplyFriction();
        ClampPosition();
    }

    private void PlayerMovement()
    {
        float moveZ = h;   // ahora mueve en Y
        float moveY = v;   // ahora mueve en Z

        Vector3 force = new Vector3(0f, moveY, moveZ) * moveForce;
        rb.AddForce(force);

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
        InclinateModel(moveY, moveZ);
    }

    public Transform rotationRight, rotationLeft, RotationUp, rotationDown;

    public void InclinateModel(float y, float z)
    {
        if (y > 0)
        {
            modelParent.transform.rotation = Quaternion.Slerp(modelParent.transform.rotation, RotationUp.rotation, Time.deltaTime * 5f);
        }
        else if (y < 0)
        {
            modelParent.transform.rotation = Quaternion.Slerp(modelParent.transform.rotation, rotationDown.rotation, Time.deltaTime * 5f);
        }
        
        if (z < 0)
        {
            modelParent.transform.rotation = Quaternion.Slerp(modelParent.transform.rotation, rotationRight.rotation, Time.deltaTime * 5f);
        }
        else if (z > 0)
        {
            modelParent.transform.rotation = Quaternion.Slerp(modelParent.transform.rotation, rotationLeft.rotation, Time.deltaTime * 5f);
        }

        if (y == 0 && z == 0)
        {
            modelParent.transform.rotation = Quaternion.Slerp(modelParent.transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    private void BotMovement()
    {
        // 1️⃣ Esquivar obstáculos
        if (TryAvoidObstacles()) return;

        // 2️⃣ Empujar a otro bot cercano
        //if (TryPushOtherBot()) return;

        // 3️⃣ Intentar empujar al Player si empujarlo lleva a pérdida
        //if (TryPushPlayer()) return;

        // 4️⃣ Movimiento aleatorio si no hay nada más
        MoveRandom();
    }

    [SerializeField] float timeToCheck = 3;
    float currentTime = 0;

    private bool TryAvoidObstacles()
    {
        if (currentTime - Time.time < 0)
        {
            ObstacleDetector.Instance.DetectCarril(DecideCarril);
            currentTime = Time.time + timeToCheck;
            return true;       
        }
        return false;
    }

    float zPosition = 0;

    public void DecideCarril(List<bool> carrils)
    {
        // Selecciona y guarda un índice al azar en la lista carrils que sea true
        List<int> indicesDisponibles = new List<int>();

        for (int i = 0; i < carrils.Count; i++)
        {
            if (!carrils[i]) // si el carril no tiene obstaculo (false)
            {
                indicesDisponibles.Add(i);
            }
        }

        if (indicesDisponibles.Count > 0)
        {
            int indiceSeleccionado = indicesDisponibles[Random.Range(0, indicesDisponibles.Count)];
            Debug.Log("Carril elegido: " + indiceSeleccionado);
            zPosition = ObstacleDetector.Instance.ReturnPositionUsingIndex(indiceSeleccionado).z;
            // Generar una posición objetivo con Z seguro y Y aleatorio dentro del rango
            randomY = Random.Range(-movementLimits.y, movementLimits.y);
        }
        else
        {
            Debug.LogWarning("No hay carriles disponibles.");
            zPosition = Random.Range(-movementLimits.x, movementLimits.x);
            randomY = Random.Range(-movementLimits.y, movementLimits.y);
        }
    }



    private bool TryPushOtherBot()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && hit.CompareTag("Player"))
            {
                SwimmingMinigameController other = hit.GetComponent<SwimmingMinigameController>();
                if (other != null && other.isBot) // es otro bot
                {
                    Vector3 dirToBot = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(dirToBot * moveForce, ForceMode.Impulse);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryPushPlayer()
    {
        if (SwimmingMinigame.Player == null) return false;

        float detectionRadius = 3f;
        Vector3 dirToPlayer = (SwimmingMinigame.Player.transform.position - transform.position).normalized;

        if (Vector3.Distance(transform.position, SwimmingMinigame.Player.transform.position) < detectionRadius)
        {
            if (CheckIfPushLeadsToLoss(SwimmingMinigame.Player.gameObject))
            {
                rb.AddForce(dirToPlayer * moveForce, ForceMode.Impulse);
                return true;
            }
        }
        return false;
    }

    public void DeadMovement()
    {
        // Calcular dirección hacia la posición segura
        Vector3 dir = Vector3.down;
        InclinateModel(0, 0);

        // Aplicar fuerza en esa dirección
        rb.AddForce(dir * moveForce);

        // Limitar velocidad máxima
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    float randomY = 0;
    private void MoveRandom()
    {
        Vector3 targetPos = new Vector3(transform.position.x, randomY, zPosition);

        // Calcular dirección hacia la posición segura
        Vector3 dir = (targetPos - transform.position).normalized;
        InclinateModel(dir.y , dir.z);

        // Aplicar fuerza en esa dirección
        rb.AddForce(dir * moveForce);

        // Limitar velocidad máxima
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    private void ApplyFriction()
    {
        // 🔹 Desacelera suavemente con fricción en los ejes Y/Z
        Vector3 vel = rb.linearVelocity;
        vel.y *= (1f - friction * Time.deltaTime);
        vel.z *= (1f - friction * Time.deltaTime);

        // 🔹 Bloquear el eje X por completo
        vel.x = 0f;

        rb.linearVelocity = vel;
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, -movementLimits.x, movementLimits.x);
        pos.z = Mathf.Clamp(pos.z, -movementLimits.y, movementLimits.y);
        pos.x = 0f; // 🔹 Bloquear X en todo momento
        transform.position = pos;
    }

    [SerializeField] float pushForce = 3;
    float inmunityTime = 0;
    
    [Header("Audio - colisiones")]
    public AudioClip obstacleCip;
    public AudioClip pushClip;
    [Range(0f, 1f)] public float obstacleVol = 1f;
    [Range(0f, 1f)] public float pushVol = 1f;
    public float collisionCoolDown = 0.25f;
    private float lastCollisiononSoundTIme = -999f;
    public bool usePositionalSFX = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector3 pushDir = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);

            Rigidbody otherRb = collision.gameObject.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                otherRb.AddForce(-pushDir * pushForce, ForceMode.Impulse);
            }
            // reproducir sonido de choque entre jugadores
            if (Time.time - lastCollisiononSoundTIme >= collisionCoolDown && pushClip != null)
            {
                Vector3 pos = collision.contacts.Length > 0 ? collision.contacts[0].point : collision.transform.position;
                PlayCollisonSFX(pushClip, pushVol, pos);
                lastCollisiononSoundTIme = Time.time;
            }
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (inmunityTime - Time.time < 0)
            {
                inmunityTime = Time.time + timeDisable;
                //receive damage
                Lifes -= 1;
                SwimmingPlayerUI.UpdateVisual(Lifes);
                StartCoroutine(DisableForSeconds());
                
                //reproducir sonido de impacto con obstáculos
                if (Time.time - lastCollisiononSoundTIme >= collisionCoolDown && obstacleCip != null)
                {
                    Vector3 pos = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                    PlayCollisonSFX(obstacleCip, obstacleVol, pos);
                    lastCollisiononSoundTIme = Time.time;
                }
            }
        }
    }
    [SerializeField] CapsuleCollider capsuleCollider;
    [SerializeField] float timeDisable = 3;
    [SerializeField] SwimmingPlayerUI SwimmingPlayerUI;
    private IEnumerator DisableForSeconds()
    {
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("NoCollision"));
        yield return new WaitForSeconds(timeDisable);
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Default"));
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private bool CheckIfPushLeadsToLoss(GameObject target)
    {
        if (SwimmingMinigame.Player == null) return false;

        Vector3 playerPos = SwimmingMinigame.Player.transform.position;
        Vector3 dirToPlayer = (playerPos - transform.position).normalized;

        float sphereRadius = 0.5f;
        float checkDistance = 3f;

        if (Physics.SphereCast(playerPos, sphereRadius, dirToPlayer, out RaycastHit hit, checkDistance))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                Vector3 dirToObstacle = (hit.point - playerPos).normalized;
                float dot = Vector3.Dot(dirToPlayer, dirToObstacle);

                if (dot > 0.7f)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    // sfx y audio
    private void PlayCollisonSFX(AudioClip clip, float volume, Vector3 position)
    {
        if (clip == null) return;

        // Si hay AudioManager, usar sus funciones (permite Mixer y grupos)
        if (AudioManager.Instance != null)
        {
            // Usamos PlaySFXAtPoint con spatialBlend = 1 (posicional)
            AudioManager.Instance.PlaySFXAtPoint(clip, position, volume);
            return;
        }

        // Fallback simple si no hay AudioManager: PlayClipAtPoint (no asigna mixer groups)
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}

