using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(CollisionMochiPush))]
public class BotMochiPush : MonoBehaviour
{
    [Header("Referencias (si están vacías se intentan auto-asignar)")]
    public GameManagerMochiPush gameManager;
    public GameObject arenaCenter;
    public float arenaRadius = 12f;

    [Header("Movimiento")]
    private Vector3 _moveVelocity;
    public float moveSpeed = 4.0f;
    public float acceleration = 20f;   // unidades/s^2, para MoveTowards en velocity
    public float rotateSpeed = 10f;
    
    [Header("Sistema de Estamina")]
    [SerializeField] private Image staminaUiImage;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;
    public float staminaCostPerPush = 30f;
    public float maxPushMultiplier = 1.5f;
    
    private bool _isStunned = false;
    
    [Header("Wander (patrullar)")]
    public float wanderSpeedMultiplier = 0.85f;
    public float wanderPointRefreshMin = 1.6f;
    public float wanderPointRefreshMax = 2.6f;
    public float wanderReachRadius = 0.45f;

    [Header("Detección y Ataque")]
    public float detectionRadius = 5.0f;      // radio para buscar rivales
    public float attackDuration = 3.0f;       // tiempo que dura el ataque
    public float recheckTargetsEvery = 0.25f; // cada cuanto revalida el target

    [Header("Empujón (contacto real)")]
    public float pushForce = 5.0f;
    [Tooltip("Distancia entre puntos más cercanos de colliders para considerar contacto (en metros).")]
    public float pushContactThreshold = 0.08f; // ~8 cm, ajustar según escala
    [Range(0, 90)] public float frontAngle = 35f;
    public float pushCooldown = 0.6f;

    [Header("Separación / borde")]
    public float separationRadius = 1.2f;
    public float separationStrength = 2.5f;
    public float edgeSafeMargin = 2f;

    [Header("Arrival / anti-órbita")]
    public float arriveRadius = 1.7f;
    public float stopRadius = 0.55f;
    public float tangentialBrake = 6.5f;
    public float antiOrbitRadius = 3.0f;

    [Header("Debug")]
    public bool debugLogs = false;

    Rigidbody _rb;
    Collider _col;
    CollisionMochiPush _self;

    enum State { Wander, Attack }
    State _state = State.Wander;

    // Attack state
    CollisionMochiPush _target;
    float _attackTimer;
    float _pushCd;
    float _nextTargetCheck;

    // Wander state
    Vector3 _wanderPoint;
    float _wanderRefreshTimer;

    private void OnEnable()
    {
        EventBus<StunSignal>.OnEvent += HandleStun;
    }

