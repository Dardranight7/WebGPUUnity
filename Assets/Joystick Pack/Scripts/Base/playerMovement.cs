
using UnityEngine;



public class playerMovement : MonoBehaviour
{
    public Joystick joystick; // Reference to the joystick
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Get input from the keybord pc
        float movex = joystick != null ? joystick.Horizontal : Input.GetAxis("Horizontal");
        float movez = joystick != null ? joystick.Vertical : Input.GetAxis("Vertical");

        Vector3 move = new Vector3(movex, 0, movez);
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f; // Small negative value to keep the player grounded
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
    }
}
