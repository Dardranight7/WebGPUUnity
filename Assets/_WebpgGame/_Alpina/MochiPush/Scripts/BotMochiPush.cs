using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BotMochiPush : MonoBehaviour
{
    #region Declarations

    [SerializeField] private bool ShowGizmos = false; 
    
    //Basic Components
    public GameManagerMochiPush gameManager;
    private Rigidbody _rb;
    private CollisionMochiPush _self;
    
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float acceleration = 1.5f;   // unidades/s^2, para MoveTowards en velocity
    [SerializeField] private float rotateSpeed = 10f;
    
    //Components used for Stamina or energi mechanic
    [Header("Stamina System")]
    [SerializeField] private Image staminaUiImage;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaCostPerPush = 30f;
    [SerializeField] private float maxPushMultiplier = 1.5f;
    [Header("Arena Components")]
    //Area of displacement components
    [SerializeField] private Transform arenaCenterGO;
    [SerializeField] private float arenaRadius = 2f;
    [SerializeField] private float edgeSafeMargin = 0.3f;
    
    // Wander state
    [Header("Wander System")]
    [SerializeField] private bool isWaiting = false;
    [SerializeField] private bool _isRecoveringFromHit = false;
    [SerializeField] private float postAttackWaitTime = 1.2f;
    [SerializeField] private Vector3 _wanderPoint;
    [SerializeField] private  float wanderSpeedMultiplier = 0.85f;
    [SerializeField] private  float wanderPointRefreshMin = 1.6f;
    [SerializeField] private  float wanderPointRefreshMax = 2.6f;
    [SerializeField] private  float wanderReachRadius = 0.45f;
    // Attack State
    [Header("Ataque")]
    public float attackRange = 1.2f; // Range to switch from Wander to Attack
    public float attackSpeedMultiplier = 1.5f;
    public float loseTargetMultiplier = 1.5f;
    public float stopDistance = 0.2f;
    public Transform targetEnemy; // Next Target
    
    //Status System
    [Serializable] public enum BotState { Patrolling, Attacking, Recovering }
    public BotState currentState = BotState.Patrolling;
    private bool _isStunned = false;
    #endregion

    #region UnityFunctions
    
    private void OnEnable() => EventBus<StunSignal>.OnEvent += HandleStun;
    private void OnDisable() => EventBus<StunSignal>.OnEvent -= HandleStun;
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _self = GetComponent<CollisionMochiPush>();
        if (!gameManager)
            gameManager = FindFirstObjectByType<GameManagerMochiPush>();
    }
    void Start() => currentStamina = maxStamina;

    void FixedUpdate()
    {
        if(!_self.canMove)
            return;
        RegenerateStamina();
        if (_isStunned || (_self != null && _self.Eliminated)) return;

        bool isTired = currentStamina < (maxStamina * 0.2f); // if less than 20% stamin, bot its tired

        if (isTired)
        {
            DoWander();
            return;
        }
        //Avoid the edges
        if (IsNearEdge(out var dirToCenter))
        {
            currentState = BotState.Recovering;
            MoveAndFace(dirToCenter, moveSpeed * 1.1f);
            return;
        }

        CheckEnemies();

        if (currentState == BotState.Attacking && targetEnemy != null && !isTired)
        {
            isWaiting = false;
            Vector3 dir = targetEnemy.position - transform.position;
            dir.y = 0f;
            float distSq = dir.sqrMagnitude;

            if (distSq < stopDistance)
            {
                // Frenar suavemente si ya estamos "encima" del enemigo para evitar jitter
                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
                FaceTowards(dir);
            }
            else
            {
                MoveAndFace(dir.normalized, moveSpeed * attackSpeedMultiplier);
            }
        }
        // 3. PATRULLA
        else
        {
            DoWander();
        }
        // if (currentState == BotState.Attacking)
        // {
        //     //Look for attacking state and no movement detected to reset movement and prevent stuck
        //     if (_rb.linearVelocity.magnitude < 0.1f)
        //     {
        //         currentState = BotState.Patrolling;
        //         targetEnemy = null;
        //     }
        // }
    }

    #endregion

    #region Wandering
    /// <summary>
    /// Do wandering arround the walking Area
    /// </summary>
    private void DoWander()
    {
        if (isWaiting) return;

        Vector3 dirToTarget = (_wanderPoint - transform.position);
        dirToTarget.y = 0;

        if (dirToTarget.sqrMagnitude < wanderReachRadius)
            StartCoroutine(WaitAndSelectNewPoint());
        else
            MoveAndFace(dirToTarget.normalized, moveSpeed * wanderSpeedMultiplier);
    }
    IEnumerator WaitAndSelectNewPoint()
    {
        isWaiting = true;
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Frenar al llegar
        yield return new WaitForSeconds(Random.Range(wanderPointRefreshMin, wanderPointRefreshMax)); // Tiempo de espera aleatorio
        GetRandomPointInArena();
        isWaiting = false;
    }
    /// <summary>
    /// Select a new Wander Point
    /// Its uses the arenaCenter and Radius to prevent bot select a point to far or outside the Walking Area
    /// </summary>
    private void GetRandomPointInArena()
    {
        // Generamos un punto aleatorio dentro de un círculo de radio 1
        Vector2 randomCirclePoint = Random.insideUnitCircle;
    
        // Lo escalamos al tamaño de la arena (restando el margen de seguridad)
        float safeRadius = arenaRadius - (edgeSafeMargin * 1.5f);
        Vector3 randomPos = new Vector3(randomCirclePoint.x * safeRadius, 0, randomCirclePoint.y * safeRadius);

        // Lo sumamos a la posición del centro de la arena
        _wanderPoint = arenaCenterGO.transform.position + randomPos;
    }
    #endregion

    #region Attacking
    void CheckEnemies()
    {
        if (_isRecoveringFromHit || currentStamina < staminaCostPerPush)
        {
            currentState = BotState.Recovering;
            return;
        }
        if (targetEnemy != null)
        {
            CollisionMochiPush enemyScript = targetEnemy.GetComponent<CollisionMochiPush>();
            float distSq = (targetEnemy.position - transform.position).sqrMagnitude;

            // Histéresis: Ahora comparamos distancia al cuadrado contra rango al cuadrado
            if (enemyScript.Eliminated || distSq > loseTargetMultiplier) 
            {
                targetEnemy = null;
                currentState = BotState.Patrolling;
            }
            return;
        }

        // Buscar el más cercano de la lista
        float closestDistSq = attackRange * attackRange;
        foreach (var mochi in gameManager.combatants)
        {
            if (mochi.transform == this.transform || mochi.Eliminated) continue;
            float dSq = (mochi.transform.position - transform.position).sqrMagnitude;
        
            if (dSq < closestDistSq)
            {
                closestDistSq = dSq;
                targetEnemy = mochi.transform;
                currentState = BotState.Attacking;
            
                // IMPORTANTE: Si estábamos esperando en un punto de patrulla, cancelamos la espera
                isWaiting = false; 
            }
        }
    }
    public void NotifyHitSuccessful()
    {
        if (_isRecoveringFromHit) return;

        // Consumir estamina como el player
        currentStamina -= staminaCostPerPush;
        currentStamina = Mathf.Max(currentStamina, 0);

        // Iniciar retroceso y cambio a patrulla
        StartCoroutine(PostAttackRoutine());
    }

    private IEnumerator PostAttackRoutine()
    {
        _isRecoveringFromHit = true;
        targetEnemy = null; 
        currentState = BotState.Patrolling;

        // Pequeño tiempo de "aturdimiento propio" o descanso tras el golpe
        _rb.linearVelocity = Vector3.zero; 
        yield return new WaitForSeconds(postAttackWaitTime);
        
        _isRecoveringFromHit = false;
        GetRandomPointInArena(); // Buscar nuevo rumbo lejos del enemigo anterior
    }

    public float GetCalculatedPushForce(float basePushForce)
    {
        // Fórmula exacta del Player: factor basado en estamina
        float staminaFactor = Mathf.Lerp(0.5f, maxPushMultiplier, currentStamina / maxStamina);
        return basePushForce * staminaFactor;
    }
    #endregion

    #region Stun System
    private void HandleStun(StunSignal signal) {
        if (signal.activator == gameObject) return;
        StartCoroutine(BotStunRoutine(signal.duration));
    }
    private IEnumerator BotStunRoutine(float time) {
        _isStunned = true;
        VfxManager.Instance.SpawnVFX("MegaStun", transform);
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Solo frenar en XZ
        yield return new WaitForSeconds(time);
        _isStunned = false;
    }
    #endregion
    #region Stamina system
    private void RegenerateStamina() 
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
    #endregion
    #region Movement Helpers
    private void MoveAndFace(Vector3 dir, float speed)
    {
        MoveTowards(dir, speed);
        FaceTowards(dir);
    }

    private void MoveTowards(Vector3 dir, float speed)
    {
        Vector3 targetVelocity = dir.normalized * speed;
        targetVelocity.y = _rb.linearVelocity.y; // Mantener gravedad
        _rb.linearVelocity = targetVelocity;
    }

    void FaceTowards(Vector3 targetDir)
    {
        Vector3 face = new Vector3(targetDir.x, 0f, targetDir.z);
        if (face.sqrMagnitude < 0.01f) return; // Umbral de estabilidad
        
        Quaternion look = Quaternion.LookRotation(face.normalized, Vector3.up);
        // Usar fixedDeltaTime para rotación en FixedUpdate
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.fixedDeltaTime);
    }
    #endregion
    #region Extra Functions
    bool IsNearEdge(out Vector3 dirToCenter)
    {
        dirToCenter = Vector3.zero;
        Vector3 offset = transform.position - arenaCenterGO.transform.position;
        offset.y = 0;

        float dangerStartDistance = arenaRadius - edgeSafeMargin;
        if (offset.sqrMagnitude > (dangerStartDistance * dangerStartDistance))
        {
            Debug.DrawRay(transform.position, -offset.normalized * 2f, Color.magenta); // Flecha visual hacia el centro
            dirToCenter = -offset.normalized;
            return true;
        }

        return false;
    }
    #endregion
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (ShowGizmos)
        {
            // Círculo del Radio Total de la Arena
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(arenaCenterGO ? arenaCenterGO.position : Vector3.zero, arenaRadius);

            // Círculo de la Zona Segura (donde empieza el IsNearEdge)
            Gizmos.color = Color.red;
            float safeDist = arenaRadius - edgeSafeMargin;
            Gizmos.DrawWireSphere(arenaCenterGO ? arenaCenterGO.position : Vector3.zero, safeDist);

            // Rango de Detección de Ataque
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            // Rango de Pérdida de Objetivo (VERDE)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, attackRange * loseTargetMultiplier);
            
            // Punto de Wander actual
            if (currentState == BotState.Patrolling)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, _wanderPoint);
                Gizmos.DrawSphere(_wanderPoint, 0.1f);
            }
        }
    }

    #endregion
}