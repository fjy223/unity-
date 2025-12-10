using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ChatWindowManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button addTopicButton;
    [SerializeField] private Button deleteTopicButton;

    [Header("预制体")]
    [SerializeField] private GameObject messageBubblePrefab;
    [SerializeField] private GameObject topicItemPrefab;

    [Header("话题管理")]
    [SerializeField] private Transform topicContainer;
    private List<string> currentTopics = new List<string>();
    private string currentTopic = "";

    private APIManager apiManager;
    private bool isWaitingForResponse = false;

    void Start()
    {
        Debug.Log("[ChatWindow] 初始化开始");

        apiManager = GetComponent<APIManager>();
        if (apiManager == null)
        {
            Debug.LogError("[ChatWindow] 找不到APIManager组件！");
            return;
        }

        // 绑定按钮事件
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);
        else
            Debug.LogError("[ChatWindow] sendButton未赋值");

        if (addTopicButton != null)
            addTopicButton.onClick.AddListener(OnAddTopic);
        else
            Debug.LogError("[ChatWindow] addTopicButton未赋值");

        if (deleteTopicButton != null)
            deleteTopicButton.onClick.AddListener(OnDeleteTopic);
        else
            Debug.LogError("[ChatWindow] deleteTopicButton未赋值");

        if (messageInputField != null)
            messageInputField.onSubmit.AddListener(OnInputSubmit);
        else
            Debug.LogError("[ChatWindow] messageInputField未赋值");

        // 初始化默认话题
        AddTopicItem("软件使用");
        AddTopicItem("功能介绍");
        AddTopicItem("故障排查");

        Debug.Log("[ChatWindow] 初始化完成");
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    private void OnSendMessage()
    {
        string message = messageInputField.text.Trim();

        Debug.Log($"[ChatWindow] 尝试发送消息: '{message}'");

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("[ChatWindow] 消息为空");
            return;
        }

        if (isWaitingForResponse)
        {
            Debug.LogWarning("[ChatWindow] 正在等待API响应，请稍候...");
            return;
        }

        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogWarning("[ChatWindow] 请先选择一个话题");
            return;
        }

        Debug.Log($"[ChatWindow] 显示用户消息: {message}");

        // 显示用户消息
        DisplayMessage(message, true);
        messageInputField.text = "";

        // 调用API获取回复
        isWaitingForResponse = true;
        sendButton.interactable = false;

        Debug.Log($"[ChatWindow] 调用API，话题: {currentTopic}");
        apiManager.GetAIResponse(message, currentTopic, OnAPIResponse);
    }

    private void OnInputSubmit(string text)
    {
        OnSendMessage();
    }

    /// <summary>
    /// 显示消息气泡
    /// </summary>
    private void DisplayMessage(string message, bool isUserMessage)
    {
        Debug.Log($"[ChatWindow] DisplayMessage 被调用 - 消息: {message}, 是用户消息: {isUserMessage}");

        if (messageBubblePrefab == null)
        {
            Debug.LogError("[ChatWindow] messageBubblePrefab未赋值！");
            return;
        }

        if (contentContainer == null)
        {
            Debug.LogError("[ChatWindow] contentContainer未赋值！");
            return;
        }

        GameObject bubbleObj = Instantiate(messageBubblePrefab, contentContainer);
        Debug.Log($"[ChatWindow] 创建气泡对象: {bubbleObj.name}");

        MessageBubble bubble = bubbleObj.GetComponent<MessageBubble>();
        if (bubble != null)
        {
            Debug.Log($"[ChatWindow] 找到MessageBubble组件，设置消息");
            bubble.SetMessage(message, isUserMessage);
        }
        else
        {
            Debug.LogError("[ChatWindow] 气泡对象上没有MessageBubble组件！");
        }

        // 强制更新布局
        Canvas.ForceUpdateCanvases();

        // 自动滚动到底部
        if (scrollRect != null)
        {
            // 延迟一帧后滚动，确保布局已更新
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    /// <summary>
    /// 延迟滚动到底部（确保布局已更新）
    /// </summary>
    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
            Debug.Log("[ChatWindow] 滚动到底部");
        }
    }

    /// <summary>
    /// API响应回调
    /// </summary>
    private void OnAPIResponse(string response)
    {
        Debug.Log($"[ChatWindow] 收到API响应: {response}");

        isWaitingForResponse = false;
        sendButton.interactable = true;

        if (!string.IsNullOrEmpty(response))
        {
            DisplayMessage(response, false);
        }
        else
        {
            DisplayMessage("抱歉，获取回复失败，请重试。", false);
        }
    }

    /// <summary>
    /// 添加话题
    /// </summary>
    private void OnAddTopic()
    {
        string newTopic = "新话题_" + System.DateTime.Now.Ticks;
        AddTopicItem(newTopic);
    }

    /// <summary>
    /// 删除话题
    /// </summary>
    private void OnDeleteTopic()
    {
        if (!string.IsNullOrEmpty(currentTopic) && currentTopics.Contains(currentTopic))
        {
            currentTopics.Remove(currentTopic);
            RefreshTopicUI();
            currentTopic = currentTopics.Count > 0 ? currentTopics[0] : "";
        }
    }

    /// <summary>
    /// 添加话题项
    /// </summary>
    private void AddTopicItem(string topicName)
    {
        if (!currentTopics.Contains(topicName))
        {
            currentTopics.Add(topicName);
            RefreshTopicUI();

            if (string.IsNullOrEmpty(currentTopic))
            {
                SelectTopic(topicName);
            }
        }
    }

    /// <summary>
    /// 刷新话题UI
    /// </summary>
    private void RefreshTopicUI()
    {
        if (topicContainer == null)
        {
            Debug.LogError("[ChatWindow] topicContainer未赋值");
            return;
        }

        // 清空现有话题UI
        foreach (Transform child in topicContainer)
        {
            Destroy(child.gameObject);
        }

        // 重新创建话题按钮
        foreach (string topic in currentTopics)
        {
            if (topicItemPrefab == null)
            {
                Debug.LogError("[ChatWindow] topicItemPrefab未赋值");
                return;
            }

            GameObject topicObj = Instantiate(topicItemPrefab, topicContainer);
            Button topicBtn = topicObj.GetComponent<Button>();
            TextMeshProUGUI topicText = topicObj.GetComponentInChildren<TextMeshProUGUI>();

            if (topicText != null)
                topicText.text = topic;

            if (topicBtn != null)
            {
                topicBtn.onClick.AddListener(() => SelectTopic(topic));
            }
        }
    }

    /// <summary>
    /// 选择话题
    /// </summary>
    private void SelectTopic(string topic)
    {
        currentTopic = topic;
        Debug.Log($"[ChatWindow] 当前话题: {currentTopic}");

        // 清空消息历史
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
