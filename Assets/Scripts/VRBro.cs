using System;
using UnityEngine;
using Valve.VR;
using OVRUtil;
using System.Threading.Tasks;

public class VRBro : MonoBehaviour
{
    public bool bindingsEnabled = true;
    public bool bufferActive = false;
    public bool recordingActive = false;
    public bool streamingActive = false;

    public bool startBuffer = false;
    public bool stopBuffer = false;

    public bool startRecording = false;
    public bool stopRecording = false;
    public bool splitRecording = false;

    public bool startStreaming = false;
    public bool stopStreaming = false;

    public Network _net = null;

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
        bindingsEnabled = Settings.Instance.BindingsEnabled;

        OVRUtil.System.Init();
        overlayHandle = Overlay.Create("VRBroKey", "VRBro");

        _net = new Network {
            serverAddr = Settings.Instance.ServerAddress,
            serverPort = Settings.Instance.ServerPort
        };

        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
        Overlay.SetFromFile(overlayHandle, filePath);
        Overlay.Show(overlayHandle);

        int rsp = await _net.IsReplayBufferActive();
        bufferActive = rsp == 1 || (rsp != 0 && bufferActive);
        
        rsp = await _net.IsRecordingActive();
        recordingActive = rsp == 1;
        
        rsp = await _net.IsStreamingActive();
        streamingActive = rsp == 1;
    }

    private async void Update() {
        if (startBuffer) {
            startBuffer = false;
            int rsp = await _net.StartReplayBuffer();
            bufferActive = rsp == 1 || (rsp != 1 && bufferActive);
        }
        if (stopBuffer) {
            stopBuffer = false;
            int rsp = await _net.StopReplayBuffer();
            bufferActive = rsp != 1 && bufferActive;
        }

        if (startRecording) {
            startRecording = false;
            int rsp = await _net.StartRecording();
            recordingActive = rsp == 1 || (rsp != 1 && recordingActive);
        }
        if (stopRecording) {
            stopRecording = false;
            int rsp = await _net.StopRecording();
            recordingActive = rsp != 1 && recordingActive;
        }
        if (splitRecording) {
            splitRecording = false;
            await _net.SplitRecording();
        }

        if (startStreaming) {
            startStreaming = false;
            int rsp = await _net.StartStreaming();
            streamingActive = rsp == 1 || (rsp != 1 && streamingActive);
        }
        if (stopStreaming) {
            stopStreaming = false;
            int rsp = await _net.StopStreaming();
            streamingActive = rsp != 1 && streamingActive;
        }

        Overlay.SetSize(overlayHandle, 0);
        if (bindingsEnabled == false) return;
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
        if (!bufferActive) {
            return;
        }
        int rsp = await _net.SaveReplayBuffer();
    }

    public async Task<int> IsReplayBuffer() {
        return await _net.IsReplayBufferActive();
    }

    public async Task<int> IsRecording() {
        return await _net.IsRecordingActive();
    }

    public async Task<int> IsStreaming() {
        return await _net.IsStreamingActive();
    }
}