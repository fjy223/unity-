using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [SerializeField] private Text messageText;
    [SerializeField] private Image bubbleBackground;
    [SerializeField] private LayoutElement layoutElement;

    [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color aiBubbleColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);

    [SerializeField] private float maxWidth = 300f;  // 限制最大宽度
    [SerializeField] private float minHeight = 50f;

    private void OnEnable()
    {
        Debug.Log("[MessageBubble] OnEnable - 检查组件");

        if (messageText == null)
        {
            messageText = GetComponentInChildren<Text>();
            Debug.Log($"[MessageBubble] 自动查找messageText: {(messageText != null ? "成功" : "失败")}");
        }

        if (bubbleBackground == null)
        {
            bubbleBackground = GetComponent<Image>();
            Debug.Log($"[MessageBubble] 自动查找bubbleBackground: {(bubbleBackground != null ? "成功" : "失败")}");
        }

        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
                Debug.Log("[MessageBubble] 添加了LayoutElement组件");
            }
        }
    }

    public void SetMessage(string message, bool isUserMessage)
    {
        Debug.Log($"[MessageBubble] SetMessage 被调用 - 消息: {message}, 是用户消息: {isUserMessage}");

        // 确保有Text组件
        if (messageText == null)
        {
            messageText = GetComponentInChildren<Text>();
            if (messageText == null)
            {
                Debug.LogError("[MessageBubble] 找不到Text组件！");
                return;
            }
        }

        // 设置文本
        messageText.text = message;
        Debug.Log($"[MessageBubble] 文本已设置: {messageText.text}");

        // 设置背景颜色
        if (bubbleBackground != null)
        {
            bubbleBackground.color = isUserMessage ? userBubbleColor : aiBubbleColor;
        }

        // 强制更新Canvas以获取正确的preferredHeight
        Canvas.ForceUpdateCanvases();

        // 配置LayoutElement
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        if (layoutElement != null)
        {
            // 计算所需高度（文本高度 + 内边距）
            float preferredHeight = messageText.preferredHeight + 20;
            float preferredWidth = Mathf.Min(messageText.preferredWidth + 20, maxWidth);

            layoutElement.preferredHeight = Mathf.Max(preferredHeight, minHeight);
            layoutElement.preferredWidth = preferredWidth;

            Debug.Log($"[MessageBubble] 设置大小 - 宽: {layoutElement.preferredWidth}, 高: {layoutElement.preferredHeight}");
        }

        // 配置RectTransform和对齐方式
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (isUserMessage)
        {
            // 用户消息靠右
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
        

            Debug.Log("[MessageBubble] 用户消息 - 靠右对齐");
        }
        else
        {
            // AI消息靠左
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);


            Debug.Log("[MessageBubble] AI消息 - 靠左对齐");
        }
    }
}
