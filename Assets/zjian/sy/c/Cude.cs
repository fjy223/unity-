using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class CircuitBlockPlacer : MonoBehaviour
{
    [Header("工作平面设置")]
    public float workspaceHeight = 0.5f;    // 工作平面高度
    public float blockPlacementHeight = 1.0f; // 方块放置高度
    public LayerMask blockLayer;            // 方块层级
    public LayerMask workspaceLayer;        // 工作区层级
    public LayerMask nodeLayer;             // 节点层级（用于射线检测）

    [Header("节点检测设置")]
    public bool showDebugRays = true;             // 是否显示调试射线
    public Color validRayColor = Color.green;      // 有效射线颜色
    public Color invalidRayColor = Color.red;       // 无效射线颜色
    public float rayDuration = 0.5f;               // 射线显示时间
    public float rayThickness = 0.01f;             // 射线粗细

    [SerializeField] internal GameObject selectedBlock;       // 当前选中的方块
    [SerializeField] internal bool isDragging;                // 是否正在拖动
    [SerializeField] internal Vector3 dragOffset;             // 拖动偏移量
    [SerializeField] internal List<GameObject> placedBlocks = new List<GameObject>(); // 已放置方块列表
    [SerializeField] internal Stack<GameObject> undoStack = new Stack<GameObject>(); // 撤销栈
    [SerializeField] internal Stack<GameObject> redoStack = new Stack<GameObject>(); // 重做栈
    [SerializeField] internal bool isCreatingNewBlock = false; // 是否正在创建新方块

    [Header("其他")]
    [SerializeField] internal Vector3 dragStartPosition;      // 拖动开始位置
    [SerializeField] internal bool canPlaceThisFrame = true;  // 防止同一帧多次放置
    public bool chick = false;              // 选择按钮点击事件
    public GameObject currentBlockPrefab;   // 当前要放置的方块预制体

    // 选中状态变量
    [SerializeField] internal GameObject focusedBlock;        // 当前聚焦的方块
    [SerializeField] internal Vector3 originalCameraPosition; // 原始相机位置
    [SerializeField] internal Quaternion originalCameraRotation; // 原始相机旋转
    [SerializeField] internal float originalFOV;              // 原始视野
    [SerializeField] internal bool isZoomed = false;          // 是否处于特写状态
    [SerializeField] internal Material[] originalMaterials;   // 原始材质
    [SerializeField] internal List<GameObject> outlineObjects = new List<GameObject>(); // 描边对象

    // 点击检测变量
    [SerializeField] internal float clickStartTime;           // 点击开始时间
    [SerializeField] internal GameObject clickedObject;       // 点击的对象
    [SerializeField] internal const float clickDurationThreshold = 0.2f; // 点击与长按的阈值（秒）

    [Header("删除模式设置")]
    public bool isDeleteMode = false;                        // 是否处于删除模式
    public Material deleteModeHighlightMaterial;             // 删除模式高亮材质
    public Color wireHighlightColor = Color.red;             // 电线高亮颜色
    public LayerMask deleteModeLayerMask;                    // 删除模式检测层级
    [SerializeField] private GameObject hoveredObject;        // 当前悬停的对象
    [SerializeField] private Material originalHoverMaterial; // 原始材质缓存
    [SerializeField] private Color originalWireColor;        // 原始电线颜色缓存


    [Header("操作广播")]
    public System.Action<string> OnModeChanged;
    public System.Action<string> OnBlockCreating;
    public System.Action<string> OnBlockDragging;

    [Header("旋转")]
    public float rotationStep = 45f;   // 每次 90°

    public Camera_control camera_control;
    public Material_Conversion material_conversion;
    public InputManager input;
    public FlowToggleUI flowtoggle;//电流显示

    // 单例,下面是教学关检测用
    public static CircuitBlockPlacer Instance { get; private set; }

    // 拖放/放置事件：任何方块被移动或新建时触发
    public System.Action OnAnyBlockMovedOrPlaced;

    // 任何方块被“拖动并放下”后触发
    public static System.Action OnBlockDragFinished;

    private void Awake()
    {
        Instance = this;
    }

    /* 在放置或拖动结束的地方调用一次即可 */
    private void RaiseMovedOrPlaced()
    {
        OnAnyBlockMovedOrPlaced?.Invoke();
    }
    //
    //
    //
    //上面是教学关检测用

    void Start()
    {
        if (camera_control == null)
            camera_control = FindObjectOfType<Camera_control>();
        if (material_conversion == null)
            material_conversion = FindObjectOfType<Material_Conversion>();
        // 存储原始相机状态
        originalCameraPosition = Camera.main.transform.position;
        originalCameraRotation = Camera.main.transform.rotation;
        originalFOV = Camera.main.fieldOfView;
        // 设置删除模式检测层级（默认包含方块和电线）
        deleteModeLayerMask = LayerMask.GetMask("Block", "Wire");
    }
    void Update()
    {
        // 重置放置标志
        canPlaceThisFrame = true;

        HandleKeyboardInput();
        HandleMouseInput();
        material_conversion.UpdateBlockVisuals();

        // 处理删除模式
        if (isDeleteMode)
        {
            HandleDeleteMode();
        }
        // 处理ESC键退出特写
        if (isZoomed && Input.GetKeyDown(KeyCode.Escape))
        {
            camera_control.ExitFocusMode();
        }
        HandleConnectionInput(); // 处理连接输入
        //实时更新位置
        UpdateBlockPosition();

        // 如果既不是删除、也不是拖动、也不是创建 → 普通模式
        // if (!isDeleteMode && !isDragging && !isCreatingNewBlock)
        //RealtimeOperationUI.Show("普通模式");


        Shortcutkey();//处理快捷键
       
    }

    /// <summary>
    /// 处理连接输入
    /// </summary>
    void HandleConnectionInput()
    {
        // 如果正在连接，优先处理连接操作
        if (CircuitConnectionManager.Instance.IsConnecting)
        {
            return;
        }

        // 右键点击删除电线
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Wire wire = hit.collider.GetComponent<Wire>();
                if (wire != null)
                {
                    CircuitConnectionManager.Instance.DeleteWire(wire);
                }
            }
        }
    }

    /// <summary>
    /// 创建组件
    /// </summary>
    void HandleKeyboardInput()
    {
        if (chick)
        {
            StartCreatingNewBlock();
            canPlaceThisFrame = false;
            chick = false;
        }
    }

    /// <summary>
    /// 处理鼠标输入
    /// </summary>
    void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 优先处理节点点击
        if (Input.GetMouseButtonDown(0) && canPlaceThisFrame)
        {
            canPlaceThisFrame = false;

            // 检查是否点击UI元素
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("点击在UI上");
                return;
            }

            // 尝试检测节点
            CircuitNode clickedNode = GetNodeUnderMouse(ray);
            if (clickedNode != null)
            {
                Debug.Log("射线点击节点：" + clickedNode.name);
                clickedNode.HandleClick();
                return; // 节点点击优先，不再处理方块操作
            }

            // 没有检测到节点，继续处理方块操作
            RaycastHit hit;

            // 处理方块跟随鼠标移动
            if (isCreatingNewBlock && selectedBlock != null)
            {
                // 更新方块位置跟随鼠标
                Vector3 targetPosition = GetWorkspacePosition(ray);
                selectedBlock.transform.position = new Vector3(
                    targetPosition.x,
                    blockPlacementHeight,
                    targetPosition.z
                );
            }

            // 如果正在创建新方块，放置它
            if (isCreatingNewBlock && selectedBlock != null)
            {
                PlaceBlock();
                return;
            }

            // 尝试选择已有方块
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, blockLayer))
            {
                // 如果已经聚焦，点击其他地方退出聚焦
                if (isZoomed)
                {
                    camera_control.ExitFocusMode();
                    return;
                }

                // 记录点击开始时间和对象
                clickStartTime = Time.time;
                clickedObject = hit.collider.gameObject;
            }
            else
            {
                // 点击空白处取消聚焦
                if (isZoomed)
                {
                    camera_control.ExitFocusMode();
                }
            }
        }

        // 鼠标左键释放（停止拖动）
        if (Input.GetMouseButtonUp(0))
        {
            // 检查是否为点击操作（按下时间小于阈值）
            if (clickedObject != null && Time.time - clickStartTime < clickDurationThreshold)
            {
                // 选择方块并聚焦
                camera_control.FocusOnBlock(clickedObject);
            }
            else if (isDragging && selectedBlock != null)
            {
                PlaceBlock();
                OnBlockDragFinished?.Invoke();   // <-- 新增

            }

            isDragging = false;
            clickedObject = null;


        }

        // 处理拖动操作（长按移动）
        if (Input.GetMouseButton(0) && clickedObject != null && Time.time - clickStartTime >= clickDurationThreshold)
        {
            // 首次进入拖动状态
            if (!isDragging)
            {
                RaiseMovedOrPlaced();

                // 计算当前进度
                float progress = Mathf.Clamp01((Time.time - clickStartTime) / clickDurationThreshold);

                // 达到长按阈值时开始拖动
                if (progress >= 1f && !isDragging)
                {
                    //RealtimeOperationUI.Show("移动模式：拖拽方块");
                    // 选择方块但不聚焦
                    SelectBlock(clickedObject);

                    // 获取当前鼠标位置
                    Ray currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    dragOffset = selectedBlock.transform.position - GetWorkspacePosition(currentRay);

                    isDragging = true;

                    // 记录拖动前的初始位置
                    dragStartPosition = selectedBlock.transform.position;
                }
            }
            // 持续拖动
            if (isDragging && selectedBlock != null)
            {
                Ray currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 targetPosition = GetWorkspacePosition(currentRay) + dragOffset;
                selectedBlock.transform.position = new Vector3(
                    targetPosition.x,
                    blockPlacementHeight,
                    targetPosition.z
                );
            }

        }

        // 鼠标右键：取消操作
        if (Input.GetMouseButtonDown(1))
        {
            CancelOperation();
        }
    }

    /// <summary>
    /// 绘制调试射线
    /// </summary>
    private void DrawDebugRay(Vector3 start, Vector3 end, bool isValid)
    {
        //弃用当前功能
    }

    /// <summary>
    /// 获取鼠标下的节点
    /// </summary>
    private CircuitNode GetNodeUnderMouse(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, nodeLayer);

        // 绘制主射线
        DrawDebugRay(ray.origin, ray.origin + ray.direction * 100, false);

        // 按距离排序找到最近的节点
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            CircuitNode node = hit.collider.GetComponent<CircuitNode>();
            if (node != null)
            {
                // 绘制命中节点的射线
                DrawDebugRay(ray.origin, hit.point, true);
                return node;
            }
        }
        return null;
    }

    /// <summary>
    /// 开始创建新方块
    /// </summary>
    public void StartCreatingNewBlock()
    {
        // 如果已经在创建方块，先取消
        if (isCreatingNewBlock && selectedBlock != null)
        {
            Destroy(selectedBlock);
        }

        // 从屏幕中心发射射线确定默认位置
        Ray centerRay = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        Vector3 defaultPosition = GetWorkspacePosition(centerRay);

        // 创建新方块
        CreateNewBlock(defaultPosition);
        isCreatingNewBlock = true;
        isDragging = false;
        // ✅ 新增
        //RealtimeOperationUI.Show("创建模式：放置新方块");
    }

    /// <summary>
    /// 设置当前方块预制体
    /// </summary>
    public void SetCurrentBlockPrefab(GameObject prefab)
    {
        currentBlockPrefab = prefab;
    }

    /// <summary>
    /// 创建新方块实例
    /// </summary>
    void CreateNewBlock(Vector3 position)
    {
        if (currentBlockPrefab == null)
        {
            Debug.LogError("没有设置方块预制体!");
            return;
        }

        // 实例化新方块
        GameObject newBlock = Instantiate(
            currentBlockPrefab,
            new Vector3(position.x, blockPlacementHeight, position.z),
            Quaternion.identity
        );

        // 选中新方块
        SelectBlock(newBlock);
        dragOffset = Vector3.zero;

        // 新方块使用有效材质
        Renderer renderer = newBlock.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material_conversion.validPlacementMaterial;
        }
    }

    private void UpdateBlockPosition()
    {
        // 如果正在创建新方块且有选中的方块，更新位置
        if (isCreatingNewBlock && selectedBlock != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 targetPosition = GetWorkspacePosition(ray);
            selectedBlock.transform.position = new Vector3(
                targetPosition.x,
                blockPlacementHeight,
                targetPosition.z
            );
        }
    }
    /// <summary>
    /// 取消当前操作
    /// </summary>
    void CancelOperation()
    {
        // 取消创建新方块
        if (isCreatingNewBlock && selectedBlock != null)
        {
            Destroy(selectedBlock);
            selectedBlock = null;
            isCreatingNewBlock = false;
        }
        // 取消拖动操作
        if (isDragging && selectedBlock != null)
        {
            // 回到原始位置
            selectedBlock.transform.position = dragStartPosition;

            // 恢复默认材质
            Renderer renderer = selectedBlock.GetComponent<Renderer>();
            if (renderer != null)
            {
                material_conversion.RestoreOriginalMaterials(selectedBlock);
            }

            selectedBlock = null;
            isDragging = false;
        }
    }

    /// <summary>
    /// 获取工作平面上的位置
    /// </summary>
    Vector3 GetWorkspacePosition(Ray ray)
    {
        // 创建工作平面
        Plane workspacePlane = new Plane(Vector3.up, new Vector3(0, workspaceHeight, 0));
        float enter;
        if (workspacePlane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 选择方块
    /// </summary>
    void SelectBlock(GameObject block)
    {
        // 取消之前的选择
        if (selectedBlock != null)
        {
            // 根据方块状态恢复材质
            Renderer prevRenderer = selectedBlock.GetComponent<Renderer>();
            if (prevRenderer != null)
            {
                if (placedBlocks.Contains(selectedBlock))
                {
                    material_conversion.RestoreOriginalMaterials(selectedBlock);
                }
                else
                {
                    // 如果是未放置的新方块，使用有效材质
                    prevRenderer.material = material_conversion.validPlacementMaterial;
                }
            }
        }

        // 选择新方块
        selectedBlock = block;

        // 应用选中材质
        Renderer newRenderer = selectedBlock.GetComponent<Renderer>();
        if (newRenderer != null)
        {
            newRenderer.material = material_conversion.selectedMaterial;
        }
    }

    /// <summary>
    /// 放置方块
    /// </summary>
    // 在 CircuitBlockPlacer 类中添加这个静态事件
    public static event Action<GameObject> OnComponentPlaced;

    // 在 PlaceBlock 方法中，当成功放置组件时触发事件
    void PlaceBlock()
    {
        // 检查位置是否有效（包括重叠检查）
        bool isValidPosition = CheckPlacementValid(selectedBlock);

        if (!isValidPosition)
        {
            // 位置无效，播放错误音效或显示提示
            Debug.LogWarning("不能放置在这里！位置无效或与其他方块重叠。");

            // 如果是拖动中的方块，回到原始位置
            if (isDragging)
            {
                selectedBlock.transform.position = dragStartPosition;

                Renderer renderer1 = selectedBlock.GetComponent<Renderer>();
                if (renderer1 != null)
                {
                    material_conversion.RestoreOriginalMaterials(selectedBlock);
                }

                selectedBlock = null;
                isDragging = false;
            }
            // 如果是新方块，继续跟随鼠标
            else if (isCreatingNewBlock)
            {
                // 保持方块跟随鼠标
                return;
            }

            return;
        }

        // 添加到放置列表（如果是新方块）
        if (!placedBlocks.Contains(selectedBlock))
        {
            placedBlocks.Add(selectedBlock);
            undoStack.Push(selectedBlock);
            redoStack.Clear(); // 新操作后清除重做栈

            // 触发组件放置事件
            OnComponentPlaced?.Invoke(selectedBlock);

            // 添加碰撞器组件（如果有的话）
            Collider collider = selectedBlock.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false; // 确保不是触发器
            }
        }

        // 应用默认材质
        Renderer renderer = selectedBlock.GetComponent<Renderer>();
        if (renderer != null)
        {
            material_conversion.RestoreOriginalMaterials(selectedBlock);
        }

        selectedBlock = null;
        isCreatingNewBlock = false;
        RaiseMovedOrPlaced();
    }


    /// <summary>
    /// 检查放置位置是否有效
    /// </summary>
    public bool CheckPlacementValid(GameObject block)
    {
        // 1. 检查是否与其他方块重叠
        Collider blockCollider = block.GetComponent<Collider>();
        if (blockCollider == null) return false;

        // 使用物体的实际位置和旋转计算边界
        Vector3 center = blockCollider.bounds.center;
        Vector3 halfExtents = blockCollider.bounds.extents;

        // 使用更精确的重叠检测方法
        Collider[] colliders = Physics.OverlapBox(
            center,
            halfExtents,
            block.transform.rotation,
            blockLayer
        );

        // 检查是否有其他方块重叠（排除自身）
        foreach (Collider col in colliders)
        {
            if (col.gameObject != block && !col.isTrigger)
            {
                Debug.Log($"重叠检测到: {col.gameObject.name}");
                return false;
            }
        }

        // 2. 检查是否在工作区内 - 使用更精确的检测方法
        Ray ray = new Ray(block.transform.position + Vector3.up * 2, Vector3.down);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, workspaceLayer))
        {
            Debug.Log("不在工作区内");
            return false;
        }

        return true;
    }

    
    /// <summary>
    /// 切换删除模式
    /// </summary>
    public void ToggleDeleteMode()
    {

        isDeleteMode = !isDeleteMode;

        // 控制所有电线的删除标记
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            if (isDeleteMode)
                wire.ShowDeleteMarker();
            else
                wire.HideDeleteMarker();
        }

        // 退出删除模式时恢复所有高亮
        if (!isDeleteMode && hoveredObject != null)
        {
            RestoreHoveredObjectAppearance();
            hoveredObject = null;
        }

        // 退出特写模式
        if (isZoomed)
        {
            camera_control.ExitFocusMode();
        }
        // ✅ 新增：实时提示
        // if (isDeleteMode)
        //RealtimeOperationUI.Show("删除模式：左键删除方块或电线");
        //else
        //RealtimeOperationUI.Show("普通模式");
    }

    /// <summary>
    /// 处理删除模式下的操作
    /// </summary>
    void HandleDeleteMode()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 检测鼠标悬停的对象（只检测指定层级）
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, deleteModeLayerMask))
        {
            GameObject newHoveredObject = hit.collider.gameObject;

            // 如果悬停对象发生变化
            if (newHoveredObject != hoveredObject)
            {
                // 恢复之前悬停对象的外观
                if (hoveredObject != null)
                {
                    RestoreHoveredObjectAppearance();
                }

                // 更新悬停对象并设置高亮
                hoveredObject = newHoveredObject;
                HighlightHoveredObject();
            }

            // 点击删除对象
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                DeleteHoveredObject();
            }
        }
        else
        {
            // 没有悬停对象时恢复之前的高亮
            if (hoveredObject != null)
            {
                RestoreHoveredObjectAppearance();
                hoveredObject = null;
            }
        }
    }

    /// <summary>
    /// 高亮悬停的对象
    /// </summary>
    void HighlightHoveredObject()
    {
        if (hoveredObject == null) return;

        // 处理方块
        Renderer renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalHoverMaterial = renderer.material;
            renderer.material = deleteModeHighlightMaterial;
        }

        // 处理电线
        LineRenderer wireRenderer = hoveredObject.GetComponent<LineRenderer>();
        if (wireRenderer != null)
        {
            originalWireColor = wireRenderer.startColor;
            wireRenderer.startColor = wireHighlightColor;
            wireRenderer.endColor = wireHighlightColor;
        }
    }

    /// <summary>
    /// 恢复悬停对象的外观
    /// </summary>
    void RestoreHoveredObjectAppearance()
    {
        if (hoveredObject == null) return;

        // 恢复方块材质
        Renderer renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer != null && originalHoverMaterial != null)
        {
            renderer.material = originalHoverMaterial;
        }

        // 恢复电线颜色
        LineRenderer wireRenderer = hoveredObject.GetComponent<LineRenderer>();
        if (wireRenderer != null)
        {
            wireRenderer.startColor = originalWireColor;
            wireRenderer.endColor = originalWireColor;
        }
    }

    /// <summary>
    /// 删除悬停的对象
    /// </summary>
    void DeleteHoveredObject()
    {
        if (hoveredObject == null) return;

        // 处理电线删除
        Wire wire = hoveredObject.GetComponent<Wire>();
        if (wire != null)
        {
            CircuitConnectionManager.Instance.DeleteWire(wire);
        }
        // 处理方块删除
        else
        {
            DeleteBlock(hoveredObject);
        }

        // 重置悬停对象
        hoveredObject = null;
    }

    /// <summary>
    /// 删除方块及其连接的电线
    /// </summary>
    private void DeleteBlock(GameObject blockObject)
    {
        // 获取方块上的所有节点
        CircuitNode[] nodes = blockObject.GetComponentsInChildren<CircuitNode>();
        foreach (var node in nodes)
        {
            // 删除与该节点相连的所有电线
            CircuitConnectionManager.Instance.DeleteWiresConnectedToNode(node);
        }

        // 从已放置方块列表中移除
        if (placedBlocks.Contains(blockObject))
        {
            placedBlocks.Remove(blockObject);
        }

        // 从撤销/重做栈中移除
        if (undoStack.Contains(blockObject))
        {
            // 创建临时栈移除该方块
            Stack<GameObject> tempStack = new Stack<GameObject>();
            while (undoStack.Count > 0)
            {
                GameObject obj = undoStack.Pop();
                if (obj != blockObject)
                {
                    tempStack.Push(obj);
                }
            }

            // 将剩余方块放回撤销栈
            while (tempStack.Count > 0)
            {
                undoStack.Push(tempStack.Pop());
            }
        }

        // 销毁方块
        Destroy(blockObject);

        // 确保所有电线更新状态
        CircuitConnectionManager.Instance.UpdateAllWires();
        // ✅ 强制刷新电路计算（电流会变为0）
        if (CircuitController.Instance != null)
        {
            CircuitController.Instance.ForceSimulationUpdate();
        }


    }

    // 旋转当前选中 / 正在创建的方块
    // 修改 RotateFocusedBlock 方法
    public void RotateFocusedBlock(float angle)
    {
        GameObject target = null;

        // 优先使用特写模式下的 focusedBlock
        if (isZoomed && focusedBlock != null)
        {
            target = focusedBlock;
        }
        // 否则使用选中的方块
        else if (selectedBlock != null)
        {
            target = selectedBlock;
        }

        if (target != null)
        {
            target.transform.Rotate(0, angle, 0, Space.World);
        }
        else
        {
            Debug.LogWarning("没有可供旋转的方块（特写或选中）");
        }
    }
    //ui点击旋转
    public void OnRotateLeftClicked()
    {
       RotateFocusedBlock(-rotationStep);
    }

    public void OnRotateRightClicked()
    {
        RotateFocusedBlock(rotationStep);
    }

    public void Shortcutkey()
    {
        if (isCreatingNewBlock || isDragging)
        {
            if (Input.GetKeyDown(input.zjleft))
                RotateFocusedBlock(-rotationStep);
            if (Input.GetKeyDown(input.zjright))
                RotateFocusedBlock(rotationStep);
        }
        if (Input.GetKeyDown(input.ToggleDeleteMode))
        {
            ToggleDeleteMode();
        }
        if (Input.GetKeyDown(input.CurrentDisplay))
        {

                flowtoggle.ToggleAllArrows();

        }
    }

}