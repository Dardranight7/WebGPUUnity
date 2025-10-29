using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class TobogganCurveDetector : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private PlayerSurfaceInput playerMovement;
    
    [Header("Configuración")]
    [SerializeField] private float distanciaDeteccion = 5f; // Qué tan adelante detectar
    [SerializeField] private float anguloMinimoCurva = 30f; // Ángulo para considerar curva cerrada
    
    [Header("UI/Indicadores")]
    [SerializeField] private GameObject indicadorCurvaIzquierda;
    [SerializeField] private GameObject indicadorCurvaDerecha;

    private void Update()
    {
        if (splineContainer == null || playerMovement == null) return;
        
        DetectarCurvaProxima();
    }

    void DetectarCurvaProxima()
    {
        float progresoActual = playerMovement.ObtenerProgresoActual();
        float longitudSpline = splineContainer.Spline.GetLength();
        float incrementoDeteccion = distanciaDeteccion / longitudSpline;
        
        float progresoFuturo = Mathf.Min(progresoActual + incrementoDeteccion, 1f);
        
        // Obtener direcciones
        float3 direccionActual = splineContainer.EvaluateTangent(progresoActual);
        float3 direccionFutura = splineContainer.EvaluateTangent(progresoFuturo);
        
        // Calcular ángulo entre direcciones
        float angulo = Vector3.Angle(direccionActual, direccionFutura);
        
        if (angulo > anguloMinimoCurva)
        {
            // Determinar si la curva es a la izquierda o derecha
            Vector3 cross = Vector3.Cross(direccionActual, direccionFutura);
            bool curvaALaDerecha = cross.y > 0;
            
            MostrarIndicador(curvaALaDerecha);
        }
        else
        {
            OcultarIndicadores();
        }
    }

    void MostrarIndicador(bool esDerecha)
    {
        if (indicadorCurvaIzquierda != null)
            indicadorCurvaIzquierda.SetActive(!esDerecha);
            
        if (indicadorCurvaDerecha != null)
            indicadorCurvaDerecha.SetActive(esDerecha);
    }

    void OcultarIndicadores()
    {
        if (indicadorCurvaIzquierda != null)
            indicadorCurvaIzquierda.SetActive(false);
            
        if (indicadorCurvaDerecha != null)
            indicadorCurvaDerecha.SetActive(false);
    }
}