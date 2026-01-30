using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Backend : MonoBehaviour
{
    public string Serial;

    const string DAILY_RECOMPENSE = "get-daily-recompense/";
    const string NOTIFY_VICTORY = "notify-victory/";
    const string LOGIN = "login-alpina/";
    const string DISCONNECT = "disconect-alpina/";
    const string REGISTER = "create-alpina-user/";
    const string UPDATE = "update-user-stats/";
    const string UPDATE_DECK = "update-alpina-deck/";
    const string GET_MOCHI_USER_DATA = "get-mochi-user-data/";
    const string UPDATE_PROFILE_DATA = "update-profile-data/";
    const string COUNT_TIME = "count-time/";
    const string GET_RIVAL_USER_DATA = "get-rival-mochi-user-data/";
    const string GET_RANKING = "get-ranking";
    
    public static Backend singleton;

    public PlayerProfileDTO playerProfile;
    public static Action OnPlayerProfileUpdate;

    public List<Mochi> MochiDB;

    [System.Serializable]
    public class Mochi
    {
        public string name;
        public Sprite image;
    }

    public enum Petition
    {
        getDailyRecompense,
        notifyVictory,
        login,
        register,
        updateDeck,
        getData,
        updateProfile,
        disconectUser,
        countTime,
        getRivalUserData,
        update,
        getRanking,
    }

    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            singleton = this;
        }
    }

    public float tiempoEspera = 5f;
    private float tiempoTranscurrido = 0f;

    private void Update()
    {
        if (string.IsNullOrEmpty(playerProfile.serial))
            tiempoTranscurrido = 0;
        
        // Sumar el tiempo transcurrido en cada frame
        tiempoTranscurrido += Time.deltaTime;

        // Verificar si el tiempo transcurrido ha superado el tiempo establecido
        if (tiempoTranscurrido >= tiempoEspera)
        {
            // Llamar a la función UpdateTime
            Backend.singleton.CountTime(playerProfile.serial);

            // Restablecer el tiempo transcurrido (si quieres que se repita cada X segundos)
            tiempoTranscurrido = 0f;
        }
    }

    public void OnQuitFromJS()
    {
        Disconnect(playerProfile.serial);
    }

    public void OnApplicationQuit()
    {
        Disconnect(playerProfile.serial);
    }

    public void TakeDailyRecopense(string serial, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.getDailyRecompense, JsonConvert.SerializeObject(new UserDataDTO
        {
            serial = serial
        }), Result);
    }

    public void Login(string email, string password ,System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.login, JsonConvert.SerializeObject(new LoginAlpinaUserDTO
        {
            email = email,
            password = password
        }), Result);
    }

    public void Disconnect(string serial, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.disconectUser, JsonConvert.SerializeObject(new UserDataDTO
        {
            serial = serial
        }), Result);
    }

    public void CountTime(string serial, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.countTime, JsonConvert.SerializeObject(new UserDataDTO
        {
            serial = serial
        }), Result);
    }

    public void NotifyWinDrawLoose(string serial , int type, int result, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.notifyVictory, JsonConvert.SerializeObject(new NotifyVictoryDTO
        {
            serial = serial,
            type = type,
            result = result
        }), Result);
    }

    public void Register(string email, string password, string name, string secondName, string userName, string phone, string city, string documentType, string documentNumber, System.Action<Response> Result = null, System.Action<string> OnError = null)
    {
        HacerPeticionPOST(Petition.register, JsonConvert.SerializeObject(new RegisterAlpinaUserDTO
        {
            email = email,
            password = password,
            nombre = name,
            apellido = secondName,
            serial = "",
            userName = userName,
            phone = phone,
            city = city,
            documentType = documentType,
            documentNumber = documentNumber,
        }), Result, OnError);
    }

    public void UpdateData(object data, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.update, JsonConvert.SerializeObject(data), Result);
    }

    public void GetRanking(object data, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.getRanking, JsonConvert.SerializeObject(data), Result);
    }

    public void UpdateProfileData(string serial, int profile, int frame, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.updateProfile, JsonConvert.SerializeObject(new UpdateProfileDTO
        {
            serial = serial,
            profile = profile,
            frame = frame,
            username = "TODO"
        }), Result);
    }

    public void UpdateDeck(string serial, string deck, string powerDeck, string familyDeck, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.updateDeck, JsonConvert.SerializeObject(new UpdateDTO
        {
            serial = serial,
            deck = deck,
            powerDeck = powerDeck,
            familyDeck = familyDeck
        }), Result);
    }

    public void GetUserData(string serial, System.Action<Response> Result = null)
    {
        HacerPeticionPOST(Petition.getData, JsonConvert.SerializeObject(new UserDataDTO
        {
            serial = serial
        }), Result);
    }

    // Endpoint de la API (puedes asignarlo desde el Inspector o directamente en el código)
    private static string endpoint = "https://fanschevrolet-backend-production-eeec.up.railway.app/api/";  // Cambia esta URL por la de tu API

    // Método para hacer la solicitud POST
    public void HacerPeticionPOST(Petition petition, string jsonData, System.Action<Response> Result = null, System.Action<string> OnError = null)
    {
        // Los datos que vas a enviar (por ejemplo, un JSON)
        // Asegúrate de que los datos se puedan serializar correctamente en JSON
        string plus = "";
        switch (petition)
        {
            case Petition.getDailyRecompense:
                plus = DAILY_RECOMPENSE;
                break;
            case Petition.notifyVictory:
                plus = NOTIFY_VICTORY;
                break;
            case Petition.login:
                plus = LOGIN;
                break;
            case Petition.register:
                plus = REGISTER;
                break;
            case Petition.update:
                plus = UPDATE;
                break;
            case Petition.updateDeck:
                plus = UPDATE_DECK;
                break;
            case Petition.getData:
                plus = GET_MOCHI_USER_DATA;
                break;
            case Petition.updateProfile:
                plus = UPDATE_PROFILE_DATA;
                break;
            case Petition.disconectUser:
                plus = DISCONNECT;
                break;
            case Petition.countTime:
                plus = COUNT_TIME;
                break;
            case Petition.getRivalUserData:
                plus = GET_RIVAL_USER_DATA;
                break;
            case Petition.getRanking:
                plus = GET_RANKING;
                break;
            default:
                break;
        }
        // Llama a la corutina que maneja la solicitud
        StartCoroutine(PostRequest(endpoint + plus, jsonData, Result, OnError));
    }

    // Corutina para realizar la solicitud POST
    IEnumerator PostRequest(string url, string jsonData,  System.Action<Response> Result = null, System.Action<string> OnError = null)
    {
        // Crea un objeto UnityWebRequest y configura los parámetros
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        // Convierte el string jsonData en un byte array para la solicitud POST
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);

        // Configura el tipo de contenido como JSON
        request.SetRequestHeader("Content-Type", "application/json");

        // Configura el handler de descarga para recibir la respuesta
        request.downloadHandler = new DownloadHandlerBuffer();

        // Envia la solicitud y espera la respuesta
        yield return request.SendWebRequest();

        // Verifica si hubo algún error
        if (request.result == UnityWebRequest.Result.Success)
        {
            // Si la solicitud fue exitosa, muestra la respuesta
            try
            {
                Response response = JsonConvert.DeserializeObject<Response>(request.downloadHandler.text);
                if (response != null)
                {
                    Result?.Invoke(response);
                }
                else
                {
                    OnError?.Invoke("Error al procesar la respuesta del servidor");
                }
            }
            catch
            {
                Result?.Invoke(new Response { code = 2, success = false, message = "Error al procesar la respuesta del servidor", data = null });
                Debug.LogError("Any was wrong with response");
                Debug.Log(request.downloadHandler.text);
            }
        }
        else
        {
            // Si hubo un error, muestra el error
            Debug.LogError("Error en la solicitud: " + request.error);
            OnError?.Invoke(request.error);
        }        
    }

    [System.Serializable]
    public class LoginAlpinaUserDTO
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    public class NotifyVictoryDTO
    {
        public string serial;
        public int type;
        public int result;
    }

    [System.Serializable]
    public class RegisterAlpinaUserDTO
    {
        public string serial;
        public string userName;
        public string nombre;
        public string apellido;
        public string email;
        public string password;
        public string phone;
        public string city;
        public string documentType;
        public string documentNumber;
    }

    [System.Serializable]
    public class Response
    {
        public int code;
        public bool success;
        public string message;
        public string data;
    }

    [System.Serializable]
    public class UserDataDTO
    {
        public string serial;
    }

    [System.Serializable]
    public class UpdateActiveMochiDTO
    {
        public string serial;
        public int activeMochiIndex;
    }

    [System.Serializable]
    public class UpdateDTO
    {
        public string serial;
        public string deck;
        public string powerDeck;
        public string familyDeck;
    }

    [System.Serializable]
    public class UpdateProfileDTO
    {
        public string serial;
        public int profile;
        public int frame;
        public string username;
    }

    public class UpdateUnlockedMochisDTO
    {
        public string serial;
        public string unlockedMochis;
    }

    [System.Serializable]
    public class PlayerProfileDTO
    {
        public string serial;
        public string nombre;
        public string apellido;
        public string email;
        public string userName;
        public string password;
        public string documentType;
        public string documentNumber;
        public string phone;
        public string city;

        public bool dataProcessingConsent;
        public bool acceptedTermsAndConditions;
        public bool completedTutorial;

        public int minigame1PlayCount;
        public int minigame2PlayCount;
        public int minigame3PlayCount;
        public int minigame4PlayCount;
        public int tournamentPlayCount;

        public int minigame1WinCount;
        public int minigame2WinCount;
        public int minigame3WinCount;
        public int minigame4WinCount;
        public int tournamentWinCount;

        public int pcConnectionCount;
        public int mobileConnectionCount;

        public int gems;
        public int mochiPoints;

        public string unlockedMochis;
        public List<int> userInventory = new List<int>();
        public List<int> userEquip = new List<int>();

        public int activeMochiIndex;
        public int consecutiveDaysStreak;

        public DateTime date;
        public DateTime createdAt;
        public DateTime lastUpdated;
        public double connectedTime;
        public int loginCount;
        public int disconectCount;
        public DateTime lastLogin;
    }
}
