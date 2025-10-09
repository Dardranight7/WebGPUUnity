using UnityEngine;

public class ZeppelinAnimationLooper : MonoBehaviour
{
    public Animator animator;
    public string animationName = "zeppelinFly";
    public int maxLoops = 4;
    public float animationLength = 6.0f; // Cambia esto por la duración de tu animación
    public float yStep = 30f;

    private int currentLoop = 0;
    private bool isPlaying = false;

    void Start()
    {
        currentLoop = 0;
        PlayAnimation();
    }

    void PlayAnimation()
    {
        if (currentLoop < maxLoops)
        {
            animator.Play(animationName, -1, 0f);
            isPlaying = true;
            Invoke(nameof(OnAnimationComplete), animationLength);
        }
    }

    void OnAnimationComplete()
    {
        // Subir el Zeppelin en Y
        transform.position += new Vector3(0f, yStep, 0f);
        currentLoop++;
        isPlaying = false;
        PlayAnimation();
    }
}