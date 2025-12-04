using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class CircuitComponent : MonoBehaviour
{
    [Header("基本参数")]
    public string displayName = "未命名组件";
    public bool isActive = true;

    [Header("连接点")]
    public List<CircuitNode> connectionNodes = new List<CircuitNode>();

    [Header("材质")]
    public Material defaultMaterial;

    // 组件点击事件
    public static event Action<CircuitComponent> OnComponentClicked;

    // 可编辑
    public virtual bool HasEditableParameters => false;
    public virtual void OnValidate() { }

    // 初始化组件
    protected virtual void Awake()
    {
        Debug.Log($"【CircuitComponent】Awake - {displayName} 初始化");
    }

    protected virtual void Start()
    {
        InitializeConnectionNodes();
        RegisterWithController();

        if (defaultMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                defaultMaterial = renderer.material;
            }
        }

        Debug.Log($"【CircuitComponent】Start - {displayName} 启动完成");
    }

    void OnEnable()
    {
        Debug.Log($"【CircuitComponent】OnEnable - {displayName} 启用");
    }

    void OnDisable()
    {
        Debug.Log($"【CircuitComponent】OnDisable - {displayName} 禁用");
    }

    // 初始化连接点
    private void InitializeConnectionNodes()
    {
        connectionNodes.Clear();
        CircuitNode[] nodes = GetComponentsInChildren<CircuitNode>();

        foreach (CircuitNode node in nodes)
        {
            if (!connectionNodes.Contains(node))
            {
                connectionNodes.Add(node);
                node.parentComponent = this;
                Debug.Log($"【CircuitComponent】初始化连接点: {node.name} -> {displayName}");
            }
        }

        Debug.Log($"【CircuitComponent】{displayName} 找到 {connectionNodes.Count} 个连接点");
    }

    // 注册到控制器
    private void RegisterWithController()
    {
        if (CircuitController.Instance != null)
        {
            CircuitController.Instance.RegisterComponent(this);
            Debug.Log($"【CircuitComponent】{displayName} 注册到控制器成功");
        }
        else
        {
            Debug.LogWarning($"【CircuitComponent】CircuitController 实例未找到，组件 '{displayName}' 未注册");
            // 延迟注册
            Invoke("DelayedRegister", 0.5f);
        }
    }

    // 延迟注册
    private void DelayedRegister()
    {
        if (CircuitController.Instance != null)
        {
            CircuitController.Instance.RegisterComponent(this);
            Debug.Log($"【CircuitComponent】{displayName} 延迟注册成功");
        }
        else
        {
            Debug.LogError($"【CircuitComponent】CircuitController 实例未找到，组件 '{displayName}' 无法注册");
        }
    }

    // 当组件被销毁时
    protected virtual void OnDestroy()
    {
        if (CircuitController.Instance != null)
        {
            CircuitController.Instance.UnregisterComponent(this);
            Debug.Log($"【CircuitComponent】{displayName} 从控制器注销");
        }
    }

    // 鼠标点击事件 - 修复版本
    private void OnMouseDown()
    {
        Debug.Log($"【CircuitComponent】OnMouseDown - {displayName} 被点击，激活状态: {isActive}");

        // 无论是否激活都触发事件用于任务检测
        if (OnComponentClicked != null)
        {
            var invocationList = OnComponentClicked.GetInvocationList();
            Debug.Log($"【CircuitComponent】事件订阅者数量: {invocationList.Length}");

            OnComponentClicked(this);
            Debug.Log($"【CircuitComponent】点击事件已触发: {displayName}");
        }
        else
        {
            Debug.LogWarning($"【CircuitComponent】没有订阅者监听 OnComponentClicked 事件");
        }
    }

    // 获取参数摘要
    public virtual string GetParameterSummary()
    {
        return $"{displayName}\n状态: {(isActive ? "启用" : "禁用")}";
    }

    /// <summary>
    /// 获取当前应使用的默认材质
    /// </summary>
    public virtual Material GetCurrentDefaultMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        return renderer != null ? renderer.material : null;
    }

    // 添加调试方法
    public void DebugComponentInfo()
    {
        Debug.Log($"【CircuitComponent】调试信息:");
        Debug.Log($"  - 名称: {displayName}");
        Debug.Log($"  - 激活状态: {isActive}");
        Debug.Log($"  - 游戏对象激活状态: {gameObject.activeInHierarchy}");
        Debug.Log($"  - 连接点数量: {connectionNodes.Count}");
        Debug.Log($"  - 渲染器存在: {GetComponent<Renderer>() != null}");

        for (int i = 0; i < connectionNodes.Count; i++)
        {
            var node = connectionNodes[i];
            Debug.Log($"  - 连接点[{i}]: {(node != null ? node.name : "null")}");
        }
    }
}