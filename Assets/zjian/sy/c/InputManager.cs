using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// 快捷键管理中心。所有快捷键在此统一定义、修改和触发。
/// 其他系统通过订阅事件响应快捷键。
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }


    // ===== 快捷键定义（可在 Inspector 中修改）=====
    [Header("相机控制")]
    public KeyCode ReturnToHome = KeyCode.Space;      // 回原位
    //镜头移动
    public KeyCode cleft = KeyCode.A;
    public KeyCode cright = KeyCode.D;
    public KeyCode cup = KeyCode.W;
    public KeyCode cdown = KeyCode.S;

    [Header("模式")]
    public KeyCode ToggleDeleteMode = KeyCode.U; // 切换删除模式
    public KeyCode CurrentDisplay = KeyCode.I; // 切换电流是否显示

    [Header("组件旋转")]
    public KeyCode zjleft = KeyCode.Q;
    public KeyCode zjright = KeyCode.E;

    private string keyBindingsSavePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // 确定保存路径
            keyBindingsSavePath = Path.Combine(Application.persistentDataPath, "keybindings.json");
            Debug.Log($"Key bindings save path: {keyBindingsSavePath}");

            // 尝试加载保存的键位
            LoadKeyBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 原有的输入检测逻辑可以放在这里
    }

    /// <summary>
    /// 从 JSON 文件加载键位设置
    /// </summary>
    public void LoadKeyBindings()
    {
        if (!File.Exists(keyBindingsSavePath))
        {
            Debug.Log("Key bindings save file not found. Using default keys.");
            return;
        }

        try
        {
            string json = File.ReadAllText(keyBindingsSavePath);
            KeyBindingsSaveData saveData = JsonUtility.FromJson<KeyBindingsSaveData>(json);

            if (saveData == null || saveData.keyBindings == null)
            {
                Debug.LogWarning("Key bindings save file is invalid or empty.");
                return;
            }

            Type inputManagerType = typeof(InputManager);
            FieldInfo[] fields = inputManagerType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var savedBinding in saveData.keyBindings)
            {
                // 查找对应的字段
                FieldInfo field = Array.Find(fields, f => f.Name == savedBinding.fieldName);

                if (field != null && field.FieldType == typeof(KeyCode))
                {
                    // 尝试解析 KeyCode
                    if (Enum.TryParse<KeyCode>(savedBinding.keyCode, out KeyCode parsedKeyCode))
                    {
                        field.SetValue(this, parsedKeyCode);
                        Debug.Log($"Loaded key binding: {field.Name} -> {parsedKeyCode}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse KeyCode '{savedBinding.keyCode}' for field '{field.Name}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Field '{savedBinding.fieldName}' not found or not a KeyCode in InputManager.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load key bindings: {e.Message}");
        }
    }
}

/// <summary>
/// 用于序列化保存键位数据的类
/// </summary>
[Serializable]
public class KeyBindingsSaveData
{
    public List<KeyBindingEntry> keyBindings;
}

[Serializable]
public class KeyBindingEntry
{
    public string fieldName;
    public string keyCode; // 以字符串形式保存 KeyCode

    public KeyBindingEntry(string fieldName, KeyCode keyCode)
    {
        this.fieldName = fieldName;
        this.keyCode = keyCode.ToString();
    }
}



