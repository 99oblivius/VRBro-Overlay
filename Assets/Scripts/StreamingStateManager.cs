public class StreamingStateManager {
    private readonly object stateLock = new();
    private readonly System.Collections.Generic.HashSet<string> pendingOperations = new();
    private bool bufferState;
    private bool recordingState;
    private bool streamingState;
    
    public bool BufferActive {
        get { lock (stateLock) return bufferState; }
        private set { lock (stateLock) bufferState = value; }
    }
    
    public bool RecordingActive {
        get { lock (stateLock) return recordingState; }
        private set { lock (stateLock) recordingState = value; }
    }
    
    public bool StreamingActive {
        get { lock (stateLock) return streamingState; }
        private set { lock (stateLock) streamingState = value; }
    }
    
    public bool IsOperationPending(string operation) {
        lock (stateLock) return pendingOperations.Contains(operation);
    }
    
    public bool IsAnyOperationPending() {
        lock (stateLock) return pendingOperations.Count > 0;
    }
    
    public void SetOperationPending(string operation, bool isPending) {
        lock (stateLock) {
            if (isPending) pendingOperations.Add(operation);
            else pendingOperations.Remove(operation);
        }
    }
    
    public void UpdateState(StreamOperation operation, bool active) {
        lock (stateLock) {
            switch (operation) {
                case StreamOperation.Buffer:
                    BufferActive = active;
                    break;
                case StreamOperation.Recording:
                    RecordingActive = active;
                    break;
                case StreamOperation.Streaming:
                    StreamingActive = active;
                    break;
            }
        }
    }
}