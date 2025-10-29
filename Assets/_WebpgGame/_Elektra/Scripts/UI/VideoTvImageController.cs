using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class VideoTvImageController : MonoBehaviour
{
    [SerializeField] private Image loadingImage;
    [SerializeField] private Sprite[] tvsSprites;
    void Start()
    {
        Sprite spriteResourcePath = GetSpriteForScene(SceneData.nextSceneId);
        LoadAndApplySprite(spriteResourcePath);
    }

    private Sprite GetSpriteForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "50s-60s":
                return tvsSprites[0];
            case "70-80s":
                return tvsSprites[1];
            case "90-2000s":
                return tvsSprites[2];
            case "2010-2025s":
                return tvsSprites[3];
            default:
                return tvsSprites[0]; 
        }
    }

    private void LoadAndApplySprite([CanBeNull] Sprite resource)
    {
        if (resource != null)
        {
            loadingImage.sprite = resource;
        }
        else
        {
            Debug.LogError("Error: No se pudo cargar el Sprite en la ruta: " + resource);
        }
    }
}
