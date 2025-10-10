using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    public TextMeshProUGUI userNameText;
    public Image profilePic;

    private void OnEnable()
    {
        UpdateVisual();
    }

    private void Start()
    {
        Backend.OnPlayerProfileUpdate += UpdateVisual;
    }

    private void OnDestroy()
    {
        Backend.OnPlayerProfileUpdate -= UpdateVisual;
    }

    public void UpdateVisual()
    {
        Backend.PlayerProfileDTO datos = Backend.singleton.playerProfile;
        userNameText.text = "Mochi" + datos.userName;
        if (profilePic != null)
        {
            profilePic.sprite = Backend.singleton.MochiDB[datos.activeMochiIndex].image;
        }
    }
}
