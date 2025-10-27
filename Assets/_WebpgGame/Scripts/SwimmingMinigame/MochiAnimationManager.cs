using UnityEngine;

public class MochiAnimationManager : MonoBehaviour
{
    public void SetVelocityOnActiveChildren(float velocity)
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy) // 🔹 solo los activos en la jerarquía
            {
                Animator anim = child.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetFloat("Velocity", velocity);
                }
            }
        }
    }
}
