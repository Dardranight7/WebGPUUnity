using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    public GameManagerAlpinaria GameManagerAlpinaria;
    
    private void OnTriggerEnter(Collider other)
    {
        if (GameManagerAlpinaria != null) return;
        if (other.CompareTag("Player") || other.CompareTag("Bot"))
        {
            GameManagerAlpinaria.OnPlayerFinish(other.transform);
        }
    }
}
