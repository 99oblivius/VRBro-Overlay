using System;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;
using OVRUtil;

public class InputController : MonoBehaviour
{
    public UnityEvent OnSaveBuffer;

    ulong actionSetHandle = 0;
    ulong actionHandle = 0;
    private void Start() {
        OVRUtil.System.Init();

        var error = OpenVR.Input.SetActionManifestPath(Application.streamingAssetsPath + "/SteamVR/actions.json");
        if (error != EVRInputError.None) {
            throw new Exception("Failed to set action manifest path: " + error);
        }

        error = OpenVR.Input.GetActionSetHandle("/actions/VRBro", ref actionSetHandle);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to get action set /actions/VRBro: " + error);
        }

        error = OpenVR.Input.GetActionHandle($"/actions/VRBro/in/savebuffer", ref actionHandle);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to get action /actions/VRBro/in/savebuffer: " + error);
        }
    }

    private void Update() {
        var actionSetList = new VRActiveActionSet_t[] {
            new VRActiveActionSet_t() {
                ulActionSet = actionSetHandle,
                ulRestrictedToDevice = OpenVR.k_ulInvalidInputValueHandle
            }
        };

        var activeActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRActiveActionSet_t));
        var error = OpenVR.Input.UpdateActionState(actionSetList, activeActionSize);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to update action state: " + error);
        }

        var result = new InputDigitalActionData_t();
        var digitalActionSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputDigitalActionData_t));
        error = OpenVR.Input.GetDigitalActionData(actionHandle, ref result, digitalActionSize, OpenVR.k_ulInvalidInputValueHandle);
        if (error != EVRInputError.None) {
            throw new Exception("Failed to get savebuffer action data: " + error);
        }

        if (result.bState && result.bChanged) {
            OnSaveBuffer.Invoke();
        }
    }

    private void Destroy() {
        OVRUtil.System.Shutdown();
    }
}
