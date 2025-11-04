using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BotAlpinaria : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private RaceManager raceManager;
  
    
    
    [Header("Configuración de Velocidad")]
    [SerializeField] private float baseSpeed = 7f;
    [SerializeField]  private float minSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float speedVariation = 2f;
    
   
    [Header("Configuración de Movimiento Lateral")]
    [SerializeField] private float lateralSpeed = 3f;
    [SerializeField] private float slideTobogan = 3f;
    [SerializeField] private float heightOffset = 0.5f;
    
    
    [Header("Inteligencia del Bot")]
    [SerializeField] private float agility = 5f;
    [SerializeField] private float accuracy = 0.8f;
    [SerializeField] private float distancePrediction = 5f;
    [SerializeField] private TipoDificultad difficulty = TipoDificultad.Normal;
    
    
    [Header("Comportamiento")]
    [SerializeField] private bool edgeAvoidance = true;
    [SerializeField] private bool findOptimalLine = true;
    [SerializeField] private bool makeMistakes = true;
    
    
    [Header("Configuración Inicial")] // ⭐ NUEVO
    [SerializeField] private bool positionInitial = true;
    [SerializeField] private float progresoInicialManual = 0f;
    
    
    
    [Header("Suavizado")]
    [SerializeField] private float rotationSmooth = 10f;
    [SerializeField] private float positionSmooth = 8f;
    
    // Variables internas
    private float progresoSpline = 0f;
    private float posicionLateral = 0f;
    private float velocidadActual;
    private float tiempoProximoError = 0f;
    private float errorActual = 0f;
    private float personalidad;
    
    private bool canMove = false;
    
    public enum TipoDificultad
    {
        Facil,
        Normal,
        Dificil,
        Experto
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        rb.freezeRotation = true;
        rb.useGravity = false;


        InitialBot();
        InitialInSpline();
        
        canMove = false;
    }

    void InitialBot()
    {
        personalidad = Random.Range(0.7f, 1.3f);
        velocidadActual = baseSpeed * personalidad;
        
        ConfigDifficult();
        
        if (splineContainer != null)
        {
            InitialInSpline();
        }
    }

    void InitialInSpline()
    {
        
        progresoSpline = FindNearestProgress(transform.position);
        posicionLateral = CalculatePositionLateralInitial();
    
    
        Debug.Log($"[{gameObject.name}] Progreso: {progresoSpline:F3}, Lateral: {posicionLateral:F2}, Posición: {transform.position}");
    }

    float CalculatePositionLateralInitial()
    {
        float3 posicionSpline = splineContainer.EvaluatePosition(progresoSpline);
        float3 tangente = splineContainer.EvaluateTangent(progresoSpline);
        float3 arriba = splineContainer.EvaluateUpVector(progresoSpline);
        float3 derecha = math.normalize(math.cross(tangente, arriba));
        
        Vector3 offsetDesdeSpline = transform.position - (Vector3)posicionSpline;
        float distanciaLateral = Vector3.Dot(offsetDesdeSpline, derecha);
        
        float posLateral = distanciaLateral / (slideTobogan / 2f);
        return Mathf.Clamp(posLateral, -1f, 1f);
    }

    void ConfigDifficult()
    {
        switch (difficulty)
        {
            case TipoDificultad.Facil:
                baseSpeed *= 0.8f;
                agility = 3f;
                accuracy = 0.6f;
                makeMistakes = true;
                break;
                
            case TipoDificultad.Normal:
                baseSpeed *= 0.95f;
                agility = 5f;
                accuracy = 0.8f;
                makeMistakes = true;
                break;
                
            case TipoDificultad.Dificil:
                baseSpeed *= 1.1f;
                agility = 7f;
                accuracy = 0.9f;
                makeMistakes = false;
                break;
                
            case TipoDificultad.Experto:
                baseSpeed *= 1.2f;
                agility = 10f;
                accuracy = 0.95f;
                makeMistakes = false;
                break;
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;             // Espera hasta el GO
        if (splineContainer == null) return;
        
        Decitions();
        AdvanceInSpline();
        UpdatePosition();
    }

    void Decitions()
    {
        float movimientoDeseado = 0f;
        
        if (findOptimalLine)
        {
            movimientoDeseado += CalculateLine();
        }
        
        if (edgeAvoidance)
        {
            movimientoDeseado += DodgeEdge();
        }
        
        if (makeMistakes)
        {
            movimientoDeseado += SimulateErrors();
        }
        
        float movimientoFinal = movimientoDeseado * accuracy;
        posicionLateral += movimientoFinal * lateralSpeed * Time.fixedDeltaTime;
        posicionLateral = Mathf.Clamp(posicionLateral, -1f, 1f);
    }

    float CalculateLine()
    {
        float progresoFuturo = progresoSpline + (distancePrediction / splineContainer.Spline.GetLength());
        progresoFuturo = Mathf.Clamp01(progresoFuturo);
        
        float3 tangentActual = splineContainer.EvaluateTangent(progresoSpline);
        float3 tangenteFutura = splineContainer.EvaluateTangent(progresoFuturo);
        
        Vector3 cross = Vector3.Cross(tangentActual, tangenteFutura);
        float intensidadCurva = cross.magnitude;
        
        if (intensidadCurva > 0.1f)
        {
            bool curvaALaDerecha = cross.y > 0;
            
            if (curvaALaDerecha)
            {
                return posicionLateral > -0.5f ? -1f : 0f;
            }
            else
            {
                return posicionLateral < 0.5f ? 1f : 0f;
            }
        }
        
        if (Mathf.Abs(posicionLateral) > 0.2f)
        {
            return -Mathf.Sign(posicionLateral) * 0.5f;
        }
        
        return 0f;
    }

    float DodgeEdge()
    {
        if (posicionLateral > 0.8f)
        {
            return -2f;
        }
        else if (posicionLateral < -0.8f)
        {
            return 2f;
        }
        
        return 0f;
    }

    float SimulateErrors()
    {
        if (Time.time > tiempoProximoError)
        {
            errorActual = Random.Range(-1f, 1f) * (1f - accuracy);
            tiempoProximoError = Time.time + Random.Range(1f, 3f);
        }
        
        return errorActual * 0.3f;
    }

    void AdvanceInSpline()
    {
        float longitudSpline = splineContainer.Spline.GetLength();
        
        velocidadActual = Mathf.Lerp(
            velocidadActual, 
            baseSpeed + Random.Range(-speedVariation, speedVariation),
            Time.fixedDeltaTime
        );
        velocidadActual = Mathf.Clamp(velocidadActual, minSpeed, maxSpeed);
        
        float incrementoProgreso = (velocidadActual * Time.fixedDeltaTime) / longitudSpline;
        progresoSpline += incrementoProgreso;
        progresoSpline = Mathf.Clamp01(progresoSpline);
    }

    void UpdatePosition()
    {
        float3 posicionSpline = splineContainer.EvaluatePosition(progresoSpline);
        float3 tangente = splineContainer.EvaluateTangent(progresoSpline);
        float3 arriba = splineContainer.EvaluateUpVector(progresoSpline);
        float3 derecha = math.normalize(math.cross(tangente, arriba));
        
        float3 offsetLateral = derecha * posicionLateral * (slideTobogan / 2f);
        float3 offsetAltura = arriba * heightOffset;
        
        Vector3 posicionObjetivo = posicionSpline + offsetLateral + offsetAltura;
        
        rb.MovePosition(Vector3.Lerp(rb.position, posicionObjetivo, Time.fixedDeltaTime * positionSmooth));
        
        Quaternion rotacionObjetivo = Quaternion.LookRotation(tangente, arriba);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivo, Time.fixedDeltaTime * rotationSmooth));
    }

    float FindNearestProgress(Vector3 posicion)
    {
        float mejorDistancia = float.MaxValue;
        float mejorProgreso = 0f;
        
        for (float t = 0; t <= 1f; t += 0.01f)
        {
            Vector3 puntoSpline = splineContainer.EvaluatePosition(t);
            float distancia = Vector3.Distance(posicion, puntoSpline);
            
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorProgreso = t;
            }
        }
        
        return mejorProgreso;
    }
    
    public void OnRacePrepare()
    {
        canMove = false;
        // Mantente quieto hasta el GO
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
    
    public void OnRaceStart()
    {
        // Reposicionar/Reset si quieres que arranquen desde el inicio del spline
        ResetBot();
        canMove = true;
    }

    public void OnRaceStop()
    {
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }


    public float GetProgress()
    {
        return progresoSpline;
    }

    public void ResetBot()
    {
        progresoSpline = 0f;
        posicionLateral = Random.Range(-0.3f, 0.3f);
        velocidadActual = baseSpeed * personalidad;
    }

    public void AdjustSpeed(float multiplicador)
    {
        baseSpeed *= multiplicador;
    }

    public bool End()
    {
        return progresoSpline >= 0.999f;
    }

    void OnDrawGizmos()
    {
        if (splineContainer == null || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        float progresoFuturo = Mathf.Clamp01(progresoSpline + (distancePrediction / splineContainer.Spline.GetLength()));
        Vector3 puntoFuturo = splineContainer.EvaluatePosition(progresoFuturo);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, puntoFuturo);
    }

}