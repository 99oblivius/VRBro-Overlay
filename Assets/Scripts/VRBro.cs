using UnityEngine;
using Valve.VR;

public class VRBro : MonoBehaviour {
    #region Public Fields
    public Network _net;
    public SteamVR_Action_Vibration hapticAction;
    #endregion

    #region Unity Lifecycle
    private void Start() {
        InitializeVR();
        InitializeNetwork();
    }

    private void OnDestroy() {
        Cleanup();
    }
    #endregion

    #region Initialization
    private void InitializeVR() {
        OVRUtil.System.Init();
    }

    private void InitializeNetwork() {
        _net = new Network {
            serverAddr = Settings.Instance.ServerAddress,
            serverPort = Settings.Instance.ServerPort
        };
    }
    #endregion

    #region Cleanup
    private void Cleanup() {
        OVRUtil.System.Shutdown();
        _net?.Close();
    }
    #endregion
}