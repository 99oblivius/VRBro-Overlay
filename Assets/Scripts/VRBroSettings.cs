using UnityEngine;
using UnityEngine.UI;

public class VRBroSettings : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Image imageBindingsEnabled;
    [SerializeField] private Image imageRecordingParent;
    [SerializeField] private Image imageStreamingParent;
    [SerializeField] private SplitRecordingButton splitRecordingButton;

    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);   // Green
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255); // Red

    public void OnEnableButtonClick() {
        VRBro.bindingsEnabled = true;
        imageBindingsEnabled.color = ActiveColor;
    }

    public void OnDisableButtonClick() {
        VRBro.bindingsEnabled = false;
        imageBindingsEnabled.color = InactiveColor;
    }

    public void OnStartBufferButtonClick() { VRBro.startBuffer = true; }

    public void OnStopBufferButtonClick() { VRBro.stopBuffer = true; }

    public void OnRecordingButtonClick() {
        if (VRBro.recordingActive) {
            VRBro.stopRecording = true;
        } else {
            VRBro.startRecording = true;
        }
    }

    public void OnSplitRecordingButtonClick() {
        if (VRBro.recordingActive) {
            VRBro.splitRecording = true;
            if (splitRecordingButton != null) {
                splitRecordingButton.OnSplitSuccessful();
            }
        }
    }

    public void OnStreamingButtonClick() {
        if (VRBro.streamingActive) {
            VRBro.stopStreaming = true;
        } else {
            VRBro.startStreaming = true;
        }
    }

    private void Update() {
        if (imageRecordingParent != null) {
            imageRecordingParent.color = VRBro.recordingActive ? ActiveColor : InactiveColor;
        }

        if (imageStreamingParent != null) {
            imageStreamingParent.color = VRBro.streamingActive ? ActiveColor : InactiveColor;
        }
    }
}