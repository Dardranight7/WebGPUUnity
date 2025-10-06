using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [SerializeField] TMP_InputField email;
    [SerializeField] TMP_InputField password;
    [SerializeField] Button button;
    [SerializeField] Transform authParent;

    [SerializeField] TMP_InputField registerEmail, registerPassword, nombre, apellido, registerUserName, phone, numeroDocumento, city;
    [SerializeField] TMP_Dropdown documentType;

    [SerializeField] List<TextMeshProUGUI> userName = new List<TextMeshProUGUI>();
    [SerializeField] List<TextMeshProUGUI> gems = new List<TextMeshProUGUI>();
    [SerializeField] List<TextMeshProUGUI> littleGems = new List<TextMeshProUGUI>();
    [SerializeField] List<Image> Frames = new(), Profiles = new();

    [SerializeField] Transform recompenseParent;
    [SerializeField] TextMeshProUGUI recompenseName;
    [SerializeField] Image recompenseImage;
    
    //[SerializeField] RewardManager rewardManager;

    [SerializeField] Sprite Errorimage, RegisterImage;
    [SerializeField] Image InputImage, InputPassword;
    [SerializeField] GameObject textincorrect;
    [SerializeField] GameObject MuteIconMain, MuteIconGeneral;
    //[SerializeField] FormValidator formValidator;

    [SerializeField] TextMeshProUGUI textoCodigoAmigo;

    public static System.Action OnNeedToUpdateProfile;

    private void Awake()
    {
        OnNeedToUpdateProfile += UpdateVisual;
    }


    private void Start()
    {
        button.onClick.AddListener(Login);
        if (!string.IsNullOrEmpty(Backend.singleton.Serial))
        {
            authParent.gameObject.SetActive(false);
            MainMenu.gameObject.SetActive(true);
            //textoCodigoAmigo.text = Backend.singleton.Serial.Substring(0, Backend.singleton.Serial.Length - 1);
            Backend.singleton.GetUserData(Backend.singleton.Serial, (a) =>
            {
                Backend.PlayerProfileDTO datos = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(a.data);
                Backend.singleton.playerProfile = datos;
                foreach (var userN in userName)
                {
                    userN.text = "Mochi" + datos.userName;
                }
                UpdateVisual();
            });
        }
        else
        {

        }
    }

    public void UpdateVisual()
    {
        int totalGems = Backend.singleton.playerProfile.gems;

        int fullPacks = totalGems / 12;
        int remainder = totalGems % 12;

        float littleGemsValue = 0;
        float gemsValue = 0;   

        gemsValue = fullPacks;
        littleGemsValue = remainder;


        foreach (var userN in gems)
        {
            userN.text = gemsValue.ToString();
        }
        foreach (var userN in littleGems)
        {
            userN.text = littleGemsValue.ToString();
        }
    }

    //public void ShowDailyRecompense(Backend.Response a)
    //{
    //    if (a.code == 3)
    //    {
    //        //Frame
    //        recompenseImage.sprite = rewardManager.database.Frames[int.Parse(a.data)].Sprite;
    //        recompenseName.text = rewardManager.database.Frames[int.Parse(a.data)].Name;
    //        recompenseParent.gameObject.SetActive(true);
    //    }
    //    if (a.code == 4)
    //    {
    //        //Profile
    //        recompenseImage.sprite = rewardManager.database.Profile[int.Parse(a.data)].Sprite;
    //        recompenseName.text = rewardManager.database.Profile[int.Parse(a.data)].Name;
    //        recompenseParent.gameObject.SetActive(true);
    //    }
    //    if (a.code == 5)
    //    {
    //        //Sticker
    //        recompenseImage.sprite = rewardManager.database.Stikers[int.Parse(a.data)].Sprite;
    //        recompenseName.text = rewardManager.database.Stikers[int.Parse(a.data)].Name;
    //        recompenseParent.gameObject.SetActive(true);
    //    }
    //}

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Login);
        OnNeedToUpdateProfile -= UpdateVisual;
    }

    [SerializeField] GameObject MainMenu;
    [SerializeField] GameObject TutorialPartOne, TutorialPartTwo, ButtomSaltarTuto;

    public void SkipTutorial()
    {
        PlayerPrefs.SetInt("TutorialState", 2); 
        PlayerPrefs.Save(); 
    }
    public void Login()
    {
        Backend.singleton.Login(email.text, password.text, (a) =>
        {
            if (a.code == 0)
            {
                PlayerPrefs.SetInt("TutorialState", 0);
                Backend.singleton.Serial = (string) a.data;
                Backend.singleton.GetUserData(Backend.singleton.Serial, (a) =>
                {
                    Backend.PlayerProfileDTO datos = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(a.data);
                    Backend.singleton.playerProfile = datos;
                    foreach (var userN in userName)
                    {
                        userN.text = "Mochi" + datos.userName;
                    }
                    authParent.gameObject.SetActive(false);
                });
                MainMenu.SetActive(false);
                MochiCourtain.Singleton.LoadSceneWithCourtain("0",1);
                UpdateVisual();
            }
            else
            {
                InputImage.sprite = Errorimage;
                InputPassword.sprite = Errorimage;
                textincorrect.SetActive(true);
            }
        });
    }

    public Image emailFieldImage;
    public Sprite IncorrectImage;
    public TextMeshProUGUI PlaceHolderEmail, Text;
    public void Register()
    {
        Backend.singleton.Register(registerEmail.text, registerPassword.text, nombre.text, apellido.text, registerUserName.text ,phone.text, city.text, documentType.options[documentType.value].text, numeroDocumento.text, (a) =>
        {
            if (a.code == 0)
            {
                Backend.PlayerProfileDTO datos = JsonConvert.DeserializeObject<Backend.PlayerProfileDTO>(a.data);
                Backend.singleton.Serial = datos.serial;
                Backend.singleton.playerProfile = datos;
                foreach (var userN in userName)
                {
                    userN.text = "Mochi" + datos.userName;
                }
                authParent.gameObject.SetActive(false);
                MainMenu.SetActive(true);
                MochiCourtain.Singleton.LoadSceneWithCourtain("0",1);
                UpdateVisual();
            }
            else if (a.code == 2) 
            {
                emailFieldImage.sprite = IncorrectImage;
                Text.text = "";
                PlaceHolderEmail.text = "Error, el Email ya ha sido registrado antes";
            }
        });
    }
}
