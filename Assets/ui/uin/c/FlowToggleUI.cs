using Unity.VisualScripting;
using UnityEngine;

public class FlowToggleUI : MonoBehaviour
{
    private bool showing = false;
    public static FlowToggleUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public void ToggleAllArrows()
    {
        showing = !showing;
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var w in allWires)
        {
            if (w.arrowObj != null)
            {
                // ✅ 直接设置，让每根电线自己决定是否显示（基于电流）
                w.arrowObj.SetActive(showing);
            }
        }
    }

    // 新增方法：强制关闭所有箭头（当电路断开时调用）
    public void ForceHideAllArrows()
    {
        showing = false;
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var w in allWires)
        {
            if (w.arrowObj != null)
            {
                w.arrowObj.SetActive(false);
            }
        }
    }

    // 新增方法：检查当前显示状态
    public bool IsShowing()
    {
        return showing;
    }
}