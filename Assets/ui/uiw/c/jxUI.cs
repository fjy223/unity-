using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class jxUI : MonoBehaviour
{
    [Header("帮助面板")]
    public GameObject jxPanel;   // 拖到帮助面板

    [Header("返回按钮")]
    public Button backBtn;         // 帮助里的“返回”按钮

    public MainMenuUIw main;

    private void Awake()
    {
        backBtn.onClick.AddListener(OnBack);
        jxPanel.SetActive(false);
    }

    public void Show()
    {
        jxPanel.SetActive(true);
    }

    private void OnBack()
    {
        jxPanel.SetActive(false);
        main.mainPanel.SetActive(true);
    }

    public void jx1() {
        SceneManager.LoadScene(2);//加载教学关1
    }//关卡跳转

    public void jx2() {
        SceneManager.LoadScene(3);//加载教学关2
    }
}
