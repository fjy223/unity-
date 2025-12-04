using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
//放置组件ui
public class UIManager : MonoBehaviour
{
    [Header("UI 组件")]
    public GameObject componentMenu;        // 组件菜单UI
    public GameObject componentButtonPrefab; // 组件按钮预制体
    public Transform buttonContainer;       // 按钮容器
    public Canvas tooltipCanvas;            // 提示文本画布

    [Header("模型预览设置")]
    public Vector3 modelPreviewPosition = new Vector3(0, 0, 0); // 模型在按钮中的位置
    public Vector3 modelPreviewRotation = Vector3.zero;        // 模型在按钮中的旋转
    public float modelPreviewScale = 1.0f;                     // 模型在按钮中的缩放
    public LayerMask modelPreviewLayer;                        // 模型预览层
    public Color previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 预览背景色

    [Header("放置组件按钮添加")]
    public List<ComponentType> componentTypes = new List<ComponentType>();

    [System.Serializable]
    public class ComponentType
    {
        public string name;         // 组件名称（如"电阻"、"电池"）
        public GameObject prefab;   // 对应的预制体
        public Sprite icon;        // 在UI中显示的图标
        public GameObject previewModel; // 预览模型（可选）
        public float previewScale = 1f;   // ← 新增：每个按钮模型独立缩放
    }

    // 定义组件选择事件委托
    public delegate void ComponentSelectedHandler(GameObject prefab);
    // 组件被选择时触发的事件
    public event ComponentSelectedHandler OnComponentSelected;

    // 所有按钮的预览模型列表
    private List<GameObject> previewModels = new List<GameObject>();

    public CircuitBlockPlacer CircuitBlockPlacer1;

    void Start()
    {
        // 初始化组件菜单
        InitializeComponentMenu();

        // 确保菜单始终显示（根据需求修改）
        if (componentMenu != null)
        {
            componentMenu.SetActive(true);
        }
    }

    /// <summary>
    /// 初始化组件菜单
    /// </summary>
    void InitializeComponentMenu()
    {
        // 检查必要的UI元素是否存在
        if (componentButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("缺少UI元素：按钮预制体或按钮容器未设置");
            return;
        }

        // 清空容器中已有的按钮
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 清空预览模型
        foreach (var model in previewModels)
        {
            Destroy(model);
        }
        previewModels.Clear();

        // 为每个组件类型创建按钮
        for (int i = 0; i < componentTypes.Count; i++)
        {
            // 使用局部变量避免闭包问题
            int index = i;

            // 实例化按钮
            GameObject buttonObj = Instantiate(componentButtonPrefab, buttonContainer);

            // 添加模型预览区域
            GameObject previewArea = CreatePreviewArea(buttonObj);

            // 创建模型预览
            if (previewArea != null)
            {
                CreateModelPreview(previewArea, componentTypes[i]);
            }

            // 获取按钮组件
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("按钮预制体缺少Button组件");
                continue;
            }

            // 设置按钮图标
            Image image = buttonObj.GetComponentInChildren<Image>();
            if (image != null && componentTypes[i].icon != null)
            {
                image.sprite = componentTypes[i].icon;
            }

            // 添加悬停提示
            UIHoverTip hoverTip = buttonObj.AddComponent<UIHoverTip>();
            hoverTip.tipText = componentTypes[i].name;
            hoverTip.tooltipCanvas = tooltipCanvas;

            // 添加点击事件监听器
            button.onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    /// <summary>
    /// 创建预览区域
    /// </summary>
    private GameObject CreatePreviewArea(GameObject buttonObj)
    {
        // 创建预览容器
        GameObject previewContainer = new GameObject("PreviewContainer");
        previewContainer.transform.SetParent(buttonObj.transform, false);
        previewContainer.AddComponent<RectTransform>();

        // 添加背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(previewContainer.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = previewBackgroundColor;

        // 设置背景大小
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 添加模型容器
        GameObject modelContainer = new GameObject("ModelContainer");
        modelContainer.transform.SetParent(previewContainer.transform, false);
        modelContainer.AddComponent<RectTransform>();

        // 设置容器位置和大小
        RectTransform containerRect = previewContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.9f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        return modelContainer;
    }

    /// <summary>
    /// 创建模型预览
    /// </summary>
    private void CreateModelPreview(GameObject container, ComponentType componentType)
    {
        // 确定要预览的模型
        GameObject previewModelPrefab = componentType.previewModel != null ?
            componentType.previewModel : componentType.prefab;

        if (previewModelPrefab == null)
        {
            Debug.LogWarning($"组件 {componentType.name} 没有预览模型");
            return;
        }

        // 实例化模型
        GameObject model = Instantiate(previewModelPrefab);
        model.layer = modelPreviewLayer.value;
        previewModels.Add(model);

        // 设置模型位置和旋转
        model.transform.position = modelPreviewPosition;
        model.transform.eulerAngles = modelPreviewRotation;
        model.transform.localScale = Vector3.one * componentType.previewScale;

        // 添加模型到UI容器
        model.transform.SetParent(container.transform, false);

        // 添加模型控制器
        ModelPreviewController controller = model.AddComponent<ModelPreviewController>();
        controller.Initialize(container.GetComponent<RectTransform>());

        // 禁用所有碰撞器
        Collider[] colliders = model.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // 禁用所有灯光
        Light[] lights = model.GetComponentsInChildren<Light>();
        foreach (Light light in lights)
        {
            light.enabled = false;
        }

        // 禁用所有刚体
        Rigidbody[] rigidbodies = model.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
        }

        // 禁用所有脚本
        MonoBehaviour[] scripts = model.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = false;
        }
    }

    /// <summary>
    /// 处理按钮点击事件
    /// </summary>
    private void OnButtonClicked(int index)
    {
        // 确保索引在有效范围内
        if (index < 0 || index >= componentTypes.Count)
        {
            Debug.LogError($"无效的按钮索引: {index}");
            return;
        }

        // 获取对应的组件预制体
        GameObject prefab = componentTypes[index].prefab;
        if (prefab == null)
        {
            Debug.LogError($"索引 {index} 的组件预制体未设置");
            return;
        }

        // 触发组件选择事件
        OnComponentSelected?.Invoke(prefab);
        Debug.Log($"选择了组件: {componentTypes[index].name}");
        CircuitBlockPlacer1.currentBlockPrefab = prefab;
        CircuitBlockPlacer1.chick = true;
        Debug.Log(CircuitBlockPlacer1.chick);
    }

    /// <summary>
    /// 设置菜单可见性
    /// </summary>
    public void SetMenuVisibility(bool visible)
    {
        if (componentMenu != null)
        {
            componentMenu.SetActive(visible);
        }
    }
}

