using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class VRPointerController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private VRBroOverlay overlay;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private RectTransform rectMenuBg;

    [Header("Pointer Settings")]
    [SerializeField] private ETrackedControllerRole pointerHand = ETrackedControllerRole.RightHand;
    [SerializeField] private float cursorZOffset = -5f;
    [SerializeField] private float dragThreshold = 50f;
    [SerializeField] private float scrollSensitivity = 1f;
    
    [Header("Haptic Feedback")]
    [SerializeField] private float hapticPulseStrength = 0.3f;

    private Camera overlayCamera;
    private GameObject lastHoveredObject;
    private TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private ulong actionSetHandle;
    private ulong interactActionHandle;

    // Drag tracking
    private bool isDragging;
    private bool exceededThreshold;
    private Vector2 dragStartPosition;
    private GameObject dragStartObject;
    private ScrollRect activeScrollRect;
    private GameObject lastHoveredForHaptics;

    private void Start() {
        if (!graphicRaycaster) graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (!eventSystem) eventSystem = EventSystem.current;
        overlayCamera = targetCanvas.worldCamera;
        
        if (cursor) {
            cursor.gameObject.SetActive(false);
            Vector3 cursorPos = cursor.localPosition;
            cursorPos.z = cursorZOffset;
            cursor.localPosition = cursorPos;
        }

        InitializeActions();
    }

    private void InitializeActions() {
        var error = OpenVR.Input.GetActionSetHandle("/actions/VRBro", ref actionSetHandle);
        if (error != EVRInputError.None) {
            Debug.LogError($"Failed to get action set handle: {error}");
            return;
        }

        error = OpenVR.Input.GetActionHandle("/actions/VRBro/in/InteractUI", ref interactActionHandle);
        if (error != EVRInputError.None) {
            Debug.LogError($"Failed to get action handle: {error}");
            return;
        }
    }

    private void Update() {
        if (!overlay.isMenuOpen) {
            if (cursor && cursor.gameObject.activeSelf)
                cursor.gameObject.SetActive(false);
            return;
        }

        UpdateActionSet();
        HandlePointerInteraction();
    }

    private void UpdateActionSet() {
        if (actionSetHandle == 0) return;

        var actionSet = new VRActiveActionSet_t {
            ulActionSet = actionSetHandle,
            ulRestrictedToDevice = OpenVR.k_ulInvalidActionHandle
        };

        OpenVR.Input.UpdateActionState(
            new[] { actionSet }, 
            (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRActiveActionSet_t))
        );
    }

    private (Vector3 position, Quaternion rotation, Vector2 dimensions) GetOverlayTransform() {
        var leftIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        if (leftIndex == OpenVR.k_unTrackedDeviceIndexInvalid) return default;

        OpenVR.System.GetDeviceToAbsoluteTrackingPose(
            ETrackingUniverseOrigin.TrackingUniverseStanding,
            0f,
            poses
        );

        if (!poses[leftIndex].bPoseIsValid) return default;

        var leftTransform = new SteamVR_Utils.RigidTransform(poses[leftIndex].mDeviceToAbsoluteTracking);
        var openState = overlay.openState;
        var overlayRotation = Quaternion.Euler(openState.rotX, openState.rotY, openState.rotZ);
        var overlayPosition = new Vector3(openState.posX, openState.posY, openState.posZ);

        var worldPosition = leftTransform.pos + leftTransform.rot * overlayPosition;
        var worldRotation = leftTransform.rot * overlayRotation;

        var aspectRatio = rectMenuBg.rect.width / rectMenuBg.rect.height;
        var dimensions = new Vector2(openState.size * aspectRatio, openState.size);

        return (worldPosition, worldRotation, dimensions);
    }

    private bool GetTriggerState() {
        if (interactActionHandle == 0) return false;

        var actionData = new InputDigitalActionData_t();
        var error = OpenVR.Input.GetDigitalActionData(
            interactActionHandle,
            ref actionData,
            (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(InputDigitalActionData_t)),
            OpenVR.k_ulInvalidInputValueHandle
        );

        return error == EVRInputError.None && actionData.bState && actionData.bChanged;
    }

    private void HandlePointerInteraction() {
        var (planePosition, planeRotation, planeDimensions) = GetOverlayTransform();
        var planeNormal = planeRotation * Vector3.forward;
        
        var rightIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(pointerHand);
        if (rightIndex == OpenVR.k_unTrackedDeviceIndexInvalid) return;

        if (!poses[rightIndex].bPoseIsValid) return;

        var rightTransform = new SteamVR_Utils.RigidTransform(poses[rightIndex].mDeviceToAbsoluteTracking);
        var rayOrigin = rightTransform.pos;
        var rayDirection = rightTransform.rot * Vector3.forward;

        float denominator = Vector3.Dot(planeNormal, rayDirection);
        if (Mathf.Abs(denominator) < 0.0001f) return;

        float t = Vector3.Dot(planePosition - rayOrigin, planeNormal) / denominator;
        if (t < 0) return;

        Vector3 hitPoint = rayOrigin + rayDirection * t;
        
        var right = planeRotation * Vector3.right;
        var up = planeRotation * Vector3.up;
        
        var localHit = hitPoint - planePosition;
        float u = Vector3.Dot(localHit, right) / planeDimensions.x + 0.5f;
        float v = Vector3.Dot(localHit, up) / planeDimensions.y + 0.5f;

        if (u < 0 || u > 1 || v < 0 || v > 1) {
            if (cursor) cursor.gameObject.SetActive(false);
            return;
        }

        Vector2 screenPos = new(
            u * overlayCamera.pixelWidth,
            v * overlayCamera.pixelHeight
        );

        var pointerData = new PointerEventData(eventSystem) {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);
        bool isOverUI = results.Count > 0;

        if (isOverUI) {
            cursor.gameObject.SetActive(true);
            var hit = results[0];
            var canvasRect = targetCanvas.GetComponent<RectTransform>().rect;
            
            var canvasPos = new Vector2(
                (screenPos.x / overlayCamera.pixelWidth - 0.5f) * canvasRect.width,
                (screenPos.y / overlayCamera.pixelHeight - 0.5f) * canvasRect.height
            );
            
            cursor.localPosition = new Vector3(canvasPos.x, canvasPos.y, cursorZOffset);
            
            if (hit.gameObject != lastHoveredObject && !isDragging) {
                HandleHoverStateChange(hit.gameObject);
            }

            HandleDragAndClick(results[0], screenPos);
        } else {
            cursor.gameObject.SetActive(false);
            if (!isDragging) {
                HandleHoverStateChange(null);
            }
        }
    }

    private void HandleDragAndClick(RaycastResult hit, Vector2 currentPos) {
        var triggerState = GetTriggerState();
        
        // Start drag
        if (triggerState && !isDragging) {
            isDragging = true;
            exceededThreshold = false;
            dragStartPosition = currentPos;
            dragStartObject = hit.gameObject;
            
            var eventModule = hit.module as GraphicRaycaster;
            if (eventModule != null) {
                activeScrollRect = eventModule.GetComponent<Canvas>()?.GetComponentInChildren<ScrollRect>();
            }
        }
        // End drag/click
        else if (!triggerState && isDragging) {
            if (!exceededThreshold && hit.gameObject == dragStartObject) {
                var button = dragStartObject.GetComponent<Button>();
                if (button != null && button.interactable) {
                    button.onClick.Invoke();
                    ProvideTactileFeedback(OpenVR.System.GetTrackedDeviceIndexForControllerRole(pointerHand));
                }
            }
            
            isDragging = false;
            activeScrollRect = null;
            dragStartObject = null;
        }
        // Handle scrolling during drag
        else if (isDragging && activeScrollRect != null) {
            var delta = currentPos - dragStartPosition;
            
            if (!exceededThreshold && delta.magnitude > dragThreshold) {
                exceededThreshold = true;
            }

            if (exceededThreshold) {
                float scrollDelta = delta.y * scrollSensitivity / overlayCamera.pixelHeight;
                var contentPos = activeScrollRect.content.localPosition;
                contentPos.y = Mathf.Lerp(contentPos.y, contentPos.y + scrollDelta * 100f, Time.deltaTime * 10f);
                activeScrollRect.content.localPosition = contentPos;
                
                UpdateScrollbar();
            }
        }
    }

    private void UpdateScrollbar() {
        RectTransform sceneList = overlay.sceneListScroll.GetComponent<RectTransform>();
        RectTransform content = overlay.sceneListScroll.content;
        RectTransform scrollbar = overlay.scrollbar;
        float offsetY = 5f;
        float endY = sceneList.sizeDelta.y - scrollbar.sizeDelta.y - offsetY;

        float normalizedDisplacement = Mathf.Clamp01(content.localPosition.y / (content.sizeDelta.y - sceneList.sizeDelta.y));

        float scrollPosition = Mathf.Lerp(offsetY, endY, normalizedDisplacement);
        scrollbar.anchoredPosition = new Vector3(92f, -scrollPosition, -3f);
    }

    private void HandleHoverStateChange(GameObject newHoverObject) {
        // Exit state handling
        if (lastHoveredObject != null) {
            var button = lastHoveredObject.GetComponent<Button>();
            if (button != null) {
                button.OnPointerExit(null);
                
                // Check if this is the current scene button
                var buttonText = button.GetComponentInChildren<TMP_Text>();
                bool isCurrentScene = buttonText != null && 
                                    buttonText.text == overlay.currentScene;
                
                // Only reset to normal color if it's not the current scene
                button.GetComponent<Image>().color = isCurrentScene ? SceneButton.ActiveColor : SceneButton.NormalColor;
            }
        }

        // Enter state handling
        if (newHoverObject != null) {
            var button = newHoverObject.GetComponent<Button>();
            if (button != null) {
                button.OnPointerEnter(null);
                button.GetComponent<Image>().color = SceneButton.HoverColor;
            }
        }

        // Handle haptic feedback separately
        if (newHoverObject != lastHoveredForHaptics) {
            var button = newHoverObject?.GetComponent<Button>();
            if (button != null) {
                ProvideTactileFeedback(
                    OpenVR.System.GetTrackedDeviceIndexForControllerRole(pointerHand),
                    hapticPulseStrength * 0.5f
                );
            }
            lastHoveredForHaptics = newHoverObject;
        }

        lastHoveredObject = newHoverObject;
    }

    private void ProvideTactileFeedback(uint controllerIndex, float strength = -1) {
        if (controllerIndex == OpenVR.k_unTrackedDeviceIndexInvalid) return;
        
        OpenVR.System.TriggerHapticPulse(
            controllerIndex,
            0,
            (ushort)(strength < 0 ? hapticPulseStrength * 3999 : strength * 3999)
        );
    }
}