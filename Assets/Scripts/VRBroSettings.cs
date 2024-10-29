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
        settings = Settings.Load();
        imageBindingsEnabled.color = settings.BindingsEnabled ? ActiveColor : InactiveColor;
    }

    public void OnEnableButtonClick() {
        VRBro.bindingsEnabled = true;
        imageBindingsEnabled.color = ActiveColor;
        settings.BindingsEnabled = true;
        settings.Save();
    }

    public void OnDisableButtonClick() {
        VRBro.bindingsEnabled = false;
        imageBindingsEnabled.color = InactiveColor;
        settings.BindingsEnabled = false;
        settings.Save();
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