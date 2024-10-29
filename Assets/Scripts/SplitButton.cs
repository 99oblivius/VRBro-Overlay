using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SplitButton : MonoBehaviour {
    [SerializeField] private VRBro VRBro;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float offsetX = 50f;
    
    private RectTransform popupTransform;
    private const float FADE_DURATION = 0.333f;
    private float transitionTime = 0f;

    private Vector2 showPosition;
    private Vector2 hidePosition;

    private void Start() {
        popupTransform = canvasGroup.GetComponent<RectTransform>();
        
        showPosition = popupTransform.anchoredPosition;
        hidePosition = showPosition + new Vector2(offsetX, 0);
        
        popupTransform.anchoredPosition = hidePosition;
        canvasGroup.alpha = 0f;
    }

    private void Update() {
        if (VRBro.recordingActive) {
            transitionTime = Mathf.Min(transitionTime + Time.deltaTime / FADE_DURATION, 1f);
        } else {
            transitionTime = Mathf.Max(transitionTime - Time.deltaTime / FADE_DURATION, 0f);
        }

        canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, transitionTime);

        popupTransform.anchoredPosition = Vector2.Lerp(
            hidePosition,
            showPosition,
            Mathf.SmoothStep(0f, 1f, transitionTime)
        );
    }

    public async void OnSplitButtonClick() {
        var originalColor = text.color;
        text.color = new Color32(116, 132, 117, 255);
        await System.Threading.Tasks.Task.Delay(200);
        text.color = originalColor;
    }
}