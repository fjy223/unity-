using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class UIMissionDisplay : MonoBehaviour
{
    [Header("任务面板")]
    public GameObject rwPanel;

    [Header("UI组件")]
    public Text menuTitleText;
    public ScrollRect missionScrollRect;
    public Transform missionListContainer;
    public GameObject missionEntryPrefab;

    [Header("返回按钮")]
    public Button backBtn;

    [Header("颜色设置")]
    public Color normalColor = Color.white;
    public Color completedColor = Color.gray;
    public Color currentMissionColor = Color.yellow; // 当前任务高亮颜色

    [Header("布局设置")]
    public float entrySpacing = 10f;
    public float entryHeight = 80f;

    private List<GameObject> missionEntries = new List<GameObject>();
    private TutorialMissionManager missionManager;

    void Start()
    {
        rwPanel.SetActive(false);

        // 确保容器有合适的布局组件
        EnsureContainerLayout();
    }

    /// <summary>
    /// 初始化任务显示
    /// </summary>
    public void Initialize(TutorialMissionManager manager)
    {
        missionManager = manager;
    }

    /// <summary>
    /// 确保容器有合适的布局组件
    /// </summary>
    private void EnsureContainerLayout()
    {
        if (missionListContainer != null)
        {
            // 添加或获取Vertical Layout Group
            VerticalLayoutGroup layoutGroup = missionListContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = missionListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            // 设置布局参数
            layoutGroup.spacing = entrySpacing;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            // 添加或获取Content Size Fitter
            ContentSizeFitter sizeFitter = missionListContainer.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = missionListContainer.gameObject.AddComponent<ContentSizeFitter>();
            }

            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    /// <summary>
    /// 显示所有任务
    /// </summary>
    public void ShowAllMissions()
    {
        if (missionManager == null)
        {
            Debug.LogError("任务管理器未初始化，请先调用Initialize方法");
            return;
        }

        // 清空现有任务列表
        ClearMissionList();

        // 设置菜单标题
        if (menuTitleText != null)
            menuTitleText.text = "任务菜单";

        // 获取所有任务数据
        List<MissionData> allMissions = missionManager.GetAllMissions();

        if (allMissions.Count == 0)
        {
            Debug.LogWarning("没有找到任何任务");
            return;
        }

        Debug.Log($"找到 {allMissions.Count} 个任务");

        // 创建任务条目
        for (int i = 0; i < allMissions.Count; i++)
        {
            MissionData mission = allMissions[i];
            bool isCompleted = i < missionManager.GetCurrentMissionIndex();
            bool isCurrent = i == missionManager.GetCurrentMissionIndex();

            CreateMissionEntry(mission, isCompleted, isCurrent);
        }

        // 更新滚动视图布局
        UpdateScrollViewLayout();
    }

    /// <summary>
    /// 创建任务条目
    /// </summary>
    private void CreateMissionEntry(MissionData mission, bool isCompleted, bool isCurrent)
    {
        if (missionEntryPrefab == null)
        {
            Debug.LogError("任务条目预制体未设置");
            return;
        }

        if (missionListContainer == null)
        {
            Debug.LogError("任务列表容器未设置");
            return;
        }

        // 实例化预制体
        GameObject entryObj = Instantiate(missionEntryPrefab, missionListContainer);
        missionEntries.Add(entryObj);

        // 确保条目有合适的高度
        RectTransform rectTransform = entryObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, entryHeight);
        }

        // 添加或获取Layout Element
        LayoutElement layoutElement = entryObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = entryObj.AddComponent<LayoutElement>();
        }
        layoutElement.minHeight = entryHeight;
        layoutElement.preferredHeight = entryHeight;

        // 直接设置文本内容和状态
        SetupMissionEntry(entryObj, mission, isCompleted, isCurrent);

        Debug.Log($"创建任务条目: {mission.title}, 完成: {isCompleted}, 当前: {isCurrent}");
    }

    /// <summary>
    /// 设置任务条目内容
    /// </summary>
    private void SetupMissionEntry(GameObject entryObj, MissionData mission, bool isCompleted, bool isCurrent)
    {
        // 查找文本组件 - 使用UnityEngine.UI.Text
        Text titleText = FindTextComponent(entryObj, "TitleText");

        if (titleText != null)
        {
            // 设置任务信息：标题用括号标上，后面跟着描述
            string displayText = $"【{mission.title}】 {mission.description}";
            titleText.text = displayText;

            // 根据状态设置颜色
            if (isCompleted)
                titleText.color = completedColor;
            else if (isCurrent)
                titleText.color = currentMissionColor;
            else
                titleText.color = normalColor;

            // 确保文本支持多行和自动换行
            titleText.supportRichText = true;
            titleText.resizeTextForBestFit = false;
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;

            Debug.Log($"成功设置任务文本: {displayText}");
        }
        else
        {
            Debug.LogError($"在预制体 {entryObj.name} 中找不到TitleText组件");
        }

        // 如果有目标文本组件，也设置它
        Text objectiveText = FindTextComponent(entryObj, "ObjectiveText");
        if (objectiveText != null)
        {
            objectiveText.text = GetMissionObjective(mission);

            // 根据状态设置颜色
            if (isCompleted)
                objectiveText.color = completedColor;
            else if (isCurrent)
                objectiveText.color = currentMissionColor;
            else
                objectiveText.color = normalColor;

            // 确保目标文本也支持多行
            objectiveText.supportRichText = true;
            objectiveText.resizeTextForBestFit = false;
            objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            objectiveText.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    /// <summary>
    /// 查找文本组件 - 适配旧版Unity UI Text
    /// </summary>
    private Text FindTextComponent(GameObject parent, string childName)
    {
        // 先尝试直接查找
        Text textComp = parent.GetComponent<Text>();
        if (textComp != null) return textComp;

        // 如果没有，尝试在子对象中查找
        Transform child = parent.transform.Find(childName);
        if (child != null)
        {
            textComp = child.GetComponent<Text>();
        }

        // 如果还是没找到，尝试通过名称查找
        if (textComp == null)
        {
            Text[] allTexts = parent.GetComponentsInChildren<Text>(true);
            textComp = allTexts.FirstOrDefault(t => t.name == childName);
        }

        return textComp;
    }

    /// <summary>
    /// 获取任务目标描述
    /// </summary>
    private string GetMissionObjective(MissionData mission)
    {
        if (mission.completionCondition == MissionConditionTypes.ConditionMethod.None)
            return "跟随指引完成操作";

        // 根据完成条件返回对应的目标描述
        switch (mission.completionCondition)
        {
            case MissionConditionTypes.ConditionMethod.EscPressed:
                return "按下ESC键";
            case MissionConditionTypes.ConditionMethod.ClickedrwcdButton:
                return "点击任务菜单按钮";
            default:
                return mission.completionCondition.ToString();
        }
    }

    /// <summary>
    /// 更新当前任务显示
    /// </summary>
    public void UpdateCurrentMission(MissionData mission)
    {
        // 刷新整个任务列表以更新状态
        ShowAllMissions();
    }

    /// <summary>
    /// 更新滚动视图布局
    /// </summary>
    private void UpdateScrollViewLayout()
    {
        // 强制更新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(missionListContainer as RectTransform);
        Canvas.ForceUpdateCanvases();

        // 重置滚动位置到顶部
        if (missionScrollRect != null)
        {
            missionScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 清空任务列表
    /// </summary>
    private void ClearMissionList()
    {
        foreach (var entry in missionEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        missionEntries.Clear();
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    public void UpdateMissionStatus(string missionId, bool isCompleted)
    {
        // 刷新整个任务列表以更新状态
        ShowAllMissions();
    }

    /// <summary>
    /// 显示任务完成
    /// </summary>
    public void ShowCompletion(string missionId)
    {
        UpdateMissionStatus(missionId, true);
    }

    public MainMenuUI main;

    private void Awake()
    {
        backBtn.onClick.AddListener(OnBack);
        rwPanel.SetActive(false);
    }

    public void Show()
    {
        rwPanel.SetActive(true);
        ShowAllMissions(); // 显示时刷新任务列表
    }

    private void OnBack()
    {
        rwPanel.SetActive(false);
        if (main != null && main.mainPanel != null)
            main.mainPanel.SetActive(true);
    }
}