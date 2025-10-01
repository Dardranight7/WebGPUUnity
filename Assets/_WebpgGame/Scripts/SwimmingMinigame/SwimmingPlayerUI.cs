using UnityEngine;
using UnityEngine.UI;

public class SwimmingPlayerUI : MonoBehaviour
{
    public Image Pick, GrayscalePick;
    public Image LifeBar;

    public void SetupUI()
    {
        Pick.gameObject.SetActive(true);
        GrayscalePick.gameObject.SetActive(false);
    }

    public void UpdateVisual(int currentLifes)
    {
        LifeBar.fillAmount = (float)currentLifes/3;
        if (currentLifes == 0)
        {
            GrayscalePick.gameObject.SetActive(true);
            Pick.gameObject.SetActive(false);
        }
    }
}
