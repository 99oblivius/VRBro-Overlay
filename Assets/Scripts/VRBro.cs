using System;
using UnityEngine;
using Valve.VR;
using OVRUtil;

public class VRBro : MonoBehaviour {
    public Network _net = null;
    public SteamVR_Action_Vibration hapticAction;
    private ulong overlayHandle = OpenVR.k_ulOverlayHandleInvalid;
    
    [Range(0, 0.5f)] public float size;
    [Range(-0.2f, 0.2f)] public float x;
    [Range(-0.2f, 0.2f)] public float y;
    [Range(-0.2f, 0.2f)] public float z;
    [Range(0, 360)] public int rotationX;
    [Range(0, 360)] public int rotationY;
    [Range(0, 360)] public int rotationZ;

    private void Start() {
        OVRUtil.System.Init();
        overlayHandle = Overlay.Create("VRBroKey", "VRBro");
        
        _net = new Network {
            serverAddr = Settings.Instance.ServerAddress,
            serverPort = Settings.Instance.ServerPort
        };

        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
        Overlay.SetFromFile(overlayHandle, filePath);
        Overlay.Show(overlayHandle);
    }

    private void OnApplicationQuit() {
        Overlay.Destroy(overlayHandle);
    }

    private void OnDestroy() {
        OVRUtil.System.Shutdown();
        _net?.Close();
    }
}