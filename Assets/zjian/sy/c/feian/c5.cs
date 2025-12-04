using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI; // 添加UI命名空间

public class C5 : MonoBehaviour
{
    [Header("放置物的各种材质")]
    public Material defaultMaterial;        // 默认材质
    public Material selectedMaterial;       // 选中材质
    public Material validPlacementMaterial; // 有效放置材质
    public Material invalidPlacementMaterial;// 无效放置材质
    public Material outlineMaterial;        // 描边材质
    public float outlineWidth = 0.05f;      // 描边宽度

    [Header("镜头设置")]
    public float zoomDistance;              // 特写镜头距离
    public float zoomSpeed;                 // 镜头移动速度
    public float zoomFOV;                   // 特写时视野

    [Header("工作平面设置")]
    public float workspaceHeight = 0.5f;    // 工作平面高度
    public float blockPlacementHeight = 1.0f; // 方块放置高度
    public LayerMask blockLayer;            // 方块层级
    public LayerMask workspaceLayer;        // 工作区层级

    [Header("进度条设置")]
    public GameObject progressCirclePrefab;   // 进度条预制体
    public float circleSize = 0.5f;           // 进度条大小（世界单位）
    public Color circleColor = Color.white;    // 进度条颜色
    public Vector3 circleOffset = new Vector3(0, 10f, 0); // 进度条在方块上方的偏移量

    // 进度条相关变量修改为每个方块独立
    private Dictionary<GameObject, GameObject> blockProgressBars = new Dictionary<GameObject, GameObject>();
    private bool isLongPressInProgress = false;    // 新增变量用于跟踪进度条状态


    private GameObject progressCircle;         // 进度条实例
    private Image progressCircleImage;        // 进度条图像组件
    private bool isShowingProgress = false;    // 是否正在显示进度条





    private GameObject selectedBlock;       // 当前选中的方块
    private bool isDragging;                // 是否正在拖动
    private Vector3 dragOffset;             // 拖动偏移量
    private List<GameObject> placedBlocks = new List<GameObject>(); // 已放置方块列表
    private Stack<GameObject> undoStack = new Stack<GameObject>(); // 撤销栈
    private Stack<GameObject> redoStack = new Stack<GameObject>(); // 重做栈
    private bool isCreatingNewBlock = false; // 是否正在创建新方块

    [Header("其他")]
    // 用于存储拖动前的方块位置
    private Vector3 dragStartPosition;
    private bool canPlaceThisFrame = true;  // 防止同一帧多次放置
    public bool chick = false;              // 选择按钮点击事件
    public GameObject currentBlockPrefab;   // 当前要放置的方块预制体

    // 选中状态变量
    private GameObject focusedBlock;        // 当前聚焦的方块
    private Vector3 originalCameraPosition; // 原始相机位置
    private Quaternion originalCameraRotation; // 原始相机旋转
    private float originalFOV;              // 原始视野
    private bool isZoomed = false;          // 是否处于特写状态
    private Material[] originalMaterials;   // 原始材质
    private List<GameObject> outlineObjects = new List<GameObject>(); // 描边对象

    // 点击检测变量
    private float clickStartTime;           // 点击开始时间
    private GameObject clickedObject;       // 点击的对象
    private const float clickDurationThreshold = 0.2f; // 点击与长按的阈值（秒）

    RaycastHit hit;

    void Start()
    {
        // 存储原始相机状态
        originalCameraPosition = Camera.main.transform.position;
        originalCameraRotation = Camera.main.transform.rotation;
        originalFOV = Camera.main.fieldOfView;
    }

    void Update()
    {
        // 重置放置标志
        canPlaceThisFrame = true;

        HandleKeyboardInput();
        HandleMouseInput();
        UpdateBlockVisuals();
        HandleShortcuts();

        // 处理ESC键退出特写
        if (isZoomed && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitFocusMode();
        }

        // 更新进度条显示
        if (isLongPressInProgress && clickedObject != null)
        {
            UpdateProgressCircle(clickedObject);
        }
    }

