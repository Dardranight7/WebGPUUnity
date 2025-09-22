using UnityEngine;

public class ThreePersonCamera : MonoBehaviour
{
    public Transform target; // The target the camera will follow
    public Vector3 offset = new Vector3(0, 2, -5); // Offset from the target position
    

    

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + target.TransformDirection(offset);

            // Optionally, make the camera look at the target
            transform.LookAt(target);
        }
    }
}

       