using UnityEngine;

public class Detector : MonoBehaviour
{
    public string uniqueID; // Coin0, Coin1, etc.
    public LayerMask detectionLayer;
    public GameObject pickupEffect;

    private DetectorManager manager;

    private void Start()
    {
        manager = GetComponentInParent<DetectorManager>();

        // Revisar en PlayerPrefs si este ya fue recogido
        if (PlayerPrefs.GetInt(uniqueID, 0) == 1)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & detectionLayer.value) != 0)
        {
            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, transform.rotation);

            // Guardar en PlayerPrefs como recogido
            PlayerPrefs.SetInt(uniqueID, 1);
            PlayerPrefs.Save();

            // Avisar al manager
            manager.OnChildCollected(this);

            // Desactivarse
            gameObject.SetActive(false);
        }
    }
}