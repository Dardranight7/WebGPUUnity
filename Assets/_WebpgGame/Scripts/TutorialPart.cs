using UnityEngine;

public class TutorialPart : MonoBehaviour
{
    [SerializeField] string keyName = "Tuto";

    public TutorialPart NextPart;

    private void OnEnable()
    {
        TutorialManager.Instance.NeedToShow(keyName, this);
    }

    public void ShowNextPart()
    {
        if (NextPart != null)
        {
            NextPart.gameObject.SetActive(true);
        }
        gameObject.SetActive(false);
    }
}
