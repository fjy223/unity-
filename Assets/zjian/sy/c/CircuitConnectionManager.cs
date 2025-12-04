using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System; // 添加这个命名空间

//管理节点之间的电线连接。
public class CircuitConnectionManager : MonoBehaviour
{
    public static CircuitConnectionManager Instance; // 单例实例

    // 添加事件声明
    public event Action<CircuitNode, CircuitNode> OnConnectionMade;

    [Header("电线设置")]
    public GameObject wirePrefab;          // 电线预制体
    public float wireThickness = 0.05f;    // 电线粗细
    public Material validWireMaterial;     // 有效电线材质
    public Material invalidWireMaterial;   // 无效电线材质

    [Header("连接规则设置")]
    public bool allowSameBlockConnection = false;   // 是否允许同方块连接
    public bool allowSamePolarityConnection = false; // 是否允许同极性连接

    [SerializeField] private CircuitNode startNode; // 连接起点节点
    [SerializeField] private GameObject currentWire; // 当前预览中的电线
    [SerializeField] private bool isConnecting = false; // 是否正在连接
    [SerializeField] public List<GameObject> allWires = new List<GameObject>(); // 所有已创建电线

    public bool IsConnecting => isConnecting; // 连接状态属性

    void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 如果正在连接且有预览电线，更新电线位置
        if (isConnecting && currentWire != null)
        {
            UpdateWireVisual();
        }

