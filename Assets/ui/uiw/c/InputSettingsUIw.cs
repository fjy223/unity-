using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;


public class InputSettingsUIw : MonoBehaviour
{
    [Header("设置ui面板")]
    public GameObject settingPanel;

    [Header("返回按钮")]
    public Button backBtn;


    public MainMenuUIw main;

    [System.Serializable]
    public class KeyBindingConfig
    {
        public string displayName = "功能名称"; // 在 Inspector 中填中文，如 "相机回原位"
        public string fieldName = "ReturnToHome"; // 必须与 InputManager 中的字段名完全一致
    }
    [Header("References")]
    public InputManager inputManager;
    public GameObject keyBindingRowPrefab;
    public Transform contentContainer;

    [Header("Manual Key Binding List")]
    public List<KeyBindingConfig> keyBindings = new List<KeyBindingConfig>();

    private List<RuntimeEntry> runtimeEntries = new List<RuntimeEntry>();
    private string keyBindingsSavePath;

    [System.Serializable]
    private class RuntimeEntry
    {
        public KeyBindingConfig config;
        public FieldInfo field;
        public Text currentKeyText;
        public Button rebindButton;
        public bool isRebinding = false;
    }

    void Start()
    {
        keyBindingsSavePath = Path.Combine(Application.persistentDataPath, "keybindings.json");

        if (inputManager == null || keyBindingRowPrefab == null || contentContainer == null)
        {
            Debug.LogError("[KeyBindingSettingsUI] Missing references in Inspector!");
            return;
        }

        if (keyBindings.Count == 0)
        {
            Debug.LogWarning("[KeyBindingSettingsUI] No key bindings configured. Add entries in Inspector.");
            return;
        }

        BuildUI();
    }

    void BuildUI()
    {
        // Clear old content
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        runtimeEntries.Clear();

        foreach (var config in keyBindings)
        {
            FieldInfo field = inputManager.GetType().GetField(
                config.fieldName,
                BindingFlags.Public | BindingFlags.Instance
            );

            if (field == null || field.FieldType != typeof(KeyCode))
            {
                Debug.LogError($"[KeyBindingSettingsUI] Field '{config.fieldName}' not found or not KeyCode in InputManager!");
                continue;
            }

            GameObject row = Instantiate(keyBindingRowPrefab, contentContainer);
            Text functionLabel = row.transform.GetChild(0).GetComponent<Text>();
            Text currentKeyText = row.transform.GetChild(1).GetComponent<Text>();
            Button rebindButton = row.transform.GetChild(2).GetComponent<Button>();

            functionLabel.text = config.displayName; // 中文显示
            currentKeyText.text = FormatKeyCode((KeyCode)field.GetValue(inputManager));

            var entry = new RuntimeEntry
            {
                config = config,
                field = field,
                currentKeyText = currentKeyText,
                rebindButton = rebindButton
            };
            runtimeEntries.Add(entry);

            int index = runtimeEntries.Count - 1;
            rebindButton.onClick.AddListener(() => StartRebind(index));
        }
    }

    string FormatKeyCode(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Space: return "空格";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "Ctrl";
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "Shift";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt: return "Alt";
            default: return key.ToString();
        }
    }

    void StartRebind(int index)
    {
        RuntimeEntry entry = runtimeEntries[index];
        if (entry.isRebinding) return;

        entry.isRebinding = true;
        entry.currentKeyText.text = "请按任意键...";
        entry.rebindButton.interactable = false;

        StartCoroutine(WaitForNewKey(entry));
    }

    System.Collections.IEnumerator WaitForNewKey(RuntimeEntry entry)
    {
        while (entry.isRebinding)
        {
            yield return null;

            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(k))
                {
                    if (k == KeyCode.Escape)
                    {
                        CancelRebind(entry);
                        yield break;
                    }

                    // 更新 InputManager 中的键位
                    entry.field.SetValue(inputManager, k);
                    // 更新 UI 显示
                    entry.currentKeyText.text = FormatKeyCode(k);
                    entry.isRebinding = false;
                    entry.rebindButton.interactable = true;

                    // 保存所有键位到文件
                    SaveKeyBindings();
                    yield break;
                }
            }
        }
    }

    void CancelRebind(RuntimeEntry entry)
    {
        KeyCode current = (KeyCode)entry.field.GetValue(inputManager);
        entry.currentKeyText.text = FormatKeyCode(current);
        entry.isRebinding = false;
        entry.rebindButton.interactable = true;
    }

    /// <summary>
    /// 将当前 InputManager 的键位保存到 JSON 文件
    /// </summary>
    void SaveKeyBindings()
    {
        if (inputManager == null)
        {
            Debug.LogError("InputManager instance is null, cannot save key bindings.");
            return;
        }

        try
        {
            KeyBindingsSaveData saveData = new KeyBindingsSaveData();
            saveData.keyBindings = new List<KeyBindingEntry>();

            Type inputManagerType = inputManager.GetType();
            FieldInfo[] fields = inputManagerType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(KeyCode))
                {
                    KeyCode keyCode = (KeyCode)field.GetValue(inputManager);
                    saveData.keyBindings.Add(new KeyBindingEntry(field.Name, keyCode));
                }
            }

            string json = JsonUtility.ToJson(saveData, true); // pretty print
            File.WriteAllText(keyBindingsSavePath, json);

            Debug.Log($"Key bindings saved to {keyBindingsSavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save key bindings: {e.Message}");
        }
    }

    // Optional: Call this from a "Refresh" button in Editor
    public void RefreshUI()
    {
        BuildUI();
    }
    private void Awake()
    {
        backBtn.onClick.AddListener(OnBack);
        settingPanel.SetActive(false);
    }

    public void Show()
    {
        settingPanel.SetActive(true);
    }

    private void OnBack()
    {
        settingPanel.SetActive(false);
        main.mainPanel.SetActive(true);
    }
}



