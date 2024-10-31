using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum StreamOperation {
    Buffer,
    Recording,
    Streaming
}

public class StreamingController : MonoBehaviour {
    [SerializeField] private VRBro vrBro;
    
    public StreamingStateManager stateManager = new();
    private float pollInterval = 0.75f;
    private float lastPollTime;
    private readonly SemaphoreSlim operationLock = new(1, 1);
    private bool isPolling;
    private bool isConnected;
    public bool IsConnected => isConnected;
    private int debouncePeriod = 1000;
    
    public event Action<StreamOperation, bool> OnStateChanged;
    public event Action<StreamOperation, bool> OnPendingStateChanged;
    public event Action<bool> OnConnectionStateChanged;

    private async void Start() {
        await PollStates();
    }
    
    private async void Update() {
        if (vrBro._net == null || isPolling) return;
        
        if (Time.time - lastPollTime >= pollInterval) {
            lastPollTime = Time.time;
            await PollStates();
        }
    }
    
    public async Task<bool> ToggleOperation(StreamOperation operation, bool targetState) {
        if (vrBro._net == null) return false;

        if (stateManager.IsOperationPending(operation.ToString()))
            return false;

        stateManager.SetOperationPending(operation.ToString(), true);
        OnPendingStateChanged?.Invoke(operation, true);

        try {
            await operationLock.WaitAsync();
            lastPollTime = Time.time;
            
            var result = operation switch {
                StreamOperation.Buffer => targetState ? 
                    await vrBro._net.StartReplayBuffer() : 
                    await vrBro._net.StopReplayBuffer(),
                StreamOperation.Recording => targetState ? 
                    await vrBro._net.StartRecording() : 
                    await vrBro._net.StopRecording(),
                StreamOperation.Streaming => targetState ? 
                    await vrBro._net.StartStreaming() : 
                    await vrBro._net.StopStreaming(),
                _ => -1
            };

            bool success = targetState ? result == 1 : result != 0;
            if (success) {
                stateManager.UpdateState(operation, targetState);
                OnStateChanged?.Invoke(operation, targetState);
            }
            
            return success;
        } catch (Exception ex) {
            Debug.LogError($"Error toggling operation {operation}: {ex.Message}");
            return false;
        } finally {
            await Task.Delay(debouncePeriod);
            stateManager.SetOperationPending(operation.ToString(), false);
            OnPendingStateChanged?.Invoke(operation, false);
            operationLock.Release();
        }
    }
    
    public async Task<bool> SaveBuffer() {
        if (vrBro._net == null || !stateManager.BufferActive)
            return false;

        if (stateManager.IsOperationPending(StreamOperation.Buffer.ToString()))
            return false;

        stateManager.SetOperationPending(StreamOperation.Buffer.ToString(), true);
        OnPendingStateChanged?.Invoke(StreamOperation.Buffer, true);

        try {
            await operationLock.WaitAsync();
            var result = await vrBro._net.SaveBuffer();
            return result >= 0;
        } catch (Exception ex) {
            Debug.LogError($"Error saving buffer: {ex.Message}");
            return false;
        } finally {
            await Task.Delay(debouncePeriod);
            stateManager.SetOperationPending(StreamOperation.Buffer.ToString(), false);
            OnPendingStateChanged?.Invoke(StreamOperation.Buffer, false);
            operationLock.Release();
        }
    }
    
    public async Task<bool> SplitRecording() {
        if (vrBro._net == null || !stateManager.RecordingActive)
            return false;

        if (stateManager.IsOperationPending(StreamOperation.Recording.ToString()))
            return false;

        stateManager.SetOperationPending(StreamOperation.Recording.ToString(), true);
        OnPendingStateChanged?.Invoke(StreamOperation.Recording, true);

        try {
            await operationLock.WaitAsync();
            var result = await vrBro._net.SplitRecording();
            return result >= 0;
        } catch (Exception ex) {
            Debug.LogError($"Error splitting recording: {ex.Message}");
            return false;
        } finally {
            await Task.Delay(debouncePeriod);
            stateManager.SetOperationPending(StreamOperation.Recording.ToString(), false);
            OnPendingStateChanged?.Invoke(StreamOperation.Recording, false);
            operationLock.Release();
        }
    }

    private async Task PollStates() {
        if (vrBro._net == null || isPolling) return;
        
        try {
            isPolling = true;
            
            // Check connection first
            bool wasConnected = isConnected;
            var bufferResult = await vrBro._net.IsReplayBufferActive();
            isConnected = bufferResult >= 0;

            if (wasConnected != isConnected) {
                OnConnectionStateChanged?.Invoke(isConnected);
            }

            // Only continue polling states if connected
            if (isConnected) {
                var recordingResult = await vrBro._net.IsRecordingActive();
                var streamingResult = await vrBro._net.IsStreamingActive();
                
                if (!stateManager.IsAnyOperationPending()) {
                    UpdateStateIfChanged(StreamOperation.Buffer, bufferResult == 1);
                    UpdateStateIfChanged(StreamOperation.Recording, recordingResult == 1);
                    UpdateStateIfChanged(StreamOperation.Streaming, streamingResult == 1);
                }
            }
        } catch (Exception) {
            if (isConnected) {
                isConnected = false;
                OnConnectionStateChanged?.Invoke(false);
            }
        } finally {
            isPolling = false;
        }
    }
    
    private void UpdateStateIfChanged(StreamOperation operation, bool newState) {
        if (stateManager.IsOperationPending(operation.ToString())) return;
        
        bool currentState = operation switch {
            StreamOperation.Buffer => stateManager.BufferActive,
            StreamOperation.Recording => stateManager.RecordingActive,
            StreamOperation.Streaming => stateManager.StreamingActive,
            _ => throw new ArgumentException("Invalid operation")
        };
        
        if (currentState != newState) {
            stateManager.UpdateState(operation, newState);
            OnStateChanged?.Invoke(operation, newState);
        }
    }
    
    private void OnDestroy() => operationLock.Dispose();
}