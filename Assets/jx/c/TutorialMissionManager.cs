using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

[Serializable]
public class MissionData
{
    [Header("基础信息")]
    public string missionID;
    public string title;
    public string description;

    [Header("指引配置")]
    public GuideConfig guideConfig;

    [Header("完成条件")]
    public MissionConditionTypes.ConditionMethod completionCondition;

    [Header("目标组件（用于点击组件条件）")]
    public List<CircuitComponent> targetComponents = new List<CircuitComponent>();

    [Header("目标节点（用于点击节点条件）")]
    public List<CircuitNode> targetNodes = new List<CircuitNode>();

    [Header("目标按钮（用于ClickedTargetButton条件）")]
    public List<Button> targetButtons = new List<Button>();   // ← 每个任务单独拖

    [Header("完成后的设置")]
    public bool showCompletionPanel = true;
    public string completionMessage = "任务完成！";
}

public class TutorialMissionManager : MonoBehaviour
{
    [Header("完成面板")]
    public GameObject completionPanel;
    public Text completionText;

    [Header("任务配置")]
    public List<MissionData> missionSequence = new List<MissionData>();

    [Header("系统引用")]
    public TutorialGuide guideSystem;
    public UIMissionDisplay missionDisplay;
    public MissionConditionTypes conditionChecker;

    private MissionData currentMission;
    private int currentMissionIndex = 0;
    private bool isMissionRunning = false;
    private bool waitingForNext = false;

    // 添加调试标志
    private bool hasStarted = false;

    void Awake()
    {
        Debug.Log("【TutorialMissionManager】Awake 方法开始");

        // 自动查找关键组件
        if (conditionChecker == null)
        {
            conditionChecker = FindObjectOfType<MissionConditionTypes>();
            Debug.Log($"【TutorialMissionManager】自动查找 conditionChecker: {conditionChecker != null}");
        }

        if (guideSystem == null)
        {
            guideSystem = FindObjectOfType<TutorialGuide>();
            Debug.Log($"【TutorialMissionManager】自动查找 guideSystem: {guideSystem != null}");
        }

        if (missionDisplay == null)
        {
            missionDisplay = FindObjectOfType<UIMissionDisplay>();
            Debug.Log($"【TutorialMissionManager】自动查找 missionDisplay: {missionDisplay != null}");
        }

        Debug.Log("【TutorialMissionManager】Awake 方法结束");
    }

    void Start()
    {
        Debug.Log("【TutorialMissionManager】Start 方法开始");

        // 等待一帧确保所有系统初始化完成
        StartCoroutine(DelayedStart());

        if (completionPanel != null)
            completionPanel.SetActive(false);

        Debug.Log("【TutorialMissionManager】Start 方法结束");
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        yield return null; // 等待一帧

        Debug.Log("【TutorialMissionManager】DelayedStart 开始");

        // 初始化任务显示
        if (missionDisplay != null)
        {
            missionDisplay.Initialize(this);
            Debug.Log("【TutorialMissionManager】初始化任务显示完成");
        }
        else
        {
            Debug.LogWarning("【TutorialMissionManager】missionDisplay 为 null");
        }

        // 开始第一个任务
        StartNextMission();
        hasStarted = true;

        Debug.Log("【TutorialMissionManager】DelayedStart 结束");
    }

    /// <summary>
    /// 开始下一个任务
    /// </summary>
    public void StartNextMission()
    {
        Debug.Log($"【TutorialMissionManager】StartNextMission - 当前索引: {currentMissionIndex}");

        isMissionRunning = false;
        waitingForNext = false;

        // 检查是否还有任务
        if (currentMissionIndex < missionSequence.Count)
        {
            StartMission(missionSequence[currentMissionIndex]);
            currentMissionIndex++;
        }
        else
        {
            Debug.Log("【TutorialMissionManager】所有教学任务完成！");
            // 所有任务完成后的处理
            OnAllMissionsCompleted();
        }
    }

    void StartMission(MissionData mission)
    {
        Debug.Log($"【TutorialMissionManager】开始任务: {mission.title}");

        currentMission = mission;
        isMissionRunning = true;

        // 重置条件检测器状态
        if (conditionChecker != null)
        {
            conditionChecker.ResetAllConditions();
            conditionChecker.ClearTargets();
            // 把「本任务指定的按钮」喂给条件器
            conditionChecker.SetTargetButtons(mission.missionID, mission.targetButtons);

            // 设置目标组件和节点
            if (mission.completionCondition == MissionConditionTypes.ConditionMethod.ClickedTargetComponent ||
                mission.completionCondition == MissionConditionTypes.ConditionMethod.ClickedAnyComponent)
            {
                conditionChecker.SetTargetComponents(mission.targetComponents);
                Debug.Log($"【TutorialMissionManager】设置目标组件，数量: {mission.targetComponents.Count}");
            }
            else if (mission.completionCondition == MissionConditionTypes.ConditionMethod.ClickedTargetNode ||
                     mission.completionCondition == MissionConditionTypes.ConditionMethod.ClickedAnyNode)
            {
                conditionChecker.SetTargetNodes(mission.targetNodes);
                Debug.Log($"【TutorialMissionManager】设置目标节点，数量: {mission.targetNodes.Count}");
            }
        }
        else
        {
            Debug.LogError("【TutorialMissionManager】conditionChecker 为 null");
        }

        // 更新UI显示
        if (missionDisplay != null)
        {
            missionDisplay.UpdateCurrentMission(mission);
            Debug.Log("【TutorialMissionManager】更新UI显示完成");
        }

        // 显示指引
        if (guideSystem != null && mission.guideConfig != null)
        {
            // 合并标题和消息
            string fullMessage = $"{mission.title}\n\n{mission.guideConfig.message}";
            mission.guideConfig.message = fullMessage;

            guideSystem.ShowGuide(mission.guideConfig, OnGuideComplete);
            Debug.Log("【TutorialMissionManager】显示指引完成");
        }
        else
        {
            // 没有指引时直接开始检测条件
            isMissionRunning = true;
            Debug.Log("【TutorialMissionManager】无指引，直接开始条件检测");
        }

        Debug.Log($"【TutorialMissionManager】任务 {mission.title} 启动完成");
    }

