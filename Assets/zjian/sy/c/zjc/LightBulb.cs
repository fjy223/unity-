using UnityEngine;

public  class LightBulb : CircuitComponent
{
    [Header("灯泡参数")]
    [Tooltip("额定电压 (V)")]
    public float ratedVoltage = 6.0f;

    [Tooltip("电阻 (Ω)")]
    public float resistance = 2.0f;  // 添加电阻属性

    [Tooltip("当前电流 (A)")]
    public float current;  // 添加电流属性

    [Tooltip("当前电压 (V)")]
    public float voltage;  // 添加电压属性

    [Tooltip("当前是否亮起")]
    public bool isLit = false;

    [Header("可视化")]
    [Tooltip("灯泡光源组件")]
    public Light bulbLight;

    [Tooltip("亮起材质")]
    public Material litMaterial;

    [Tooltip("熄灭材质")]
    public Material unlitMaterial;

    private Renderer bulbRenderer;



    [Header("可编辑参数")]
    [SerializeField] private float _resistance = 2f;
    [SerializeField] private float _ratedVoltage = 6f;

    public float Resistance { get => _resistance; set { _resistance = Mathf.Max(value, 0.01f); OnValidate(); } }
    public float RatedVoltage { get => _ratedVoltage; set { _ratedVoltage = Mathf.Max(value, 0.01f); OnValidate(); } }
    public static int LitBulbCount { get; private set; }



    public override void OnValidate()   // 已在基类声明 virtual
    {
        resistance = _resistance;
        ratedVoltage = _ratedVoltage;
        UpdateState();                  // 立即刷新亮灭
    }
    // 更新灯泡状态 - 确保使用正确的电流计算
    public void UpdateState()
    {
        // 根据欧姆定律计算实际功率
        float power = current * voltage;
        float minPower = 0.1f; // 最小发光功率

        // 当功率达到额定功率的90%时点亮
        float ratedPower = (ratedVoltage * ratedVoltage) / resistance;
        bool shouldBeLit = power >= ratedPower * 0.9f && power > minPower;

        SetBulbState(shouldBeLit);
    }
    // 正确重写基类的Start方法
    protected override void Start()
    {
        base.Start();
        // 额外保存两种状态材质
        if (litMaterial != null && unlitMaterial != null)
        {
            // 第一次运行时根据状态设置一次
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = GetCurrentDefaultMaterial();
            }
        }
        bulbRenderer = GetComponent<Renderer>();
        if (bulbRenderer == null)
        {
            Debug.LogWarning("灯泡缺少渲染器组件");
        }

        if (bulbLight == null)
        {
            bulbLight = GetComponentInChildren<Light>();
            if (bulbLight == null)
            {
                Debug.LogWarning("灯泡缺少光源组件");
            }
        }

        SetBulbState(false);
    }

    /// <summary>
    /// 返回当前应使用的默认材质（亮/灭）
    /// </summary>
    public Material GetCurrentDefaultMaterial()
    {
        return isLit ? litMaterial : unlitMaterial;
    }
    // 更新灯泡状态
    public void UpdateBulbState(float positiveVoltage, float negativeVoltage)
    {
        float voltageDifference = Mathf.Abs(positiveVoltage - negativeVoltage);
        bool shouldBeLit = voltageDifference >= ratedVoltage * 0.9f;

        if (shouldBeLit != isLit)
        {
            SetBulbState(shouldBeLit);
        }

        Debug.Log($"灯泡 {displayName} 电压差 = {voltageDifference}V，亮起={shouldBeLit}");
    }

    // 设置灯泡状态
    public void SetBulbState(bool lit)
    {
        if (isLit == lit) return;          // 无变化直接返回

        /* 1. 先维护计数器，再改状态 */
        if (lit) LightBulb.LitBulbCount++;
        else LightBulb.LitBulbCount--;

        isLit = lit;                       // 2. 再赋值

        /* 3. 下面保持你原来的灯光/材质逻辑不动... */
        if (bulbLight != null) bulbLight.enabled = lit;
        if (bulbRenderer != null)
        {
            bulbRenderer.material = lit ? litMaterial : unlitMaterial;
        }
    }
    void OnEnable()
    {
        /* 场景重新加载时把亮着的灯泡算进去 */
        if (isLit) LitBulbCount++;
    }
    void OnDisable()
    {
        if (isLit) LitBulbCount = Mathf.Max(0, LitBulbCount - 1);
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
            Debug.LogWarning($"灯泡 '{displayName}' 没有明确的正极节点，使用第一个节点");
            return connectionNodes[0];
        }

        Debug.LogError($"灯泡 '{displayName}' 没有连接节点");
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
            Debug.LogWarning($"灯泡 '{displayName}' 没有明确的负极节点，使用第二个节点");
            return connectionNodes[1];
        }
        else if (connectionNodes.Count > 0)
        {
            Debug.LogWarning($"灯泡 '{displayName}' 只有一个节点，无法确定负极");
        }

        Debug.LogError($"灯泡 '{displayName}' 没有足够的连接节点");
        return null;
    }
    //ui 信息显示
    public override string GetParameterSummary()
    {
        return $"{displayName}\n" +
               $"状态: {(isActive ? (isLit ? "亮起" : "熄灭") : "禁用")}\n" +
               $"额定电压: {ratedVoltage}V\n" +
               $"电阻: {resistance}Ω\n" +
               $"当前电流: {current:F2}A\n" +
               $"当前电压: {voltage:F2}V";
    }
}