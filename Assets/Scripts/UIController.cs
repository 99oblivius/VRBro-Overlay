using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {
    [SerializeField] private Controller streamingController;
    [SerializeField] private Image bufferIndicator;
    [SerializeField] private Image recordingIndicator;
    [SerializeField] private Image streamingIndicator;
    
    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255);
    private static readonly Color32 PendingColor = new(132, 132, 117, 255);

    private bool bindingsEnabled = true;
    
    private void Awake() {
        bindingsEnabled = Settings.Instance.BindingsEnabled;
        streamingController.OnStateChanged += HandleStateChanged;
        streamingController.OnPendingStateChanged += HandlePendingStateChanged;
        
        // Initialize UI states
        UpdateIndicatorState(StreamOperation.Buffer, streamingController.stateManager.BufferActive);
        UpdateIndicatorState(StreamOperation.Recording, streamingController.stateManager.RecordingActive);
        UpdateIndicatorState(StreamOperation.Streaming, streamingController.stateManager.StreamingActive);
    }

    private void OnDestroy() {
        if (streamingController != null) {
            streamingController.OnStateChanged -= HandleStateChanged;
            streamingController.OnPendingStateChanged -= HandlePendingStateChanged;
        }
    }
    
    private void HandleStateChanged(StreamOperation operation, bool active) {
        UpdateIndicatorState(operation, active);
    }

    private void HandlePendingStateChanged(StreamOperation operation, bool isPending) {
        Image indicator = GetIndicatorForOperation(operation);
        if (indicator == null) return;

        if (isPending) {
            indicator.color = PendingColor;
        } else {
            UpdateIndicatorState(operation, GetOperationState(operation));
        }
    }

    private void UpdateIndicatorState(StreamOperation operation, bool active) {
        Image indicator = GetIndicatorForOperation(operation);
        if (indicator == null) return;

        // Don't update if operation is pending
        if (streamingController.stateManager.IsOperationPending(operation.ToString())) {
            return;
        }

        if (operation == StreamOperation.Buffer) {
            indicator.material.SetFloat("_WaveAmplitude", active ? 0.5f : 0f);
        } else {
            indicator.color = active ? ActiveColor : InactiveColor;
        }
    }

    private Image GetIndicatorForOperation(StreamOperation operation) {
        return operation switch {
            StreamOperation.Buffer => bufferIndicator,
            StreamOperation.Recording => recordingIndicator,
            StreamOperation.Streaming => streamingIndicator,
            _ => null
        };
    }

    private bool GetOperationState(StreamOperation operation) {
        return operation switch {
            StreamOperation.Buffer => streamingController.stateManager.BufferActive,
            StreamOperation.Recording => streamingController.stateManager.RecordingActive,
            StreamOperation.Streaming => streamingController.stateManager.StreamingActive,
            _ => false
        };
    }

    // Input binding methods
    public async void StartBuffer() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Buffer, true);
    }

    public async void StopBuffer() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Buffer, false);
    }

    public async void StartRecording() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Recording, true);
    }

    public async void StopRecording() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Recording, false);
    }

    public async void StartStreaming() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Streaming, true);
    }

    public async void StopStreaming() {
        if (!bindingsEnabled) return;
        await streamingController.ToggleOperation(StreamOperation.Streaming, false);
    }

    public async void SaveBuffer() {
        if (!bindingsEnabled) return;
        await streamingController.SaveBuffer();
    }

    public async void SplitRecording() {
        if (!bindingsEnabled) return;
        await streamingController.SplitRecording();
    }

    // Button click methods
    public async void OnBufferButtonClick() {
        if (!bindingsEnabled) return;
        bool isActive = streamingController.stateManager.BufferActive;
        await streamingController.ToggleOperation(StreamOperation.Buffer, !isActive);
    }

    public async void OnRecordingButtonClick() {
        if (!bindingsEnabled) return;
        bool isActive = streamingController.stateManager.RecordingActive;
        await streamingController.ToggleOperation(StreamOperation.Recording, !isActive);
    }
    
    public async void OnStreamingButtonClick() {
        if (!bindingsEnabled) return;
        bool isActive = streamingController.stateManager.StreamingActive;
        await streamingController.ToggleOperation(StreamOperation.Streaming, !isActive);
    }

    public async void OnSplitButtonClick() {
        if (!bindingsEnabled) return;
        await streamingController.SplitRecording();
    }
}