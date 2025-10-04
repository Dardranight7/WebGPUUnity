using UnityEngine;

public class AxisLookAt : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Ejes de rotación")]
    public bool useX = true;
    public bool useY = true;
    public bool useZ = true;

    [Tooltip("Si está activado, rota en dirección contraria al target")]
    public bool invert = false;

    void LateUpdate()
    {
        if (target == null) return;

        // Dirección hacia el target
        Vector3 direction = target.position - transform.position;
        if (invert) direction = -direction;

        // Guardar la rotación actual
        Quaternion currentRotation = transform.rotation;

        // Obtener la rotación hacia el target
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

        // Aplicar solo los ejes activados
        Vector3 finalEuler = lookRotation.eulerAngles;
        Vector3 currentEuler = currentRotation.eulerAngles;

        float x = useX ? finalEuler.x : currentEuler.x;
        float y = useY ? finalEuler.y : currentEuler.y;
        float z = useZ ? finalEuler.z : currentEuler.z;

        transform.rotation = Quaternion.Euler(x, y, z);
    }
}
