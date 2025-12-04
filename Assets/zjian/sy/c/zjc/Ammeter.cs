using TMPro;
using UnityEngine;

public class Ammeter : CircuitComponent
{
    [Header("电流表")]
    public float current;
    public TextMeshPro display; // 3D/UGUI TextMeshPro

    public void UpdateReading(float newCurrent)
    {
        current = newCurrent;
        if (display) display.text = $"{current:F2} A";
    }

    public override string GetParameterSummary() =>
        $"{displayName}\n状态: {(isActive ? "启用" : "禁用")}\n电流: {current:F2} A";

    public CircuitNode GetPositiveTerminal() =>
        connectionNodes.Count > 0 ? connectionNodes[0] : null;

    public CircuitNode GetNegativeTerminal() =>
        connectionNodes.Count > 1 ? connectionNodes[1] : null;
}