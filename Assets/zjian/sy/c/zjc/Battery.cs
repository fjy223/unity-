using UnityEngine;

public  class Battery : CircuitComponent
{
    [Header("电池参数")]
    [Tooltip("输出电压 (V)")]
    public float voltage = 9.0f;

    [Tooltip("最大输出电流 (A)")]
    public float maxCurrent = 2.0f;  // 添加最大电流属性

    [Tooltip("是否开启")]
    public bool isOn = true;
    // ui实时修改属性 最大电流和额定电压
    [SerializeField] private float _voltage = 9f;
    [SerializeField] private float _maxCurrent = 2f;

    public float Voltage { get => _voltage; set { _voltage = Mathf.Max(value, 0.01f); OnValidate(); } }
    public float MaxCurrent { get => _maxCurrent; set { _maxCurrent = Mathf.Max(value, 0.01f); OnValidate(); } }
    // 当前场景里激活的电池数量


    //计数器，教学关卡用
    public static int BatteryCount = 0;

    protected override void Start()
    {
        base.Start();
        BatteryCount++;
    }

    private void OnDestroy()
    {
        BatteryCount--;
    }
    //计数器，教学关卡用


    public override void OnValidate()
    {
        voltage = _voltage;
        maxCurrent = _maxCurrent;
    }

    // 简单获取电压值（无限电量）
    public float GetOutputVoltage()
    {
        return isOn ? voltage : 0f;
    }

    // 切换电源开关
    public void TogglePower()
    {
        isOn = !isOn;
        Debug.Log($"{displayName} {(isOn ? "开启" : "关闭")}");
    }

    // 获取正极节点
    public CircuitNode GetPositiveTerminal()
    {
        foreach (CircuitNode node in connectionNodes)
        {
            if (node.nodeType == CircuitNode.NodeType.Positive)
                return node;
        }

        // 如果没有明确标记正极，使用第一个节点
        if (connectionNodes.Count > 0)
        {
            Debug.LogWarning($"电池 '{displayName}' 没有明确的正极节点，使用第一个节点");
            return connectionNodes[0];
        }

        Debug.LogError($"电池 '{displayName}' 没有连接节点");
        return null;
    }

    // 获取负极节点
    public CircuitNode GetNegativeTerminal()
    {
        foreach (CircuitNode node in connectionNodes)
        {
            if (node.nodeType == CircuitNode.NodeType.Negative)
                return node;
        }

        // 如果没有明确标记负极，使用第二个节点
        if (connectionNodes.Count > 1)
        {
            Debug.LogWarning($"电池 '{displayName}' 没有明确的负极节点，使用第二个节点");
            return connectionNodes[1];
        }
        else if (connectionNodes.Count > 0)
        {
            Debug.LogWarning($"电池 '{displayName}' 只有一个节点，无法确定负极");
        }

        Debug.LogError($"电池 '{displayName}' 没有足够的连接节点");
        return null;
    }
    //ui信息显示
    public override string GetParameterSummary()
    {
        return $"{displayName}\n" +
               $"状态: {(isActive ? (isOn ? "开启" : "关闭") : "禁用")}\n" +
               $"电压: {voltage}V\n" +
               $"最大电流: {maxCurrent}A";
    }
}