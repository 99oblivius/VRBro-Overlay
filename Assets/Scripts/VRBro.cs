using System;
using UnityEngine;
using Valve.VR;

public class VRBro : MonoBehaviour {
    public Network _net = null;
    public SteamVR_Action_Vibration hapticAction;

    private void Start() {
        OVRUtil.System.Init();
        
        
        _net = new Network {
            serverAddr = Settings.Instance.ServerAddress,
            serverPort = Settings.Instance.ServerPort
        };

        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
    }

    private void OnDestroy() {
        OVRUtil.System.Shutdown();
        _net?.Close();
    }
}