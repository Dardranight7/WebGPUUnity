using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Cinemachine;

public class WorldUI : MonoBehaviour
{
    public enum FacingTarget
    {
        Camera,
        Player,
        Hybrid
    }; 
    
    [Header("Referent")]
    public Canvas canvas;
    public Button button;
    
    [Header("Behavior")]
    public Transform player;
    public Camera uiCamera;
    [FormerlySerializedAs("FollowPlayer")] public bool isFollow; 
    public FacingTarget facingTarget = FacingTarget.Camera;
    [Min(0f)] public float triggerRadius;
    public UnityEvent onButtonClick;
    
    [Header("Cinemachine")]
    [Min(0f)] public float groupWeight;
    [Min(0f)] public float groupRadius;
    public CinemachineInputAxisController inputAxisController;
    public float fadeSmoothTime = 0.2f;
    


    [Header("UI Fade")]
    public CanvasGroup canvasGroup;
    private bool pendingHide;
    private float targetAlpha, currentAlpha, alphaVelocity; 
    
    
    
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
        

    }

    private void Update()
    { 
        UpdateTargetAction();
        if (canvasGroup)
        {
            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, fadeSmoothTime);
            canvasGroup.alpha = currentAlpha;

            bool interact = currentAlpha > 0.5f;
            canvasGroup.interactable = interact;
            canvasGroup.blocksRaycasts = interact;
        }

        if (pendingHide)
        {
            bool alphaDone = !canvasGroup || currentAlpha <= 0.01f || Mathf.Approximately(currentAlpha, 0f);

            if (alphaDone)
            {
                canvas.gameObject.SetActive(false);
                inputAxisController.enabled = true;
                pendingHide = false;

            }

        }

    } 
        

    private void UpdateTargetAction()
    {
        if (!isFollow) return;
        targetAction = facingTarget switch
        {
            FacingTarget.Camera => FacaCamera,
            FacingTarget.Player => FacePlayer,
            FacingTarget.Hybrid => FaceHybrid,
            _=> null
        };
    }

    private void FacaCamera()
    {
        if (!canvas || !uiCamera ) return;
        var toCam = canvas.transform.position - uiCamera.transform.position;
        canvas.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
    }

    private void FaceHybrid()
    {
        if (!canvas || !uiCamera ) return;
        var camFwdFlat = Vector3.ProjectOnPlane(uiCamera.transform.position, Vector3.up);
        if (camFwdFlat.sqrMagnitude <= Vector3.kEpsilon) return;
        canvas.transform.rotation = Quaternion.LookRotation(camFwdFlat, Vector3.up);
    }

    private void FacePlayer()
    {
        if (!canvas || !uiCamera ) return;
        var toPlayer = canvas.transform.position - player.position;
        var flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (flat.sqrMagnitude <= Vector3.kEpsilon) return;
        canvas.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
    }

    private void LateUpdate()
    {
        if (canvas.gameObject.activeSelf) targetAction?.Invoke();
        
    }

    private void OnEnable()
    {
        if (!canvas)
        {
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = uiCamera;
        canvas.gameObject.SetActive(false);

        if (!canvasGroup) canvas.TryGetComponent(out canvasGroup);

        if (canvasGroup)
        {
            currentAlpha = targetAlpha = 0;
            alphaVelocity = 0f;
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

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
        inputAxisController.enabled = false;
        targetAlpha = 1f;
        
    }
    private void HideUI()
    {
        // canvas.gameObject.SetActive(false);
        // inputAxisController.enabled = true;
        targetAlpha = 0f;
        pendingHide = true;
    }

}
