using UnityEngine;
using System.Collections.Generic;

public class c3 : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;          // 方块预制体
    public Material defaultMaterial;        // 默认材质
    public Material selectedMaterial;       // 选中材质
    public Material validPlacementMaterial; // 有效放置材质
    public Material invalidPlacementMaterial;// 无效放置材质

    [Header("Workspace Settings")]
    public float workspaceHeight = 0.5f;    // 工作平面高度
    public float blockPlacementHeight = 1.0f; // 方块放置高度
    public LayerMask blockLayer;            // 方块层级
    public LayerMask workspaceLayer;        // 工作区层级

    private GameObject selectedBlock;       // 当前选中的方块
    private bool isDragging;                // 是否正在拖动
    private Vector3 dragOffset;             // 拖动偏移量
    private List<GameObject> placedBlocks = new List<GameObject>(); // 已放置方块列表
    private Stack<GameObject> undoStack = new Stack<GameObject>(); // 撤销栈
    private Stack<GameObject> redoStack = new Stack<GameObject>(); // 重做栈
    private bool isCreatingNewBlock = false; // 是否正在创建新方块

    // 新增：用于存储拖动前的方块位置
    private Vector3 dragStartPosition;
    private bool canPlaceThisFrame = true;  // 防止同一帧多次放置

    void Update()
    {
        // 重置放置标志
        canPlaceThisFrame = true;

        HandleKeyboardInput();
        HandleMouseInput();
        UpdateBlockVisuals();
        HandleShortcuts();
    }

    void HandleKeyboardInput()
    {
        // 按下Q键：开始创建新方块
        if (Input.GetKeyDown(KeyCode.Q) && canPlaceThisFrame)
        {
            StartCreatingNewBlock();
            canPlaceThisFrame = false;
        }
    }

    void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

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

            RaycastHit hit;

            // 如果正在创建新方块，放置它
            if (isCreatingNewBlock && selectedBlock != null)
            {
                PlaceBlock();
                return;
            }

            // 尝试选择已有方块
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, blockLayer))
            {
                SelectBlock(hit.collider.gameObject);
                dragOffset = selectedBlock.transform.position - GetWorkspacePosition(ray);
                isDragging = true;

                // 记录拖动前的初始位置
                dragStartPosition = selectedBlock.transform.position;
            }
        }

        // 鼠标左键释放（停止拖动）
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            if (selectedBlock != null)
            {
                PlaceBlock();
            }
            isDragging = false;
        }

        // 拖动已有方块
        if (isDragging && selectedBlock != null)
        {
            Vector3 targetPosition = GetWorkspacePosition(ray) + dragOffset;
            selectedBlock.transform.position = new Vector3(
                targetPosition.x,
                blockPlacementHeight,
                targetPosition.z
            );
        }

        // 鼠标右键：取消操作
        if (Input.GetMouseButtonDown(1))
        {
            CancelOperation();
        }
    }

    void StartCreatingNewBlock()
    {
        // 如果已经在创建方块，先取消
        if (isCreatingNewBlock && selectedBlock != null)
        {
            Destroy(selectedBlock);
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, workspaceLayer))
        {
            CreateNewBlock(hit.point);
            isCreatingNewBlock = true;
            isDragging = false;
        }
    }

    void CreateNewBlock(Vector3 position)
    {
        GameObject newBlock = Instantiate(
            blockPrefab,
            new Vector3(position.x, blockPlacementHeight, position.z),
            Quaternion.identity
        );

        SelectBlock(newBlock);
        dragOffset = Vector3.zero;

        // 新方块使用有效材质
        newBlock.GetComponent<Renderer>().material = validPlacementMaterial;
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
            selectedBlock.GetComponent<Renderer>().material = defaultMaterial;
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
            if (placedBlocks.Contains(selectedBlock))
            {
                selectedBlock.GetComponent<Renderer>().material = defaultMaterial;
            }
            else
            {
                // 如果是未放置的新方块，使用有效材质
                selectedBlock.GetComponent<Renderer>().material = validPlacementMaterial;
            }
        }

        // 选择新方块
        selectedBlock = block;
        selectedBlock.GetComponent<Renderer>().material = selectedMaterial;
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
                selectedBlock.GetComponent<Renderer>().material = defaultMaterial;
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
        }

        // 应用默认材质
        selectedBlock.GetComponent<Renderer>().material = defaultMaterial;
        selectedBlock = null;
        isCreatingNewBlock = false;
    }

    void UpdateBlockVisuals()
    {
        if (selectedBlock != null)
        {
            // 检查放置位置是否有效
            bool isValidPosition = CheckPlacementValid(selectedBlock);

            // 根据方块状态设置材质
            if (isCreatingNewBlock)
            {
                // 创建中的方块使用有效/无效材质
                selectedBlock.GetComponent<Renderer>().material =
                    isValidPosition ? validPlacementMaterial : invalidPlacementMaterial;
            }
            else if (isDragging)
            {
                // 拖动中的方块使用选中材质或无效材质
                selectedBlock.GetComponent<Renderer>().material =
                    isValidPosition ? selectedMaterial : invalidPlacementMaterial;
            }
        }
    }

    // 改进的位置有效性检查
    bool CheckPlacementValid(GameObject block)
    {
        // 1. 检查是否与其他方块重叠
        Collider blockCollider = block.GetComponent<Collider>();
        if (blockCollider == null) return false;

        // 计算方块的实际边界 - 使用更严格的检测
        Vector3 halfExtents = blockCollider.bounds.extents * 0.8f; // 缩小更多避免边界问题
        Vector3 center = blockCollider.bounds.center;

        // 使用OverlapBox检查重叠
        Collider[] colliders = Physics.OverlapBox(
            center,
            halfExtents,
            block.transform.rotation,
            blockLayer
        );

        // 检查是否有其他方块重叠（排除自身）
        foreach (Collider col in colliders)
        {
            if (col.gameObject != block)
            {
                // 记录重叠的方块
                Debug.Log($"方块重叠: {block.name} 与 {col.gameObject.name}");
                return false;
            }
        }

        // 2. 检查是否在工作区内
        Ray ray = new Ray(block.transform.position + Vector3.up * 5, Vector3.down);
        if (!Physics.Raycast(ray, Mathf.Infinity, workspaceLayer))
        {
            return false;
        }

        // 3. 检查是否在合理高度范围内
        if (Mathf.Abs(block.transform.position.y - blockPlacementHeight) > 0.1f)
        {
            return false;
        }

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
}