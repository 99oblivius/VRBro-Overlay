using UnityEngine;
using UnityEngine.UI;
using OVRUtil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Valve.VR;
using System;
using Button = UnityEngine.UI.Button;
using UnityEngine.EventSystems;

[Serializable]
public class OverlayTransform {
    [Tooltip("Size of the overlay in meters")]
    [Range(0, 0.5f)]
    public float size;

    [Header("Position")]
    [Tooltip("X position relative to controller")]
    [Range(-1f, 1f)]
    public float posX;
    
    [Tooltip("Y position relative to controller")]
    [Range(-1f, 1f)]
    public float posY;
    
    [Tooltip("Z position relative to controller")]
    [Range(-1f, 1f)]
    public float posZ;

    [Header("Rotation")]
    [Tooltip("X rotation in degrees")]
    [Range(0, 360)]
    public int rotX;
    
    [Tooltip("Y rotation in degrees")]
    [Range(0, 360)]
    public int rotY;
    
    [Tooltip("Z rotation in degrees")]
    [Range(0, 360)]
    public int rotZ;

    public Vector3 Position => new(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);

    public OverlayTransform(float size, float x, float y, float z, int rotX, int rotY, int rotZ) {
        this.size = size;
        this.posX = x;
        this.posY = y;
        this.posZ = z;
        this.rotX = rotX;
        this.rotY = rotY;
        this.rotZ = rotZ;
    }
}

public static class OverlayTransformExtensions {
    public static OverlayTransform Lerp(OverlayTransform a, OverlayTransform b, float t) {
        return new OverlayTransform(
            Mathf.Lerp(a.size, b.size, t),
            Mathf.Lerp(a.posX, b.posX, t),
            Mathf.Lerp(a.posY, b.posY, t),
            Mathf.Lerp(a.posZ, b.posZ, t),
            Mathf.RoundToInt(Mathf.Lerp(a.rotX, b.rotX, t)),
            Mathf.RoundToInt(Mathf.Lerp(a.rotY, b.rotY, t)),
            Mathf.RoundToInt(Mathf.Lerp(a.rotZ, b.rotZ, t))
        );
    }
}

public class VRBroOverlay : MonoBehaviour {
    [Header("References")]
    [SerializeField] private VRBro vrBro;
    public RenderTexture renderTexture;
    [SerializeField] private InputController inputController;
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;
    [SerializeField] private Camera overlayCamera;
    [SerializeField] private RenderTexture overlayTexture;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform menuContainer;
    [SerializeField] private ScrollRect sceneListScroll;
    [SerializeField] private Button sceneButtonPrefab;

