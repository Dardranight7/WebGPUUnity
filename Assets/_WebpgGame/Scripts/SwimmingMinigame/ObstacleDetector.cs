using System;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleDetector : MonoBehaviour
{
    public static ObstacleDetector Instance;
    [SerializeField] private List<Transform> raycastOrigin = new List<Transform>();
    [SerializeField] private LayerMask layer;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 ReturnPositionUsingIndex(int index)
    {
        return raycastOrigin[index].position;
    }

    public void DetectCarril(Action<List<bool>> Response)
    {
        List<bool> carril = new List<bool>();
        //raycast for each raycast origin trying to find obstacle tag
        foreach (Transform t in raycastOrigin)
        {
            Ray ray = new Ray(t.position, t.up);
            RaycastHit[] result = Physics.RaycastAll(ray, float.PositiveInfinity, layer);
            if (result.Length > 0)
            {
                carril.Add(true);
            }
            else
            {
                carril.Add(false);
            }
        }
        Response?.Invoke(carril);
    }
}
