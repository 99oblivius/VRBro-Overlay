using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SplitRecordingButton : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Button recordButton; // Reference to the Rec button
    [SerializeField] private CanvasGroup canvasGroup; // For fading

    private bool isHovering = false;
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private const float FADE_DURATION = 0.333f;
    
    private void Awake()
    {
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        var recordButtonEventTrigger = recordButton.gameObject.GetComponent<EventTrigger>();
        if (recordButtonEventTrigger == null) {
            recordButtonEventTrigger = recordButton.gameObject.AddComponent<EventTrigger>();
        }

        var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => OnRecordButtonHoverEnter());
        recordButtonEventTrigger.triggers.Add(pointerEnter);

        var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener((data) => OnRecordButtonHoverExit());
        recordButtonEventTrigger.triggers.Add(pointerExit);
    }

    private void OnDisable()
    {
        var recordButtonEventTrigger = recordButton.gameObject.GetComponent<EventTrigger>();
        if (recordButtonEventTrigger != null) {
            recordButtonEventTrigger.triggers.Clear();
        }
    }

    private void Update()
    {
        bool shouldBeVisible = isHovering && VRBro.recordingActive;
        targetAlpha = shouldBeVisible ? 1f : 0f;

        if (currentAlpha != targetAlpha)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / FADE_DURATION);
            canvasGroup.alpha = currentAlpha;
            canvasGroup.blocksRaycasts = currentAlpha > 0;
        }
    }

    private void OnRecordButtonHoverEnter()
    {
        isHovering = true;
    }

    private void OnRecordButtonHoverExit()
    {
        isHovering = false;
    }

    public void OnSplitSuccessful()
    {
        StartCoroutine(FlashFeedback());
    }

    private IEnumerator FlashFeedback()
    {
        var originalColor = GetComponent<Image>().color;
        GetComponent<Image>().color = new Color32(116, 132, 117, 255);
        yield return new WaitForSeconds(0.2f);
        GetComponent<Image>().color = originalColor;
    }
}