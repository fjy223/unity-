using UnityEngine;
using System.Collections;

public class Arrow3DGuide : MonoBehaviour
{
    [Header("3D箭头设置")]
    public GameObject arrow3D;           // 3D箭头模型

    [Header("位置设置")]
    public float heightOffset = 2f;      // 在目标上方的高度

    [Header("动画设置")]
    public float pulseIntensity = 0.2f;
    public float pulseSpeed = 2f;
    public float hoverAmplitude = 0.2f;  // 上下浮动幅度
    public float hoverSpeed = 1f;        // 上下浮动速度

    private Transform target;
    private Vector3 offset;
    private System.Action onClick;
    private Vector3 originalScale;
    private bool isActive = false;
    private float hoverTimer = 0f;
    private Vector3 basePosition;

    void Awake()
    {
        if (arrow3D == null)
            arrow3D = gameObject;

        originalScale = transform.localScale;

        // 初始隐藏
        if (arrow3D != null)
            arrow3D.SetActive(false);
    }

    void Update()
    {
        if (isActive && target != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 显示3D箭头并指向目标
    /// </summary>
    public void Show(Transform targetTransform, Vector3 worldOffset, System.Action clickCallback = null)
    {
        target = targetTransform;
        offset = worldOffset;
        onClick = clickCallback;
        isActive = true;

        if (arrow3D != null)
        {
            arrow3D.SetActive(true);

            // 设置箭头旋转指向下方（如果你的模型默认指向上方）
            transform.rotation = Quaternion.Euler(270f, 0f, 0f);

            StartCoroutine(PulseAnimation());
            StartCoroutine(HoverAnimation());
        }

        // 立即更新位置
        UpdatePosition();
    }

    /// <summary>
    /// 隐藏3D箭头
    /// </summary>
    public void Hide()
    {
        isActive = false;
        if (arrow3D != null)
            arrow3D.SetActive(false);

        StopAllCoroutines();
        transform.localScale = originalScale;
        hoverTimer = 0f;
    }

    /// <summary>
    /// 设置点击回调
    /// </summary>
    public void SetClickCallback(System.Action callback)
    {
        onClick = callback;
    }

    /// <summary>
    /// 更新箭头位置
    /// </summary>
    private void UpdatePosition()
    {
        if (target == null) return;

        // 简单计算：在目标正上方指定高度
        basePosition = target.position + Vector3.up * heightOffset + offset;
        transform.position = basePosition + Vector3.up * (Mathf.Sin(hoverTimer) * hoverAmplitude);
    }

    /// <summary>
    /// 脉冲动画（缩放）
    /// </summary>
    private IEnumerator PulseAnimation()
    {
        while (isActive)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            transform.localScale = originalScale * (1 + pulse);
            yield return null;
        }
    }

    /// <summary>
    /// 悬浮动画（上下浮动）
    /// </summary>
    private IEnumerator HoverAnimation()
    {
        while (isActive)
        {
            hoverTimer += Time.deltaTime * hoverSpeed;
            yield return null;
        }
    }

    /// <summary>
    /// 处理点击事件（如果需要交互）
    /// </summary>
    void OnMouseDown()
    {
        if (isActive)
        {
            onClick?.Invoke();
        }
    }

    /// <summary>
    /// 设置目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 检查是否正在显示
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
}