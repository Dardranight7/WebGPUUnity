using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BotControllerBotCollet : MonoBehaviour
{
    public float moveForce = 12f;
    public float maxSpeed = 4f;
    public float checkInterval = 0.5f;
    public float reachDistance = 0.6f; // distancia para considerar que recogió la gomita (se activa trigger en la candy también)

    Rigidbody rb;
    Collector collector;
    Transform targetCandy;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        collector = GetComponent<Collector>();
    }

    void OnEnable()
    {
        StartCoroutine(DecisionLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator DecisionLoop()
    {
        while (true)
        {
            FindNearestCandy();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    void FixedUpdate()
    {
        if (targetCandy == null) return;

        Vector3 dir = (targetCandy.position - transform.position);
        dir.y = 0;
        float dist = dir.magnitude;
        if (dist < reachDistance)
        {
            // si está cerca, dejar que el trigger de la candy lo recoja (o destruir manualmente)
            return;
        }

        dir.Normalize();

        if (rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(dir * moveForce, ForceMode.Acceleration);

        // rotar bot hacia movimiento suave (opcional)
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.fixedDeltaTime);
        }
    }

    void FindNearestCandy()
    {
        GameObject[] candies = GameObject.FindGameObjectsWithTag("Candy");
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var c in candies)
        {
            if (c == null) continue;
            float d = Vector3.SqrMagnitude(c.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }

        targetCandy = best;
    }
}