using UnityEngine;
using UnityEngine.UI;

public class VRBroSettings : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] private Image imageBindingsEnabled;
    #endregion

    #region Constants
    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255);
    #endregion

    #region Unity Lifecycle
    private void Awake() {
        InitializeState();
        SubscribeToEvents();
    }

    private void OnDestroy() {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    private void InitializeState() {
        UpdateBindingsUI(Settings.Instance.BindingsEnabled);
    }

    private void SubscribeToEvents() {
        Settings.Instance.OnSettingsChanged += OnSettingsChanged;
    }

    private void UnsubscribeFromEvents() {
        Settings.Instance.OnSettingsChanged -= OnSettingsChanged;
    }
    #endregion

    #region Event Handlers
    private void OnSettingsChanged() {
        UpdateBindingsUI(Settings.Instance.BindingsEnabled);
    }
    #endregion

    #region UI Updates
    private void UpdateBindingsUI(bool enabled) {
        imageBindingsEnabled.color = enabled ? ActiveColor : InactiveColor;
    }
    #endregion

    #region Public Methods
    public void OnEnableButtonClick() {
        Settings.Instance.BindingsEnabled = true;
    }

    public void OnDisableButtonClick() {
        Settings.Instance.BindingsEnabled = false;
    }
    #endregion
}