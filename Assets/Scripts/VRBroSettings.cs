using UnityEngine;
using UnityEngine.UI;

public class VRBroSettings : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Image imageBindingsEnabled;
    [SerializeField] private SplitButton splitRecordingButton;
    [SerializeField] private StatusTracker statusTracker;

    private Settings settings;
    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);   // Green
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255); // Red

    private void Awake() {
        imageBindingsEnabled.color = Settings.Instance.BindingsEnabled ? ActiveColor : InactiveColor;
        Settings.Instance.OnSettingsChanged += OnSettingsChanged;
    }

    private void OnDestroy() {
        Settings.Instance.OnSettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged() {
        UpdateBindingsUI(Settings.Instance.BindingsEnabled);
    }

    private void UpdateBindingsUI(bool enabled) {
        imageBindingsEnabled.color = enabled ? ActiveColor : InactiveColor;
        VRBro.bindingsEnabled = enabled;
    }

    public void OnEnableButtonClick() {
        Settings.Instance.BindingsEnabled = true;
    }

    public void OnDisableButtonClick() {
        Settings.Instance.BindingsEnabled = false;
    }

    public async void OnStartBufferButtonClick() {
        await statusTracker.StartReplayBuffer();
    }

    public async void OnStopBufferButtonClick() {
        await statusTracker.StopReplayBuffer();
    }

    public async void OnRecordingButtonClick()
    {
        if (VRBro.recordingActive) {
            await statusTracker.StopRecording();
        } else {
            await statusTracker.StartRecording();
        }
    }

    public void OnSplitRecordingButtonClick() {
        if (VRBro.recordingActive) {
            VRBro.splitRecording = true;
            splitRecordingButton.OnSplitButtonClick();
        }
    }

    public async void OnStreamingButtonClick()
    {
        if (VRBro.streamingActive) {
            await statusTracker.StopStreaming();
        } else {
            await statusTracker.StartStreaming();
        }
    }
}