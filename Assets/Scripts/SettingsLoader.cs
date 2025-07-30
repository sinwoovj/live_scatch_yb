using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Threading;

public class SettingsLoader : MonoBehaviour
{
    public static SettingsLoader Instance;

    public Dictionary<string, float> settings = new Dictionary<string, float>();
    private FileSystemWatcher fileWatcher;
    private string settingsFilePath;
    public bool fileChanged = false;
    public List<GameObject> applyObjs;

    void Start()
    {
        Instance = this;
        settingsFilePath = Path.Combine(Application.streamingAssetsPath, "settings.txt");
        LoadSettings();

#if UNITY_EDITOR || UNITY_STANDALONE
        SetupFileWatcher();
#endif
    }

    void Update()
    {
        // ������ �ٲ���ٸ� �ٽ� �ε�
        if (fileChanged)
        {
            fileChanged = false;
            LoadSettings();
            ApplySettings();
            Debug.Log("[SettingsLoader] ���� ���� �����. ���� �ε���.");
        }
    }

    private void ApplySettings()
    {
        foreach (var o in applyObjs)
        {
            if(o.GetComponent<DrawingWithGuide>() != null)
                o.GetComponent<DrawingWithGuide>().ChangeLoadSettingsValues();
        }
    }

    void LoadSettings()
    {
        if (!File.Exists(settingsFilePath))
        {
            Debug.LogWarning("���� ������ ã�� �� �����ϴ�: " + settingsFilePath);
            return;
        }

        var newSettings = new Dictionary<string, float>();
        string[] lines = File.ReadAllLines(settingsFilePath);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("=")) continue;

            string[] pair = line.Split('=');
            if (pair.Length != 2) continue;

            string key = pair[0].Trim();
            if (float.TryParse(pair[1].Trim(), out float value))
            {
                newSettings[key] = value;
            }
        }

        settings = newSettings;
    }

    void SetupFileWatcher()
    {
        string directory = Path.GetDirectoryName(settingsFilePath);
        string fileName = Path.GetFileName(settingsFilePath);

        fileWatcher = new FileSystemWatcher(directory, fileName);
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

        fileWatcher.Changed += OnSettingsFileChanged;
        fileWatcher.EnableRaisingEvents = true;

        Debug.Log("[SettingsLoader] ���� ���� ���۵�: " + settingsFilePath);
    }

    void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
    {
        // ���� ���� �� ���� �̺�Ʈ�� �������� �߻��� �� �־ �����̸� �ִ� �� ������
        Thread.Sleep(100);
        fileChanged = true;
    }

    void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.Dispose();
        }
    }

    public float GetSetting(string key, float defaultValue = 0f)
    {
        return settings.TryGetValue(key, out float value) ? value : defaultValue;
    }
}
