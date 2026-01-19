using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BotControllerBonCollect : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 3.0f;
    public float maxAcceleration = 8f;
    public float rotationSpeed = 8f;
    public float checkInterval = 0.25f;
    public float reachDistance = 0.6f;

    [Header("Control de Dificultad (0 a 1)")]
    [Range(0f, 1f)]
    public float difficultyFactor = 0.5f;
    public float baseSeparationRadius = 1.0f;
    public float maxSeparationWeight = 2.0f;
    public float maxPredictionFactor = 0.4f;
    public float maxObstacleAvoidDistance = 1.5f;

    [Header("Comportamiento")]
    public float retargetCooldown = 0.4f;
    public float targetCloserMargin = 0.8f;
    public LayerMask obstacleMask;

    [Header("Empuje y Audio")]
    public float pushForce = 2f;
    public AudioSource audioSource;
    public AudioClip pushClip;
    [Header("Vfx positions")] 
    [SerializeField] private Transform feetVFXTransform;
    [SerializeField] private Transform headVFXTransform;
    [SerializeField] Rigidbody rb;
    Collector collector;
    Transform targetCandy;
    Rigidbody targetCandyRb;

    Vector3 velocity = Vector3.zero;
    Vector3 randomOffset;
    float lastTargetTime = -10f;

    // Propiedades calculadas en base a la dificultad
    float CurrentSeparationRadius => baseSeparationRadius * (1f + difficultyFactor * 0.5f);
    float CurrentSeparationWeight => maxSeparationWeight * difficultyFactor;
    float CurrentPredictionFactor => maxPredictionFactor * difficultyFactor;
    float CurrentObstacleAvoidDistance => maxObstacleAvoidDistance * difficultyFactor;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collector = GetComponent<Collector>();

        // Configuración de Rigidbody
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // offset único por bot para evitar congregación exacta
        randomOffset = new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
    }

    void OnEnable() => StartCoroutine(DecisionLoop());
    void OnDisable() => StopAllCoroutines();

    IEnumerator DecisionLoop()
    {
        while (true)
        {
            TryFindBestCandy();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    // Mantener la función de aumento temporal de velocidad
    public void UpdateSpeed(float speedFactor)
    {
        maxSpeed += (maxSpeed * speedFactor);
        switch (speedFactor)
        {
            case > 1f:
                VfxManager.Instance.SpawnVFX("SpeedGummyVfx", feetVFXTransform);
                break;
            case < 0f:
                VfxManager.Instance.SpawnVFX("BadGummyVfx", headVFXTransform);
                break;
        }
        StartCoroutine(RestoreSpeed());
    }

    private IEnumerator RestoreSpeed()
    {
        yield return new WaitForSeconds(2f);
        maxSpeed = 3.0f; 
    }

    void FixedUpdate()
    {
        // posición en XZ
        Vector3 pos = rb.position;

        if (targetCandy == null)
        {
            // desacelerar suavemente a 0 cuando no hay objetivo
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, maxAcceleration * Time.fixedDeltaTime);
            ApplyMovement(velocity);
            return;
        }

        Vector3 targetMovement = CalculateTargetMovement(pos);

        // Si está cerca del objetivo, desacelerar y detener el cálculo de fuerzas
        if (targetMovement.magnitude < reachDistance)
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, maxAcceleration * Time.fixedDeltaTime * 2f);
            ApplyMovement(velocity);
            return;
        }

        Vector3 desiredVel = targetMovement.normalized * maxSpeed;

        // Comportamiento dinámico según la dificultad
        Vector3 separation = ComputeSeparation() * CurrentSeparationWeight;
        Vector3 avoidance = ComputeAvoidance();
        
        // Simula el comportamiento de "jugador" mediante la combinación de estas fuerzas
        Vector3 desiredWithForces = desiredVel + separation + avoidance;

        Vector3 steering = desiredWithForces - velocity;

        // Limitar y aplicar rotación o giro.
        float maxSteer = maxAcceleration * Time.fixedDeltaTime;
        if (steering.magnitude > maxSteer)
            steering = steering.normalized * maxSteer;

        velocity += steering;
        float verticalVelocity = velocity.y;
        velocity.y = 0; // Temporarily ignore Y for XZ magnitude calculation
        
        if (velocity.magnitude > maxSpeed)
        {
            // Normalizar (obtener dirección) y multiplicar por el límite
            velocity = velocity.normalized * maxSpeed;
        }

        velocity.y = verticalVelocity;
        
        ApplyMovement(velocity); // Aquí solo se usa X y Z, garantizando que el movimiento horizontal esté limitado por maxSpeed.


        if (velocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            Quaternion s = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(s);
        }
    }

    // Nueva función para calcular el movimiento hacia el objetivo
    Vector3 CalculateTargetMovement(Vector3 currentPos)
    {
        Vector3 predictedTargetPos = targetCandy.position;
        
        // Predicción de objetivo: Más activa con mayor dificultad
        if (targetCandyRb != null)
            predictedTargetPos += targetCandyRb.linearVelocity * CurrentPredictionFactor;

        predictedTargetPos += randomOffset;

        Vector3 toTarget = predictedTargetPos - currentPos;
        toTarget.y = 0f;
        return toTarget;
    }

    void ApplyMovement(Vector3 vel)
    {
        Vector3 move = new Vector3(vel.x, 0f, vel.z) * Time.fixedDeltaTime;
        Vector3 newPos = rb.position + move;
        rb.MovePosition(newPos);
    }

    // MODOFIEDÑ radio y peso escalan con la dificultad
    Vector3 ComputeSeparation()
    {
        Vector3 sep = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, CurrentSeparationRadius);
        int count = 0;
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == this.gameObject) continue;
            
            // Revisa si es otro bot o personaje( podemos mejorarlo eliminando el getcomponent, pero como?)
            var other = c.GetComponentInParent<Collector>(); 
            if (other == null) continue;
            
            Vector3 away = transform.position - c.transform.position;
            away.y = 0f;
            float d = away.magnitude;
            
            if (d > 0.001f)
            {
                // Inverso de la distancia (más cerca = más fuerza)
                sep += away.normalized / d; 
                count++;
            }
        }
        if (count > 0)
            return (sep / count).normalized;
        return Vector3.zero;
    }

    // Evasion de obstaculos: distancia de raycast
    Vector3 ComputeAvoidance()
    {
        // Con dificultad 0, la distancia de evasion será 0.
        if (CurrentObstacleAvoidDistance <= 0.01f) return Vector3.zero;
        
        // Raycast hacia adelante en la dirección actual deseada
        Vector3 ahead = (velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward);
        Vector3 origin = rb.position + Vector3.up * 0.25f;
        RaycastHit hit;
        
        if (Physics.SphereCast(origin, 0.25f, ahead, out hit, CurrentObstacleAvoidDistance, obstacleMask))
        {
            // vector que empuja lateralmente
            Vector3 away = (origin - hit.point);
            away.y = 0f;
            
            // La fuerza de evasión depende de la velocidad actual
            return away.normalized * (maxSpeed); 
        }
        return Vector3.zero;
    }

    void TryFindBestCandy()
    {
        // Se mantiene la lógica de elegir la más cercana, pero con el cooldown
        // y la mejora condicional (targetCloserMargin) para evitar *flickering* de objetivo.
        // se puede agregar la probabilidad de evitar gomitas negativas
        //TODO
        
        GameObject[] candies = GameObject.FindGameObjectsWithTag("Candy");
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var c in candies)
        {
            if (c == null) continue;
            float d = Vector3.SqrMagnitude(c.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }

        if (best == null)
        {
            targetCandy = null;
            targetCandyRb = null;
            return;
        }

        // Si no tenía target o si el cooldown pasó, retargetear.
        if (targetCandy == null || Time.time - lastTargetTime > retargetCooldown)
        {
            targetCandy = best;
            targetCandyRb = best.GetComponent<Rigidbody>();
            lastTargetTime = Time.time;
            return;
        }

        // Si el nuevo candidato está más cerca, cambiar de objetivo.
        float newDist = Vector3.SqrMagnitude(best.position - transform.position);
        float oldDist = Vector3.SqrMagnitude(targetCandy.position - transform.position);

        if (newDist < oldDist * targetCloserMargin)
        {
            targetCandy = best;
            targetCandyRb = best.GetComponent<Rigidbody>();
            lastTargetTime = Time.time;
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        Rigidbody other = collision.rigidbody;
        if (other != null && other != rb)
        {
            Vector3 toOther = collision.transform.position - transform.position;
            Vector3 toOtherFlat = new Vector3(toOther.x, 0f, toOther.z).normalized;
            float angle = Vector3.Angle(transform.forward, toOtherFlat);

            float frontAngle = 100; 
            if (angle <= frontAngle)
            {
                other.AddForce(transform.forward * pushForce, ForceMode.Impulse); 

                if (audioSource != null && pushClip != null)
                    audioSource.PlayOneShot(pushClip);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Alcance de deteccion de gomitas
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reachDistance);
        
        // Radio de separación actual
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, CurrentSeparationRadius);
        
        // Distancia de evasión de obstáculos actual
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rb.position + Vector3.up * 0.25f, rb.position + Vector3.up * 0.25f + (velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward) * CurrentObstacleAvoidDistance);
    }
}