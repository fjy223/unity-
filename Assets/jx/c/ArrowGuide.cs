using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ArrowGuide : MonoBehaviour, IPointerClickHandler
{
    [Header("箭头组件")]
    public GameObject arrow;
    public Image arrowImage;

    [Header("指向设置")]
    public float distanceFromTarget = 100f;
    public float pulseIntensity = 0.2f;
    public float pulseSpeed = 2f;

    private RectTransform target;
    private Vector2 offset;
    private RectTransform rectTransform;
    private System.Action onClick;
    private Vector3 originalScale;
    private bool isActive = false;
    private Canvas parentCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (arrowImage == null)
            arrowImage = GetComponent<Image>();

        originalScale = rectTransform.localScale;
    }

    void Update()
    {
        if (isActive && target != null)
        {
            UpdatePositionAndRotation();
        }
    }

    /// <summary>
    /// 显示箭头并指向目标
    /// </summary>
    public void Show(RectTransform targetTransform, Vector2 screenOffset, System.Action clickCallback = null)
    {
        target = targetTransform;
        offset = screenOffset;
        onClick = clickCallback;
        isActive = true;

        arrow.SetActive(true);
        StartCoroutine(PulseAnimation());

        // 立即更新位置
        UpdatePositionAndRotation();
    }

    /// <summary>
    /// 隐藏箭头
    /// </summary>
    public void Hide()
    {
        isActive = false;
        arrow.SetActive(false);
        StopAllCoroutines();
        rectTransform.localScale = originalScale;
    }

    /// <summary>
    /// 设置点击回调
    /// </summary>
    public void SetClickCallback(System.Action callback)
    {
        onClick = callback;
    }

    /// <summary>
    /// 更新箭头位置和旋转 - 使用Canvas坐标
    /// </summary>
    private void UpdatePositionAndRotation()
    {
        if (target == null || parentCanvas == null) return;

        // 获取目标在Canvas中的位置
        Vector2 targetAnchoredPosition = GetTargetCanvasPosition();

        // 计算箭头在Canvas中的位置
        Vector2 arrowAnchoredPosition = CalculateArrowPosition(targetAnchoredPosition);

        // 应用位置
        rectTransform.anchoredPosition = arrowAnchoredPosition;

        // 计算并应用旋转
        UpdateRotation(targetAnchoredPosition, arrowAnchoredPosition);
    }

    /// <summary>
    /// 获取目标在Canvas坐标系中的位置
    /// </summary>
    private Vector2 GetTargetCanvasPosition()
    {
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Screen Space - Overlay 模式
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                screenPoint,
                null,
                out localPoint
            );
            return localPoint;
        }
        else
        {
            // Screen Space - Camera 或 World Space 模式
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, target.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                screenPoint,
                parentCanvas.worldCamera,
                out localPoint
            );
            return localPoint;
        }
    }

    /// <summary>
    /// 计算箭头在Canvas中的位置
    /// </summary>
    private Vector2 CalculateArrowPosition(Vector2 targetPosition)
    {
        // 计算从Canvas中心到目标的方向
        Vector2 canvasCenter = GetCanvasCenter();
        Vector2 direction = (targetPosition - canvasCenter).normalized;

        // 计算箭头位置（在目标外部一定距离）
        Vector2 arrowPosition = targetPosition - direction * distanceFromTarget;

        // 应用偏移
        arrowPosition += offset;

        return arrowPosition;
    }

    /// <summary>
    /// 获取Canvas中心位置
    /// </summary>
    private Vector2 GetCanvasCenter()
    {
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        return canvasRect.rect.center;
    }

    /// <summary>
    /// 更新箭头旋转
    /// </summary>
    private void UpdateRotation(Vector2 targetPosition, Vector2 arrowPosition)
    {
        // 计算从箭头指向目标的方向
        Vector2 direction = (targetPosition - arrowPosition).normalized;

        // 计算Z轴旋转角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 直接设置Z轴旋转
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }

    /// <summary>
    /// 脉冲动画
    /// </summary>
    private IEnumerator PulseAnimation()
    {
        while (isActive)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            rectTransform.localScale = originalScale * (1 + pulse);
            yield return null;
        }
    }

    /// <summary>
    /// 点击回调
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isActive)
        {
            onClick?.Invoke();
        }
    }

    /// <summary>
    /// 调试信息
    /// </summary>
    void OnGUI()
    {
        if (isActive && target != null)
        {
            Vector3 localAngles = rectTransform.localEulerAngles;
            GUI.Label(new Rect(10, 10, 300, 60),
                $"Canvas模式: {parentCanvas.renderMode}\n" +
                $"本地旋转: X={localAngles.x:F1}, Y={localAngles.y:F1}, Z={localAngles.z:F1}\n" +
                $"目标位置: {target.anchoredPosition}");
        }
    }
}