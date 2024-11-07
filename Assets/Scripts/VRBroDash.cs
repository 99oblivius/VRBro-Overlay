using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Valve.VR;

public class VRBroDash : MonoBehaviour {
    #region Serialized Fields
    [Header("Rendering")]
    public RenderTexture renderTexture;
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;

    [Header("UI Buttons")]
    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;
    public Button button5;
    public Button button6;
    #endregion

    #region Private Fields
    private ulong dashboardHandle = OpenVR.k_ulOverlayHandleInvalid;
    private ulong thumbnailHandle = OpenVR.k_ulOverlayHandleInvalid;
    #endregion

    #region Unity Lifecycle
    private void Start() {
        InitializeVR();
        InitializeDashboard();
    }

    private void Update() {
        UpdateOverlay();
        ProcessOverlayEvents();
    }

    private void OnApplicationQuit() {
        OVRUtil.Overlay.Destroy(dashboardHandle);
    }

    private void OnDestroy() {
        OVRUtil.System.Shutdown();
    }
    #endregion

    #region Initialization
    private void InitializeVR() {
        OVRUtil.System.Init();
    }

    private void InitializeDashboard() {
        (dashboardHandle, thumbnailHandle) = OVRUtil.Overlay.CreateDashboard("VRBroDashKey", "VRBro");
        
        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_icon.png";
        OVRUtil.Overlay.SetFromFile(thumbnailHandle, filePath);

        OVRUtil.Overlay.FlipVertical(dashboardHandle);
        OVRUtil.Overlay.SetSize(dashboardHandle, 1.25f);
        OVRUtil.Overlay.SetMouseScale(dashboardHandle, renderTexture.width, renderTexture.height);
    }
    #endregion

    #region Overlay Management
    private void UpdateOverlay() {
        OVRUtil.Overlay.SetRenderTexture(dashboardHandle, renderTexture);
    }

    private void ProcessOverlayEvents() {
        var vrEvent = new VREvent_t();
        var uncbVREvent = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));

        while (OpenVR.Overlay.PollNextOverlayEvent(dashboardHandle, ref vrEvent, uncbVREvent)) {
            switch (vrEvent.eventType) {
                case (uint)EVREventType.VREvent_MouseMove:
                    HandleMouseMove(vrEvent);
                    break;
                
                case (uint)EVREventType.VREvent_MouseButtonUp:
                    HandleMouseUp(vrEvent);
                    break;
            }
        }
    }
    #endregion

    #region Event Handlers
    private void HandleMouseMove(VREvent_t vrEvent) {
        ResetButtonColors();
        
        var button = GetButtonByPosition(new Vector2(
            vrEvent.data.mouse.x, 
            renderTexture.height - vrEvent.data.mouse.y
        ));
        
        if (button != null) {
            button.gameObject.GetComponent<Image>().color = new Color32(88, 97, 112, 255);
        }
    }

    private void HandleMouseUp(VREvent_t vrEvent) {
        var button = GetButtonByPosition(new Vector2(
            vrEvent.data.mouse.x, 
            renderTexture.height - vrEvent.data.mouse.y
        ));
        
        if (button != null) {
            button.onClick.Invoke();
            button.gameObject.GetComponent<Image>().color = new Color32(61, 68, 80, 255);
        }
    }
    #endregion

    #region UI Helpers
    private void ResetButtonColors() {
        Button[] buttons = { button1, button2, button3, button4, button5, button6 };
        foreach (var button in buttons) {
            button.gameObject.GetComponent<Image>().color = new Color32(61, 68, 80, 255);
        }
    }

    private Button GetButtonByPosition(Vector2 position) {
        var pointerEventData = new PointerEventData(eventSystem) { position = position };
        var raycastResultList = new List<RaycastResult>();
        
        graphicRaycaster.Raycast(pointerEventData, raycastResultList);
        var raycastResult = raycastResultList.Find(element => element.gameObject.GetComponent<Button>());

        return raycastResult.gameObject?.GetComponent<Button>();
    }
    #endregion
}