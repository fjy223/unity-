// MissionConditionTypes.cs – 完整改版
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionConditionTypes : MonoBehaviour
{
    /* -------------------- 兼容旧关卡 -------------------- */
    [Header("旧关卡固定按钮（兼容）")]
    public Button rw;      // 任务菜单
    public Button rwfh;    // 任务菜单返回
    public Button fhmn;    // 返回模拟器

    [Header("目标组件/节点")]
    public List<CircuitComponent> targetComponents = new List<CircuitComponent>();
    public List<CircuitNode> targetNodes = new List<CircuitNode>();

    /* -------------------- 检测枚举 -------------------- */
    public enum ConditionMethod
    {
        None,
        EscPressed,
        ClickedrwcdButton,
        ClickedrwcdfhButton,
        ClickedfhmnqButton,
        AnyComponentPlaced,
        AnyComponentMoved,
        AnyWireConnected,
        BatteryCreated,
        ClickedTargetComponent,
        ClickedAnyComponent,
        ClickedTargetNode,
        ClickedAnyNode,
        ClickedTargetButton,   // 指定按钮
        AnyBulbLit             // 任意灯泡点亮
    }

    /* -------------------- 内部状态 -------------------- */
    // 旧条件
    private int lastPlacedCount = 0;
    private int lastWireCount = 0;
    private int lastBatteryCount = 0;
    private bool movedThisFrame = false;

    private bool rwButtonClicked = false;
    private bool rwfhButtonClicked = false;
    private bool fhmnqButtonClicked = false;
    private bool targetComponentClicked = false;
    private bool anyComponentClicked = false;
    private bool targetNodeClicked = false;
    private bool anyNodeClicked = false;

    // 指定按钮
    private bool targetButtonClicked = false;
    private string curMissionID = "";
    private readonly Dictionary<string, List<Button>> missionButtonMap = new Dictionary<string, List<Button>>();

    // 灯泡
    private int lastLitBulbCount = 0;

    /* -------------------- 生命周期 -------------------- */
    void Awake() => SubscribeToEvents();
    void OnEnable() => SubscribeToEvents();
    void OnDisable() => UnsubscribeFromEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    void Start()
    {
        if (rw != null) rw.onClick.AddListener(() => rwButtonClicked = true);
        if (rwfh != null) rwfh.onClick.AddListener(() => rwfhButtonClicked = true);
        if (fhmn != null) fhmn.onClick.AddListener(() => fhmnqButtonClicked = true);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            Debug.Log($"[TEST] 当前亮起灯泡数 = " + LightBulb.LitBulbCount);
    }
    /* -------------------- 事件订阅 -------------------- */
    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();
        CircuitComponent.OnComponentClicked += OnComponentClicked;
        CircuitNode.OnNodeClicked += OnNodeClicked;
        CircuitBlockPlacer.OnBlockDragFinished += () => movedThisFrame = true;
    }
    private void UnsubscribeFromEvents()
    {
        CircuitComponent.OnComponentClicked -= OnComponentClicked;
        CircuitNode.OnNodeClicked -= OnNodeClicked;
        CircuitBlockPlacer.OnBlockDragFinished -= () => movedThisFrame = true;
    }

    /* -------------------- 公有接口 -------------------- */
    public void SetTargetComponents(List<CircuitComponent> list)
    {
        targetComponents.Clear();
        if (list != null) targetComponents.AddRange(list);
        targetComponentClicked = false; anyComponentClicked = false;
    }
    public void SetTargetNodes(List<CircuitNode> list)
    {
        targetNodes.Clear();
        if (list != null) targetNodes.AddRange(list);
        targetNodeClicked = false; anyNodeClicked = false;
    }

    // 指定按钮
    public void SetTargetButtons(string missionID, List<Button> buttons)
    {
        UnsubscribeButtonsForMission(curMissionID);
        curMissionID = missionID; targetButtonClicked = false;
        if (buttons == null || buttons.Count == 0) return;

        missionButtonMap[missionID] = new List<Button>(buttons);
        foreach (var btn in buttons)
            if (btn != null) btn.onClick.AddListener(() => OnTargetButtonClicked(missionID));
    }
    public void UnsubscribeButtonsForMission(string missionID)
    {
        if (string.IsNullOrEmpty(missionID)) return;
        if (!missionButtonMap.TryGetValue(missionID, out var list)) return;
        foreach (var btn in list) if (btn != null) btn.onClick.RemoveAllListeners();
        missionButtonMap.Remove(missionID);
    }

    /* -------------------- 条件检测 -------------------- */
    public bool ExecuteConditionCheck(ConditionMethod method)
    {
        bool result = false;
        switch (method)
        {
            case ConditionMethod.None: break;
            case ConditionMethod.EscPressed: result = Input.GetKeyDown(KeyCode.Escape); break;
            case ConditionMethod.ClickedrwcdButton: result = rwButtonClicked; rwButtonClicked = false; break;
            case ConditionMethod.ClickedrwcdfhButton: result = rwfhButtonClicked; rwfhButtonClicked = false; break;
            case ConditionMethod.ClickedfhmnqButton: result = fhmnqButtonClicked; fhmnqButtonClicked = false; break;
            case ConditionMethod.AnyComponentPlaced:
                result = CircuitBlockPlacer.Instance.placedBlocks.Count > lastPlacedCount;
                if (result) lastPlacedCount = CircuitBlockPlacer.Instance.placedBlocks.Count;
                break;
            case ConditionMethod.AnyComponentMoved: result = movedThisFrame; movedThisFrame = false; break;
            case ConditionMethod.AnyWireConnected:
                result = CircuitConnectionManager.Instance.allWires.Count > lastWireCount;
                if (result) lastWireCount = CircuitConnectionManager.Instance.allWires.Count;
                break;
            case ConditionMethod.BatteryCreated:
                result = Battery.BatteryCount > lastBatteryCount;
                if (result) lastBatteryCount = Battery.BatteryCount;
                break;
            case ConditionMethod.ClickedTargetComponent: result = targetComponentClicked; targetComponentClicked = false; break;
            case ConditionMethod.ClickedAnyComponent: result = anyComponentClicked; anyComponentClicked = false; break;
            case ConditionMethod.ClickedTargetNode: result = targetNodeClicked; targetNodeClicked = false; break;
            case ConditionMethod.ClickedAnyNode: result = anyNodeClicked; anyNodeClicked = false; break;
            case ConditionMethod.ClickedTargetButton: result = targetButtonClicked; targetButtonClicked = false; break;
            case ConditionMethod.AnyBulbLit:
                result = LightBulb.LitBulbCount > lastLitBulbCount;
                if (result) lastLitBulbCount = LightBulb.LitBulbCount;
                break;
            default: Debug.LogWarning($"未实现的检测方法: {method}"); break;
        }
        return result;
    }

    /* -------------------- 内部回调 -------------------- */
    private void OnComponentClicked(CircuitComponent component)
    {
        anyComponentClicked = true;
        if (component != null && targetComponents.Contains(component))
            targetComponentClicked = true;
    }
    private void OnNodeClicked(CircuitNode node)
    {
        anyNodeClicked = true;
        if (node != null && targetNodes.Contains(node))
            targetNodeClicked = true;
    }
    private void OnTargetButtonClicked(string missionID)
    {
        if (missionID == curMissionID) targetButtonClicked = true;
    }

    /* -------------------- 工具 -------------------- */
    public void ClearTargets()
    {
        targetComponents.Clear();
        targetNodes.Clear();
        targetComponentClicked = false;
        targetNodeClicked = false;
        anyComponentClicked = false;
        anyNodeClicked = false;
    }
    public void ResetAllConditions()
    {
        rwButtonClicked = false;
        rwfhButtonClicked = false;
        fhmnqButtonClicked = false;
        targetComponentClicked = false;
        anyComponentClicked = false;
        targetNodeClicked = false;
        anyNodeClicked = false;
        targetButtonClicked = false;
        movedThisFrame = false;
        lastLitBulbCount = LightBulb.LitBulbCount; // 重置灯泡基准
    }

    public string GetMethodDisplayName(ConditionMethod method)
    {
        switch (method)
        {
            case ConditionMethod.EscPressed: return "按下ESC键";
            case ConditionMethod.ClickedrwcdButton: return "点击任务菜单按钮";
            case ConditionMethod.ClickedrwcdfhButton: return "点击任务菜单返回按钮";
            case ConditionMethod.ClickedfhmnqButton: return "点击返回模拟器按钮";
            case ConditionMethod.ClickedTargetComponent: return "点击指定组件";
            case ConditionMethod.ClickedAnyComponent: return "点击任意组件";
            case ConditionMethod.ClickedTargetNode: return "点击指定节点";
            case ConditionMethod.ClickedAnyNode: return "点击任意节点";
            case ConditionMethod.ClickedTargetButton: return "点击指定按钮";
            case ConditionMethod.AnyBulbLit: return "任意灯泡亮起";
            case ConditionMethod.None: return "无检测";
            default: return method.ToString();
        }
    }
}