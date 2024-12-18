using System.Collections.Generic;

public class StateManager {
    #region Fields
    private readonly HashSet<string> pendingOperations = new();
    #endregion

    #region Properties
    public bool BufferActive { get; private set; }
    public bool RecordingActive { get; private set; }
    public bool StreamingActive { get; private set; }
    #endregion

    #region State Operations
    public bool IsOperationPending(string operation) => pendingOperations.Contains(operation);
    
    public bool IsAnyOperationPending() => pendingOperations.Count > 0;
    
    public void SetOperationPending(string operation, bool isPending) {
        if (isPending) {
            pendingOperations.Add(operation);
        } else {
            pendingOperations.Remove(operation);
        }
    }
    
    public void UpdateState(StreamOperation operation, bool active) {
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
    #endregion
}