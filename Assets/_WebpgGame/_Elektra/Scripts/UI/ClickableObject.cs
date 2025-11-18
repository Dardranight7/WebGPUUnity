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
            //infoCanvasPicture.ShowUI();
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
            infoCanvasPicture.ShowUI();
            infoCanvasPicture.SetPicture(new PopUpInfo
            {
                title = title,
                description = description,
                image =  image,
            });
            return;
        }

        Debug.Log($"¡Click detectado! esto es una imagen con informacion");
        //ShowPopup();
    }

}

