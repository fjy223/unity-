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

    [Header("布局设置")]
    [SerializeField] private float messageSpacing = 10f;
    [SerializeField] private float verticalPadding = 20f;

    private APIManager apiManager;
    private bool isWaitingForResponse = false;
    private VerticalLayoutGroup contentLayoutGroup;

    void Start()
    {
        Debug.Log("[ChatWindow] 初始化开始");

        apiManager = GetComponent<APIManager>();
        if (apiManager == null)
        {
            Debug.LogError("[ChatWindow] 找不到APIManager组件！");
            return;
        }

        SetupContentContainer();
        SetupScrollRect();
        BindButtonEvents();
        InitializeTopics();

        AddTestBubbles();//测试文本

        Debug.Log("[ChatWindow] 初始化完成");
    }

    private void AddTestBubbles()//测试文本
    {
        DisplayMessage("你好，我是AI助手。有什么我可以帮助你的吗？", false);
        DisplayMessage("这是一条用户消息，用来测试气泡的显示效果。", true);
        DisplayMessage("这是一条很长很长很长的AI回复消息，用来测试多行文本的换行效果和气泡的自适应大小。希望能正确显示。", false);
    }
    private void SetupContentContainer()
    {
        contentLayoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (contentLayoutGroup == null)
        {
            contentLayoutGroup = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        contentLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        contentLayoutGroup.childControlHeight = false;
        contentLayoutGroup.childControlWidth = false;  // 改为 false，让气泡自己控制宽度
        contentLayoutGroup.childForceExpandHeight = false;
        contentLayoutGroup.childForceExpandWidth = false;
        contentLayoutGroup.spacing = messageSpacing;
        contentLayoutGroup.padding = new RectOffset(10, 10, (int)verticalPadding, (int)verticalPadding);

        ContentSizeFitter sizeFitter = contentContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void SetupScrollRect()
    {
        if (scrollRect != null)
        {
            scrollRect.content = contentContainer as RectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }
    }

    private void BindButtonEvents()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);

        if (addTopicButton != null)
            addTopicButton.onClick.AddListener(OnAddTopic);

        if (deleteTopicButton != null)
            deleteTopicButton.onClick.AddListener(OnDeleteTopic);

        if (messageInputField != null)
            messageInputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void InitializeTopics()
    {
        AddTopicItem("软件使用");
        AddTopicItem("功能介绍");
        AddTopicItem("故障排查");
    }

    private void OnSendMessage()
    {
        string message = messageInputField.text.Trim();

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("[ChatWindow] 消息为空");
            return;
        }

        if (isWaitingForResponse)
        {
            Debug.LogWarning("[ChatWindow] 正在等待API响应");
            return;
        }

        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogWarning("[ChatWindow] 请先选择一个话题");
            return;
        }

        DisplayMessage(message, true);
        messageInputField.text = "";

        isWaitingForResponse = true;
        sendButton.interactable = false;

        apiManager.GetAIResponse(message, currentTopic, OnAPIResponse);
    }

    private void OnInputSubmit(string text)
    {
        OnSendMessage();
    }

    private void DisplayMessage(string message, bool isUserMessage)
    {
        if (messageBubblePrefab == null || contentContainer == null)
        {
            Debug.LogError("[ChatWindow] 缺少必要的引用");
            return;
        }

        // 实例化气泡
        GameObject bubbleObj = Instantiate(messageBubblePrefab, contentContainer);
        bubbleObj.name = isUserMessage ? "UserBubble" : "AIBubble";

        // 获取RectTransform
        RectTransform bubbleRect = bubbleObj.GetComponent<RectTransform>();
        if (bubbleRect == null)
        {
            bubbleRect = bubbleObj.AddComponent<RectTransform>();
        }

        // 设置气泡在容器中的锚点（全宽）
        bubbleRect.anchorMin = new Vector2(0, 1);
        bubbleRect.anchorMax = new Vector2(1, 1);
        bubbleRect.pivot = new Vector2(0.5f, 1);

        // 设置消息
        MessageBubble bubble = bubbleObj.GetComponent<MessageBubble>();
        if (bubble == null)
        {
            bubble = bubbleObj.AddComponent<MessageBubble>();
        }
        bubble.SetMessage(message, isUserMessage);

        Debug.Log($"[ChatWindow] 显示消息 - 用户消息: {isUserMessage}");

        StartCoroutine(UpdateLayoutAndScroll());
    }

    private System.Collections.IEnumerator UpdateLayoutAndScroll()
    {
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer as RectTransform);
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }

        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void OnAPIResponse(string response)
    {
        Debug.Log($"[ChatWindow] 收到API响应");

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

    private void OnAddTopic()
    {
        string newTopic = "新话题_" + System.DateTime.Now.Ticks;
        AddTopicItem(newTopic);
    }

    private void OnDeleteTopic()
    {
        if (!string.IsNullOrEmpty(currentTopic) && currentTopics.Contains(currentTopic))
        {
            currentTopics.Remove(currentTopic);
            RefreshTopicUI();
            currentTopic = currentTopics.Count > 0 ? currentTopics[0] : "";
        }
    }

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

    private void RefreshTopicUI()
    {
        if (topicContainer == null)
            return;

        foreach (Transform child in topicContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string topic in currentTopics)
        {
            if (topicItemPrefab == null)
                return;

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

    private void SelectTopic(string topic)
    {
        currentTopic = topic;
        Debug.Log($"[ChatWindow] 当前话题: {currentTopic}");

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
