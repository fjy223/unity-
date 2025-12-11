using UnityEngine;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [Header("预制体 - 组件A（透明容器）")]
    [SerializeField] private float containerWidth = 500f;  // 容器A的固定宽度，可在Unity中修改
    private RectTransform containerRect;
    private Image containerImage;
    private HorizontalLayoutGroup horizontalLayoutGroup;

    [Header("预制体 - 组件B（气泡）")]
    [SerializeField] private Image bubbleBackground;
    [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color aiBubbleColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float minHeight = 60f;
    [SerializeField] private float horizontalPadding = 15f;
    [SerializeField] private float verticalPadding = 10f;

    [Header("预制体 - 文本组件")]
    [SerializeField] private Text messageText;
    [SerializeField] private float charWidth = 30f;
    [SerializeField] private float lineHeight = 45f;

    private RectTransform bubbleRect;
    private RectTransform textRectTransform;
    private bool isUserMessage;

    private void Awake()
    {
        // 获取当前对象的 RectTransform（这是组件A - 容器）
        containerRect = GetComponent<RectTransform>();

        // 获取或添加容器的 Image 组件（透明）
        containerImage = GetComponent<Image>();
        if (containerImage == null)
        {
            containerImage = gameObject.AddComponent<Image>();
        }
        containerImage.color = new Color(1, 1, 1, 0);  // 完全透明
        containerImage.raycastTarget = false;

        // ===== 关键改动：获取或添加 HorizontalLayoutGroup =====
        horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup == null)
        {
            horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        // 配置 HorizontalLayoutGroup
        horizontalLayoutGroup.childControlHeight = false;
        horizontalLayoutGroup.childControlWidth = false;
        horizontalLayoutGroup.childForceExpandHeight = false;
        horizontalLayoutGroup.childForceExpandWidth = false;
        horizontalLayoutGroup.spacing = 0;
        horizontalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);

        // 获取气泡（组件B）- 应该是这个对象的第一个子物体
        if (transform.childCount > 0)
        {
            Transform bubbleTransform = transform.GetChild(0);
            bubbleRect = bubbleTransform.GetComponent<RectTransform>();
            bubbleBackground = bubbleTransform.GetComponent<Image>();

            // 获取文本 - 应该是气泡的第一个子物体
            if (bubbleTransform.childCount > 0)
            {
                messageText = bubbleTransform.GetChild(0).GetComponent<Text>();
                if (messageText != null)
                {
                    textRectTransform = messageText.GetComponent<RectTransform>();
                }
            }
        }

        SetupRoundedCorners();
    }

    private void SetupRoundedCorners()
    {
        if (bubbleBackground == null)
            return;

        bubbleBackground.pixelsPerUnitMultiplier = 1f;

        Shadow shadow = bubbleBackground.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = bubbleBackground.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.2f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        Debug.Log("[MessageBubble] 圆角效果已应用");
    }

    public void SetMessage(string message, bool isUserMessage)
    {
        this.isUserMessage = isUserMessage;

        if (messageText == null)
        {
            Debug.LogError("[MessageBubble] 找不到文本组件");
            return;
        }

        messageText.text = message;
        messageText.alignment = TextAnchor.UpperLeft;

        if (bubbleBackground != null)
            bubbleBackground.color = isUserMessage ? userBubbleColor : aiBubbleColor;

        Canvas.ForceUpdateCanvases();
        CalculateAndSetSize(message);
        SetupAlignment();

        Debug.Log($"[MessageBubble] 消息设置完成 - 用户消息: {isUserMessage}");
    }

    private void CalculateAndSetSize(string message)
    {
        if (messageText == null || bubbleRect == null)
            return;

        int messageLength = message.Length;
        float textMaxWidth = maxWidth - horizontalPadding * 2;

        int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(textMaxWidth / charWidth));
        int lineCount = Mathf.CeilToInt((float)messageLength / charsPerLine);
        lineCount = Mathf.Max(1, lineCount);

        float textWidth;
        float textHeight;

        if (messageLength <= charsPerLine)
        {
            textWidth = messageLength * charWidth;
            textHeight = lineHeight;
        }
        else
        {
            textWidth = textMaxWidth;
            textHeight = lineCount * lineHeight;
        }

        float bubbleWidth = textWidth + horizontalPadding * 2;
        float bubbleHeight = textHeight + verticalPadding * 2;

        bubbleWidth = Mathf.Min(bubbleWidth, maxWidth);
        bubbleHeight = Mathf.Max(bubbleHeight, minHeight);

        // ===== 设置组件B（气泡）的大小 =====
        bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        // ===== 设置组件A（容器）的大小 =====
        // 宽度固定为 containerWidth，高度根据气泡高度计算
        if (containerRect != null)
        {
            containerRect.sizeDelta = new Vector2(containerWidth, bubbleHeight);
        }

        if (textRectTransform != null)
        {
            textRectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        }

        Debug.Log($"[MessageBubble] 容器A大小 - 宽: {containerWidth}, 高: {bubbleHeight}");
        Debug.Log($"[MessageBubble] 气泡B大小 - 宽: {bubbleWidth}, 高: {bubbleHeight}");
    }

    private void SetupAlignment()
    {
        if (bubbleRect == null || horizontalLayoutGroup == null)
            return;

        // ===== 关键改动：通过改变 HorizontalLayoutGroup 的 childAlignment 来控制气泡位置 =====
        if (isUserMessage)
        {
            // 用户消息：气泡在右边
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
            Debug.Log("[MessageBubble] 用户消息 - 气泡在右边");
        }
        else
        {
            // AI消息：气泡在左边
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            Debug.Log("[MessageBubble] AI消息 - 气泡在左边");
        }

        // 气泡的锚点设置为中心（由 HorizontalLayoutGroup 控制位置）
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.anchoredPosition = Vector2.zero;
    }
}
