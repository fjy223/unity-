using UnityEngine;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [SerializeField] private Text messageText;
    [SerializeField] private Image bubbleBackground;

    [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color aiBubbleColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float minHeight = 60f;
    [SerializeField] private float horizontalPadding = 15f;
    [SerializeField] private float verticalPadding = 10f;

    // 字体参数
    [SerializeField] private float charWidth = 30f;      // 单个字符平均宽度
    [SerializeField] private float lineHeight = 45f;     // 行高

    private RectTransform rectTransform;
    private RectTransform textRectTransform;
    private bool isUserMessage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (messageText == null)
            messageText = GetComponentInChildren<Text>();

        if (messageText != null)
            textRectTransform = messageText.GetComponent<RectTransform>();

        if (bubbleBackground == null)
            bubbleBackground = GetComponent<Image>();
    }

    public void SetMessage(string message, bool isUserMessage)
    {
        this.isUserMessage = isUserMessage;

        if (messageText == null)
        {
            messageText = GetComponentInChildren<Text>();
            if (messageText != null)
                textRectTransform = messageText.GetComponent<RectTransform>();
        }

        messageText.text = message;
        messageText.alignment = TextAnchor.UpperLeft;

        // 设置气泡颜色
        if (bubbleBackground != null)
            bubbleBackground.color = isUserMessage ? userBubbleColor : aiBubbleColor;

        // 强制更新画布
        Canvas.ForceUpdateCanvases();

        // 计算并设置大小
        CalculateAndSetSize(message);

        // 设置对齐
        SetupAlignment();

        Debug.Log($"[MessageBubble] 消息设置完成 - 用户消息: {isUserMessage}, 消息长度: {message.Length}");
    }

    private void CalculateAndSetSize(string message)
    {
        if (messageText == null || rectTransform == null)
            return;

        // 计算需要的行数
        int messageLength = message.Length;
        float textMaxWidth = maxWidth - horizontalPadding * 2;

        // 每行能容纳的字符数
        int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(textMaxWidth / charWidth));

        // 计算需要的行数
        int lineCount = Mathf.CeilToInt((float)messageLength / charsPerLine);
        lineCount = Mathf.Max(1, lineCount);

        // 计算文本的实际宽度和高度
        float textWidth;
        float textHeight;

        if (messageLength <= charsPerLine)
        {
            // 单行
            textWidth = messageLength * charWidth;
            textHeight = lineHeight;
        }
        else
        {
            // 多行
            textWidth = textMaxWidth;
            textHeight = lineCount * lineHeight;
        }

        // 计算气泡总大小（包括内边距）
        float bubbleWidth = textWidth + horizontalPadding * 2;
        float bubbleHeight = textHeight + verticalPadding * 2;

        // 限制最大宽度
        bubbleWidth = Mathf.Min(bubbleWidth, maxWidth);

        // 应用最小高度
        bubbleHeight = Mathf.Max(bubbleHeight, minHeight);

        // 直接设置气泡的 RectTransform 大小
        rectTransform.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        // 设置文本的 RectTransform 大小
        if (textRectTransform != null)
        {
            textRectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        }

        Debug.Log($"[MessageBubble] 计算结果 - 消息长度: {messageLength}, 每行字符: {charsPerLine}, 行数: {lineCount}");
        Debug.Log($"[MessageBubble] 文本大小 - 宽: {textWidth}, 高: {textHeight}");
        Debug.Log($"[MessageBubble] 气泡大小 - 宽: {bubbleWidth}, 高: {bubbleHeight}");
    }

    private void SetupAlignment()
    {
        if (rectTransform == null)
            return;

        if (isUserMessage)
        {
            // 用户消息：右对齐
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
        }
        else
        {
            // AI消息：左对齐
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
        }

        rectTransform.anchoredPosition = Vector2.zero;
    }
}