    [Header("Animation Settings")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Overlay Configuration")]
    private ulong overlayHandle = OpenVR.k_ulOverlayHandleInvalid;
    
    [Space(10)]
    [Header("Overlay States")]
    [Tooltip("Overlay configuration when menu is closed")]
    [SerializeField] 
    private OverlayTransform closedState = new(
        size: 0.016f,
        x: 0.002f,
        y: -0.014f,
        z: -0.179f,
        rotX: 64,
        rotY: 0,
        rotZ: 4
    );

    [Tooltip("Overlay configuration when menu is opened")]
    [SerializeField] 
    private OverlayTransform openState = new(
        size: 0.3f,
        x: 0.1f,
        y: 0.05f,
        z: -0.1f,
        rotX: 30,
        rotY: 0,
        rotZ: 0
    );

    [Space(10)]
    [Header("Menu scaling")]
    [SerializeField] private RectTransform rectMenuBg;
    [SerializeField] private RectTransform rectMenuBorder;
    [SerializeField] private RectTransform rectMenuBottom;

    private string currentScene;
    private string logoPath;
    private bool isMenuOpen;
    private bool isAnimating;
    private List<Button> sceneButtons = new();
    private Coroutine animationCoroutine;

    private void Start() {
        logoPath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
        InitializeOverlay();
        menuContainer.gameObject.SetActive(false);
    }
    
    private void InitializeOverlay() {
        overlayHandle = Overlay.Create("VRBroOverlayKey", "VRBroOverlay");
        
        // Enable input handling
        var error = OpenVR.Overlay.SetOverlayInputMethod(overlayHandle, VROverlayInputMethod.Mouse);
        if (error != EVROverlayError.None) {
            Debug.LogError($"Failed to set overlay input method: {error}");
        }
        
        // Enable interaction flags
        error = OpenVR.Overlay.SetOverlayFlag(overlayHandle, VROverlayFlags.SendVRSmoothScrollEvents, true);
        if (error != EVROverlayError.None) {
            Debug.LogError($"Failed to set overlay flags: {error}");
        }
        
        UpdateOverlayTransform(closedState);
        Overlay.SetFromFile(overlayHandle, logoPath);
        PopulateSceneList();
    }
    
    private void Update() {
        var leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(
            ETrackedControllerRole.LeftHand);
            
        if (leftControllerIndex != OpenVR.k_unTrackedDeviceIndexInvalid) {
            if (!Overlay.DashboardOverlayVisibility(overlayHandle)) {
                Overlay.Show(overlayHandle);
            }

            if (!isAnimating) {
                UpdateOverlayTransform(isMenuOpen ? openState : closedState);
            }
            
            if (isMenuOpen) {
                var bounds = new VRTextureBounds_t {
                    uMin = 0,
                    uMax = 1,
                    vMin = 1,
                    vMax = 0
                };
                OpenVR.Overlay.SetOverlayTextureBounds(overlayHandle, ref bounds);
                Overlay.SetRenderTexture(overlayHandle, overlayTexture);
                ProcessOverlayEvents();
            }
        } else if (Overlay.DashboardOverlayVisibility(overlayHandle)) {
            Overlay.Hide(overlayHandle);
        }
    }

    private void ProcessOverlayEvents() {
        var vrEvent = new VREvent_t();
        var uncbVREvent = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));

