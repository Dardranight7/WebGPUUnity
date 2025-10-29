using UnityEngine;
using UnityEngine.InputSystem;

public class TestJoystick : MonoBehaviour
{
    [Header("Configuración del movimiento")]
    public float moveSpeed = 5f; // Velocidad de movimiento
    public float maxSpeed = 10f; // Velocidad máxima permitida
    public float drag = 2f;      // Resistencia (fricción aérea)

    private Rigidbody rb;
    ThirdPerson inputActions;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Evita que el personaje gire al chocar
        rb.linearDamping = drag;
    }
    

    void FixedUpdate()
    {
        // Aplica fuerza en el eje X
        rb.AddForce(Vector3.right * horizontalMovement * moveSpeed, ForceMode.Acceleration);

        // Limita la velocidad máxima
        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector3(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
        }
    }
    
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
    

    float horizontalMovement = 0;
    

    public void OnMove(InputAction.CallbackContext movement)
    {
        Vector2 vector2 = movement.ReadValue<Vector2>();
        horizontalMovement = vector2.x;
    }
}