// HelpUI.cs
using UnityEngine;
using UnityEngine.UI;

public class HelpUI : MonoBehaviour
{
    [Header("帮助面板")]
    public GameObject helpPanel;   // 拖到帮助面板

    [Header("返回按钮")]
    public Button backBtn;         // 帮助里的“返回”按钮

    public MainMenuUI main;

    private void Awake()
    {
        backBtn.onClick.AddListener(OnBack);
        helpPanel.SetActive(false);
    }

    public void Show()
    {
        helpPanel.SetActive(true);

    }

    private void OnBack()
    {
        helpPanel.SetActive(false);
        main.mainPanel.SetActive(true);

    }
}