using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BotControllerBonCollet: MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 3.0f;             // velocidad 
    public float maxAcceleration = 8f;        // cuanto puede cambiar la velocidad por segundo
    public float rotationSpeed = 8f;          // slerp para rotación visual
    public float checkInterval = 0.25f;       // cada cuánto busca candy (segundos)
    public float reachDistance = 0.6f;        // distancia para considerar recogida
    public float separationRadius = 1.0f;     // radio para separación de otros agents
    public float separationWeight = 1.2f;     // fuerza de separación
    private bool hasPowerUp = false;

    [Header("Comportamiento adicional")]
    public float retargetCooldown = 0.4f;     // mínimo tiempo entre cambios de objetivo
    public float targetCloserMargin = 0.8f;   // para considerar cambiar target: nuevoDist < oldDist * margin
    public float predictionFactor = 0.2f;     // cuánto "adelantar" la posición de la candy (si tiene Rigidbody)
    public float obstacleAvoidDistance = 0.8f; // distancia de raycast para evitar obstáculos
    public LayerMask obstacleMask;            // layers que consideramos obstáculo

    Rigidbody rb;
    Collector collector;
    Transform targetCandy;
    Rigidbody targetCandyRb;

    Vector3 velocity = Vector3.zero;         // velocidad actual en plano XZ
    Vector3 randomOffset;                    // ligera desviación para cada bot
    float lastTargetTime = -10f;
    Vector3 lastTargetPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collector = GetComponent<Collector>();

        // Rigidbody recomendado (asegúrate en Inspector también)
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // offset único por bot para evitar congregación exacta
        randomOffset = new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
    }

    void OnEnable()
    {
        StartCoroutine(DecisionLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator DecisionLoop()
    {
        while (true)
        {
            TryFindBestCandy();
            yield return new WaitForSeconds(checkInterval);
        }
    }
    public void UpdateSpeed(float speedFactor)
    {
        maxSpeed += (maxSpeed * speedFactor);
        hasPowerUp = true;
        StartCoroutine(RestoreSpeed());
    }

    private IEnumerator RestoreSpeed()
    {
        yield return 2f;
        maxSpeed = 2.5f;
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

        // calcular objetivo previsto (predicción si la candy tiene rigidbody)
        Vector3 predictedTargetPos = targetCandy.position;
        if (targetCandyRb != null)
            predictedTargetPos += targetCandyRb.linearVelocity * predictionFactor;

        predictedTargetPos += randomOffset; // evita que todos vayan exactamente al mismo punto

        Vector3 toTarget = predictedTargetPos - pos;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist < reachDistance)
        {
            // cerca: desacelerar para permitir trigger de recogida
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, maxAcceleration * Time.fixedDeltaTime * 2f);
            ApplyMovement(velocity);
            return;
        }

        Vector3 desiredVel = toTarget.normalized * maxSpeed;

        // separation
        Vector3 separation = ComputeSeparation() * separationWeight;

        // obstacle avoidance simple (raycast ahead)
        Vector3 avoidance = ComputeAvoidance();

        Vector3 desiredWithSep = desiredVel + separation + avoidance;
        Vector3 steering = desiredWithSep - velocity;

        // limitar steering
        float maxSteer = maxAcceleration * Time.fixedDeltaTime;
        if (steering.magnitude > maxSteer)
            steering = steering.normalized * maxSteer;

        // integrar velocidad
        velocity += steering;

        // limitar velocidad
        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        ApplyMovement(velocity);

        // rotación visual suave
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            Quaternion s = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(s);
        }
    }

    void ApplyMovement(Vector3 vel)
    {
        Vector3 move = new Vector3(vel.x, 0f, vel.z) * Time.fixedDeltaTime;
        Vector3 newPos = rb.position + move;
        rb.MovePosition(newPos);
    }

    Vector3 ComputeSeparation()
    {
        Vector3 sep = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);
        int count = 0;
        foreach (var c in hits)
        {
            if (c == null) continue;
            if (c.gameObject == this.gameObject) continue;
            var other = c.GetComponentInParent<Collector>();
            if (other == null) continue;
            Vector3 away = transform.position - c.transform.position;
            away.y = 0f;
            float d = away.magnitude;
            if (d > 0.001f)
            {
                sep += away.normalized / d;
                count++;
            }
        }
        if (count > 0)
            return (sep / count).normalized;
        return Vector3.zero;
    }

    Vector3 ComputeAvoidance()
    {
        // Raycast hacia adelante en la dirección actual deseada
        Vector3 ahead = (velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward);
        Vector3 origin = rb.position + Vector3.up * 0.25f;
        RaycastHit hit;
        if (Physics.SphereCast(origin, 0.25f, ahead, out hit, obstacleAvoidDistance, obstacleMask))
        {
            // devolver vector que empuja lateralmente para evitar obstáculo
            Vector3 away = (origin - hit.point);
            away.y = 0f;
            return away.normalized * (maxSpeed * 0.8f);
        }
        return Vector3.zero;
    }

    void TryFindBestCandy()
    {
        // optimizado: evita retargeting muy frecuente
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

        // Si no tenía target, elegir el mejor
        if (targetCandy == null)
        {
            targetCandy = best;
            targetCandyRb = best.GetComponent<Rigidbody>();
            lastTargetTime = Time.time;
            lastTargetPos = best.position;
            return;
        }

        // si hay un nuevo candidato mucho más cercano o pasó cooldown, retarget
        float newDist = Vector3.SqrMagnitude(best.position - transform.position);
        float oldDist = Vector3.SqrMagnitude(targetCandy.position - transform.position);

        if ((newDist < oldDist * targetCloserMargin) || (Time.time - lastTargetTime > retargetCooldown))
        {
            targetCandy = best;
            targetCandyRb = best.GetComponent<Rigidbody>();
            lastTargetTime = Time.time;
            lastTargetPos = best.position;
        }
    }

    // Opcional: dibuja gizmos para debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reachDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.forward * 0.5f));
    }
}