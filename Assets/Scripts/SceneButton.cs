using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneButton : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Image backgroundImage;
    
    private static readonly Color32 NormalColor = new(61, 68, 80, 255);
    private static readonly Color32 HoverColor = new(88, 97, 112, 255);
    private static readonly Color32 PressedColor = new(42, 47, 56, 255);
    
    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        
        SetupButtonColors();
        SetupTextStyle();
    }
    
    private void SetupButtonColors() {
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
    
    public void SetSceneName(string sceneName) {
        if (buttonText != null) {
            buttonText.text = sceneName;
        }
    }
}