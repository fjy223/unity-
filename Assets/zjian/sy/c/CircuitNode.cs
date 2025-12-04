using UnityEngine;
using System;

public class CircuitNode : MonoBehaviour
{
    [Header("节点设置")]
    public NodeType nodeType = NodeType.Normal; // 节点类型
    public CircuitComponent parentComponent;   // 父组件

    [Header("连接状态")]
    public bool isConnected = false;

    [Header("材质设置")]
    public Material positiveMaterial;          // 正极节点材质
    public Material negativeMaterial;          // 负极节点材质
    public Material normalMaterial;            // 普通节点材质
    public Material connectingMaterial;        // 连接中材质

    private Material originalMaterial;         // 原始材质缓存
    private Renderer nodeRenderer;             // 节点渲染器

    // 节点点击事件 - 使用静态事件确保全局访问
    public static event Action<CircuitNode> OnNodeClicked;

    public enum NodeType
    {
        Positive, // 正极
        Negative, // 负极
        Normal    // 普通节点
    }

    void Awake()
    {
        // 确保节点渲染器存在
        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer == null)
        {
            nodeRenderer = GetComponentInChildren<Renderer>();
        }

        // 保存原始材质
        if (nodeRenderer != null)
        {
            originalMaterial = nodeRenderer.material;
        }

        Debug.Log($"【CircuitNode】Awake - {gameObject.name} 初始化完成");
    }

    void Start()
    {
        // 自动分配节点材质
        AutoAssignNodeMaterial();

        Debug.Log($"【CircuitNode】Start - {gameObject.name} active={gameObject.activeInHierarchy}, " +
                  $"parent={parentComponent?.name}, isConnected={isConnected}");
    }

    void OnEnable()
    {
        Debug.Log($"【CircuitNode】OnEnable - {gameObject.name} 启用");
    }

    void OnDisable()
    {
        Debug.Log($"【CircuitNode】OnDisable - {gameObject.name} 禁用");
    }

    /// <summary>
    /// 自动分配节点材质
    /// </summary>
    private void AutoAssignNodeMaterial()
    {
        if (nodeRenderer == null) return;

        switch (nodeType)
        {
            case NodeType.Positive:
                nodeRenderer.material = positiveMaterial ?? originalMaterial;
                break;
            case NodeType.Negative:
                nodeRenderer.material = negativeMaterial ?? originalMaterial;
                break;
            case NodeType.Normal:
                nodeRenderer.material = normalMaterial ?? originalMaterial;
                break;
        }
    }

    /// <summary>
    /// 设置连接中材质
    /// </summary>
    public void SetConnectingMaterial()
    {
        if (nodeRenderer != null && connectingMaterial != null)
        {
            nodeRenderer.material = connectingMaterial;
            Debug.Log($"【CircuitNode】设置连接中材质: {gameObject.name}");
        }
    }

    /// <summary>
    /// 恢复原始节点材质
    /// </summary>
    public void RestoreOriginalMaterial()
    {
        AutoAssignNodeMaterial();
        Debug.Log($"【CircuitNode】恢复原始材质: {gameObject.name}");
    }

    /// <summary>
    /// 更新节点可视化状态
    /// </summary>
    public void UpdateVisuals()
    {
        // 这里只更新连接状态，不再改变材质
        // 材质变化由连接管理器在连接过程中控制//
        //已经废弃
    }
    public void RefreshConnectionState()
    {
        // 正确计算：只要还有任何电线连接，isConnected就为true
        isConnected = CircuitConnectionManager.Instance.GetConnectedNodes(this).Count > 0;
        UpdateVisuals();
    }
    // 当鼠标点击节点时 - 修复版本
    void OnMouseDown()
    {
        Debug.Log($"【CircuitNode】OnMouseDown - {gameObject.name} 被点击");

        // 无论父组件是否激活，都触发点击事件用于任务检测
        if (OnNodeClicked != null)
        {
            var invocationList = OnNodeClicked.GetInvocationList();
            Debug.Log($"【CircuitNode】事件订阅者数量: {invocationList.Length}");

            OnNodeClicked(this);
            Debug.Log($"【CircuitNode】点击事件已触发: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"【CircuitNode】没有订阅者监听 OnNodeClicked 事件");
        }

        // 原有的连接处理逻辑 - 只在父组件激活时执行
        if (parentComponent != null)
        {
            Debug.Log($"【CircuitNode】父组件状态 - isActive: {parentComponent.isActive}");

            if (parentComponent.isActive && CircuitConnectionManager.Instance != null)
            {
                CircuitConnectionManager.Instance.HandleNodeClick(this);
                Debug.Log($"【CircuitNode】连接管理器处理点击: {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"【CircuitNode】父组件引用为null");
        }
    }

    // 处理节点点击
    public void HandleClick()
    {
        Debug.Log($"【CircuitNode】HandleClick - 节点点击: {gameObject.name}");

        if (CircuitConnectionManager.Instance != null)
        {
            if (!CircuitConnectionManager.Instance.IsConnecting)
            {
                CircuitConnectionManager.Instance.StartConnection(this);
                Debug.Log($"【CircuitNode】开始连接: {gameObject.name}");
            }
            else
            {
                CircuitConnectionManager.Instance.CompleteConnection(this);
                Debug.Log($"【CircuitNode】完成连接: {gameObject.name}");
            }
        }
    }

    // 添加调试方法
    public void DebugNodeInfo()
    {
        Debug.Log($"【CircuitNode】调试信息:");
        Debug.Log($"  - 名称: {gameObject.name}");
        Debug.Log($"  - 类型: {nodeType}");
        Debug.Log($"  - 父组件: {(parentComponent != null ? parentComponent.displayName : "null")}");
        Debug.Log($"  - 激活状态: {gameObject.activeInHierarchy}");
        Debug.Log($"  - 父组件激活状态: {(parentComponent != null ? parentComponent.isActive : "N/A")}");
        Debug.Log($"  - 连接状态: {isConnected}");
        Debug.Log($"  - 渲染器存在: {nodeRenderer != null}");
    }
}