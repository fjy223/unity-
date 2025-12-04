using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 实时操作提示 UI：普通模式隐藏，其他模式显示文字
/// </summary>
public class RealtimeOperationUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Text tipText;   // 拖进来

    private CircuitConnectionManager connMgr;
    private CircuitBlockPlacer       blockPlacer;

    private void Start()
    {
        connMgr    = CircuitConnectionManager.Instance;
        blockPlacer = FindObjectOfType<CircuitBlockPlacer>();

        tipText.gameObject.SetActive(false);   // 默认隐藏
    }

    private void Update()
    {
        if (connMgr == null || blockPlacer == null) return;

        // 1. 连接中
        if (connMgr.IsConnecting)
        {
            ShowTip("连接节点");
            return;
        }

        // 2. 删除模式
        if (blockPlacer.isDeleteMode)
        {
            ShowTip("删除模式");
            return;
        }

        // 3. 创建模式
        if (blockPlacer.isCreatingNewBlock)
        {
            ShowTip("创建组件");
            return;
        }

        // 4. 移动模式
        if (blockPlacer.isDragging)
        {
            ShowTip("移动组件");
            return;
        }

        // 5. 普通模式
        Hide();
    }

    private void ShowTip(string msg)
    {
        tipText.text = msg;
        tipText.gameObject.SetActive(true);
    }

    private void Hide()
    {
        tipText.gameObject.SetActive(false);
    }
    // 接口
    public static void Show(string msg)
    {
        var ui = FindObjectOfType<RealtimeOperationUI>();
        if (ui != null && ui.tipText != null)
        {
            ui.tipText.text = msg;
            ui.tipText.gameObject.SetActive(true);
        }
    }
}