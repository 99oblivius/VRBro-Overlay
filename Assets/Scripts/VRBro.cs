using System;
using UnityEngine;
using Valve.VR;
using OVRUtil;
using System.Threading.Tasks;
using UnityEngine.UIElements;
public class VRBro : MonoBehaviour
{
    public Network _net = null;
    public bool active = true;
    public bool startbuffer = false;
    public bool stopbuffer = false;
    public bool bufferactive = false;
    public SteamVR_Action_Vibration hapticAction;
    private ulong overlayHandle = OpenVR.k_ulOverlayHandleInvalid;
    private Settings settings;


    [Range(0, 0.5f)] public float size;

    [Range(-0.2f, 0.2f)] public float x;
    [Range(-0.2f, 0.2f)] public float y;
    [Range(-0.2f, 0.2f)] public float z;

    [Range(0, 360)] public int rotationX;
    [Range(0, 360)] public int rotationY;
    [Range(0, 360)] public int rotationZ;

    private async void Start() {
        settings = Settings.Load();

        OVRUtil.System.Init();
        overlayHandle = Overlay.Create("VRBroKey", "VRBro");

        _net = new Network {
            serverAddr = settings.ServerAddress,
            serverPort = settings.ServerPort
        };

        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
        Overlay.SetFromFile(overlayHandle, filePath);
        Overlay.Show(overlayHandle);

        int rsp = await _net.IsReplayBufferActive();
        bufferactive = rsp == 1 || (rsp != 0 && bufferactive);
    }

    private async void Update() {
        if (startbuffer) {
            startbuffer = false;
            int rsp = await _net.StartReplayBuffer();
            bufferactive = rsp == 1 || (rsp != 1 && bufferactive);
        }
        if (stopbuffer) {
            stopbuffer = false;
            int rsp = await _net.StopReplayBuffer();
            bufferactive = rsp != 1 && bufferactive;
        }
        Overlay.SetSize(overlayHandle, 0);
        if (active == false) return;
        var leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        if (leftControllerIndex != OpenVR.k_unTrackedDeviceIndexInvalid) {
            var position = new Vector3(x, y, z);
            var rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
            Overlay.SetSize(overlayHandle, 0.016f);
            Overlay.SetTransformRelative(overlayHandle, leftControllerIndex, position, rotation);
        }
    }

    private void OnApplicationQuit() {
        Overlay.Destroy(overlayHandle);
    }

    private void OnDestroy() {
        OVRUtil.System.Shutdown();
        _net?.Close();
    }

    public async void OnSaveBuffer() {
        if (!active) {
            return;
        }
        int rsp = await _net.SaveReplayBuffer();
        // if (rsp == 1) {
        //     hapticAction.Execute(0, 1, 150, 50, SteamVR_Input_Sources.LeftHand);
        // } else if (rsp == 0) {
        //     hapticAction.Execute(0, 0.25f, 150, 75, SteamVR_Input_Sources.LeftHand);
        //     hapticAction.Execute(0.5f, 0.25f, 150, 75, SteamVR_Input_Sources.LeftHand);
        // } else {
        //     hapticAction.Execute(0, 3, 50, 80, SteamVR_Input_Sources.LeftHand);
        // }
    }

    public async Task<int> IsReplayBuffer() {
        return await _net.IsReplayBufferActive();
    }
}