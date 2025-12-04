// MainMenuUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUIw : MonoBehaviour
{
    [Header("主菜单面板")]
    public GameObject mainPanel;   // 拖入主菜单的 Panel

    [Header("按钮")]
    public Button beginMenuBtn;    // 开始自定义
    public Button Teachinglevel; //开启教学关卡
    public Button helpBtn;         // 打开帮助
    public Button settingBtn;      //输入设置
    public Button quitBtn;         // 退出模拟器

    [Header("帮助面板")]
    public HelpUIw helpUI;          // 拖到场景里的 HelpUI
    public jxUI jxUI;
    public InputSettingsUIw inputSettingsUI;


    [Header("场景索引")]
    public int customSceneIndex = 1;     // 自定义场景在Build Settings中的索引
    public int teachingSceneIndex = 2;   // 教学关卡场景在Build Settings中的索引


    public FlowToggleUI FlowToggleUI;

    private void Awake()
    {
        beginMenuBtn.onClick.AddListener(BeginCustom);
        Teachinglevel.onClick.AddListener(BeginTeachingLevel);
        helpBtn.onClick.AddListener(OpenHelp);
        settingBtn.onClick.AddListener(inputsetting);
        quitBtn.onClick.AddListener(QuitGame);

    }
    private void Start()
    {
        // 确保电路控制器已初始化
        if (CircuitController.Instance != null && CircuitController.Instance.IsCircuitComplete())
        {
                FlowToggleUI.ToggleAllArrows(); // 这会设置 showing = true
        }
    }

    private void Update()
    {
    }


    /// <summary>
    /// 开始自定义场景
    /// </summary>
    private void BeginCustom()
    {

        // 隐藏菜单
        mainPanel.SetActive(false);

        // 加载自定义场景
        LoadSceneByIndex(customSceneIndex);
    }

    /// <summary>
    /// 开始教学关卡
    /// </summary>
    private void BeginTeachingLevel()
    {

        // 隐藏菜单
        mainPanel.SetActive(false);
        jxUI.Show();

    }

    /// <summary>
    /// 通过索引加载场景
    /// </summary>
    /// <param name="sceneIndex">场景索引</param>
    private void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"场景索引 {sceneIndex} 超出范围！Build Settings中共有 {SceneManager.sceneCountInBuildSettings} 个场景。");
            return;
        }

        try
        {
            SceneManager.LoadScene(sceneIndex);
            Debug.Log($"正在加载场景索引: {sceneIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载场景失败: {e.Message}");
        }
    }

    private void OpenHelp()
    {
        mainPanel.SetActive(false);
        helpUI.Show();
    }

    private void inputsetting()
    {
        mainPanel.SetActive(false);
        inputSettingsUI.Show();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}