// MainMenuUI.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("主菜单面板")]
    public GameObject mainPanel;   // 拖入主菜单的 Panel

    [Header("按钮")]
    public Button closeMenuBtn;    // 关闭菜单
    public Button helpBtn;         // 打开帮助
    public Button settingBtn;      //输入设置
    public Button quitBtn;         // 返回主菜单
    public Button rw;//任务菜单

    [Header("帮助面板")]
    public HelpUI helpUI;          // 拖到场景里的 HelpUI
    public InputSettingsUI inputSettingsUI;
    public UIMissionDisplay rwUI;

    private void Awake()
    {
        closeMenuBtn.onClick.AddListener(CloseMenu);
        helpBtn.onClick.AddListener(OpenHelp);
        settingBtn.onClick.AddListener(inputsetting);
        quitBtn.onClick.AddListener(QuitGame);
        rw.onClick.AddListener(Openrw);

        // 默认隐藏
        mainPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleMainMenu();
        }
    }

    private void ToggleMainMenu()
    {
        bool nowOpen = !mainPanel.activeSelf;
        mainPanel.SetActive(nowOpen);

    }

    private void CloseMenu()
    {
        mainPanel.SetActive(false);
    }

    private void OpenHelp()
    {
        mainPanel.SetActive(false);
        helpUI.Show();
    }

    private void Openrw()
    {
        mainPanel.SetActive(false);
        rwUI.Show();
    }

    private void inputsetting() {
        mainPanel.SetActive(false);
        inputSettingsUI.Show();
    }

    private void QuitGame()
    {

        // 隐藏菜单
        mainPanel.SetActive(false);

        // 加载自定义场景
        LoadSceneByIndex(0);
    }

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
}