    private void OnDisable()
    {
        EventBus<StunSignal>.OnEvent -= HandleStun;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _self = GetComponent<CollisionMochiPush>();

        // Si tienes bloqueadas posiciones (FreezePositionX/Z) eso impide que el bot se mueva.
        // Permitimos solo FreezeRotationX|Z por estabilidad.
        RigidbodyConstraints desired = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if ((_rb.constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ)) != 0)
        {
            _rb.constraints = (_rb.constraints & ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ)) | desired;
        }
        else
        {
            _rb.constraints = (_rb.constraints | desired);
        }
    }

    void Start()
    {
        currentStamina = maxStamina;
        PickNewWanderPoint();
    }

    void FixedUpdate()
    {
        RegenerateStamina();
        if (_isStunned) return;
        if (_self != null && _self.Eliminated) return;

        //Evitando el borde
        if (IsNearEdge(out var dirToCenter))
        {
            // Si el bot esta en peligro, ignoramos el ataque y volvemos al centro
            MoveTowards(dirToCenter, moveSpeed);
            FaceTowards(dirToCenter);
            return; 
        }
        
        bool isTired = currentStamina < (maxStamina * 0.2f); // si tiene menos del 20% de estamina, determinamos que esta cansado

        if (_state == State.Wander)
        {
            HandleWanderTransitions(isTired);
        }
        else // State.Attack
        {
            HandleAttackTransitions(isTired);
        }
        
        if (_state == State.Wander) DoWander();
        else DoAttack();
    }
    void RegenerateStamina() 
    {
        // Update de la estamina visualmente en la UI
        if(staminaUiImage != null)
            staminaUiImage.fillAmount = currentStamina / maxStamina;
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.fixedDeltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }
    void HandleStun(StunSignal signal) {
        if (signal.activator == gameObject) return;
        StartCoroutine(BotStunRoutine(signal.duration));
    }

    IEnumerator BotStunRoutine(float time) {
        _isStunned = true;
        VfxManager.Instance.SpawnVFX("MegaStun", transform);
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Solo frenar en XZ
        yield return new WaitForSeconds(time);
        _isStunned = false;
    }
    void HandleWanderTransitions(bool isTired)
    {
        // Solo busca un rival si no esta cansado
        if (!isTired)
        {
            var found = FindNearestCompetitor(detectionRadius);
            if (found != null) 
            {
                _target = found;
                _state = State.Attack;
                _attackTimer = attackDuration;
            }
        }
    }

    void HandleAttackTransitions(bool isTired)
    {
        _attackTimer -= Time.fixedDeltaTime;

        // Si se cansa, el tiempo se agota o el target muere entonces vuelve a patrullar
        if (isTired || _attackTimer <= 0f || _target == null || _target.Eliminated)
        {
            _state = State.Wander;
            _target = null;
            PickNewWanderPoint();
        }
    }

    void DoWander()
    {
        _wanderRefreshTimer -= Time.fixedDeltaTime;
        float dist = HorizontalDistanceTo(_wanderPoint);

        if (_wanderRefreshTimer <= 0f || dist <= wanderReachRadius)
        {
            PickNewWanderPoint();
        }

        Vector3 dirToPoint = (_wanderPoint - transform.position).normalized;
    
        // Mezclamos la dirección al punto con una pequeña fuerza de separación
        // pero con un peso muy bajo (0.3f) para que no parezca que huye
        Vector3 sep = ComputeSeparation(null);
        Vector3 finalDir = Vector3.Lerp(dirToPoint, sep, 0.3f).normalized;

        float speed = moveSpeed * wanderSpeedMultiplier;
    
        // Aplicamos el movimiento suavizado que definimos anteriormente
        MoveTowards(finalDir, speed);
        FaceTowards(finalDir);
    }

    void DoAttack()
    {
        if (_target == null || _target.Eliminated)
        {
            _state = State.Wander;
            return;
        }
        
        // Mejora de movimiento
        Vector3 toTarget = _target.transform.position - transform.position;
        Vector3 dirToTarget = Flat(toTarget).normalized;
    
        Vector3 separation = ComputeSeparation(_target);
        
        Vector3 finalDir = dirToTarget;
        if (separation.sqrMagnitude > 0.01f)
        {
            finalDir = Vector3.Lerp(dirToTarget, separation, 0.5f).normalized;
        }
        // Si el target está muy lejos, usamos velocidad de patrulla, si está cerca, velocidad de ataque
        float speed = (toTarget.magnitude > detectionRadius * 0.5f) ? moveSpeed * 0.8f : moveSpeed;

        MoveTowards(finalDir, speed);
        FaceTowards(dirToTarget);
        // Utilizamos la logica de la estamina para realizar el empuje
        _pushCd -= Time.fixedDeltaTime;
        if (_pushCd <= 0f && IsColliderNear(_target, pushContactThreshold))
        {
            float ang = Vector3.Angle(transform.forward, Flat(toTarget).normalized);
            if (ang <= frontAngle && _target.TryGetComponent<Rigidbody>(out var trgRb))
            {
                // Calcular fuerza basada en estamina - minimo 50% de la fuerza base, maximo 150%
                float staminaFactor = Mathf.Lerp(0.5f, maxPushMultiplier, currentStamina / maxStamina);
                float finalPush = pushForce * staminaFactor;
                // Si el objetivo nos está mirando de frente, reducimos la fuerza
                if (Vector3.Dot(transform.forward, _target.transform.forward) < -0.5f)
                {
                    finalPush *= 0.5f;
                }
                trgRb.AddForce(transform.forward * finalPush, ForceMode.Impulse);
            
                // Consumir estamina
                currentStamina -= staminaCostPerPush;
                currentStamina = Mathf.Max(currentStamina, 0);
                _pushCd = pushCooldown;
                _target = null;
                PickNewWanderPoint();
            }
        }
    }

    void MoveTowards(Vector3 dir, float speed)
    {
        Vector3 desiredXZ = (dir != Vector3.zero) ? dir.normalized * speed : Vector3.zero;

        Vector3 flatNow = Flat(_rb.linearVelocity);
        Vector3 flatNext = Vector3.MoveTowards(flatNow, desiredXZ, acceleration * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(flatNext.x, _rb.linearVelocity.y, flatNext.z);
    }

    // Compute separation but optionally ignore the current target so attacker doesn't repel from target.
    Vector3 ComputeSeparation(CollisionMochiPush ignoreTarget)
    {
        float dynamicSeparationRadius = separationRadius; 
    
        var cols = Physics.OverlapSphere(transform.position, dynamicSeparationRadius, ~0, QueryTriggerInteraction.Ignore);
        Vector3 repulse = Vector3.zero; 
        int count = 0;

        foreach (var c in cols)
        {
            if (c.attachedRigidbody == null || c.attachedRigidbody == _rb) continue;
            if (ignoreTarget != null && c.gameObject == ignoreTarget.gameObject) continue;

            Vector3 away = Flat(transform.position - c.transform.position);
            float dist = away.magnitude;
            
            // Si están casi uno encima del otro (dist < 0.5), la fuerza de repulsión es masiva
            float force = (dist < 0.5f) ? 10f : (10f / Mathf.Max(dist, 0.1f));
            // La fuerza de repulsión ahora es inversamente proporcional a la distancia pero con un tope
            repulse += away.normalized * force;
            count++;
        }

        return (count > 0) ? (repulse / count) * separationStrength : Vector3.zero;
    }

    void FaceTowards(Vector3 fallbackDir)
    {
        Vector3 flatVel = Flat(_rb.linearVelocity);
        Vector3 face = flatVel.sqrMagnitude > 0.01f ? flatVel.normalized
                      : (fallbackDir.sqrMagnitude > 0.0001f ? fallbackDir.normalized : transform.forward);

        Quaternion look = Quaternion.LookRotation(face, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.fixedDeltaTime);
    }

    CollisionMochiPush FindNearestCompetitor(float radius)
    {
        CollisionMochiPush bestC = null;
        float best = float.MaxValue;

        // Preferir la lista del GameManager si está poblada
        var source = (gameManager != null && gameManager.combatants != null && gameManager.combatants.Count > 0)
            ? gameManager.combatants.ToArray()
            : FindObjectsOfType<CollisionMochiPush>(true);

        foreach (var c in source)
        {
            if (c == null || c == _self || !c.gameObject.activeInHierarchy || c.Eliminated) continue;
            float dSq = HorizontalSqrDistanceTo(c.transform.position);
            if (dSq <= radius && dSq < best)
            {
                best = dSq; 
                bestC = c;
            }
        }

        return bestC;
    }

    bool IsColliderNear(CollisionMochiPush other, float threshold)
    {
        if (other == null) return false;
        if (_col == null) _col = GetComponent<Collider>();
        if (!other.TryGetComponent<Collider>(out var otherCol)) return false;

        // Closest points entre colliders (0 si overlap)
        Vector3 a = _col.ClosestPoint(otherCol.bounds.center);
        Vector3 b = otherCol.ClosestPoint(_col.bounds.center);
        float separation = Vector3.Distance(a, b);
        if (debugLogs) Debug.Log($"[Bot] {name} sep({other.name}) = {separation:F3}");
        return separation <= Mathf.Max(0.001f, threshold);
    }

    bool IsPointInsideArena(Vector3 p)
    {
        if (!arenaCenter || arenaRadius <= 0f) return true;
        Vector3 c = arenaCenter.transform.position;
        float dx = p.x - c.x, dz = p.z - c.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);
        return dist <= (arenaRadius - edgeSafeMargin * 0.5f);
    }

    bool IsNearEdge(out Vector3 dirToCenter)
    {
        dirToCenter = Vector3.zero;
        if (!arenaCenter || arenaRadius <= 0f) return false;

        Vector2 flatFromCenter = new Vector2(transform.position.x - arenaCenter.transform.position.x,
                                             transform.position.z - arenaCenter.transform.position.z);
        float dist = flatFromCenter.magnitude;
        if (dist > (arenaRadius - edgeSafeMargin))
        {
            Vector3 toCenter = arenaCenter.transform.position - transform.position;
            dirToCenter = Flat(toCenter).normalized;
            return true;
        }
        return false;
    }

    void PickNewWanderPoint()
    {
        if (arenaCenter && arenaRadius > 0f)
        {
            // Buscamos al rival mas cercano
            CollisionMochiPush nearestRival = FindNearestCompetitor(detectionRadius);

            if (nearestRival != null)
            {
                // El siguiente punto de patrulla será cerca del rival encontrado
                Vector3 targetPos = nearestRival.transform.position;
                Vector3 dirFromTarget = (transform.position - targetPos).normalized;

                // Buscamos un punto cercano pero no el mismo del rival, para que parezca un amague
                Vector3 sideStep = Quaternion.Euler(0, Random.Range(60, 120) * (Random.value > 0.5f ? 1 : -1), 0) * dirFromTarget;
                _wanderPoint = targetPos + (sideStep * Random.Range(2f, 4f));
            }
            else
            {
                // Si no hay nadie cerca, patrullar el area central de la arena
                float safeR = arenaRadius;
                Vector2 rnd = Random.insideUnitCircle * safeR;
                _wanderPoint = arenaCenter.transform.position + new Vector3(rnd.x, 0f, rnd.y);
            }
        }

        // Si el punto quedó fuera de la arena, volver al centro
        if (!IsPointInsideArena(_wanderPoint)) 
            _wanderPoint = arenaCenter.transform.position;

        _wanderRefreshTimer = Random.Range(wanderPointRefreshMin, wanderPointRefreshMax);
    }

    Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    float HorizontalDistanceTo(Vector3 worldPos)
    {
        Vector3 d = worldPos - transform.position; d.y = 0f; return d.magnitude;
    }

    float HorizontalSqrDistanceTo(Vector3 worldPos)
    {
        Vector3 d = worldPos - transform.position; d.y = 0f; return d.sqrMagnitude;
    }
}