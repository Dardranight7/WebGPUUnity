using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class WorldUI : MonoBehaviour
{
    public enum FacingTarget
    {
        Camera,
        Player,
        Hybrid
    }; 
    
    public Canvas canvas;
    public Button button;

    public Transform player;
    public Camera uiCamera;

    [FormerlySerializedAs("FollowPlayer")] public bool FollowCameraToPlayer; 

    [Min(0f)] public float triggerRadius;
    public FacingTarget facingTarget = FacingTarget.Camera;
    public UnityEvent onButtonClick;
    
    private SphereCollider sphereCollider;
    private Action targetAction;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player").transform;
        if(!uiCamera) uiCamera = Camera.main;

        sphereCollider = gameObject.GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = triggerRadius;
        
        UpdateTargetAction();
    }

    private void UpdateTargetAction()
    {
        if (!FollowCameraToPlayer) return;
        targetAction = facingTarget switch
        {
            FacingTarget.Camera => FacaCamera,
            _=> null
        };
    }

    private void FacaCamera()
    {
        if (!canvas || !uiCamera ) return;
        var toCam = canvas.transform.position - player.transform.position;
        canvas.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
    }

    private void LateUpdate()
    {
        if (canvas.gameObject.activeSelf) targetAction?.Invoke();
        
    }

    private void OnEnable()
    {
        if (!canvas)
        {
            Debug.LogError("Canvas referent misss");
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = uiCamera;
        canvas.gameObject.SetActive(false);
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(()=>{
                onButtonClick?.Invoke();
            });
        }
    }

    // private void OnDisable()
    // {
    //     throw new NotImplementedException();
    // }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"enter:{other.name}");
        if (!other.CompareTag("Player")) return;
        
        ShowUI();
    }    
    
    private void OnTriggerExit(Collider other)
    {
        Debug.Log("exit");
        if (!other.CompareTag("Player")) return;
        
        HideUI();
    }

    private void ShowUI()
    {
        canvas.gameObject.SetActive(true);
    }
    private void HideUI()
    {
        canvas.gameObject.SetActive(false);
    }

}
