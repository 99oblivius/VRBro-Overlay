using UnityEngine;
using UnityEngine.UI;

public class VRBroSettings : MonoBehaviour {
    [SerializeField] private Image imageBindingsEnabled;
    
    private static readonly Color32 ActiveColor = new(116, 132, 117, 255);
    private static readonly Color32 InactiveColor = new(132, 117, 127, 255);

    private void Awake() {
        UpdateBindingsUI(Settings.Instance.BindingsEnabled);
        Settings.Instance.OnSettingsChanged += OnSettingsChanged;
    }

    private void OnDestroy() {
        Settings.Instance.OnSettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged() {
        UpdateBindingsUI(Settings.Instance.BindingsEnabled);
    }

    private void UpdateBindingsUI(bool enabled) {
        imageBindingsEnabled.color = enabled ? ActiveColor : InactiveColor;
    }

    public void OnEnableButtonClick() {
        Settings.Instance.BindingsEnabled = true;
    }

    public void OnDisableButtonClick() {
        Settings.Instance.BindingsEnabled = false;
    }
}