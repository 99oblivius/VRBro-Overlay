using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class SplitButton : MonoBehaviour {
    [SerializeField] private Controller streamingController;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float offsetX = 50f;
    
    private RectTransform popupTransform;
    private const float FADE_DURATION = 0.333f;
    private float transitionTime = 0f;
    private bool isVisible = false;

    private Vector2 showPosition;
    private Vector2 hidePosition;

    private void Start() {
        popupTransform = canvasGroup.GetComponent<RectTransform>();
        
        showPosition = popupTransform.anchoredPosition;
        hidePosition = showPosition + new Vector2(offsetX, 0);
        
        popupTransform.anchoredPosition = hidePosition;
        canvasGroup.alpha = 0f;

        if (streamingController != null) {
            streamingController.OnStateChanged += HandleStateChanged;
        }
    }

    private void HandleStateChanged(StreamOperation operation, bool active) {
        if (operation == StreamOperation.Recording) {
            isVisible = active;
        }
    }

    private void Update() {
        if (isVisible) {
            transitionTime = Mathf.Min(transitionTime + Time.deltaTime / FADE_DURATION, 1f);
        } else {
            transitionTime = Mathf.Max(transitionTime - Time.deltaTime / FADE_DURATION, 0f);
        }

        var t = Mathf.SmoothStep(0f, 1f, transitionTime);
        canvasGroup.alpha = t;
        popupTransform.anchoredPosition = Vector2.Lerp(hidePosition, showPosition, t);
    }

    public async void OnSplitButtonClick() {
        if (!streamingController.stateManager.RecordingActive || 
            streamingController.stateManager.IsOperationPending(StreamOperation.Recording.ToString())) {
            return;
        }

        var originalColor = text.color;
        text.color = new Color32(116, 132, 117, 255);
        
        try {
            // Network operation for splitting recording would go here
            await streamingController.SplitRecording();
        } finally {
            await Task.Delay(200);
            text.color = originalColor;
        }
    }

    private void OnDestroy() {
        if (streamingController != null) {
            streamingController.OnStateChanged -= HandleStateChanged;
        }
    }
}