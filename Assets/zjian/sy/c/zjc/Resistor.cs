using UnityEngine;
//电阻
public class Resistor : CircuitComponent
{
    [Header("电阻参数")]
    [Tooltip("电阻值 (Ω)")] public float resistance = 10f;

    [Header("实时电参量（只读）")]
    [Tooltip("当前电流 (A)")] public float current;
    [Tooltip("当前电压 (V)")] public float voltage;

    [SerializeField] private float _resistance = 10f;
    public float Resistance
    {
        get => _resistance;
        set { _resistance = Mathf.Max(value, 0.01f); OnValidate(); }
    }

    public override void OnValidate()
    {
        resistance = _resistance;
    }

    /// <summary>
    /// 由 CircuitController 在求解完成后调用，把支路结果写回本组件
    /// </summary>
    public void UpdateElectricalState(float newCurrent, float newVoltage)
    {
        current = newCurrent;
        voltage = newVoltage;
    }

    public override string GetParameterSummary()
    {
        return $"{displayName}\n" +
               $"状态: {(isActive ? "启用" : "禁用")}\n" +
               $"电阻: {resistance:F2} Ω\n" +
               $"当前电流: {current:F2} A\n" +
               $"当前电压: {voltage:F2} V";
    }

    public CircuitNode GetPositiveTerminal()
    {
        return connectionNodes.Find(n => n.nodeType == CircuitNode.NodeType.Positive) ?? connectionNodes[0];
    }

    public CircuitNode GetNegativeTerminal()
    {
        return connectionNodes.Find(n => n.nodeType == CircuitNode.NodeType.Negative) ?? connectionNodes[1];
    }
}