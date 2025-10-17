using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PopUpInformationScript : MonoBehaviour
{
    public TMP_Text textTitle;
    public TMP_Text textYear ;
    public TMP_Text textDescription ;
    public Image picture;

    public void SetInformationPicture(PopUpInfo popUpInfo)
    {
        textTitle.gameObject.SetActive(true);
        textYear.gameObject.SetActive(true);
        textDescription.gameObject.SetActive(true);
        
        textTitle.text = popUpInfo.title;
        textYear.text = popUpInfo.year;
        textDescription.text = popUpInfo.description;
        picture.sprite = popUpInfo.image;
    }
    
    public void SetPicture(PopUpInfo popUpInfo)
    {
        picture.sprite = popUpInfo.image;
        
        RectTransform rect = picture.GetComponent<RectTransform>();
        
        // Configurar anchors al centro
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Configurar pivot al centro
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        // Posición en 0,0 (centro)
        rect.anchoredPosition = Vector2.zero;
        
        picture.SetNativeSize();
        RectTransform parentRect = rect.parent.GetComponent<RectTransform>();

        float maxWidth = parentRect.rect.width;
        float maxHeight = parentRect.rect.height;

        float scaleWidth = maxWidth / rect.sizeDelta.x;
        float scaleHeight = maxHeight / rect.sizeDelta.y;

        float scale = Mathf.Min(scaleWidth, scaleHeight, 1f);
        rect.sizeDelta *= scale;

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
