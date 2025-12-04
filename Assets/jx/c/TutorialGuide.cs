using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class GuideConfig
{
    public enum GuideType
    {
        Highlight,      // 高亮目标
        Arrow,          // 箭头指示
        HandPointer,    // 手型指针
        Circle,         // 圆圈环绕
        Arrow3D,//3d箭头
        Message         // 纯消息提示
    }

    public GuideType type;
    public Transform target;           // 指引目标
    public string message;             // 提示文字
    public Vector2 screenOffset;       // 屏幕偏移
    public float duration = 3f;        // 持续时间
    public bool waitForClick = true;   // 是否等待点击
}

public class TutorialGuide : MonoBehaviour
{
    [Header("指引预制体")]
    public GameObject highlightPrefab;
    public GameObject arrowPrefab;
    public GameObject handPointerPrefab;
    public GameObject circlePrefab;
    public GameObject messagePrefab;
    public GameObject arrow3DPrefab; // 新增3D箭头预制体

    [Header("设置")]
    public Canvas guideCanvas;
    public float animationDuration = 0.5f;

    [Header("箭头指引引用")]
    public ArrowGuide arrowGuide;
    public Arrow3DGuide arrow3DGuide; // 3D箭头引用
    private GameObject currentGuide;
    private System.Action onGuideComplete;

    void Start()
    {
        // 初始隐藏箭头
        if (arrowGuide != null)
        {
            arrowGuide.Hide();
        }
    }

    /// <summary>
    /// 显示指引
    /// </summary>
    public void ShowGuide(GuideConfig config, System.Action onComplete = null)
    {
        onGuideComplete = onComplete;

        // 清除之前的指引
        if (currentGuide != null)
            Destroy(currentGuide);

        // 隐藏箭头（如果之前显示）
        if (arrowGuide != null)
            arrowGuide.Hide();

        switch (config.type)
        {
            case GuideConfig.GuideType.Highlight:
                CreateHighlightGuide(config);
                break;
            case GuideConfig.GuideType.Arrow:
                CreateArrowGuide(config);
                break;
            case GuideConfig.GuideType.HandPointer:
                CreateHandPointerGuide(config);
                break;
            case GuideConfig.GuideType.Circle:
                CreateCircleGuide(config);
                break;
            case GuideConfig.GuideType.Message:
                CreateMessageGuide(config);
                break;
            case GuideConfig.GuideType.Arrow3D:
                CreateArrow3DGuide(config);
                break;
        }

        // 设置自动消失或等待交互
        if (config.duration > 0 && !config.waitForClick)
        {
            StartCoroutine(AutoHideGuide(config.duration));
        }
    }

    /// <summary>
    /// 创建高亮指引
    /// </summary>
    private void CreateHighlightGuide(GuideConfig config)
    {
        if (config.target == null) return;

        currentGuide = Instantiate(highlightPrefab, guideCanvas.transform);
        HighlightGuide highlight = currentGuide.GetComponent<HighlightGuide>();

        if (highlight != null)
        {
            highlight.Initialize(config.target, config.screenOffset);

            if (config.waitForClick)
            {
                highlight.SetClickCallback(() => CompleteGuide());
            }
        }
    }

    /// <summary>
    /// 创建箭头指引
    /// </summary>
    private void CreateArrowGuide(GuideConfig config)
    {
        if (config.target == null) return;

        // 获取目标的RectTransform
        RectTransform targetRect = config.target.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            Debug.LogWarning($"箭头指引的目标 {config.target.name} 没有RectTransform组件");
            return;
        }

