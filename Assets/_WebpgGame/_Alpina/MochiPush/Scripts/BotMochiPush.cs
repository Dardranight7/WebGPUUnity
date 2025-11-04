using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CollisionMochiPush))]
public class BotMochiPush : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] GameManagerMochiPush gameManager;    
    [SerializeField] Transform arenaCenter;
    [SerializeField] float arenaRadius = 12f;

    [Header("Movimiento (más natural)")]
    public float moveSpeed = 1f;            
    public float acceleration = 2f;          
    public float rotateSpeed = 2f;           

    [Header("Micro-variación de trayectoria")]
    public float jitterAmplitude = 0.25f;     
    public float jitterFrequency = 0.9f;     

    [Header("Empujón (solo de frente)")]
    public float pushForce = 1.0f;            // Empujón pequeño
    public float pushRange = 1.2f;
    [Range(0f, 90f)] public float frontAngle = 35f;
    public float pushCooldown = 0.6f;

    [Header("Borde")]
    public float edgeSafeMargin = 2f;         // Si está cerca del borde, vuelve al centro con prioridad

    [Header("Evitar pegado")]
    public float separationRadius = 0.9f;     // Radio para repulsión suave entre bots
    public float separationStrength = 2.2f;   // Intensidad de repulsión

    [Header("Desatascador")]
    public float stuckCheckTime = 0.8f;       // Tiempo sin moverse para aplicar nudge
    public float stuckMinDistance = 0.05f;    // Distancia mínima de movimiento para resetear
    public float unstuckImpulse = 2.0f;       // Impulso lateral para desatascar

    [Header("Cambio de objetivo")]
    public float maxFocusTime = 5.0f;         // Si no progresa en 5s, cambia de objetivo
    public float progressEpsilon = 0.2f;      // Umbral de mejora de distancia para considerar progreso
    public float avoidSameTargetCooldown = 3f;// Evita volver al mismo objetivo inmediatamente

    Rigidbody _rb;
    CollisionMochiPush _self;
    CollisionMochiPush _target;

    float _nextRepath;
    float _cd;

    // Estado para naturalidad
    Vector3 _lastPos;
    float _stuckTimer;

    // Estado de "progreso" y cambio de objetivo
    float _focusTimer;
    float _bestDistanceThisFocus = float.MaxValue;
    readonly Dictionary<int, float> _avoidTargetUntil = new Dictionary<int, float>();

    // Ruido lateral estable por instancia
    float _noiseSeed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _self = GetComponent<CollisionMochiPush>();

        

        _noiseSeed = (Mathf.Abs(GetInstanceID()) % 1000) * 0.137f; // semilla estable por objeto
    }

    void Start()
    {
        if (!arenaCenter && gameManager) arenaCenter = gameManager.arenaCenter;
        if (arenaRadius <= 0f && gameManager) arenaRadius = gameManager.arenaRadius;

        _lastPos = transform.position;
    }

    void FixedUpdate()
    {
        if (_self.Eliminated) return;

        // Reevaluar objetivo periódicamente
        if (Time.time >= _nextRepath || _target == null || !_target.gameObject.activeInHierarchy)
        {
            AcquireTarget();
            _nextRepath = Time.time + Random.Range(0.25f, 0.4f);
        }

        UpdateFocusAndMaybeSwitchTarget();

        // Dirección base
        Vector3 dir = Vector3.zero;

        // 1) Evitar borde (máxima prioridad)
        if (arenaCenter && arenaRadius > 0f)
        {
            Vector2 flatFromCenter = new Vector2(transform.position.x - arenaCenter.position.x,
                                                 transform.position.z - arenaCenter.position.z);
            float distEdge = flatFromCenter.magnitude;

            if (distEdge > (arenaRadius - edgeSafeMargin))
            {
                Vector3 toCenter = arenaCenter.position - transform.position;
                dir = new Vector3(toCenter.x, 0f, toCenter.z).normalized;
            }
        }

        // 2) Perseguir objetivo (sin orbitar)
        if (dir == Vector3.zero && _target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = _target.transform.position - transform.position;
            Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
            float dist = toTargetFlat.magnitude;

            // Micro variación lateral con Perlin para que no parezca robótico (no orbita)
            Vector3 baseDir = toTargetFlat.sqrMagnitude > 0.001f ? toTargetFlat.normalized : transform.forward;
            Vector3 lateral = Vector3.Cross(Vector3.up, baseDir); // derecha
            float n = Mathf.PerlinNoise(Time.time * jitterFrequency + _noiseSeed, 0f) * 2f - 1f; // [-1,1]
            Vector3 jitter = lateral * (n * jitterAmplitude);

            // Mantener una ligera distancia objetivo para no chocar y quedarse pegado
            float desiredApproach = Mathf.Max(0.9f, pushRange * 0.9f);
            Vector3 forwardBias = baseDir * Mathf.Clamp01((dist - desiredApproach) / Mathf.Max(0.001f, desiredApproach));

            dir = (baseDir + jitter + forwardBias).normalized;
        }

        // 3) Separación local (repulsión suave)
        if (separationRadius > 0.05f)
        {
            var cols = Physics.OverlapSphere(transform.position, separationRadius, ~0, QueryTriggerInteraction.Ignore);
            Vector3 repulse = Vector3.zero;
            foreach (var c in cols)
            {
                if (c.attachedRigidbody == null || c.attachedRigidbody == _rb) continue;
                if (!c.TryGetComponent<CollisionMochiPush>(out var other)) continue;
                if (other == _self || !other.gameObject.activeInHierarchy) continue;

                Vector3 away = transform.position - other.transform.position;
                Vector3 awayFlat = new Vector3(away.x, 0f, away.z);
                float d = Mathf.Max(awayFlat.magnitude, 0.001f);
                float strength = separationStrength / Mathf.Max(d, 0.25f);
                repulse += awayFlat.normalized * strength;
            }
            if (repulse != Vector3.zero)
                dir = (dir + repulse).normalized;
        }

        // 4) Aplicar movimiento físico (velocidad con aceleración)
        Vector3 desiredVel = dir != Vector3.zero ? dir.normalized * moveSpeed : Vector3.zero;
        desiredVel.y = _rb.linearVelocity.y; // conservar gravedad
        _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, desiredVel, acceleration * Time.fixedDeltaTime);

        // 5) Rotar suavemente hacia la dirección de movimiento horizontal (evita quedarse "girado")
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (flatVel.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.fixedDeltaTime);
        }

        // 6) Empujar si corresponde (solo de frente)
        _cd -= Time.fixedDeltaTime;
        TryPush();

        // 7) Desatascador si apenas se movió
        float moved = (transform.position - _lastPos).magnitude;
        if (moved < stuckMinDistance)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer >= stuckCheckTime)
            {
                // Pequeño impulso aleatorio lateral
                Vector2 rnd = Random.insideUnitCircle.normalized;
                Vector3 nudge = new Vector3(rnd.x, 0f, rnd.y) * unstuckImpulse;
                _rb.AddForce(nudge, ForceMode.Impulse);
                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
        _lastPos = transform.position;
    }

    void UpdateFocusAndMaybeSwitchTarget()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _focusTimer = 0f;
            _bestDistanceThisFocus = float.MaxValue;
            return;
        }

        // Medir progreso de acercamiento
        float currentDist = HorizontalDistanceTo(_target.transform.position);
        if (currentDist + progressEpsilon < _bestDistanceThisFocus)
        {
            _bestDistanceThisFocus = currentDist;
            _focusTimer = 0f; // hubo progreso
        }
        else
        {
            _focusTimer += Time.fixedDeltaTime;
        }

        // Si no hubo progreso por demasiado tiempo, cambia de objetivo
        if (_focusTimer >= maxFocusTime)
        {
            int id = _target.GetInstanceID();
            _avoidTargetUntil[id] = Time.time + avoidSameTargetCooldown;

            AcquireTarget(excludeId: id);
            _focusTimer = 0f;
            _bestDistanceThisFocus = float.MaxValue;
        }
    }

    void AcquireTarget(int excludeId = int.MinValue)
    {
        if (!gameManager) return;

        // Candidates: otros combatientes activos
        var candidates = gameManager.combatants
            .Where(c => c != null && c != _self && c.gameObject.activeInHierarchy)
            .ToList();

        // Filtrar evitados temporalmente
        float now = Time.time;
        candidates.RemoveAll(c =>
        {
            int id = c.GetInstanceID();
            return id == excludeId || (_avoidTargetUntil.TryGetValue(id, out float until) && now < until);
        });

        // Si se quedaron sin candidatos por el filtro, vuelve a permitir todos menos el que no existe
        if (candidates.Count == 0)
        {
            candidates = gameManager.combatants
                .Where(c => c != null && c != _self && c.gameObject.activeInHierarchy)
                .ToList();
        }

        float best = float.MaxValue;
        CollisionMochiPush bestC = null;

        foreach (var c in candidates)
        {
            float d = HorizontalSqrDistanceTo(c.transform.position);
            if (d < best) { best = d; bestC = c; }
        }

        _target = bestC;
        _focusTimer = 0f;
        _bestDistanceThisFocus = float.MaxValue;
    }

    void TryPush()
    {
        if (_cd > 0f || _target == null || !_target.gameObject.activeInHierarchy) return;

        Vector3 toTarget = _target.transform.position - transform.position;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = toTargetFlat.magnitude;
        if (dist > pushRange) return;

        float angle = Vector3.Angle(transform.forward, toTargetFlat.normalized);
        if (angle > frontAngle) return; // No empujar con la espalda

        if (_target.TryGetComponent<Rigidbody>(out var trgRb))
        {
            trgRb.AddForce(transform.forward * pushForce, ForceMode.Impulse);
            _cd = pushCooldown;
        }
    }

    float HorizontalDistanceTo(Vector3 worldPos)
    {
        Vector3 d = worldPos - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    float HorizontalSqrDistanceTo(Vector3 worldPos)
    {
        Vector3 d = worldPos - transform.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }
}