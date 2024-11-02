using System;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

public class InputController : MonoBehaviour {
    public UnityEvent OnSaveBuffer;
    public UnityEvent OnStartBuffer;
    public UnityEvent OnStopBuffer;
    public UnityEvent OnStartRecording;
    public UnityEvent OnStopRecording;
    public UnityEvent OnStartStreaming;
    public UnityEvent OnStopStreaming;
    public UnityEvent OnSplitRecording;
    public UnityEvent OnToggleSceneMenu;

    private bool bindingsEnabled = true;
    private ulong actionSetHandle = 0;
    private ulong toggleSceneMenuHandle = 0;
    private ulong saveBufferHandle = 0;
    private ulong startBufferHandle = 0;
    private ulong stopBufferHandle = 0;
    private ulong startRecordingHandle = 0;
    private ulong stopRecordingHandle = 0;
    private ulong startStreamingHandle = 0;
    private ulong stopStreamingHandle = 0;
    private ulong splitRecordingHandle = 0;
    
    private void Start() {
        OVRUtil.System.Init();
        InitializeActions();
        bindingsEnabled = Settings.Instance.BindingsEnabled;
        Settings.Instance.OnSettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged() {
        bindingsEnabled = Settings.Instance.BindingsEnabled;
    }

    private void InitializeActions() {
        var error = OpenVR.Input.SetActionManifestPath(Application.streamingAssetsPath + "/SteamVR/actions.json");
        if (error != EVRInputError.None) {
            throw new Exception("Failed to set action manifest path: " + error);
        }

        error = OpenVR.Input.GetActionSetHandle("/actions/VRBro", ref actionSetHandle);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to get action set /actions/VRBro: " + error);
        }

        GetActionHandle("/actions/VRBro/in/savebuffer", ref saveBufferHandle);
        GetActionHandle("/actions/VRBro/in/startbuffer", ref startBufferHandle);
        GetActionHandle("/actions/VRBro/in/stopbuffer", ref stopBufferHandle);
        GetActionHandle("/actions/VRBro/in/startrecording", ref startRecordingHandle);
        GetActionHandle("/actions/VRBro/in/stoprecording", ref stopRecordingHandle);
        GetActionHandle("/actions/VRBro/in/startstreaming", ref startStreamingHandle);
        GetActionHandle("/actions/VRBro/in/stopstreaming", ref stopStreamingHandle);
        GetActionHandle("/actions/VRBro/in/splitrecording", ref splitRecordingHandle);
        GetActionHandle("/actions/VRBro/in/togglesceneselect", ref toggleSceneMenuHandle);
    }

    private void GetActionHandle(string actionPath, ref ulong handle) {
        var error = OpenVR.Input.GetActionHandle(actionPath, ref handle);
        if (error != EVRInputError.None) {
            Debug.LogWarning($"Failed to get action {actionPath}: {error}");
            return;
        }
    }

    private void Update() {
        if (!bindingsEnabled) return;
        
        UpdateActionState();
        CheckActions();
    }

    private void UpdateActionState() {
        var actionSetList = new VRActiveActionSet_t[] {
            new() {
                ulActionSet = actionSetHandle,
                ulRestrictedToDevice = OpenVR.k_ulInvalidInputValueHandle
            }
        };

        var digitalActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRActiveActionSet_t));
        var error = OpenVR.Input.UpdateActionState(actionSetList, digitalActionSize);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to update action state: " + error);
        }
    }

    private void CheckActions() {
        var digitalActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputDigitalActionData_t));
        
        CheckDigitalAction(saveBufferHandle, OnSaveBuffer, digitalActionSize);
        CheckDigitalAction(startBufferHandle, OnStartBuffer, digitalActionSize);
        CheckDigitalAction(stopBufferHandle, OnStopBuffer, digitalActionSize);
        CheckDigitalAction(startRecordingHandle, OnStartRecording, digitalActionSize);
        CheckDigitalAction(stopRecordingHandle, OnStopRecording, digitalActionSize);
        CheckDigitalAction(startStreamingHandle, OnStartStreaming, digitalActionSize);
        CheckDigitalAction(stopStreamingHandle, OnStopStreaming, digitalActionSize);
        CheckDigitalAction(splitRecordingHandle, OnSplitRecording, digitalActionSize);
        CheckDigitalAction(toggleSceneMenuHandle, OnToggleSceneMenu, digitalActionSize);
    }

    private void CheckDigitalAction(ulong actionHandle, UnityEvent action, uint actionSize) {
        if (actionHandle == 0 || action == null) return;

        var actionData = new InputDigitalActionData_t();
        var error = OpenVR.Input.GetDigitalActionData(
            actionHandle, 
            ref actionData, 
            actionSize, 
            OpenVR.k_ulInvalidInputValueHandle
        );

        if (error != EVRInputError.None) {
            Debug.LogError($"Failed to get action data: {error}");
            return;
        }

        if (actionData.bState && actionData.bChanged) {
            action.Invoke();
        }
    }

    private void OnDestroy() {
        Settings.Instance.OnSettingsChanged -= OnSettingsChanged;
        OVRUtil.System.Shutdown();
    }
}