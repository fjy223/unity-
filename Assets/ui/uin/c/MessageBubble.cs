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

    [SerializeField] private float charWidth = 30f;
    [SerializeField] private float lineHeight = 45f;

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

        SetupRoundedCorners();
    }

    private void SetupRoundedCorners()
    {
        if (bubbleBackground == null)
            return;

        // 方法1: 使用 Image 的 pixelsPerUnitMultiplier
        bubbleBackground.pixelsPerUnitMultiplier = 1f;

        // 方法2: 添加 Shadow 效果增强视觉
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
            messageText = GetComponentInChildren<Text>();
            if (messageText != null)
                textRectTransform = messageText.GetComponent<RectTransform>();
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
        if (messageText == null || rectTransform == null)
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

        rectTransform.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        if (textRectTransform != null)
        {
            textRectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        }
    }

    private void SetupAlignment()
    {
        if (rectTransform == null)
            return;

        if (isUserMessage)
        {
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
        }

        rectTransform.anchoredPosition = Vector2.zero;
    }
}
