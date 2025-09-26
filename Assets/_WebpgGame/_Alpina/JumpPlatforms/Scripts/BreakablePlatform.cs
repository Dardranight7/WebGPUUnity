using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    public float breakDelay = 2f; // Segundos antes de romperse
    private bool breaking = false;
    private GameObject playerOnTop;

    void OnCollisionEnter(Collision collision)
    {
        if (!breaking && collision.gameObject.CompareTag("Player"))
        {
            breaking = true;
            playerOnTop = collision.gameObject;
            Invoke("BreakPlatform", breakDelay);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == playerOnTop)
        {
            playerOnTop = null;
        }
    }

    void BreakPlatform()
    {
        if (playerOnTop != null)
        {
            var pc = playerOnTop.GetComponent<PlayerController>();
            if (pc != null && !pc.isDead)
            {
                pc.DieWithMessage("Has perdido: la plataforma se rompió bajo tus pies.");
            }
        }
        Destroy(gameObject);
    }
}