        while (OpenVR.Overlay.PollNextOverlayEvent(overlayHandle, ref vrEvent, uncbVREvent)) {
            switch (vrEvent.eventType) {
            case (uint)EVREventType.VREvent_MouseMove:
                foreach (var b in sceneButtons) {
                    b.gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color32(61, 68, 80, 255);
                }
                var button = GetButtonByPosition(new Vector2(vrEvent.data.mouse.x, renderTexture.height - vrEvent.data.mouse.y));
                if (button != null) {
                    button.gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color32(88, 97, 112, 255);
                };
                break;
            
            case (uint)EVREventType.VREvent_MouseButtonUp:
                button = GetButtonByPosition(new Vector2(vrEvent.data.mouse.x, renderTexture.height - vrEvent.data.mouse.y));
                if (button != null) {
                    button.onClick.Invoke();
                    button.gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color32(61, 68, 80, 255);
                };
                break;
            }
        }
    }

    private Button GetButtonByPosition(Vector2 position) {
        var pointerEventData = new PointerEventData(eventSystem) { position = position };

        var raycastResultList = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, raycastResultList);
        var raycastResult = raycastResultList.Find(element => element.gameObject.GetComponent<Button>());

        if (raycastResult.gameObject == null) return null;

        return raycastResult.gameObject.GetComponent<Button>();
    }
    
    private void UpdateOverlayTransform(OverlayTransform state) {
        if (overlayHandle == OpenVR.k_ulOverlayHandleInvalid) return;
        
        var leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(
            ETrackedControllerRole.LeftHand);
            
        if (leftControllerIndex != OpenVR.k_unTrackedDeviceIndexInvalid) {
            Overlay.SetSize(overlayHandle, state.size);
            Overlay.SetTransformRelative(
                overlayHandle,
                leftControllerIndex,
                state.Position,
                state.Rotation
            );
        }
    }
    
    public void ToggleMenu() {
        if (isAnimating) return;
        
        isMenuOpen = !isMenuOpen;
        menuContainer.gameObject.SetActive(isMenuOpen);

        if (animationCoroutine != null) {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateOverlay(isMenuOpen));

        if (isMenuOpen) {
            overlayCamera.targetTexture = overlayTexture;
            UpdateSceneList();
        }
    }
    
    private IEnumerator AnimateOverlay(bool opening) {
        isAnimating = true;
        float time = 0;
        float duration = opening ? openDuration : closeDuration;
        
        var startState = opening ? closedState : openState;
        var endState = opening ? openState : closedState;
        
        while (time < duration) {
            time += Time.deltaTime;
            float t = transitionCurve.Evaluate(time / duration);
            
            var currentState = OverlayTransformExtensions.Lerp(startState, endState, t);
            UpdateOverlayTransform(currentState);
            
            yield return null;
        }
        
        UpdateOverlayTransform(endState);
        
        if (!opening) {
            var bounds = new VRTextureBounds_t {
                uMin = 0,
                uMax = 1,
                vMin = 0,
                vMax = 1
            };
            OpenVR.Overlay.SetOverlayTextureBounds(overlayHandle, ref bounds);
            Overlay.SetFromFile(overlayHandle, logoPath);
        }
        
        isAnimating = false;
        animationCoroutine = null;
    }
    
    private async void PopulateSceneList() {
        int n = 0;
        foreach (var button in sceneButtons) {
            if (button != null) Destroy(button.gameObject);
        }
        sceneButtons.Clear();

        if (vrBro._net == null) return;

        var (status, scenesPayload) = await vrBro._net.GetScenes();
        if (status < 0 || string.IsNullOrEmpty(scenesPayload)) return;

        var (currentStatus, currentSceneName) = await vrBro._net.GetCurrentScene();
        if (currentStatus >= 0) {
            currentScene = currentSceneName;
        }

        var scenes = scenesPayload.Split('\0');
        foreach (var scene in scenes) {
            if (!string.IsNullOrEmpty(scene)) {
                AddSceneButton(scene);
                n++;
            }
        }

        var contentTransform = sceneListScroll.content.GetComponent<RectTransform>();
        float fontSize = sceneButtonPrefab.GetComponent<SceneButton>().buttonText.fontSize;
        float buttonHeight = fontSize + 3;
        
        float UIYScaling = n * buttonHeight <= 290 ? (n+1) * buttonHeight - 10 : 300;

        rectMenuBg.sizeDelta = rectMenuBorder.sizeDelta = new Vector2(200, UIYScaling);
        rectMenuBorder.sizeDelta = new Vector2(200, UIYScaling);
        rectMenuBottom.anchoredPosition = new Vector3(0, -UIYScaling, -4);

        contentTransform.sizeDelta = n * buttonHeight <= 290 ? 
            new Vector2(190, 0) : 
            new Vector2(190, n * buttonHeight - 290);
    }
    
    private void AddSceneButton(string sceneName) {
        var buttonInstance = Instantiate(sceneButtonPrefab, sceneListScroll.content);
        var buttonText = buttonInstance.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = sceneName;
        buttonText.fontStyle = sceneName == currentScene ? FontStyles.Underline : FontStyles.Normal;
        buttonInstance.onClick.AddListener(() => SelectScene(sceneName));
        sceneButtons.Add(buttonInstance);
    }
    
    public async void SelectScene(string sceneName) {
        if (vrBro._net == null) return;
        var result = await vrBro._net.SetScene(sceneName);
        
        if (result >= 0) {
            currentScene = sceneName;
            foreach (var button in sceneButtons) {
                var sceneButton = button.GetComponent<SceneButton>();
                if (sceneButton != null) {
                    foreach (var b in sceneButtons) {
                        if (b != null) {
                            var text = b.gameObject.GetComponentInChildren<TMP_Text>();
                            text.fontStyle = FontStyles.Normal;
                        }
                    }
                    var buttonText = sceneButton.GetComponentInChildren<TMP_Text>();
                    buttonText.fontStyle = FontStyles.Underline;
                }
            }
        }
        
        ToggleMenu();
    }

    private void UpdateSceneList() {
        PopulateSceneList();
    }
    
    private void OnDestroy() {
        if (animationCoroutine != null) {
            StopCoroutine(animationCoroutine);
        }
        
        if (overlayHandle != 0) {
            Overlay.Destroy(overlayHandle);
        }
    }
}