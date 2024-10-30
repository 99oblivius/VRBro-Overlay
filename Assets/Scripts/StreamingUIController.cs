using UnityEngine;
using UnityEngine.UI;

public class StreamingUIController : MonoBehaviour {
    [SerializeField] private StreamingController streamingController;
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
        
        // Initialize UI states
        UpdateIndicatorState(StreamOperation.Buffer, streamingController.stateManager.BufferActive);
        UpdateIndicatorState(StreamOperation.Recording, streamingController.stateManager.RecordingActive);
        UpdateIndicatorState(StreamOperation.Streaming, streamingController.stateManager.StreamingActive);
    }

    private void OnDestroy() {
        streamingController.OnStateChanged -= HandleStateChanged;
    }
    
    private void HandleStateChanged(StreamOperation operation, bool active) {
        UpdateIndicatorState(operation, active);
    }

    private void UpdateIndicatorState(StreamOperation operation, bool active) {
        Image indicator = GetIndicatorForOperation(operation);
        if (indicator == null) return;

        if (streamingController.stateManager.IsOperationPending(operation.ToString())) {
            // Don't update while operation is pending
            return;
        }

        if (operation == StreamOperation.Buffer) {
            indicator.material.SetFloat("_WaveAmplitude", active ? 0.5f : 0f);
        }
        else {
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

    private void UpdatePendingState(StreamOperation operation, bool isPending) {
        Image indicator = GetIndicatorForOperation(operation);
        if (indicator == null) return;

        if (isPending && operation != StreamOperation.Buffer) {
            indicator.color = PendingColor;
        }
        else {
            // Restore proper state after pending
            UpdateIndicatorState(operation, GetOperationState(operation));
        }
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
        UpdatePendingState(StreamOperation.Buffer, true);
        await streamingController.ToggleOperation(StreamOperation.Buffer, true);
        UpdatePendingState(StreamOperation.Buffer, false);
    }

    public async void StopBuffer() {
        if (!bindingsEnabled) return;
        UpdatePendingState(StreamOperation.Buffer, true);
        await streamingController.ToggleOperation(StreamOperation.Buffer, false);
        UpdatePendingState(StreamOperation.Buffer, false);
    }

    public async void StartRecording() {
        if (!bindingsEnabled) return;
        UpdatePendingState(StreamOperation.Recording, true);
        await streamingController.ToggleOperation(StreamOperation.Recording, true);
        UpdatePendingState(StreamOperation.Recording, false);
    }

    public async void StopRecording() {
        if (!bindingsEnabled) return;
        UpdatePendingState(StreamOperation.Recording, true);
        await streamingController.ToggleOperation(StreamOperation.Recording, false);
        UpdatePendingState(StreamOperation.Recording, false);
    }

    public async void StartStreaming() {
        if (!bindingsEnabled) return;
        UpdatePendingState(StreamOperation.Streaming, true);
        await streamingController.ToggleOperation(StreamOperation.Streaming, true);
        UpdatePendingState(StreamOperation.Streaming, false);
    }

    public async void StopStreaming() {
        if (!bindingsEnabled) return;
        UpdatePendingState(StreamOperation.Streaming, true);
        await streamingController.ToggleOperation(StreamOperation.Streaming, false);
        UpdatePendingState(StreamOperation.Streaming, false);
    }

    public async void SaveBuffer() {
        if (!bindingsEnabled || !streamingController.stateManager.BufferActive)
            return;
        await streamingController.SaveBuffer();
    }

    public async void SplitRecording() {
        if (!bindingsEnabled || !streamingController.stateManager.RecordingActive)
            return;
        await streamingController.SplitRecording();
    }


    // Button binding methods
    public async void OnBufferButtonClick() {
        bool isActive = streamingController.stateManager.BufferActive;
        UpdatePendingState(StreamOperation.Buffer, true);
        await streamingController.ToggleOperation(StreamOperation.Buffer, !isActive);
        UpdatePendingState(StreamOperation.Buffer, false);
    }

    public async void OnRecordingButtonClick() {
        bool isActive = streamingController.stateManager.RecordingActive;
        UpdatePendingState(StreamOperation.Recording, true);
        await streamingController.ToggleOperation(StreamOperation.Recording, !isActive);
        UpdatePendingState(StreamOperation.Recording, false);
    }
    
    public async void OnStreamingButtonClick() {
        bool isActive = streamingController.stateManager.StreamingActive;
        UpdatePendingState(StreamOperation.Streaming, true);
        await streamingController.ToggleOperation(StreamOperation.Streaming, !isActive);
        UpdatePendingState(StreamOperation.Streaming, false);
    }

    public async void OnSplitButtonClick() {
        if (!streamingController.stateManager.RecordingActive)
            return;
        await streamingController.SplitRecording();
    }
}