        // 右键取消连接
        if (isConnecting && Input.GetMouseButtonDown(1))
        {
            CancelConnection();
        }
    }

    // 开始一个新连接
    public void StartConnection(CircuitNode node)
    {
        startNode = node;
        isConnecting = true;

        currentWire = Instantiate(wirePrefab);
        currentWire.GetComponent<LineRenderer>().material = validWireMaterial;

        // 设置起始节点为连接中材质
        node.SetConnectingMaterial();
    }

    // 更新预览电线位置
    private void UpdateWireVisual()
    {
        LineRenderer lineRenderer = currentWire.GetComponent<LineRenderer>();
        Vector3 startPos = startNode.transform.position; // 起始位置

        // 从鼠标位置发射射线检测节点
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 endPos = Vector3.zero;
        bool foundNode = false;
        CircuitNode closestNode = null;

        // 检测所有节点
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        foreach (RaycastHit hit in hits)
        {
            CircuitNode node = hit.collider.GetComponent<CircuitNode>();
            if (node != null)
            {
                closestNode = node;
                endPos = node.transform.position;
                foundNode = true;
                break;
            }
        }

        if (foundNode && closestNode != null)
        {
            bool isValid = IsConnectionValid(startNode, closestNode);
            lineRenderer.material = isValid ? validWireMaterial : invalidWireMaterial;
            DrawWire(lineRenderer, startPos, endPos);
        }
        else
        {
            // 没有悬停在节点上，绘制到鼠标位置
            Plane plane = new Plane(Vector3.up, startNode.transform.position);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                endPos = ray.GetPoint(distance);
                DrawWire(lineRenderer, startPos, endPos);
                lineRenderer.material = invalidWireMaterial;
            }
        }
    }

    // 绘制电线
    private void DrawWire(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount = 2; // 设置两个点
        lineRenderer.SetPosition(0, start); // 起点
        lineRenderer.SetPosition(1, end);   // 终点
        lineRenderer.startWidth = wireThickness; // 起点粗细
        lineRenderer.endWidth = wireThickness;   // 终点粗细
    }

    // 完成连接
    public void CompleteConnection(CircuitNode endNode)
    {
        if (IsConnectionValid(startNode, endNode))
        {
            CreatePermanentWire(startNode, endNode);
            startNode.isConnected = true;
            endNode.isConnected = true;

            // 触发连接建立事件
            OnConnectionMade?.Invoke(startNode, endNode);
        }

        // 恢复两个节点的原始材质
        if (startNode != null) startNode.RestoreOriginalMaterial();
        if (endNode != null) endNode.RestoreOriginalMaterial();

        CleanupConnection();
    }

    // 取消连接
    public void CancelConnection()
    {
        CleanupConnection();
    }

    // 清理连接状态
    private void CleanupConnection()
    {
        if (currentWire != null)
        {
            Destroy(currentWire);
        }

        if (startNode != null)
        {
            startNode.RestoreOriginalMaterial();
        }

        startNode = null;
        isConnecting = false;
    }

    // 检查连接是否有效
    private bool IsConnectionValid(CircuitNode start, CircuitNode end)
    {
        // 不能连接到自身
        if (start == end) return false;

        // 检查是否允许同方块连接
        if (!allowSameBlockConnection && start.parentComponent == end.parentComponent)
            return false;

        // 检查是否允许同极性连接
        if (!allowSamePolarityConnection && start.nodeType == end.nodeType)
            return false;

        // 检查是否已经存在连接
        foreach (var wire in allWires)
        {
            Wire wireComponent = wire.GetComponent<Wire>();
            if ((wireComponent.startNode == start && wireComponent.endNode == end) ||
                (wireComponent.startNode == end && wireComponent.endNode == start))
            {
                return false;
            }
        }

        return true; // 所有检查通过，连接有效
    }

    // 创建永久电线
    private void CreatePermanentWire(CircuitNode start, CircuitNode end)
    {
        // 实例化电线对象
        GameObject wireObj = Instantiate(wirePrefab);

        // 添加Wire组件并初始化
        Wire wire = wireObj.GetComponent<Wire>();
        if (wire == null)
        {
            wire = wireObj.AddComponent<Wire>();
        }

        wire.Initialize(start, end, wireThickness);

        // 添加到电线列表
        allWires.Add(wireObj);

        // 通知电路状态改变
        if (CircuitController.Instance != null)
        {
            CircuitController.Instance.ForceSimulationUpdate();
        }
    }

    /// <summary>
    /// 删除与指定节点相连的所有电线
    /// </summary>
    public void DeleteWiresConnectedToNode(CircuitNode node)
    {
        // 创建待删除列表（避免在遍历中修改集合）
        List<GameObject> wiresToDelete = new List<GameObject>();

        // 查找所有与该节点相连的电线
        foreach (var wireObj in allWires)
        {
            Wire wire = wireObj.GetComponent<Wire>();
            if (wire != null && (wire.startNode == node || wire.endNode == node))
            {
                wiresToDelete.Add(wireObj);
            }
        }

        // 删除找到的电线
        foreach (var wireObj in wiresToDelete)
        {
            DeleteWire(wireObj.GetComponent<Wire>());
        }

        // 更新节点状态
        if (node != null)
        {
            node.isConnected = false;
            node.UpdateVisuals();
        }
    }

    /// <summary>
    /// 删除电线
    /// </summary>
    public void DeleteWire(Wire wire)
    {
        if (wire == null) return;

        // 在删除前保存节点引用
        var start = wire.startNode;
        var end = wire.endNode;

        // 删除电线
        allWires.Remove(wire.gameObject);
        Destroy(wire.gameObject);

        // 删除后刷新节点状态（而不是直接设为false）
        if (start != null) start.RefreshConnectionState();
        if (end != null) end.RefreshConnectionState();

        // 通知更新
        CircuitController.Instance?.ForceSimulationUpdate();
    }

    /// <summary>
    /// 更新所有电线状态
    /// </summary>
    public void UpdateAllWires()
    {
        // 创建临时列表（避免在遍历中修改集合）
        List<GameObject> wiresToRemove = new List<GameObject>();

        foreach (var wireObj in allWires)
        {
            if (wireObj == null)
            {
                wiresToRemove.Add(wireObj);
                continue;
            }

            Wire wire = wireObj.GetComponent<Wire>();
            if (wire == null || wire.startNode == null || wire.endNode == null)
            {
                wiresToRemove.Add(wireObj);
                continue;
            }

            // 更新节点连接状态
            wire.startNode.isConnected = true;
            wire.endNode.isConnected = true;
            wire.startNode.UpdateVisuals();
            wire.endNode.UpdateVisuals();
        }

        // 移除无效电线
        foreach (var wireObj in wiresToRemove)
        {
            allWires.Remove(wireObj);
            if (wireObj != null) Destroy(wireObj);
        }
    }

    // 添加HandleNodeClick方法
    public void HandleNodeClick(CircuitNode node)
    {
        if (!isConnecting)
        {
            StartConnection(node);
        }
        else
        {
            CompleteConnection(node);
        }
    }

    // 获取与指定节点连接的所有节点
    public List<CircuitNode> GetConnectedNodes(CircuitNode node)
    {
        var connectedNodes = new List<CircuitNode>();

        foreach (var wireObj in allWires)
        {
            Wire wire = wireObj.GetComponent<Wire>();
            if (wire == null) continue;

            if (wire.startNode == node)
            {
                connectedNodes.Add(wire.endNode);
            }
            else if (wire.endNode == node)
            {
                connectedNodes.Add(wire.startNode);
            }
        }

        return connectedNodes;
    }

    //调试方法
    public void DebugConnections()
    {
        foreach (var wireObj in allWires)
        {
            Wire wire = wireObj.GetComponent<Wire>();
            Debug.Log($"电线连接：{wire.startNode.name} -> {wire.endNode.name}");
        }
    }
}