using System;
using UnityEngine;
using System.IO;

[Serializable]
public class NetworkSettings {
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private int serverPort = 33390;

    public string ServerAddress { get => serverAddress; set => serverAddress = value; }
    public int ServerPort { get => serverPort; set => serverPort = value; }
}

[Serializable]
public class UISettings {
    [SerializeField] private bool bindingsEnabled = true;
    public bool BindingsEnabled { get => bindingsEnabled; set => bindingsEnabled = value; }
}

public class Settings {
    private const string NetworkSettingsFile = "Config/network_settings.json";
    private const string UISettingsFile = "Config/ui_settings.json";
    private static string NetworkSettingsPath => Path.Combine(Application.persistentDataPath, NetworkSettingsFile);
    private static string UISettingsPath => Path.Combine(Application.persistentDataPath, UISettingsFile);
    private NetworkSettings networkSettings;
    private UISettings uiSettings;

    public string ServerAddress { get => networkSettings.ServerAddress; set => networkSettings.ServerAddress = value; }
    public int ServerPort { get => networkSettings.ServerPort; set => networkSettings.ServerPort = value; }
    public bool BindingsEnabled { get => uiSettings.BindingsEnabled; set => uiSettings.BindingsEnabled = value; }

    public static Settings Load() {
        var settings = new Settings {
            networkSettings = new NetworkSettings(),
            uiSettings = new UISettings()
        };

        try {
            if (File.Exists(NetworkSettingsPath)) {
                string json = File.ReadAllText(NetworkSettingsPath);
                var loadedNetworkSettings = JsonUtility.FromJson<NetworkSettings>(json);
                if (loadedNetworkSettings != null) settings.networkSettings = loadedNetworkSettings;
            }

            if (File.Exists(UISettingsPath)) {
                string json = File.ReadAllText(UISettingsPath);
                var loadedUISettings = JsonUtility.FromJson<UISettings>(json);
                if (loadedUISettings != null) settings.uiSettings = loadedUISettings;
            }
        } catch (Exception ex) {
            Debug.LogError($"Failed to load settings: {ex.Message}");
        }

        return settings;
    }

    public void Save() {
        try {
            string configDir = Path.GetDirectoryName(NetworkSettingsPath);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            string networkJson = JsonUtility.ToJson(networkSettings, true);
            File.WriteAllText(NetworkSettingsPath, networkJson);

            string uiJson = JsonUtility.ToJson(uiSettings, true);
            File.WriteAllText(UISettingsPath, uiJson);
        } catch (Exception ex) {
            Debug.LogError($"Failed to save settings: {ex.Message}");
        }
    }
}