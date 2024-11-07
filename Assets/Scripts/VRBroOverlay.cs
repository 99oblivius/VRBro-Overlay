using UnityEngine;
using UnityEngine.UI;
using System;
using Valve.VR;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[Serializable]
public class OverlayTransform {
    #region Properties
    [Range(0, 0.5f)] 
    public float size;
    
    [Header("Position")]
    [Range(-1f, 1f)] public float posX;
    [Range(-1f, 1f)] public float posY;
    [Range(-1f, 1f)] public float posZ;

    [Header("Rotation")]
    [Range(0, 360)] public int rotX;
    [Range(0, 360)] public int rotY;
    [Range(0, 360)] public int rotZ;

    public Vector3 Position => new(posX, posY, posZ);
    public Quaternion Rotation => Quaternion.Euler(rotX, rotY, rotZ);
    #endregion

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
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private VRBro vrBro;
    [SerializeField] private Camera overlayCamera;
    [SerializeField] private RectTransform menuContainer;
    [SerializeField] public ScrollRect sceneListScroll;
    [SerializeField] public RectTransform scrollbar;
    [SerializeField] private Button sceneButtonPrefab;
    [SerializeField] private RectTransform cursor;
    
    [Header("Components")]
    public RenderTexture renderTexture;
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;
    public RenderTexture overlayTexture;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("UI Scaling")]
    [SerializeField] private RectTransform rectMenuBg;
    [SerializeField] private RectTransform rectMenuBorder;
    [SerializeField] private RectTransform rectMenuBottom;

    [Header("Overlay States")]
    [SerializeField] private OverlayTransform closedState = new(
        size: 0.016f, x: 0.002f, y: -0.014f, z: -0.179f,
        rotX: 64, rotY: 0, rotZ: 4
    );

    [SerializeField] public OverlayTransform openState = new(
        size: 0.3f, x: 0.1f, y: 0.05f, z: -0.1f,
        rotX: 30, rotY: 0, rotZ: 0
    );
    #endregion

    #region Private Fields
    private ulong overlayHandle = OpenVR.k_ulOverlayHandleInvalid;
    private string logoPath;
    private List<Button> sceneButtons = new();
    private Coroutine animationCoroutine;
    private bool isAnimating;
    #endregion

    #region Public Properties
    public string currentScene { get; private set; }
    public bool isMenuOpen { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start() {
        InitializeOverlay();
        SetupInitialState();
    }

    private void Update() {
        UpdateOverlayState();
    }

    private void OnDestroy() {
        if (animationCoroutine != null) {
            StopCoroutine(animationCoroutine);
        }
        
        if (overlayHandle != 0) {
            OVRUtil.Overlay.Destroy(overlayHandle);
        }
    }
    #endregion

    #region Initialization
    private void InitializeOverlay() {
        logoPath = Application.streamingAssetsPath + "/Textures/VRBro_logo.png";
        overlayHandle = OVRUtil.Overlay.Create("VRBroOverlayKey", "VRBroOverlay");
        
        var error = OpenVR.Overlay.SetOverlayInputMethod(overlayHandle, VROverlayInputMethod.Mouse);
        if (error != EVROverlayError.None) {
            Debug.LogError($"Failed to set overlay input method: {error}");
        }
        
        UpdateOverlayTransform(closedState);
        OVRUtil.Overlay.SetFromFile(overlayHandle, logoPath);
    }

    private void SetupInitialState() {
        menuContainer.gameObject.SetActive(false);
        if (cursor != null) {
            cursor.gameObject.SetActive(false);
            Vector3 cursorPos = cursor.position;
            cursor.position = new Vector3(cursorPos.x, cursorPos.y, -6f);
        }
        PopulateSceneList();
    }
    #endregion

    #region Overlay Management
    private void UpdateOverlayState() {
        var leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            
        if (leftControllerIndex != OpenVR.k_unTrackedDeviceIndexInvalid) {
            HandleVisibleOverlay(leftControllerIndex);
        } 
        else if (OVRUtil.Overlay.DashboardOverlayVisibility(overlayHandle)) {
            OVRUtil.Overlay.Hide(overlayHandle);
        }
    }

    private void HandleVisibleOverlay(uint leftControllerIndex) {
        if (!OVRUtil.Overlay.DashboardOverlayVisibility(overlayHandle)) {
            OVRUtil.Overlay.Show(overlayHandle);
        }

        if (!isAnimating) {
            UpdateOverlayTransform(isMenuOpen ? openState : closedState);
        }
        
        if (isMenuOpen) {
            UpdateOverlayTexture();
        }
    }

    private void UpdateOverlayTexture() {
        var bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 1, vMax = 0 };
        OpenVR.Overlay.SetOverlayTextureBounds(overlayHandle, ref bounds);
        OVRUtil.Overlay.SetRenderTexture(overlayHandle, overlayTexture);
    }

    private void UpdateOverlayTransform(OverlayTransform state) {
        if (overlayHandle == OpenVR.k_ulOverlayHandleInvalid) return;
        
        var leftControllerIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            
        if (leftControllerIndex != OpenVR.k_unTrackedDeviceIndexInvalid) {
            OVRUtil.Overlay.SetSize(overlayHandle, state.size);
            OVRUtil.Overlay.SetTransformRelative(overlayHandle, leftControllerIndex, state.Position, state.Rotation);
        }
    }
    #endregion

    #region Menu Management
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
            UpdateOverlayTransform(OverlayTransformExtensions.Lerp(startState, endState, t));
            yield return null;
        }
        
        UpdateOverlayTransform(endState);
        
        if (!opening) {
            var bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 0, vMax = 1 };
            OpenVR.Overlay.SetOverlayTextureBounds(overlayHandle, ref bounds);
            OVRUtil.Overlay.SetFromFile(overlayHandle, logoPath);
        }
        
        isAnimating = false;
        animationCoroutine = null;
    }
    #endregion

    #region Scene Management
    private async void PopulateSceneList() {
        ClearSceneButtons();

        if (vrBro._net == null) return;

        var (status, scenesPayload) = await vrBro._net.GetScenes();
        if (status < 0 || string.IsNullOrEmpty(scenesPayload)) return;

        var (currentStatus, currentSceneName) = await vrBro._net.GetCurrentScene();
        if (currentStatus >= 0) {
            currentScene = currentSceneName;
        }

        CreateSceneButtons(scenesPayload.Split('\0'));
        UpdateMenuScaling();
        UpdateScrollbarScaling();
    }

    private void ClearSceneButtons() {
        foreach (var button in sceneButtons) {
            if (button != null) {
                Destroy(button.gameObject);
            }
        }
        sceneButtons.Clear();
    }

    private void CreateSceneButtons(string[] scenes) {
        foreach (var scene in scenes) {
            if (!string.IsNullOrEmpty(scene)) {
                AddSceneButton(scene);
            }
        }
    }

    private void AddSceneButton(string sceneName) {
        var buttonInstance = Instantiate(sceneButtonPrefab, sceneListScroll.content);
        var buttonText = buttonInstance.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) {
            buttonText.text = sceneName;
            buttonText.fontStyle = sceneName == currentScene ? FontStyles.Bold : FontStyles.Normal;
            buttonInstance.GetComponent<Image>().color = 
                sceneName == currentScene ? SceneButton.ActiveColor : SceneButton.NormalColor;
        }
        buttonInstance.onClick.AddListener(() => SelectScene(sceneName));
        sceneButtons.Add(buttonInstance);
    }
    
    public async void SelectScene(string sceneName) {
        if (vrBro._net == null) return;
        
        var result = await vrBro._net.SetScene(sceneName);
        if (result >= 0) {
            UpdateSelectedSceneUI(sceneName);
        }
        
        ToggleMenu();
    }

    private void UpdateSelectedSceneUI(string sceneName) {
        currentScene = sceneName;
        foreach (var button in sceneButtons) {
            if (button != null) {
                var text = button.gameObject.GetComponentInChildren<TMP_Text>();
                text.fontStyle = FontStyles.Normal;
            }
        }

        var selectedButton = sceneButtons.Find(b => 
            b.GetComponentInChildren<TMP_Text>().text == sceneName);
        if (selectedButton != null) {
            selectedButton.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Underline;
        }
    }

    private void UpdateSceneList() => PopulateSceneList();
    #endregion

    #region UI Scaling
    private void UpdateMenuScaling() {
        var contentTransform = sceneListScroll.content.GetComponent<RectTransform>();
        float buttonHeight = sceneButtonPrefab.gameObject.GetComponent<RectTransform>().rect.height;
        float buttonSpacing = sceneListScroll.content.GetComponent<VerticalLayoutGroup>().spacing;
        float scenePad = sceneListScroll.GetComponent<RectTransform>().position.y;
        float buttonPad = sceneListScroll.content.GetComponent<VerticalLayoutGroup>().padding.top;

        float YScaling = sceneButtons.Count * (buttonHeight + buttonSpacing) + buttonPad + scenePad;
        float UIYScaling = Mathf.Min(300f, YScaling);

        rectMenuBg.sizeDelta = rectMenuBorder.sizeDelta = new Vector2(200, UIYScaling);
        rectMenuBottom.anchoredPosition = new Vector2(0, -UIYScaling);
        contentTransform.sizeDelta = new Vector2(190, YScaling);
    }

    private void UpdateScrollbarScaling() {
        var sceneListHeight = sceneListScroll.GetComponent<RectTransform>().sizeDelta.y;
        var contentHeight = sceneListScroll.content.sizeDelta.y;
        
        if (contentHeight > sceneListHeight) {
            scrollbar.sizeDelta = new Vector2(3.5f, 290f * sceneListHeight / contentHeight);
            scrollbar.gameObject.SetActive(true);
            sceneListScroll.GetComponent<RectTransform>().localPosition = new Vector3(-3f, -3f, -2f);
        } else {
            scrollbar.gameObject.SetActive(false);
            sceneListScroll.GetComponent<RectTransform>().localPosition = new Vector3(0f, -3f, -2f);
        }
    }
    #endregion
}