    void HandleKeyboardInput()
    {
        // 按下Q键：开始创建新方块
        if (Input.GetKeyDown(KeyCode.Q) || chick)
        {
            StartCreatingNewBlock();
            canPlaceThisFrame = false;
            chick = false;
        }
    }

    void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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

        // 鼠标左键按下
        if (Input.GetMouseButtonDown(0) && canPlaceThisFrame)
        {
            canPlaceThisFrame = false;

            // 如果正在创建新方块，放置它
            if (isCreatingNewBlock && selectedBlock != null)
            {
                PlaceBlock();
                return;
            }

            // 尝试选择已有方块
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, blockLayer))
            {
                // 检查是否点击UI元素
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                // 如果已经聚焦，点击其他地方退出聚焦
                if (isZoomed)
                {
                    ExitFocusMode();
                    return;
                }

                // 记录点击开始时间和对象
                clickStartTime = Time.time;
                clickedObject = hit.collider.gameObject;


                // 在放置物上方显示进度条
                CreateProgressCircleForBlock(clickedObject);
                isLongPressInProgress = true;
            }
            else
            {
                // 点击空白处取消聚焦
                if (isZoomed)
                {
                    ExitFocusMode();
                }
            }
        }

        // 鼠标左键释放（停止拖动）
        if (Input.GetMouseButtonUp(0))
        {
            // 隐藏并移除当前方块的进度条
            if (clickedObject != null && blockProgressBars.ContainsKey(clickedObject))
            {
                DestroyProgressCircleForBlock(clickedObject);
            }
            isLongPressInProgress = false;

            // 检查是否为点击操作（按下时间小于阈值）
            if (clickedObject != null && Time.time - clickStartTime < clickDurationThreshold)
            {
                // 选择方块并聚焦
                FocusOnBlock(clickedObject);
            }
            else if (isDragging && selectedBlock != null)
            {
                PlaceBlock();
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
                // 计算当前进度
                float progress = Mathf.Clamp01((Time.time - clickStartTime) / clickDurationThreshold);

                // 达到长按阈值时开始拖动
                if (progress >= 1f && !isDragging)
                {
                    // 选择方块但不聚焦
                    SelectBlock(clickedObject);

                    // 获取当前鼠标位置
                    Ray currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    dragOffset = selectedBlock.transform.position - GetWorkspacePosition(currentRay);

                    isDragging = true;
                    isLongPressInProgress = false; // 停止进度条显示

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
                if (blockProgressBars.ContainsKey(clickedObject))
                {
                    UpdateProgressCirclePosition(clickedObject);
                }
            }
        }

        // 鼠标右键：取消操作
        if (Input.GetMouseButtonDown(1))
        {
            CancelOperation();
            // 隐藏进度条
            if (clickedObject != null && blockProgressBars.ContainsKey(clickedObject))
            {
                DestroyProgressCircleForBlock(clickedObject);
            }
            isLongPressInProgress = false;
        }
    }


    /// <summary>
    /// 为方块创建进度条
    /// </summary>
    void CreateProgressCircleForBlock(GameObject block)
    {
        if (progressCirclePrefab == null || block == null)
        {
            Debug.LogWarning("进度条预制体未设置或方块无效");
            return;
        }

        // 如果已有进度条，先销毁
        if (blockProgressBars.ContainsKey(block))
        {
            Destroy(blockProgressBars[block]);
            blockProgressBars.Remove(block);
        }

        // 创建进度条作为方块的子对象
        GameObject progressCircle = Instantiate(progressCirclePrefab, block.transform);
        progressCircle.name = "ProgressCircle";

        // 设置位置（在方块上方）
        progressCircle.transform.localPosition = circleOffset;

        // 设置大小
        progressCircle.transform.localScale = new Vector3(circleSize, circleSize, circleSize);

        // 获取图像组件并设置颜色
        Image progressCircleImage = progressCircle.GetComponentInChildren<Image>();
        if (progressCircleImage != null)
        {
            progressCircleImage.color = circleColor;
            progressCircleImage.fillAmount = 0f; // 初始填充为0
        }
        else
        {
            Debug.LogWarning("进度条预制体缺少Image组件");
        }

        // 添加到字典中
        blockProgressBars.Add(block, progressCircle);

        // 确保进度条面向相机
        progressCircle.transform.LookAt(Camera.main.transform);
        progressCircle.transform.rotation = Quaternion.Euler(0, progressCircle.transform.rotation.eulerAngles.y + 180, 0);
    }

    /// <summary>
    /// 更新进度条显示
    /// </summary>
    void UpdateProgressCircle(GameObject block)
    {
        if (!blockProgressBars.ContainsKey(block) || blockProgressBars[block] == null)
            return;

        // 计算当前进度 (0到1之间)
        float progress = Mathf.Clamp01((Time.time - clickStartTime) / clickDurationThreshold);

        // 更新进度条填充
        Image progressCircleImage = blockProgressBars[block].GetComponentInChildren<Image>();
        if (progressCircleImage != null)
        {
            progressCircleImage.fillAmount = progress;
        }

        // 确保进度条面向相机
        blockProgressBars[block].transform.LookAt(Camera.main.transform);
        blockProgressBars[block].transform.rotation = Quaternion.Euler(0, blockProgressBars[block].transform.rotation.eulerAngles.y + 180, 0);
    }

    /// <summary>
    /// 更新进度条位置（跟随方块）
    /// </summary>
    void UpdateProgressCirclePosition(GameObject block)
    {
        if (blockProgressBars.ContainsKey(block) && blockProgressBars[block] != null)
        {
            // 确保位置在方块上方
            blockProgressBars[block].transform.localPosition = circleOffset;

            // 确保进度条面向相机
            blockProgressBars[block].transform.LookAt(Camera.main.transform);
            blockProgressBars[block].transform.rotation = Quaternion.Euler(0, blockProgressBars[block].transform.rotation.eulerAngles.y + 180, 0);
        }
    }

    /// <summary>
    /// 销毁方块的进度条
    /// </summary>
    void DestroyProgressCircleForBlock(GameObject block)
    {
        if (blockProgressBars.ContainsKey(block))
        {
            if (blockProgressBars[block] != null)
            {
                Destroy(blockProgressBars[block]);
            }
            blockProgressBars.Remove(block);
        }
    }


    // 开始创建新方块
    public void StartCreatingNewBlock()
    {
        // 如果已经在创建方块，先取消
        if (isCreatingNewBlock && selectedBlock != null)
        {
            Destroy(selectedBlock);
        }

        Ray centerRay = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2, Screen.height / 2));
        Vector3 defaultPosition = GetWorkspacePosition(centerRay);
        CreateNewBlock(defaultPosition);
        isCreatingNewBlock = true;
        isDragging = false;
    }

    // 设置当前方块预制体
    public void SetCurrentBlockPrefab(GameObject prefab)
    {
        currentBlockPrefab = prefab;
    }

    void CreateNewBlock(Vector3 position)
    {
        if (currentBlockPrefab == null)
        {
            Debug.LogError("没有设置方块预制体!");
            return;
        }

        GameObject newBlock = Instantiate(
            currentBlockPrefab,
            new Vector3(position.x, blockPlacementHeight, position.z),
            Quaternion.identity
        );

        SelectBlock(newBlock);
        dragOffset = Vector3.zero;

        // 新方块使用有效材质
        Renderer renderer = newBlock.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = validPlacementMaterial;
        }
    }

    void CancelOperation()
    {
        if (isCreatingNewBlock && selectedBlock != null)
        {
            // 如果是创建中的新方块，销毁它
            Destroy(selectedBlock);
            selectedBlock = null;
            isCreatingNewBlock = false;
        }
        else if (isDragging && selectedBlock != null)
        {
            // 如果是拖动中的方块，回到原始位置并取消选择
            selectedBlock.transform.position = dragStartPosition;

            Renderer renderer = selectedBlock.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = defaultMaterial;
            }

            selectedBlock = null;
            isDragging = false;
        }
    }

    Vector3 GetWorkspacePosition(Ray ray)
    {
        Plane workspacePlane = new Plane(Vector3.up, new Vector3(0, workspaceHeight, 0));
        float enter;
        if (workspacePlane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }

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
                    prevRenderer.material = defaultMaterial;
                }
                else
                {
                    // 如果是未放置的新方块，使用有效材质
                    prevRenderer.material = validPlacementMaterial;
                }
            }
        }

        // 选择新方块
        selectedBlock = block;

        Renderer newRenderer = selectedBlock.GetComponent<Renderer>();
        if (newRenderer != null)
        {
            newRenderer.material = selectedMaterial;
        }
    }

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
                    renderer1.material = defaultMaterial;
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
            renderer.material = defaultMaterial;
        }

        selectedBlock = null;
        isCreatingNewBlock = false;
    }

    void UpdateBlockVisuals()
    {
        if (selectedBlock != null)
        {
            // 检查放置位置是否有效
            bool isValidPosition = CheckPlacementValid(selectedBlock);

            Renderer renderer = selectedBlock.GetComponent<Renderer>();
            if (renderer == null) return;

            // 根据方块状态设置材质
            if (isCreatingNewBlock)
            {
                // 创建中的方块使用有效/无效材质
                renderer.material = isValidPosition ? validPlacementMaterial : invalidPlacementMaterial;
            }
            else if (isDragging)
            {
                // 拖动中的方块使用选中材质或无效材质
                renderer.material = isValidPosition ? selectedMaterial : invalidPlacementMaterial;
            }

            // 添加额外的视觉反馈
            if (isValidPosition)
            {
                // 有效位置时显示轻微发光效果
                renderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f));
            }
            else
            {
                // 无效位置时不发光
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
    // 改进的位置有效性检查
    bool CheckPlacementValid(GameObject block)
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

        // 检查实际工作区高度是否匹配
        // if (Mathf.Abs(hit.point.y - workspaceHeight) > 0.05f)
        // {
        //    Debug.Log($"工作区高度不匹配: {hit.point.y} vs {workspaceHeight}");
        //     return false;
        //}

        // 3. 检查是否在合理高度范围内
        //if (Mathf.Abs(block.transform.position.y - blockPlacementHeight) > 0.1f)
        // {
        //     Debug.Log($"高度不匹配: {block.transform.position.y} vs {blockPlacementHeight}");
        //     return false;
        // }

        return true;
    }
    void HandleShortcuts()
    {
        // 撤销操作
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            UndoPlacement();
        }

        // 重做操作
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
        {
            RedoPlacement();
        }

        // 删除选中方块
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelectedBlock();
        }
    }

    void UndoPlacement()
    {
        if (undoStack.Count > 0)
        {
            GameObject lastBlock = undoStack.Pop();
            lastBlock.SetActive(false);
            redoStack.Push(lastBlock);
            placedBlocks.Remove(lastBlock);
        }
    }

    void RedoPlacement()
    {
        if (redoStack.Count > 0)
        {
            GameObject lastBlock = redoStack.Pop();
            lastBlock.SetActive(true);
            undoStack.Push(lastBlock);
            placedBlocks.Add(lastBlock);
        }
    }

    void DeleteSelectedBlock()
    {
        if (selectedBlock != null)
        {
            placedBlocks.Remove(selectedBlock);
            Destroy(selectedBlock);
            selectedBlock = null;
            isCreatingNewBlock = false;
        }
    }

    // 清空工作区
    public void ClearWorkspace()
    {
        foreach (GameObject block in placedBlocks)
        {
            Destroy(block);
        }
        placedBlocks.Clear();
        undoStack.Clear();
        redoStack.Clear();

        if (selectedBlock != null)
        {
            Destroy(selectedBlock);
            selectedBlock = null;
            isCreatingNewBlock = false;
        }
    }

    // ======== 新增的聚焦功能 ========

    /// <summary>
    /// 聚焦到特定方块
    /// </summary>
    void FocusOnBlock(GameObject block)
    {
        // 如果已经在聚焦状态，先退出
        if (isZoomed)
        {
            ExitFocusMode();
        }

        // 设置聚焦方块
        focusedBlock = block;
        isZoomed = true;

        // 保存原始材质并应用描边效果
        ApplyOutlineEffect(block);

        // 移动相机到特写位置
        StartCoroutine(ZoomToBlock(block));

        // TODO: 这里可以显示属性菜单
        // UIManager.Instance.ShowPropertyMenu(block);
    }

    /// <summary>
    /// 应用描边效果
    /// </summary>
    void ApplyOutlineEffect(GameObject block)
    {
        // 保存原始材质
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterials = renderer.materials;

            // 创建新的材质数组（原始材质+描边材质）
            Material[] newMaterials = new Material[originalMaterials.Length + 1];
            originalMaterials.CopyTo(newMaterials, 0);
            newMaterials[originalMaterials.Length] = outlineMaterial;

            // 应用新材质
            renderer.materials = newMaterials;

            // 设置描边宽度
            renderer.materials[renderer.materials.Length - 1].SetFloat("_OutlineWidth", outlineWidth);
        }

        // 对于子物体也应用描边
        foreach (Transform child in block.transform)
        {
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                Material[] childOriginalMats = childRenderer.materials;
                Material[] newChildMaterials = new Material[childOriginalMats.Length + 1];
                childOriginalMats.CopyTo(newChildMaterials, 0);
                newChildMaterials[childOriginalMats.Length] = outlineMaterial;
                childRenderer.materials = newChildMaterials;

                // 设置描边宽度
                childRenderer.materials[childRenderer.materials.Length - 1].SetFloat("_OutlineWidth", outlineWidth);

                // 保存原始材质
                outlineObjects.Add(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 移除描边效果
    /// </summary>
    void RemoveOutlineEffect()
    {
        if (focusedBlock == null) return;

        // 恢复主物体的原始材质
        Renderer renderer = focusedBlock.GetComponent<Renderer>();
        if (renderer != null && originalMaterials != null)
        {
            renderer.materials = originalMaterials;
        }

        // 恢复子物体的原始材质
        foreach (GameObject obj in outlineObjects)
        {
            if (obj != null)
            {
                Renderer childRenderer = obj.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    Material[] mats = new Material[childRenderer.materials.Length - 1];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = childRenderer.materials[i];
                    }
                    childRenderer.materials = mats;
                }
            }
        }

        outlineObjects.Clear();
    }

    /// <summary>
    /// 镜头移动到特写位置（只改变位置和视野，不改变旋转）
    /// </summary>
    System.Collections.IEnumerator ZoomToBlock(GameObject block)
    {
        Transform camTransform = Camera.main.transform;

        // 计算目标位置：在方块前方一定距离，但保持当前相机角度
        Vector3 directionToBlock = (block.transform.position - camTransform.position).normalized;
        Vector3 targetPosition = block.transform.position - directionToBlock * zoomDistance;

        // 保持当前相机旋转
        Quaternion targetRotation = camTransform.rotation;

        // 目标视野
        float targetFOV = zoomFOV;

        Vector3 startPosition = camTransform.position;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * zoomSpeed;

            // 只插值位置和视野，保持旋转不变
            camTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, progress);

            yield return null;
        }

        // 确保最终位置和视野准确
        camTransform.position = targetPosition;
        Camera.main.fieldOfView = targetFOV;
    }

    /// <summary>
    /// 退出特写模式
    /// </summary>
    public void ExitFocusMode()
    {
        if (!isZoomed) return;

        // 移除描边效果
        RemoveOutlineEffect();

        // 恢复相机位置
        StartCoroutine(ResetCamera());

        // 重置状态
        focusedBlock = null;
        isZoomed = false;

        // TODO: 这里可以隐藏属性菜单
        // UIManager.Instance.HidePropertyMenu();
    }

    /// <summary>
    /// 恢复相机到原始位置
    /// </summary>
    System.Collections.IEnumerator ResetCamera()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 startPosition = camTransform.position;
        Quaternion startRotation = camTransform.rotation;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * zoomSpeed;

            camTransform.position = Vector3.Lerp(startPosition, originalCameraPosition, progress);
            camTransform.rotation = Quaternion.Slerp(startRotation, originalCameraRotation, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, originalFOV, progress);

            yield return null;
        }
    }
}