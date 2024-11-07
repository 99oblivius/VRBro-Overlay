using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneButton : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] public Button button;
    [SerializeField] public TextMeshProUGUI buttonText;
    [SerializeField] public Image backgroundImage;
    #endregion

    #region Constants
    public static readonly Color32 NormalColor = new(61, 68, 80, 255);
    public static readonly Color32 HoverColor = new(88, 97, 112, 255);
    public static readonly Color32 PressedColor = new(42, 47, 56, 255);
    public static readonly Color32 ActiveColor = new(116, 132, 117, 255);
    #endregion

    #region Unity Lifecycle
    private void Awake() {
        InitializeComponents();
        SetupButtonColors();
        SetupTextStyle();
    }
    #endregion

    #region Initialization
    private void InitializeComponents() {
        button ??= GetComponent<Button>();
        buttonText ??= GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage ??= GetComponent<Image>();
    }
    
    private void SetupButtonColors() {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = NormalColor;
        colors.highlightedColor = HoverColor;
        colors.pressedColor = PressedColor;
        colors.selectedColor = HoverColor;
        colors.disabledColor = new Color32(61, 68, 80, 128);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }
    
    private void SetupTextStyle() {
        if (buttonText == null) return;
        
        buttonText.color = Color.white;
        buttonText.fontSize = 16;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.margin = new Vector4(10, 0, 10, 0);
    }
    #endregion

    #region Public Methods
    public void SetSceneName(string sceneName) {
        if (buttonText != null) {
            buttonText.text = sceneName;
        }
    }
    #endregion
}