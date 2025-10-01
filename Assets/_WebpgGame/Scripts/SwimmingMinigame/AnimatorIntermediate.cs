using UnityEngine;

public class AnimatorIntermediate : MonoBehaviour
{
    [SerializeField] MochiAnimationManager m_Manager;

    public void Walk()
    {
        m_Manager.SetVelocityOnActiveChildren(1);
    }

    public void Idle()
    {
        m_Manager.SetVelocityOnActiveChildren(0);
    }
}
