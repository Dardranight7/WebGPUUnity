using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class PlayerSurfaceInput : MonoBehaviour
{
    #region Declarations
    [Header("Configuración del movimiento")]
    public float moveSpeed = 5f; // Velocidad de movimiento lateral
    public float forwardSpeed = 8f; // Velocidad de avance a lo largo del spline
    public float maxSpeed = 10f; // Velocidad máxima permitida
    public float drag = 2f;  
    
    [Header("Configuración del Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private float anchoTobogan = 3f; // Ancho del tobogán
    [SerializeField] private float alturaOffset = 0.5f; // Altura sobre el spline
    [SerializeField] private bool usarGravedad = true;
    
    [Header("Suavizado")]
    [SerializeField] private float suavizadoRotacion = 10f;
    [SerializeField] private float suavizadoPosicion = 5f;
    
    // Configuracion para el choque/aturdimiento
    [Header("Interaccion con Obstaculos")]
    [SerializeField] private float tiempoAturdimiento = 0.5f; // Tiempo que el jugador queda quieto
    
    private Rigidbody rb;
    ThirdPerson inputActions;
    
    // Variables del Spline
    private float progresoSpline = 0f; // Posición actual en el spline (0 a 1)
    private float posicionLateral = 0f; // Posición lateral (-1 a 1)
    private float horizontalMovement = 0;

    private bool canMove = false;
    private bool isStunned = false; // Controla si el jugador esta aturdido
    private bool savedUseGravity = true;
    
    #endregion Declarations

    #region Unity Functions

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new ThirdPerson();
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        
        // Almacenar la configuración inicial de gravedad
        savedUseGravity = usarGravedad;
        
        rb.freezeRotation = true; // Evita que el personaje gire al chocar
        rb.linearDamping = drag;
        rb.useGravity = usarGravedad;
    }
    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        //iniciar en el spline
        if (splineContainer != null)
        {
            InicializationSpline();
        }

        OnRacePrepare();
    }
    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player.Move.performed -= OnMove;
            inputActions.Player.Move.canceled -= OnMove;
            inputActions.Disable();
        }
    }
    
    void FixedUpdate()
    {
        //Código antiguo
        /*if (splineContainer == null)
        {
            MovementWithOutSpline();
            return;
        }

        MovementWithSpline();*/
        
        if (!canMove)
        {
            // Mantener quieto mientras espera el GO
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (splineContainer == null)
        {
            MovementWithOutSpline();
            return;
        }

        MovementWithSpline();
    }
    #endregion Unity Functions
    void InicializationSpline()
    {
        progresoSpline = EncontrarProgresoMasCercano(transform.position);
        posicionLateral = 0f;
    }
    void MovementWithOutSpline()
    {
        // Aplica fuerza en el eje X
        rb.AddForce(Vector3.right * horizontalMovement * moveSpeed, ForceMode.Acceleration);

        // Limita la velocidad máxima
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector3(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
        }
    }

    void MovementWithSpline()
    {
        //avanzar por spline automaticamente
        float longitudSpline = splineContainer.Spline.GetLength();
        float incrementoProgreso = (forwardSpeed * Time.fixedDeltaTime) / longitudSpline;
        progresoSpline += incrementoProgreso;
        
        //limitar progreso entre 0 y 1
        progresoSpline = Mathf.Clamp01(progresoSpline);
        
        //mover lateralmente
        
        posicionLateral -= horizontalMovement * moveSpeed * Time.fixedDeltaTime;
        posicionLateral = Mathf.Clamp(posicionLateral, -1f,1f);
        
        //calcular posición objetivo del spline
        Vector3 posicionObjetivo = CalcularPosicionEnSpline();
        
        // Mover el Rigidbody hacia la posición objetivo
        Vector3 direccion = (posicionObjetivo - rb.position);
        rb.MovePosition(Vector3.Lerp(rb.position, posicionObjetivo, Time.fixedDeltaTime * suavizadoPosicion));
        
        // Rotar el jugador en la dirección del spline
        AjustarRotacion();
        
        // Limitar velocidad lateral
        LimitarVelocidadLateral();
    }
    
    
    Vector3 CalcularPosicionEnSpline()
    {
        // Obtener la posición base en el spline
        float3 posicionSpline = splineContainer.EvaluatePosition(progresoSpline);
        
        // Obtener vectores del spline
        float3 tangente = splineContainer.EvaluateTangent(progresoSpline);
        float3 arriba = splineContainer.EvaluateUpVector(progresoSpline);
        
        // Calcular el vector derecha (perpendicular al avance)
        float3 derecha = math.normalize(math.cross(tangente, arriba));
        
        // Calcular offset lateral basado en la posición lateral del jugador
        float3 offsetLateral = derecha * posicionLateral * (anchoTobogan / 2f);
        
        // Offset de altura para que el jugador esté sobre el tobogán
        float3 offsetAltura = arriba * alturaOffset;
        
        // Posición final
        return posicionSpline + offsetLateral + offsetAltura;
    }
    
    void AjustarRotacion()
    {
        // Obtener la dirección del spline
        float3 tangente = splineContainer.EvaluateTangent(progresoSpline);
        float3 arriba = splineContainer.EvaluateUpVector(progresoSpline);
        
        // Crear rotación objetivo
        Quaternion rotacionObjetivo = Quaternion.LookRotation(tangente, arriba);
        
        // Aplicar rotación suavizada
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivo, Time.fixedDeltaTime * suavizadoRotacion));
    }
    
    
    void LimitarVelocidadLateral()
    {
        // Obtener el vector derecha local
        Vector3 derecha = transform.right;
        
        // Calcular velocidad lateral
        float velocidadLateral = Vector3.Dot(rb.linearVelocity, derecha);
        
        // Limitar si excede el máximo
        if (Mathf.Abs(velocidadLateral) > maxSpeed)
        {
            Vector3 velocidadLateralVector = derecha * Mathf.Sign(velocidadLateral) * maxSpeed;
            Vector3 velocidadOtrasDirecciones = rb.linearVelocity - (derecha * velocidadLateral);
            rb.linearVelocity = velocidadLateralVector + velocidadOtrasDirecciones;
        }
    }
    
    float EncontrarProgresoMasCercano(Vector3 posicion)
    {
        float mejorDistancia = float.MaxValue;
        float mejorProgreso = 0f;
        
        // Buscar el punto más cercano en el spline
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
    public void OnMove(InputAction.CallbackContext movement)
    {
        // No permitir input si esta stuneado.
        if (isStunned)
        {
            horizontalMovement = 0f;
            return;
        }
        Vector2 vector2 = movement.ReadValue<Vector2>();
        horizontalMovement = vector2.x;
    }
    
    public void OnRacePrepare()
    {
        canMove = false;
        isStunned = false; // Nos aseguramos que no esta aturtido en este momento
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false; // no se deslice antes de tiempo
    }

    public void OnRaceStart()
    {
        ReiniciarEnSpline();
        canMove = true;
        rb.useGravity = savedUseGravity; // restaura la gravedad que configuraste
    }

    public void OnRaceStop()
    {
        canMove = false;
        rb.linearVelocity = Vector3.zero;
    }
    /// <summary>
    /// Detiene el movimiento y reduce la velocidad por un tiempo.
    /// </summary>
    /// <param name="slowdownFactor">Factor de reduccion de velocidad (e.g., 0.5f para reducir a la mitad)</param>
    public void HitObstacle(float forwardSlowdownFactor = 0.5f)
    {
        if (isStunned) return; // Evitar aturdimientos anidados

        // 1. Reducir velocidad de avance
        forwardSpeed *= forwardSlowdownFactor;
        forwardSpeed = Mathf.Max(1f, forwardSpeed); // Asegurar una velocidad mínima

        // 2. Ejecutar la Corrutina de aturdimiento
        StartCoroutine(StunRoutine());
    }

    /// <summary>
    /// Corrutina para manejar el tiempo de aturdimiento.
    /// </summary>
    IEnumerator StunRoutine()
    {
        isStunned = true;
        // La lógica en FixedUpdate y OnMove se encarga de detener el movimiento.
        
        yield return new WaitForSeconds(tiempoAturdimiento);
        
        isStunned = false;
    }
    
    // Métodos públicos útiles
    public void ReiniciarEnSpline()
    {
        progresoSpline = 0f;
        posicionLateral = 0f;
        if (splineContainer != null)
        {
            transform.position = CalcularPosicionEnSpline();
        }
    }

    public float ObtenerProgresoActual()
    {
        return progresoSpline;
    }

    public bool Finished()
    {
        return progresoSpline >= 0.99f;
    }

    // Visualización en el editor
    private void OnDrawGizmos()
    {
        if (splineContainer == null || !Application.isPlaying) return;

        // Dibujar posición objetivo
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(CalcularPosicionEnSpline(), 0.3f);

        // Dibujar límites laterales
        float3 posicionSpline = splineContainer.EvaluatePosition(progresoSpline);
        float3 tangente = splineContainer.EvaluateTangent(progresoSpline);
        float3 arriba = splineContainer.EvaluateUpVector(progresoSpline);
        float3 derecha = math.normalize(math.cross(tangente, arriba));

        Gizmos.color = Color.red;
        Vector3 bordeIzquierdo = posicionSpline - derecha * (anchoTobogan / 2f);
        Vector3 bordeDerecho = posicionSpline + derecha * (anchoTobogan / 2f);
        
        Gizmos.DrawLine(bordeIzquierdo, bordeIzquierdo + (Vector3)arriba * 2f);
        Gizmos.DrawLine(bordeDerecho, bordeDerecho + (Vector3)arriba * 2f);
    }
}