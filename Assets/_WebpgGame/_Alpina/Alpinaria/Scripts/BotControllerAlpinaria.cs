using UnityEngine;

// Bot simple que compite por llegar a la meta.
// Usa un MeshCollider de la pista (asignado en inspector) para "pegado" a superficie.
// No usa waypoints: orden se calcula por distancia a finish (GameManager).
[RequireComponent(typeof(Rigidbody))]
public class BotControllerAlpinaria : MonoBehaviour
{
    public Rigidbody rb;
    [Header("Movimiento")]
    public float baseAccel = 6f;      // aceleración base
    public float maxSpeed = 10f;      // velocidad máxima
    [Range(0.5f, 2f)] public float difficulty = 1f; // multiplica velocidad/accel
    public float lateralRandomness = 0.6f; // ligera desviación para que no sean idénticos

    [Header("Referencias")]
    public MeshCollider trackMeshCollider; // asignar la malla del tobogán en Inspector
    public Transform finishTransform;      // asignar la meta (finish) en Inspector

    [HideInInspector] public bool canMove = false;

    Vector3 randomOffset;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        // generar ligera desviación inicial para diferenciar bots
        randomOffset = new Vector3(Random.Range(-lateralRandomness, lateralRandomness), 0f, Random.Range(-0.4f, 0.4f));
    }

    void FixedUpdate()
    {
        if (!canMove || rb == null || finishTransform == null) return;

        // Intentamos raycastar a la malla para obtener la normal y ajustar movimiento sobre superficie
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Ray ray = new Ray(origin, Vector3.down);

        RaycastHit hit = default;           // declarado pero puede no ser asignado si no hay hit
        bool hasHit = false;      // marcará si hit fue rellenado por un Raycast

        if (trackMeshCollider != null)
        {
            // Collider.Raycast asigna 'hit' mediante out si devuelve true
            if (trackMeshCollider.Raycast(ray, out hit, 3f))
            {
                hasHit = true;
            }
        }

        // fallback general si no impactó la malla asignada
        if (!hasHit)
        {
            if (Physics.Raycast(ray, out hit, 3f))
            {
                hasHit = true;
            }
        }

        // Dirección hacia la meta (incluye la desviación aleatoria para no ser robots exactos)
        Vector3 toFinish = (finishTransform.position - transform.position) + randomOffset;
        Vector3 desiredDir;

        if (hasHit)
        {
            // Aquí sólo usamos hit.normal si hasHit == true => evita error de variable no asignada
            desiredDir = Vector3.ProjectOnPlane(toFinish.normalized, hit.normal).normalized;
        }
        else
        {
            // sin referencia de suelo: mover horizontalmente hacia la meta
            desiredDir = toFinish.normalized;
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude == 0f) desiredDir = transform.forward;
        }

        // aplicar fuerza en la dirección deseada (simula deslizamiento)
        Vector3 accel = desiredDir * baseAccel * difficulty;
        rb.AddForce(accel, ForceMode.Acceleration);

        // limitar velocidad
        float allowedMaxSpeed = maxSpeed * difficulty;
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVel.magnitude > allowedMaxSpeed)
        {
            Vector3 limited = horizontalVel.normalized * allowedMaxSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }

        // pequeña fuerza hacia abajo para mantener contacto con la pendiente
        rb.AddForce(Vector3.down * 6f, ForceMode.Acceleration);
    }

    // Detiene el bot: usado por GameManager cuando termina la carrera
    public void StopMovement()
    {
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}