/// <summary>
/// 模型预览控制器
/// </summary>
public class ModelPreviewController : MonoBehaviour
{
    private RectTransform container;
    private Camera previewCamera;

    public void Initialize(RectTransform container)
    {
        this.container = container;

        // 创建预览相机
        CreatePreviewCamera();
    }

    private void CreatePreviewCamera()
    {
        // 创建相机对象
        GameObject cameraObj = new GameObject("PreviewCamera");
        cameraObj.transform.SetParent(transform, false);

        // 添加相机组件
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 1.0f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0, 0, 0, 0);
        previewCamera.cullingMask = LayerMask.GetMask("ModelPreview");
        previewCamera.depth = -100; // 确保在UI后面渲染

        // 设置渲染纹理
        RenderTexture renderTexture = new RenderTexture(256, 256, 24);
        previewCamera.targetTexture = renderTexture;

        // 添加RawImage显示渲染结果
        GameObject renderImage = new GameObject("RenderImage");
        renderImage.transform.SetParent(container, false);
        RawImage rawImage = renderImage.AddComponent<RawImage>();
        rawImage.texture = renderTexture;

        // 设置渲染图像位置
        RectTransform imageRect = renderImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (container != null && previewCamera != null)
        {
            // 使模型始终面向相机
            previewCamera.transform.LookAt(transform.position);
        }
    }
}



/// <summary>
/// UI悬停提示组件
/// </summary>
public class UIHoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("提示设置")]
    public string tipText = "";          // 提示文本内容
    public Canvas tooltipCanvas;         // 提示文本使用的画布
    public Vector2 offset = new Vector2(0, 40); // 提示框相对于鼠标的偏移量

    [Header("样式设置")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 背景颜色
    public Color textColor = Color.white;      // 文本颜色
    public int fontSize = 16;                  // 字体大小

    private GameObject tooltipPanel;     // 提示面板对象
    private bool isHovering = false;     // 是否正在悬停

    /// <summary>
    /// 当鼠标进入UI元素时调用
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(tipText) && tooltipCanvas != null)
        {
            ShowTooltip();
            isHovering = true;
        }
    }

    /// <summary>
    /// 当鼠标离开UI元素时调用
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
        isHovering = false;
    }

    void Update()
    {
        // 如果正在悬停，更新提示框位置
        if (isHovering && tooltipPanel != null)
        {
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// 显示提示框
    /// </summary>
    void ShowTooltip()
    {
        // 确保提示框不存在
        if (tooltipPanel != null) return;

        // 创建提示面板
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(tooltipCanvas.transform, false);

        // 添加RectTransform组件
        RectTransform panelRT = tooltipPanel.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(200, 50); // 初始大小

        // 添加背景
        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = backgroundColor;
        bg.raycastTarget = false; // 防止阻挡鼠标事件

        // 添加文本
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = tipText;
        text.color = textColor;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        // 设置文本位置
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // 更新位置
        UpdateTooltipPosition();
    }

    /// <summary>
    /// 更新提示框位置
    /// </summary>
    void UpdateTooltipPosition()
    {
        if (tooltipPanel == null) return;

        // 将鼠标位置转换为画布空间位置
        RectTransform canvasRT = tooltipCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            Input.mousePosition,
            null,
            out localPoint
        );

        // 设置位置（添加偏移）
        tooltipPanel.GetComponent<RectTransform>().anchoredPosition = localPoint + offset;
    }

    /// <summary>
    /// 隐藏提示框
    /// </summary>
    void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            Destroy(tooltipPanel);
            tooltipPanel = null;
        }
    }

    /// <summary>
    /// 当对象被销毁时清理提示框
    /// </summary>
    void OnDestroy()
    {
        HideTooltip();
    }
}