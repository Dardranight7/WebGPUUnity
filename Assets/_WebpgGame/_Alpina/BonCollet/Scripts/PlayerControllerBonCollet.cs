using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerBonCollet : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 20f;       // Fuerza aplicada al moverse (usada con AddForce/Acceleration)
    public float maxSpeed = 6f;         // Velocidad máxima (u/s)
    public float drag = 2f;             // Freno pasivo en XZ
    public float joystickDeadzone = 0.1f;

    [Header("Referencias")]
    public Transform cameraTransform;   // Cámara para orientación (si es null usa Camera.main)
    public Transform model3D;           // Modelo 3D del personaje (se rota visualmente)
    public Animator animator;           // Animator del personaje
    [SerializeField] MochiAnimationManager mochiAnimationManager;

    [Header("Rotación y Animación")]
    public float rotationSpeed = 10f;   // Qué tan rápido rota hacia la dirección de movimiento
    public float animLerpSpeed = 5f;    // Qué tan rápido interpola el valor de Velocity en el Animator
    public string ParameterAnimation;

    [Header("Input")]
    [Tooltip("Asigna aquí el InputAction (Vector2) del Move. Si se deja vacío se usará Gamepad.current.leftStick o Input.GetAxis fallback.")]
    public InputActionReference moveAction;

    // Internals
    Rigidbody rb;
    private float animVelocity; // valor interpolado entre 0 y 1

    // cached input
    float h = 0f;
    float v = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Evita que el rigidbody se voltee por la física

        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : null;

        // If a moveAction reference is set, ensure it's enabled
        if (moveAction != null && moveAction.action != null)
        {
            // do not subscribe here; we'll read value directly in FixedUpdate to keep things simple
            if (!moveAction.action.enabled) moveAction.action.Enable();
        }
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null && !moveAction.action.enabled)
            moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null && moveAction.action.enabled)
            moveAction.action.Disable();
    }

    void FixedUpdate()
    {
        // --- Read input (priority: InputActionReference -> Gamepad -> legacy Input) ---
        Vector2 inputVec = Vector2.zero;

        if (moveAction != null && moveAction.action != null)
        {
            inputVec = moveAction.action.ReadValue<Vector2>();
        }
        else if (Gamepad.current != null)
        {
            inputVec = Gamepad.current.leftStick.ReadValue();
        }
        else
        {
            // fallback to legacy axes if user still uses old input
            inputVec.x = Input.GetAxis("Horizontal");
            inputVec.y = Input.GetAxis("Vertical");
        }

        // deadzone
        if (inputVec.magnitude < joystickDeadzone)
        {
            inputVec = Vector2.zero;
        }

        h = inputVec.x;
        v = inputVec.y;

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.001f)
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            // direcciones basadas en la cámara
            Vector3 camForward = (cameraTransform != null) ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = (cameraTransform != null) ? cameraTransform.right : Vector3.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // dirección final de movimiento en mundo
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

            // Aplicar fuerza si no excede la velocidad máxima (usando rb.velocity estándar)
            Vector3 velXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (velXZ.magnitude < maxSpeed)
            {
                rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);
            }

            // Rotación suave del modelo hacia la dirección de movimiento
            if (model3D != null && moveDir.sqrMagnitude > 0.0001f)
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
        if (animator != null && !string.IsNullOrEmpty(ParameterAnimation))
        {
            animator.SetFloat(ParameterAnimation, animVelocity);
        }

        if (mochiAnimationManager != null)
        {
            mochiAnimationManager.SetVelocityOnActiveChildren(animVelocity);
        }

        // Limitamos la velocidad horizontal a maxSpeed (clamp XZ) — evita que la física nos deje demasiado rápido
        Vector3 currentVel = rb.linearVelocity;
        Vector3 currentVelXZ = new Vector3(currentVel.x, 0f, currentVel.z);
        if (currentVelXZ.magnitude > maxSpeed)
        {
            Vector3 clamped = currentVelXZ.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, currentVel.y, clamped.z);
        }

        // Aplicar drag en el plano XZ (sin afectar la gravedad) usando una decaimiento estable
        currentVel = rb.linearVelocity;
        Vector3 vxz = new Vector3(currentVel.x, 0f, currentVel.z);
        vxz = vxz * (1f / (1f + drag * Time.fixedDeltaTime));
        rb.linearVelocity = new Vector3(vxz.x, currentVel.y, vxz.z);
    }
}
