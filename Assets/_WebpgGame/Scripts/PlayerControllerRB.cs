using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerRB : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 20f;       // Fuerza aplicada al moverse
    public float maxSpeed = 6f;         // Velocidad máxima
    public float drag = 2f;             // Freno pasivo

    [Header("Referencias")]
    public Transform cameraTransform;   // Cámara para orientación
    public Transform model3D;           // Modelo 3D del personaje
    public Animator animator;           // Animator del personaje

    [Header("Rotación y Animación")]
    public float rotationSpeed = 10f;   // Qué tan rápido rota hacia la dirección de movimiento
    public float animLerpSpeed = 5f;    // Qué tan rápido interpola el valor de Velocity en el Animator

    public string ParameterAnimation;
    
    private Rigidbody rb;
    private float animVelocity; // valor interpolado entre 0 y 1
    ThirdPerson inputActions;

    [SerializeField] MochiAnimationManager mochiAnimationManager;

    private void Awake()
    {
        inputActions = new ThirdPerson();
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        //Cursor.lockState = CursorLockMode.Confined;
        //Cursor.visible = false;
    }

    private void OnDestroy()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Disable();
    }

    void Start()
    {
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
}
