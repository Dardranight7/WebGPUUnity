using System;
using UnityEngine;
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
    public float moveSpeed = 4.0f;
    public float acceleration = 20f;   // unidades/s^2, para MoveTowards en velocity
    public float rotateSpeed = 10f;

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
            Debug.LogWarning($"[BotMochiPush] Se detectaron constraints de POSICIÓN en {name}. Se quitarán para permitir movimiento XZ. (Se conservarán rotaciones bloqueadas)");
            _rb.constraints = (_rb.constraints & ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ)) | desired;
        }
        else
        {
            _rb.constraints = (_rb.constraints | desired);
        }
    }

    void Start()
    {
        PickNewWanderPoint();
        if (debugLogs) Debug.Log($"[Bot] {name} start wanderPoint -> {_wanderPoint}");
    }

    void FixedUpdate()
    {
        if (_self != null && _self.Eliminated) return;

        // si estamos muy cerca del borde volvemos al centro
        if (IsNearEdge(out var dirToCenter))
        {
            Vector3 smoothedDir = Vector3.Lerp(transform.forward, dirToCenter, 0.5f).normalized;
            MoveTowards(smoothedDir, moveSpeed);
            FaceTowards(smoothedDir);
            return;
        }

        // Estado / detección
        if (_state == State.Wander)
        {
            var found = FindNearestCompetitor(detectionRadius);
            if (found != null && HorizontalDistanceTo(found.transform.position) > separationRadius * 1.2f) 
            {
                _target = found;
                _state = State.Attack;
                _attackTimer = attackDuration;
                _nextTargetCheck = Time.time + recheckTargetsEvery;
                if (debugLogs) Debug.Log($"[Bot] {name} entra en ATTACK a {_target.name}");
            }
        }
        else // Attack
        {
            _attackTimer -= Time.fixedDeltaTime;
            if (Time.time >= _nextTargetCheck)
            {
                if (_target == null || !_target.gameObject.activeInHierarchy || _target.Eliminated || HorizontalDistanceTo(_target.transform.position) < separationRadius * 0.8f)
                    _target = FindNearestCompetitor(detectionRadius);
                _nextTargetCheck = Time.time + recheckTargetsEvery;
            }
            if (_attackTimer <= 0f)
            {
                _state = State.Wander;
                _target = null;
                if (debugLogs) Debug.Log($"[Bot] {name} vuelve a WANDER");
            }
        }

        // Ejecutar comportamiento por estado
        if (_state == State.Wander) DoWander();
        else DoAttack();
    }

    void DoWander()
    {
        // refrescar punto si expira o si está cerca
        _wanderRefreshTimer -= Time.fixedDeltaTime;
        float dist = HorizontalDistanceTo(_wanderPoint);

        if (Random.value < 0.05f) return;

        if (_wanderRefreshTimer <= 0f || dist <= wanderReachRadius || !IsPointInsideArena(_wanderPoint))
        {
            PickNewWanderPoint();
            dist = HorizontalDistanceTo(_wanderPoint);
            if (debugLogs) Debug.Log($"[Bot] {name} nuevo wanderPoint -> {_wanderPoint}");
        }

        Vector3 dir = Flat(_wanderPoint - transform.position);
        if (dir.sqrMagnitude < 0.0001f) { PickNewWanderPoint(); dir = Flat(_wanderPoint - transform.position); }
        dir = dir.normalized;

        // arrival: desacelera al acercarse
        float t = Mathf.InverseLerp(stopRadius, arriveRadius, Mathf.Clamp(dist, stopRadius, arriveRadius));
        float speed = Mathf.Lerp(moveSpeed * 0.4f, moveSpeed * wanderSpeedMultiplier, t);

        // separación (considera TODOS los otros bots en wander)
        Vector3 sep = ComputeSeparation(ignoreTarget: null);
        float sepWeight = 1.2f;
        Vector3 desired = dir + sep * sepWeight;
        if (desired.sqrMagnitude < 0.0001f) desired = dir;
        desired.Normalize();
        
        desired = Quaternion.Euler(0, Random.Range(-8f, 8f), 0) * desired;

        MoveTowards(desired, speed);
        FaceTowards(desired);
    }

    void DoAttack()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy || _target.Eliminated)
        {
            // Si no hay target válido, caminar (para no quedarse quieto)
            DoWander();
            return;
        }

        Vector3 toTarget = _target.transform.position - transform.position;
        Vector3 toTargetFlat = Flat(toTarget);
        float dist = toTargetFlat.magnitude;

        // Anti-órbita: frenar componente tangencial si se genera
        if (dist <= antiOrbitRadius && dist > 0.001f)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, toTargetFlat.normalized);
            Vector3 flatVel = Flat(_rb.linearVelocity);
            float tangential = Vector3.Dot(flatVel, tangent);
            _rb.AddForce(-tangent * (tangential * tangentialBrake), ForceMode.Acceleration);
        }

        // acercarse con arrival
        Vector3 dir = dist > 0.001f ? toTargetFlat.normalized : transform.forward;
        float t = Mathf.InverseLerp(stopRadius, arriveRadius, Mathf.Clamp(dist, stopRadius, arriveRadius));
        float speed = Mathf.Lerp(moveSpeed * 0.45f, moveSpeed, t);

        // separación: IGNORAR el target en la separación para no "huir" de él
        Vector3 sep = ComputeSeparation(ignoreTarget: _target);
        float sepWeightAttack = 0.45f; // menos influencia durante el ataque
        Vector3 desired = dir + sep * sepWeightAttack;
        if (desired.sqrMagnitude < 0.0001f) desired = dir;
        desired.Normalize();

        MoveTowards(desired, speed);
        FaceTowards(desired);

        // intento de empuje: SOLO si colliders prácticamente tocan (ClosestPoint)
        _pushCd -= Time.fixedDeltaTime;
        if (_pushCd <= 0f && IsColliderNear(_target, pushContactThreshold))
        {
            float ang = Vector3.Angle(transform.forward, toTargetFlat.normalized);
            if (ang <= frontAngle && _target.TryGetComponent<Rigidbody>(out var trgRb))
            {
                if (debugLogs) Debug.Log($"[Bot] {name} empuja a {_target.name} (sep <= {pushContactThreshold})");
                trgRb.AddForce(transform.forward * pushForce, ForceMode.Impulse);
                _pushCd = pushCooldown;
            }
        }

        if (debugLogs && _target != null)
        {
            // útil para ver por qué "huyen": imprime separación respecto al target y la fuerza de repulsión aplicada
            if (_col != null && _target.TryGetComponent<Collider>(out var otherCol))
            {
                Vector3 a = _col.ClosestPoint(otherCol.bounds.center);
                Vector3 b = otherCol.ClosestPoint(_col.bounds.center);
                float separation = Vector3.Distance(a, b);
                Debug.Log($"[Bot] {name} sep({_target.name}) = {separation:F3} desiredDir={desired} vel={Flat(_rb.linearVelocity)}");
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
        if (separationRadius <= 0.01f) return Vector3.zero;

        var cols = Physics.OverlapSphere(transform.position, separationRadius, ~0, QueryTriggerInteraction.Ignore);
        Vector3 repulse = Vector3.zero; int count = 0;

        foreach (var c in cols)
        {
            if (c.attachedRigidbody == null || c.attachedRigidbody == _rb) continue;
            if (!c.TryGetComponent<CollisionMochiPush>(out var other)) continue;
            if (other == _self || !other.gameObject.activeInHierarchy) continue;
            if (ignoreTarget != null && other == ignoreTarget) continue; // <- IGNORA EL TARGET

            Vector3 away = Flat(transform.position - other.transform.position);
            float d = Mathf.Max(away.magnitude, 0.001f);
            float str = separationStrength / Mathf.Max(d, 0.25f);
            // cap individual contribution so one close bot can't dominate
            float cap = separationStrength * 0.9f;
            Vector3 contrib = away.normalized * Mathf.Min(str, cap);
            repulse += contrib;
            count++;
        }

        if (count > 0)
        {
            Vector3 avg = repulse / count;
            // limiter global: no más de separationStrength magnitude
            if (avg.magnitude > separationStrength)
                avg = avg.normalized * separationStrength;
            return avg;
        }

        return Vector3.zero;
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
            if (dSq <= radius * radius && dSq < best) { best = dSq; bestC = c; }
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
            float maxR = Mathf.Max(0.5f, arenaRadius - edgeSafeMargin - 0.5f);
            Vector2 rnd = Random.insideUnitCircle * Random.Range(maxR * 0.2f, maxR);
            _wanderPoint = arenaCenter.transform.position + new Vector3(rnd.x, 0f, rnd.y);
        }
        else
        {
            Vector2 rnd = Random.insideUnitCircle * Random.Range(2f, 6f);
            _wanderPoint = transform.position + new Vector3(rnd.x, 0f, rnd.y);
        }
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