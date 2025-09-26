using UnityEngine;

public class Botcontroller : MonoBehaviour
{
    public float jumpInterval = 1.5f;
    private float jumpForce = 3f;
    public LayerMask PlatformLayer;
    public bool isAlive = true;
    private Animator animator;
    private Rigidbody rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        animator.SetBool("Idle", true);
    }

    void Update()
    {
        if (!isAlive) return; 
        
        if (IsGrounded())
        {
            Transform nextPlatform = FindNextPlatform();
            if (nextPlatform != null)
            {
                JumpToPlatform(nextPlatform.position);
            }
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f, PlatformLayer);
    }
    
    Transform FindNextPlatform()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 10f, PlatformLayer);
        Transform closest = null;
        float minDist = Mathf.Infinity;
        foreach (var col in hitColliders)
        {
            if (!col.CompareTag("Platform")) continue;
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist > 1f && dist < minDist)
            {
                minDist = dist;
                closest = col.transform;
            }
        }

        return closest;
    }
    
    void JumpToPlatform(Vector3 target)
    {
        animator.SetTrigger("Jump");
        animator.SetBool("Idle", false);
        
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0.5f; //ajusta la altura del salto 
        rb.AddForce(direction * jumpForce, ForceMode.VelocityChange);
        
        Invoke("SetIdle", 0.5f);
    }
    
    void SetIdle()
    {
        animator.SetBool("Idle", true);
    }

    public void Die()
    {
        isAlive = false;
        animator.SetBool("Idle", true);
        Debug.Log("Bot ha muerto");
    }
}
