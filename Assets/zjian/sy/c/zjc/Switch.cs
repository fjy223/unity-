using UnityEngine;
using System.Collections;
//开关
public class Switch : CircuitComponent
{
    [Header("开关参数")]
    [Tooltip("是否闭合")]
    public bool isClosed = true;

    [Tooltip("闭合时电阻 (Ω)")]
    public float closedResistance = 0.01f;

    [Tooltip("断开时电阻 (Ω)")]
    public float openResistance = 999999f;

    [Header("动画")]
    public Transform pivotRoot;          // 空物体轴心
    public Transform openTarget;         // 开闸状态
    public Transform closedTarget;       // 合闸状态
    public float animationDuration = 0.2f;
    private Coroutine animationRoutine;

    public bool IsClosed => isClosed;

    public void ToggleSwitch()
    {
        isClosed = !isClosed;
        Debug.Log($"{displayName} 已{(isClosed ? "闭合" : "断开")}");

        // 如果有旧动画在跑，先停掉
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateSwitch(isClosed));

        CircuitController.Instance?.ForceSimulationUpdate();
    }

    private IEnumerator AnimateSwitch(bool close)
    {
        var startPos = pivotRoot.localPosition;
        var startRot = pivotRoot.localRotation;
        var endPos = close ? closedTarget.localPosition : openTarget.localPosition;
        var endRot = close ? closedTarget.localRotation : openTarget.localRotation;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / animationDuration;
            pivotRoot.localPosition = Vector3.Lerp(startPos, endPos, t);
            pivotRoot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        pivotRoot.localPosition = endPos;
        pivotRoot.localRotation = endRot;
    }

    public float CurrentResistance => isClosed ? closedResistance : openResistance;

    public override string GetParameterSummary()
    {
        return $"{displayName}\n状态: {(isActive ? (isClosed ? "闭合" : "断开") : "禁用")}";
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