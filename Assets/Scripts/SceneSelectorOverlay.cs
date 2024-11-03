using UnityEngine;
using UnityEngine.UI;
using OVRUtil;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Valve.VR;
using System;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

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

public class SceneSelectorOverlay : MonoBehaviour {
    [Header("References")]
    [SerializeField] private VRBro vrBro;
    [SerializeField] private InputController inputController;
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
            }
        } else if (Overlay.DashboardOverlayVisibility(overlayHandle)) {
            Overlay.Hide(overlayHandle);
        }
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
        int n = 1;
        foreach (var button in sceneButtons) {
            if (button != null) Destroy(button.gameObject);
        }
        sceneButtons.Clear();

        if (vrBro._net == null) return;

        var (status, scenesPayload) = await vrBro._net.GetScenes();
        if (status < 0 || string.IsNullOrEmpty(scenesPayload)) return;

        var scenes = scenesPayload.Split('\0');
        foreach (var scene in scenes) {
            if (!string.IsNullOrEmpty(scene)) {
                AddSceneButton(scene);
                n++;
            }
        }

        var (currentStatus, currentSceneName) = await vrBro._net.GetCurrentScene();
        if (currentStatus >= 0) {
            currentScene = currentSceneName;
        }

        var contentTransform = sceneListScroll.content.GetComponent<RectTransform>();
        float fontSize = sceneButtonPrefab.GetComponent<SceneButton>().buttonText.fontSize;
        float buttonHeight = fontSize + 3;
        
        contentTransform.sizeDelta = n * buttonHeight <= 290 ? 
            new Vector2(190, 0) : 
            new Vector2(190, n * buttonHeight - 290);
        
        Debug.Log($"Scenes: {scenes}");
        Debug.Log($"CurrentScene: {currentSceneName}");
    }
    
    private void AddSceneButton(string sceneName) {
        var buttonInstance = Instantiate(sceneButtonPrefab, sceneListScroll.content);
        var buttonText = buttonInstance.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = sceneName;
        buttonText.fontStyle = sceneName == currentScene ? FontStyles.Underline : FontStyles.Normal;
        buttonInstance.onClick.AddListener(() => SelectScene(sceneName));
        sceneButtons.Add(buttonInstance);
    }
    
    private async void SelectScene(string sceneName) {
        if (vrBro._net == null) return;

        Debug.Log($"Switching to scene: {sceneName}");
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