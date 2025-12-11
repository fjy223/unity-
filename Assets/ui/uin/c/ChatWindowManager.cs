using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;

public class ChatWindowManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button addTopicButton;
    [SerializeField] private Button deleteTopicButton;

    [Header("话题滚动视图")]  // ← 新增
    [SerializeField] private ScrollRect topicScrollRect;  // ← 新增
    [SerializeField] private Transform topicContainer;

    [Header("预制体")]
    [SerializeField] private GameObject messageBubblePrefab;
    [SerializeField] private GameObject topicItemPrefab;

    [Header("布局设置")]
    [SerializeField] private float messageSpacing = 10f;
    [SerializeField] private float verticalPadding = 20f;

    private APIManager apiManager;
    private bool isWaitingForResponse = false;
    private VerticalLayoutGroup contentLayoutGroup;
    private VerticalLayoutGroup topicLayoutGroup;  // ← 新增

    private string saveFolder;
    private string saveFileName => Path.Combine(saveFolder, "chat_history.json");
    private List<TopicData> allTopics = new List<TopicData>();

    void Start()
    {
        Debug.Log("[ChatWindow] 初始化开始");

        apiManager = GetComponent<APIManager>();
        if (apiManager == null)
        {
            Debug.LogError("[ChatWindow] 找不到APIManager组件！");
            return;
        }

        // 初始化保存路径
        saveFolder = Path.Combine(Application.persistentDataPath, "ChatHistory");
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        SetupContentContainer();
        SetupScrollRect();
        SetupTopicContainer();  // ← 新增
        BindButtonEvents();

        // 1. 先读盘
        LoadFromDisk();
        // 2. 根据读出来的数据刷新 UI
        PopulateTopicButtons();
        // 3. 把第一个话题的聊天记录刷到聊天窗里
        if (allTopics.Count > 0)
        {
            currentTopic = allTopics[0].topicName;
            SelectTopic(currentTopic);
        }

        Debug.Log("[ChatWindow] 初始化完成");
    }

    // ===== 新增方法：设置话题容器 =====
    private void SetupTopicContainer()
    {
        if (topicContainer == null)
        {
            Debug.LogError("[ChatWindow] topicContainer 未设置！");
            return;
        }

        topicLayoutGroup = topicContainer.GetComponent<VerticalLayoutGroup>();
        if (topicLayoutGroup == null)
        {
            topicLayoutGroup = topicContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        topicLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        topicLayoutGroup.childControlHeight = false;
        topicLayoutGroup.childControlWidth = false;
        topicLayoutGroup.childForceExpandHeight = false;
        topicLayoutGroup.childForceExpandWidth = true;  // 让按钮充满宽度
        topicLayoutGroup.spacing = 5f;
        topicLayoutGroup.padding = new RectOffset(5, 5, 5, 5);

        ContentSizeFitter sizeFitter = topicContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = topicContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 设置滚动视图
        if (topicScrollRect != null)
        {
            topicScrollRect.content = topicContainer as RectTransform;
            topicScrollRect.horizontal = false;
            topicScrollRect.vertical = true;
            topicScrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        Debug.Log("[ChatWindow] 话题容器设置完成");
    }

    // 仅用于启动时：把 json 里所有话题名一次性读到 UI
    private void PopulateTopicButtons()
    {
        // 清空旧按钮
        foreach (Transform t in topicContainer)
            Destroy(t.gameObject);

        // 按 json 顺序建按钮
        for (int i = 0; i < allTopics.Count; i++)
        {
            TopicData tp = allTopics[i];
            GameObject btnGo = Instantiate(topicItemPrefab, topicContainer);

            // 获取按钮上的文本
            Text txt = btnGo.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = tp.topicName;

            // 点按钮就切话题
            string captureName = tp.topicName;
            Button btn = btnGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectTopic(captureName));
            }

            Debug.Log($"[ChatWindow] 创建话题按钮: {tp.topicName}");
        }

        // 重建布局
        if (topicScrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(topicContainer as RectTransform);
        }
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
        contentLayoutGroup.childControlWidth = false;
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

        // 记录用户消息
        AppendMessageToCurrentTopic(message, true);
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

        // 实例化气泡预制体
        GameObject bubbleObj = Instantiate(messageBubblePrefab, contentContainer);
        bubbleObj.name = isUserMessage ? "UserBubble" : "AIBubble";

        // 获取 MessageBubble 脚本
        MessageBubble bubble = bubbleObj.GetComponent<MessageBubble>();
        if (bubble == null)
        {
            bubble = bubbleObj.AddComponent<MessageBubble>();
        }

        // 设置消息
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
            // 记录 AI 回复
            AppendMessageToCurrentTopic(response, false);
        }
        else
        {
            DisplayMessage("抱歉，获取回复失败，请重试。", false);
        }
    }

    private void OnAddTopic()
    {
        // ===== 改动：使用年月日时分秒作为话题名 =====
        System.DateTime now = System.DateTime.Now;
        string newTopic = now.ToString("yyyy-MM-dd HH:mm:ss");

        // 创建新话题数据
        TopicData newTopicData = new TopicData
        {
            topicName = newTopic,
            messages = new List<MessageData>()
        };

        allTopics.Add(newTopicData);
        SaveToDisk();
        PopulateTopicButtons();
        SelectTopic(newTopic);

        Debug.Log($"[ChatWindow] 新增话题: {newTopic}");
    }


    private void OnDeleteTopic()
    {
        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogWarning("[ChatWindow] 没有选中的话题");
            return;
        }

        // 从 allTopics 中删除
        int index = allTopics.FindIndex(t => t.topicName == currentTopic);
        if (index >= 0)
        {
            allTopics.RemoveAt(index);
            SaveToDisk();

            // 切换到其他话题
            if (allTopics.Count > 0)
            {
                currentTopic = allTopics[0].topicName;
            }
            else
            {
                currentTopic = "";
            }

            PopulateTopicButtons();
            SelectTopic(currentTopic);

            Debug.Log($"[ChatWindow] 删除话题成功");
        }
    }

    private string currentTopic = "";  // ← 补充声明

    private void SelectTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogWarning("[ChatWindow] 话题名为空");
            return;
        }

        currentTopic = topic;
        Debug.Log($"[ChatWindow] 当前话题: {currentTopic}");

        // 清空聊天窗
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        // 把该话题的历史消息全部重放一遍
        var topicData = allTopics.Find(t => t.topicName == currentTopic);
        if (topicData != null)
        {
            foreach (var msg in topicData.messages)
            {
                DisplayMessage(msg.text, msg.isUser);
            }
            Debug.Log($"[ChatWindow] 加载话题 '{currentTopic}' 的 {topicData.messages.Count} 条消息");
        }
    }

    private void AppendMessageToCurrentTopic(string text, bool isUser)
    {
        // 找到当前话题
        var topic = allTopics.Find(t => t.topicName == currentTopic);
        if (topic == null)
        {
            Debug.LogError($"[ChatWindow] 找不到话题: {currentTopic}");
            return;
        }

        topic.messages.Add(new MessageData
        {
            isUser = isUser,
            text = text,
            utcTicks = System.DateTime.UtcNow.Ticks
        });

        SaveToDisk();
        Debug.Log($"[ChatWindow] 消息已保存到话题 '{currentTopic}'");
    }

    // ===== 数据结构 =====
    [System.Serializable]
    public class TopicData
    {
        public string topicName;
        public List<MessageData> messages;
    }

    [System.Serializable]
    public class MessageData
    {
        public bool isUser;
        public string text;
        public long utcTicks;
    }

    [System.Serializable]
    private class SerializationWrapper
    {
        public List<TopicData> list;
    }

    // ===== 文件操作 =====
    private void SaveToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(new SerializationWrapper { list = allTopics }, true);
            File.WriteAllText(saveFileName, json, System.Text.Encoding.UTF8);
            Debug.Log($"[ChatWindow] 数据已保存到: {saveFileName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChatWindow] 保存失败: {e.Message}");
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(saveFileName))
            {
                Debug.Log($"[ChatWindow] 文件不存在，创建默认数据");
                allTopics = new List<TopicData>
                {
                    new TopicData
                    {
                        topicName = "默认话题",
                        messages = new List<MessageData>
                        {
                            new MessageData
                            {
                                isUser = false,
                                text = "你好，我是AI助手。有什么我可以帮助你的吗？",
                                utcTicks = System.DateTime.UtcNow.Ticks
                            }
                        }
                    }
                };
                SaveToDisk();
                return;
            }

            string json = File.ReadAllText(saveFileName, System.Text.Encoding.UTF8);
            SerializationWrapper wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
            allTopics = wrapper.list ?? new List<TopicData>();

            Debug.Log($"[ChatWindow] 从文件加载了 {allTopics.Count} 个话题");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChatWindow] 加载失败: {e.Message}");
            allTopics = new List<TopicData>();
        }
    }
}
