using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class BotSpawnerAlpinaria : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject prefabBot;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private RaceManager raceManager;
    
    [Header("Configuración de Spawn")]
    [SerializeField] private int cantidadBots = 5;
    [SerializeField] private float espaciadoInicial = 2f; // Distancia entre bots al inicio
    [SerializeField] private float anchoFormacion = 2f;
    
    [Header("Dificultad de Bots")]
    [SerializeField] private BotAlpinaria.TipoDificultad dificultadGeneral = BotAlpinaria.TipoDificultad.Normal;
    [SerializeField] private bool dificultadVariada = true;
    
    private List<GameObject> botsSpawneados = new List<GameObject>();

    void Start()
    {
        if (prefabBot != null && splineContainer != null)
        {
            SpawnearBots();
        }
    }

    public void SpawnearBots()
    {
        LimpiarBots();
        
        for (int i = 0; i < cantidadBots; i++)
        {
            CrearBot(i);
        }
    }

    void CrearBot(int indice)
    {
        // Crear instancia del bot
        GameObject bot = Instantiate(prefabBot, transform);
        bot.name = $"Bot_{indice + 1}";
        
        // Configurar componente BotSplineAI
        BotAlpinaria botAI = bot.GetComponent<BotAlpinaria>();
        if (botAI == null)
        {
            botAI = bot.AddComponent<BotAlpinaria>();
        }
        
        // Asignar el spline
        var splineField = botAI.GetType().GetField("splineContainer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        splineField?.SetValue(botAI, splineContainer);
        
        // Configurar dificultad
        if (dificultadVariada)
        {
            BotAlpinaria.TipoDificultad[] dificultades = 
                (BotAlpinaria.TipoDificultad[])System.Enum.GetValues(typeof(BotAlpinaria.TipoDificultad));
            var dificultadField = botAI.GetType().GetField("dificultad",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dificultadField?.SetValue(botAI, dificultades[Random.Range(0, dificultades.Length)]);
        }
        
        // Posición inicial (en formación)
        float offsetLateral = (indice % 3 - 1) * anchoFormacion;
        float3 posicionInicial = splineContainer.EvaluatePosition(0);
        bot.transform.position = posicionInicial + new float3(offsetLateral, 0.5f, -espaciadoInicial * (indice / 3));
        
        botsSpawneados.Add(bot);
    }

    public void LimpiarBots()
    {
        foreach (var bot in botsSpawneados)
        {
            if (bot != null)
            {
                Destroy(bot);
            }
        }
        botsSpawneados.Clear();
    }

    public List<BotAlpinaria> ObtenerBots()
    {
        List<BotAlpinaria> bots = new List<BotAlpinaria>();
        foreach (var botObj in botsSpawneados)
        {
            if (botObj != null)
            {
                var botAI = botObj.GetComponent<BotAlpinaria>();
                if (botAI != null)
                {
                    bots.Add(botAI);
                }
            }
        }
        return bots;
    }
}