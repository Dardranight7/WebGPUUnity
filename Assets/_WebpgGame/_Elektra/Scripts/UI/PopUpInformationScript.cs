using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PopUpInformationScript : MonoBehaviour
{
    public TMP_Text textTitle;
    public TMP_Text textYear ;
    public TMP_Text textDescription ;
    public Image picture;
    public GameObject TextBackground;
    public event Action OnUIHidden;
    public CanvasGroup canvasGroup;

    public void SetInformationPicture(PopUpInfo popUpInfo)
    {
        textTitle?.gameObject.SetActive(true);
        textYear?.gameObject.SetActive(true);
        textDescription?.gameObject.SetActive(true);
        
        
        if (textTitle != null) textTitle.text = popUpInfo.title;
        if (textYear != null) textYear.text = popUpInfo.year;
        if (textDescription != null) textDescription.text = popUpInfo.description;
        picture.sprite = popUpInfo.image;
    }
    
    public void SetPicture(PopUpInfo popUpInfo)
    {
        picture.sprite = popUpInfo.image;
        if (!string.IsNullOrEmpty(popUpInfo.description))
        {
            textDescription.text = popUpInfo.description;
            TextBackground?.gameObject.SetActive(true);
            textDescription?.gameObject.SetActive(true);
        }
    }
    public void ShowUI()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeIn(0.3f));
    }
    public void HideUI()
    {
        StartCoroutine(FadeOut(0.3f));

    }

    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
            
        }
        canvasGroup.alpha = 1f;
    }    
    
    private IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f; // Asegurar que empieza en 1

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
            
        }

        canvasGroup.alpha = 0f;
        
        //Disparar el evento antes de desactivar
        OnUIHidden?.Invoke(); 
        
        gameObject.SetActive(false);
    }
    
}

public struct PopUpInfo
{
    public Sprite image;
    public string title;
    public string year;
    public string description;

    public PopUpInfo(Sprite image, string title, string year, string description  )
    {
        this.image = image;
        this.title = title;
        this.year = year;
        this.description = description;
    }
}
