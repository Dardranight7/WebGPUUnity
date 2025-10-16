using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableObject : MonoBehaviour,IPointerClickHandler
{
    [Header("Referent")]
    public PopUpInformationScript infoCanvasPicture;
    
    [Header("information to panel")]
    public Sprite image;
    public string title;
    public string year;
    [TextArea]
    public string description;
    
    
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameObject.CompareTag($"InfPicture"))
        {
            infoCanvasPicture.gameObject.SetActive(true);
            
            infoCanvasPicture.SetInformationPicture(new PopUpInfo
            {
                image =  image,
                title = title,
                year = year,
                description = description
            });
            return;
        }

        if (gameObject.CompareTag($"pictures"))
        {
            infoCanvasPicture.gameObject.SetActive(true);
            infoCanvasPicture.SetPicture(new PopUpInfo
            {
                image =  image,
            });
            return;
        }

        Debug.Log($"¡Click detectado! esto es una imagen con informacion");
        //ShowPopup();
    }

}

