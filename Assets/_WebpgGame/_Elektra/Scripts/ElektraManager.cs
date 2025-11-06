using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElektraManager : MonoBehaviour
{
    public static ElektraManager Instance;

    // Referencia al ScriptableObject de datos
    [Header("Activity Data")]
    [SerializeField] private ActivityDataSO globalActivityData;
    [SerializeField] private SceneConfigSO sceneConfig;

    // Campo para guardar el total de actividades para la escena actualmente cargada.
    [SerializeField] private int _currentSceneActivitiesCount = 0;
    
    public event Action<string, string> OnActivityCompleted;
    public event Action<string, int> OnCountCurrent;
    
    // Propiedad de acceso al estado del tutorial
    public bool IsTutorialComplete => globalActivityData != null && globalActivityData.isTutorialComplete;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Verificación importante: El SO debe estar asignado
            if (globalActivityData == null || sceneConfig == null)
            {
                Debug.LogError("FATAL ERROR: GlobalActivityDataSO no asignado en ElektraManager.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        OnActivityCompleted += RegisterActivityCompleted;
    }

    private void OnDisable()
    {
        OnActivityCompleted -= RegisterActivityCompleted;
    }
    
    // Ahora usa el ScriptableObject para registrar el estado
    // Llamado por el evento OnActivityCompleted
    private void RegisterActivityCompleted(string scene, string activity)
    {
        string key = $"{scene}_{activity}";
        // 1. Guarda el estado en el ScriptableObject
        globalActivityData.RegisterActivityCompleted(key); 
        
        // 2. Notifica a otros sistemas
        int completed = GetActivityCompleted(scene);
        OnCountCurrent?.Invoke(scene, completed);

        //Verifica si se completaron todas las actividades de la escena para el premio
        if (completed >= _currentSceneActivitiesCount && _currentSceneActivitiesCount > 0)
        {
            GiveRewardForScene(scene);
        }
    }
    // Llamado por ElektraActivity
    public void ActivityCompleted(string scene, string activity)
    {
        Debug.Log($"Reporting activity completed: {scene}_{activity}");
        OnActivityCompleted?.Invoke(scene, activity);
    }

    // Usa el ScriptableObject para consultar el estado
    // Llamado por ElektraActivity
    public bool ThisActivityCompleted(string key)
    {
        return globalActivityData.IsActivityCompleted(key);
    }

    // Usa el ScriptableObject para contar las actividades
    public int GetActivityCompleted(string scene)
    {
        return globalActivityData.GetActivityCompletedCount(scene);
    }

    // Usa el ScriptableObject para registrar la escena
    public void RegisterSceneVisited(string sceneId)
    {
        globalActivityData.RegisterSceneVisited(sceneId);
        Debug.Log($"User intro the scene {sceneId}");
    }
    public void CompleteTutorial()
    {
        if (globalActivityData != null)
        {
            globalActivityData.CompleteTutorial();
        }
    }
    // Permite al SceneActivityInitializer establecer el total esperado.
    public void SetCurrentSceneActivitiesCount(int count)
    {
        _currentSceneActivitiesCount = count;
        Debug.Log($"ElektraManager: Total de actividades para la escena actual establecido en {_currentSceneActivitiesCount}.");
    }
    // Obtener el Maximo desde el Manager
    public int GetCurrentSceneActivitiesMax()
    {
        return _currentSceneActivitiesCount;
    }
    // Metodo para que el AgesContainer obtenga el valor
    public int GetMaxActivitiesForScene(string sceneId)
    {
        if (sceneConfig == null)
        {
            Debug.LogError("SceneConfigSO no asignado en ElektraManager.");
            return 0;
        }
        return sceneConfig.GetMaxActivities(sceneId);
    }
    public bool ValidateProgress(out string message, string idScene)
    {
        message = "";
        
        // Usamos el valor dinámico guardado
        int requiredActivities = _currentSceneActivitiesCount;

        int sceneActivities = GetActivityCompleted(idScene);
        string detailedProgress = $"\n- {idScene}: {sceneActivities}/{requiredActivities} actividades";

        if (sceneActivities >= requiredActivities && requiredActivities > 0)
        {
            message = $"¡Felicidades! Has completado todas las actividades.{detailedProgress}";
            return true;
        }
        else
        {
            int remaining = requiredActivities - sceneActivities;
            message =  $" Te faltan {remaining} actividades.{detailedProgress}";
            return false;
        }
    }
    
    // Logica para entregar el premio
    private void GiveRewardForScene(string sceneId)
    {
        Debug.Log($"¡PREMIO ENTREGADO por completar todas las actividades en {sceneId}!");
        // TODO
        // aun no se ha definido la logica ni la realidad de cual sera el premio
    }
    
    
    public void LoadLevelAsync(string targetSceneName)
    { 
        SceneData.nextSceneId = targetSceneName;
        
        StartCoroutine(LoadSceneAsync(SceneData.baseLoadSceneId));
    }
    
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null; 
        }
    }
}