    void Update()
    {
        if (!hasStarted) return;

        if (!isMissionRunning || waitingForNext || currentMission == null)
            return;

        // 检测任务完成条件
        if (conditionChecker != null && currentMission.completionCondition != MissionConditionTypes.ConditionMethod.None)
        {
            if (conditionChecker.ExecuteConditionCheck(currentMission.completionCondition))
            {
                Debug.Log($"【TutorialMissionManager】检测到任务完成条件满足: {currentMission.completionCondition}");
                CompleteCurrentMission();
            }
        }
    }

    /// <summary>
    /// 指引完成回调
    /// </summary>
    private void OnGuideComplete()
    {
        Debug.Log("【TutorialMissionManager】指引完成");

        // 如果指引完成就是任务完成条件，则完成任务
        if (currentMission != null && currentMission.completionCondition == MissionConditionTypes.ConditionMethod.None)
        {
            Debug.Log("【TutorialMissionManager】指引完成任务");
            CompleteCurrentMission();
        }
        else
        {
            // 指引完成后开始条件检测
            isMissionRunning = true;
            Debug.Log("【TutorialMissionManager】指引完成，开始条件检测");
        }
    }

    /// <summary>
    /// 完成当前任务
    /// </summary>
    private void CompleteCurrentMission()
    {
        isMissionRunning = false;
        Debug.Log($"【TutorialMissionManager】任务完成：{currentMission.title}");

        // 重要：任务完成时隐藏所有指引
        if (guideSystem != null)
        {
            guideSystem.HideCurrentGuide();
            Debug.Log("【TutorialMissionManager】隐藏指引");
        }

        conditionChecker.UnsubscribeButtonsForMission(currentMission.missionID);

        // 更新任务显示
        if (missionDisplay != null)
        {
            missionDisplay.UpdateMissionStatus(currentMission.missionID, true);
            Debug.Log("【TutorialMissionManager】更新任务状态");
        }

        // 显示完成面板
        if (currentMission.showCompletionPanel && completionPanel != null)
        {
            waitingForNext = true;
            completionPanel.SetActive(true);

            if (completionText != null)
                completionText.text = currentMission.completionMessage;

            Debug.Log("【TutorialMissionManager】显示完成面板");
        }
        else
        {
            // 不显示完成面板，直接进入下一个任务
            Debug.Log("【TutorialMissionManager】直接进入下一个任务");
            StartNextMission();
        }

        // 触发任务完成事件
        OnMissionCompleted(currentMission);
    }

    /// <summary>
    /// 继续下一个任务（由完成面板按钮调用）
    /// </summary>
    public void ContinueToNextMission()
    {
        Debug.Log("【TutorialMissionManager】ContinueToNextMission");

        if (completionPanel != null)
            completionPanel.SetActive(false);

        StartNextMission();
    }

    /// <summary>
    /// 强制跳过当前任务（调试用）
    /// </summary>
    public void SkipCurrentMission()
    {
        Debug.Log($"【TutorialMissionManager】跳过任务: {currentMission?.title}");
        CompleteCurrentMission();
    }

    /// <summary>
    /// 获取所有任务（用于UI显示）
    /// </summary>
    public List<MissionData> GetAllMissions()
    {
        return missionSequence;
    }

    /// <summary>
    /// 获取当前任务索引
    /// </summary>
    public int GetCurrentMissionIndex()
    {
        return currentMissionIndex - 1;
    }

    /// <summary>
    /// 获取当前任务
    /// </summary>
    public MissionData GetCurrentMission()
    {
        return currentMission;
    }

    /// <summary>
    /// 单个任务完成时的回调
    /// </summary>
    private void OnMissionCompleted(MissionData mission)
    {
        Debug.Log($"【TutorialMissionManager】任务完成回调: {mission.title}");
        // 可以在这里添加任务完成的额外处理逻辑
    }

    /// <summary>
    /// 所有任务完成时的回调
    /// </summary>
    private void OnAllMissionsCompleted()
    {
        Debug.Log("【TutorialMissionManager】所有任务完成回调");
        // 可以在这里添加所有任务完成的额外处理逻辑

        // 例如：显示 congratulations 消息，解锁新功能等
        if (guideSystem != null)
        {
            var congratsConfig = new GuideConfig
            {
                message = "恭喜！您已完成所有教学任务！",
            };
            guideSystem.ShowGuide(congratsConfig, null);
        }
    }

    // 添加调试方法
    public void DebugMissionInfo()
    {
        Debug.Log("【TutorialMissionManager】调试信息:");
        Debug.Log($"  - 当前任务索引: {currentMissionIndex}");
        Debug.Log($"  - 总任务数量: {missionSequence.Count}");
        Debug.Log($"  - 任务运行状态: {isMissionRunning}");
        Debug.Log($"  - 等待下一个任务: {waitingForNext}");
        Debug.Log($"  - 当前任务: {currentMission?.title ?? "null"}");
        Debug.Log($"  - conditionChecker: {conditionChecker != null}");
        Debug.Log($"  - guideSystem: {guideSystem != null}");
        Debug.Log($"  - missionDisplay: {missionDisplay != null}");
    }
}