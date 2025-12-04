using TMPro;
using UnityEngine;

public class Voltmeter : CircuitComponent
{
    [Header("电压表")]
    public float voltage;
    public TextMeshPro display;

    public void UpdateReading(float newVoltage)
    {
        voltage = newVoltage;
        if (display) display.text = $"{voltage:F2} V";
    }

    public override string GetParameterSummary() =>
        $"{displayName}\n状态: {(isActive ? "启用" : "禁用")}\n电压: {voltage:F2} V";

    public CircuitNode GetPositiveTerminal() =>
        connectionNodes.Count > 0 ? connectionNodes[0] : null;

    public CircuitNode GetNegativeTerminal() =>
        connectionNodes.Count > 1 ? connectionNodes[1] : null;
}