using System.Collections;
using UnityEngine;

public class UIEffect : MonoBehaviour
{

    public CanvasGroup canvasGroup;
    
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
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
            
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
