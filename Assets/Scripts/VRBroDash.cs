using UnityEngine;
using Valve.VR;
using OVRUtil;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class VRBroDash : MonoBehaviour
{
    [SerializeField] private VRBro VRBro;
    [SerializeField] private Image imageStartStopBuffer;
    public Camera m_Camera;
    public RenderTexture renderTexture;
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;

    [Space]

    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;

    private ulong dashboardHandle = OpenVR.k_ulOverlayHandleInvalid;
    private ulong thumbnailHandle = OpenVR.k_ulOverlayHandleInvalid;

    private float BufferActiveAmplitude = 0.5f;

    
    private void Start()
    {
        OVRUtil.System.Init();

        (dashboardHandle, thumbnailHandle) = Overlay.CreateDashboard("VRBroDashKey", "VRBro");
        
        var filePath = Application.streamingAssetsPath + "/Textures/VRBro_icon.png";
        Overlay.SetFromFile(thumbnailHandle, filePath);

        Overlay.FlipVertical(dashboardHandle);
        Overlay.SetSize(dashboardHandle, 0.5f);
        Overlay.SetMouseScale(dashboardHandle, renderTexture.width, renderTexture.height);

    }

    private void Update() {
        Overlay.SetRenderTexture(dashboardHandle, renderTexture);
        ProcessOverlayEvents();
    }

    private void OnApplicationQuit() {
        Overlay.Destroy(dashboardHandle);
    }

    private void OnDestroy() {
        OVRUtil.System.Shutdown();
    }

    private void ProcessOverlayEvents() {
        var vrEvent = new VREvent_t();
        var uncbVREvent = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));

        while (OpenVR.Overlay.PollNextOverlayEvent(dashboardHandle, ref vrEvent, uncbVREvent)) {
            switch (vrEvent.eventType) {
            case (uint)EVREventType.VREvent_MouseMove:
                Button[] buttons = {button1, button2, button3, button4};
                foreach (var b in buttons) {
                    b.gameObject.GetComponent<Image>().color = new Color32(61, 68, 80, 255);
                }
                var button = GetButtonByPosition(new Vector2(vrEvent.data.mouse.x, renderTexture.height - vrEvent.data.mouse.y));
                if (button != null) {
                    button.gameObject.GetComponent<Image>().color = new Color32(88, 97, 112, 255);
                };
                break;
            
            case (uint)EVREventType.VREvent_MouseButtonUp:
                button = GetButtonByPosition(new Vector2(vrEvent.data.mouse.x, renderTexture.height - vrEvent.data.mouse.y));
                if (button != null) {
                    button.onClick.Invoke();
                    button.gameObject.GetComponent<Image>().color = new Color32(61, 68, 80, 255);
                };
                break;
            }
        }

        float newAmplitude = VRBro.bufferactive ? 0.5f : 0.0f;
        if (newAmplitude != BufferActiveAmplitude) {
            BufferActiveAmplitude = newAmplitude;
            imageStartStopBuffer.material.SetFloat("_WaveAmplitude", BufferActiveAmplitude);
        }
    }

    private Button GetButtonByPosition(Vector2 position) {
        var pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = position;
        
        var raycastResultList = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, raycastResultList);
        var raycastResult = raycastResultList.Find(element => element.gameObject.GetComponent<Button>());

        if (raycastResult.gameObject == null) return null;

        return raycastResult.gameObject.GetComponent<Button>();
    }
}

