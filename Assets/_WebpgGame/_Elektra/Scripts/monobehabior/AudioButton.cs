using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioButton : MonoBehaviour
{

    public Sprite spriteActiveAudio;  
    public Sprite spriteDesactiveAudio;
    public AudioSource audioSource;
    public Button button;
    public void Start()
    {
        button.onClick.AddListener(()=> DesactiveAudio(audioSource));
    }
    
    public void DesactiveAudio(AudioSource desactuveAudioSource)
    {
        desactuveAudioSource.Pause();
        button.onClick.RemoveAllListeners();
        
        SpriteState state = button.spriteState;
        state.selectedSprite = spriteDesactiveAudio;
        button.spriteState = state;
        
        button.onClick.AddListener(()=> ActiveAudio(audioSource));
    }
    
    public void ActiveAudio(AudioSource desactuveAudioSource)
    {
        desactuveAudioSource.Play();
        button.onClick.RemoveAllListeners();
        
        SpriteState state = button.spriteState;
        state.selectedSprite = spriteActiveAudio;
        button.spriteState = state;
        
        button.onClick.AddListener(()=> DesactiveAudio(audioSource));
    }
    

}