        if (arrowGuide != null)
        {
            // 设置点击回调
            System.Action clickCallback = null;
            if (config.waitForClick)
            {
                clickCallback = () => CompleteGuide();
            }

            // 显示箭头并指向目标
            arrowGuide.Show(targetRect, config.screenOffset, clickCallback);

            // 设置自动消失
            if (config.duration > 0 && !config.waitForClick)
            {
                StartCoroutine(AutoHideArrow(config.duration));
            }
        }
        else
        {
            Debug.LogError("ArrowGuide引用未设置！");
        }
    }

    /// <summary>
    /// 自动隐藏箭头
    /// </summary>
    private IEnumerator AutoHideArrow(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (arrowGuide != null)
        {
            arrowGuide.Hide();
        }
        CompleteGuide();
    }

    /// <summary>
    /// 隐藏当前指引
    /// </summary>
    public void HideCurrentGuide()
    {
        if (currentGuide != null)
        {
            Destroy(currentGuide);
            currentGuide = null;
        }

        // 隐藏箭头
        if (arrowGuide != null)
        {
            arrowGuide.Hide();
        }
        // 隐藏3D箭头
        if (arrow3DGuide != null)
        {
            arrow3DGuide.Hide();
        }
    }

    /// <summary>
    /// 创建手型指针指引
    /// </summary>
    private void CreateHandPointerGuide(GuideConfig config)
    {
        currentGuide = Instantiate(handPointerPrefab, guideCanvas.transform);
        HighlightGuide handPointer = currentGuide.GetComponent<HighlightGuide>();

        if (handPointer != null)
        {
            handPointer.Initialize(config.target, config.screenOffset);

            if (config.waitForClick)
            {
                handPointer.SetClickCallback(() => CompleteGuide());
            }
        }
    }

    /// <summary>
    /// 创建圆圈环绕指引
    /// </summary>
    private void CreateCircleGuide(GuideConfig config)
    {
        currentGuide = Instantiate(circlePrefab, guideCanvas.transform);
        HighlightGuide circle = currentGuide.GetComponent<HighlightGuide>();

        if (circle != null)
        {
            circle.Initialize(config.target, config.screenOffset);

            if (config.waitForClick)
            {
                circle.SetClickCallback(() => CompleteGuide());
            }
        }
    }

    /// <summary>
    /// 创建消息指引
    /// </summary>
    private void CreateMessageGuide(GuideConfig config)
    {
        currentGuide = Instantiate(messagePrefab, guideCanvas.transform);

        // 设置消息文本
        Text messageText = currentGuide.GetComponentInChildren<Text>();
        if (messageText != null)
            messageText.text = config.message;

        if (config.waitForClick)
        {
            HighlightGuide message = currentGuide.GetComponent<HighlightGuide>();
            if (message != null)
            {
                message.SetClickCallback(() => CompleteGuide());
            }
        }
    }
    private void CreateArrow3DGuide(GuideConfig config)//3d箭头指引
    {
        if (config.target == null) return;

        if (arrow3DGuide != null)
        {
            // 设置点击回调
            System.Action clickCallback = null;
            if (config.waitForClick)
            {
                clickCallback = () => CompleteGuide();
            }

            // 显示3D箭头并指向目标
            arrow3DGuide.Show(config.target, config.screenOffset, clickCallback);

            // 设置自动消失
            if (config.duration > 0 && !config.waitForClick)
            {
                StartCoroutine(AutoHideArrow3D(config.duration));
            }
        }
        else
        {
            Debug.LogError("Arrow3DGuide引用未设置！");
        }
    }
    /// <summary>
    /// 自动隐藏3D箭头
    /// </summary>
    private IEnumerator AutoHideArrow3D(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (arrow3DGuide != null)
        {
            arrow3DGuide.Hide();
        }
        CompleteGuide();
    }

    /// <summary>
    /// 自动隐藏指引
    /// </summary>
    private IEnumerator AutoHideGuide(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteGuide();
    }

    /// <summary>
    /// 完成指引
    /// </summary>
    private void CompleteGuide()
    {
        onGuideComplete?.Invoke();
        HideCurrentGuide();
    }

    /// <summary>
    /// 调试方法：测试箭头指向
    /// </summary>
    public void TestArrowPointing(Transform testTarget)
    {
        if (arrowGuide != null && testTarget != null)
        {
            RectTransform targetRect = testTarget.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                arrowGuide.Show(targetRect, Vector2.zero);
            }
        }
    }
}