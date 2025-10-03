using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class OptionSelector : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public string label;              
        public UnityEvent onSubmit;        
        public UnityEvent onHighlighted;  
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI optionText;         
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button centerButton;         

    [Header("Opciones")]
    [SerializeField] private Option[] options;
    [SerializeField] private int currentIndex = 0;

    [Header("Eventos globales")]
    public UnityEvent<int, string> onOptionChanged; // (índice, label)

    void Awake()
    {
        leftButton.onClick.AddListener(Previous);
        rightButton.onClick.AddListener(Next);
        centerButton.onClick.AddListener(InvokeCurrent);
    }

    void Start() => Refresh();

    void Next()
    {
        currentIndex = (currentIndex + 1) % options.Length;
        Refresh();
    }

    void Previous()
    {
        currentIndex = (currentIndex - 1 + options.Length) % options.Length;
        Refresh();
    }

    void Refresh()
    {
        if (options == null || options.Length == 0) return;

        var opt = options[currentIndex];

        if (optionText) optionText.text = opt.label;

        opt.onHighlighted?.Invoke();

        onOptionChanged?.Invoke(currentIndex, opt.label);
    }

    void InvokeCurrent()
    {
        if (options == null || options.Length == 0) return;
        options[currentIndex]?.onSubmit?.Invoke();
    }

    public void SetIndex(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, options.Length - 1);
        Refresh();
    }

    public int GetIndex() => currentIndex;
    public string GetLabel() => options != null && options.Length > 0 ? options[currentIndex].label : "";
}
