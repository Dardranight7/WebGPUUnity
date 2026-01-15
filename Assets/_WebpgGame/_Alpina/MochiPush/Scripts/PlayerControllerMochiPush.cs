using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerMochiPush : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 20f;       // Fuerza aplicada al moverse
    public float maxSpeed = 6f;         // Velocidad máxima
    public float drag = 2f;
    public float pushForce = 5f;     // fuerza d empuje

    [Header("Referencias")]
    public Transform cameraTransform;   // Cámara para orientación
    public Transform model3D;           // Modelo 3D del personaje
    public Animator animator;           // Animator del personaje

    [Header("Rotación y Animación")]
    public float rotationSpeed = 10f;   // Qué tan rápido rota hacia la dirección de movimiento
    public float animLerpSpeed = 5f;    // Qué tan rápido interpola el valor de Velocity en el Animator

    [Header("Sistema de Estamina")] 
    [SerializeField] private Image staminaUiImage;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;
    public float staminaCostPerPush = 30f;
    public float maxPushMultiplier = 1.5f;
    
    private bool isStunned = false;
    
    public string ParameterAnimation;
    
    private Rigidbody rb;
    private float animVelocity; // valor interpolado entre 0 y 1
    ThirdPerson inputActions;

    [SerializeField] MochiAnimationManager mochiAnimationManager;
    
    public AudioSource audioSource;
    public AudioClip pushClip;

    private void Awake()
    {
        inputActions = new ThirdPerson();
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    private void OnEnable()
    {
        EventBus<PlayerExitZoneSignal>.OnEvent += HandleExitZone;
        EventBus<StunSignal>.OnEvent += HandleStun;
    }

    private void OnDisable()
    {
        EventBus<PlayerExitZoneSignal>.OnEvent -= HandleExitZone;
        EventBus<StunSignal>.OnEvent -= HandleStun;
    }

    private void OnDestroy()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Disable();
    }

    void Start()
    {
        currentStamina = maxStamina;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Evita que el rigidbody se voltee
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    float h = 0;
    float v = 0;

    public void OnMove(InputAction.CallbackContext movement)
    {
        Vector2 vector2 = movement.ReadValue<Vector2>();
        h = vector2.x;
        v = vector2.y;
    }

    void FixedUpdate()
    {
        
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
        if (isStunned) return;
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            // Direcciones basadas en la cámara
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            // Update de la estamina visualmente en la UI
            if (staminaUiImage != null)
                staminaUiImage.fillAmount = currentStamina / maxStamina;
            // Dirección final de movimiento
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

            // Aplica fuerza si no excede la velocidad máxima
            if (rb.linearVelocity.magnitude < maxSpeed)
                rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);

            // Rotación suave del modelo hacia la dirección de movimiento
            if (model3D != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                model3D.rotation = Quaternion.Slerp(model3D.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }

            // Actualizar animación hacia 1
            animVelocity = Mathf.Lerp(animVelocity, 1f, animLerpSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Actualizar animación hacia 0
            animVelocity = Mathf.Lerp(animVelocity, 0f, animLerpSpeed * Time.fixedDeltaTime);
        }

        // Asignar valor al Animator
        if (animator != null)
        {
            animator.SetFloat(ParameterAnimation, animVelocity);
        }

        if (mochiAnimationManager != null)
        {
            mochiAnimationManager.SetVelocityOnActiveChildren(animVelocity);
        }

        // Aplicar drag en el plano XZ (sin afectar la gravedad)
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        rb.linearVelocity = vel * (1f / (1f + drag * Time.fixedDeltaTime)) + Vector3.up * rb.linearVelocity.y;
    }
    void HandleStun(StunSignal signal) {
        if (signal.activator == gameObject) return; // Si yo lo activé, no me aturdo
        StartCoroutine(StunRoutine(signal.duration));
    }

    IEnumerator StunRoutine(float time) {
        isStunned = true;
        animVelocity = 0; // Detener animación
        rb.linearVelocity = Vector3.zero; // Detener movimiento físico
        yield return new WaitForSeconds(time);
        isStunned = false;
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("MochiPushBoundary"))
        return;

    Rigidbody other = collision.rigidbody;

    if (other != null && other != rb)
    {
        Vector3 toOther = collision.transform.position - transform.position;
        Vector3 toOtherFlat = new Vector3(toOther.x, 0f, toOther.z).normalized;
        float angle = Vector3.Angle(transform.forward, toOtherFlat);

        if (angle <= 100 && currentStamina > 5f)
        {
            float staminaFactor = Mathf.Lerp(0.5f, maxPushMultiplier, currentStamina / maxStamina);
            float calculatedForce = pushForce * staminaFactor;
            
            // Revisamos si el otro objeto tambien es un luchador (Bot o Player)
            bool isOtherPushing = false;
            
            // Intentamos obtener el script del Bot o de otro Jugador
            if (collision.gameObject.TryGetComponent<BotMochiPush>(out var bot))
            {
                // Si el bot nos está mirando (ángulo entre forwards es cercano a -1)
                if (Vector3.Dot(transform.forward, collision.transform.forward) < -0.5f)
                    isOtherPushing = true;
            }
            else if (collision.gameObject.TryGetComponent<PlayerControllerMochiPush>(out var otherPlayer))
            {
                if (Vector3.Dot(transform.forward, collision.transform.forward) < -0.5f)
                    isOtherPushing = true;
            }

            if (isOtherPushing)
            {
                calculatedForce *= 0.5f; // Reducción a la mitad por choque mutuo
            }
            // ---------------------------------------

            other.AddForce(transform.forward * calculatedForce, ForceMode.Impulse);
            
            currentStamina -= staminaCostPerPush;
            currentStamina = Mathf.Max(currentStamina, 0);

            if (audioSource != null && pushClip != null && !audioSource.isPlaying)
                audioSource.PlayOneShot(pushClip);
        }
    }
    }
    /// <summary>
    /// If signal its no This Gameobject we stop
    /// If this script has no Rigidbody assigned we stop
    /// Then:
    /// First Stops all forces
    /// Second enable gravity if disabled
    /// Third w8 for kill zone to trigger
    /// </summary>
    /// <param name="signal">Gameobject to controll how is going to handle the event</param>
    private void HandleExitZone(PlayerExitZoneSignal signal)
    {
        if (signal.player != gameObject)
            return;

        if (rb == null)
        {
            Debug.LogWarning($"PlayerControllerMochiPush - {gameObject.name}- has no ridigbody assigned to script");   
            return;
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        inputActions.Disable();

        animVelocity = 0;
        if(animator != null)
            animator.SetFloat(ParameterAnimation,0);
        this.enabled = false;
    }
}

