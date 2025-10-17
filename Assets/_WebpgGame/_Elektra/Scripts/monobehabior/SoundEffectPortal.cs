using UnityEngine;

public class SoundEffectPortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"enter:{other.name}");
        if (!other.CompareTag("Player")) return;
        UIAudioManager.Instance?.PlayTeportSound();
    }    
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        UIAudioManager.Instance?.StoptTeportSound();

    }
}
