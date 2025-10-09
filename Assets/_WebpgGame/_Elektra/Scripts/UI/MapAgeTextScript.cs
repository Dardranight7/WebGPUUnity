using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MapAgeTextScript : MonoBehaviour
{
    public string nameSceneScore;
    public TMP_Text score;
    void Start()
    {
        if (SceneManager.GetActiveScene().name != nameSceneScore)
        {
            this.gameObject.SetActive(false);
            return;
        }

        Debug.Log(ElektraManager.Instance._activityCompletadas.Count);
        score.text = $"{ElektraManager.Instance.GetActivityCompleted(nameSceneScore).ToString()}/25";

    }

    private void OnEnable()
    {
        ElektraManager.Instance.OnCountCurrent += UpdateProgress;
    }
    private void OnDisable()
    {
        ElektraManager.Instance.OnCountCurrent -= UpdateProgress;
    }

    private void UpdateProgress(string scene, int progress)
    {
        score.text = $"{ElektraManager.Instance.GetActivityCompleted(nameSceneScore).ToString()}/25";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
