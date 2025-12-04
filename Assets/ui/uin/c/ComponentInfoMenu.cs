// ComponentInfoMenu.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.ComponentModel;

public class ComponentInfoMenu : MonoBehaviour
{
    [Header("信息展示")]
    public GameObject infoPanel;
    public GameObject editPanel;
    public TextMeshProUGUI componentNameText;
    public TextMeshProUGUI componentInfoText;
    public Button closeButton;

    [Header("通用 4 槽编辑")]
    public TMP_Text[] slotTitles = new TMP_Text[4];
    public TMP_InputField[] slotInputs = new TMP_InputField[4];
    public Button saveButton;

    public Toggle[] slotToggles = new Toggle[4];   // 拖进来

    private GameObject focusedTarget;
    private string[] curTitles = new string[4];
    private float[] curValues = new float[4];
    private System.Action<float[]> onApply;

    private Camera_control cameraControl;

    void Start()
    {
        infoPanel.SetActive(false);  // 开始隐藏菜单
        cameraControl = FindObjectOfType<Camera_control>();
        if (cameraControl != null)
        {
            cameraControl.OnFocusModeEntered += ShowInfo;
            cameraControl.OnFocusModeExited += HideInfo;
        }
        closeButton.onClick.AddListener(cameraControl.ExitFocusMode);
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    void OnDestroy()
    {
        if (cameraControl != null)
        {
            cameraControl.OnFocusModeEntered -= ShowInfo;
            cameraControl.OnFocusModeExited -= HideInfo;
        }
    }

    public void ShowInfo(GameObject go)
    {
        focusedTarget = go;
        infoPanel.SetActive(true);
        editPanel.SetActive(true);   // ← 编辑面板也一起打开

        var bulb = go.GetComponent<LightBulb>();
        var bat = go.GetComponent<Battery>();
        var res = go.GetComponent<Resistor>();
        var sw = go.GetComponent<Switch>();
        var vm = go.GetComponent<Voltmeter>();
        var am = go.GetComponent<Ammeter>();
       

        for (int i = 0; i < 4; i++)
            slotToggles[i].gameObject.SetActive(false);

        if (bulb != null)
        {
            curTitles[0] = "电阻 (Ω)"; curValues[0] = bulb.Resistance;
            curTitles[1] = "额定电压 (V)"; curValues[1] = bulb.RatedVoltage;
            SetupSlots(2);
            onApply = vals =>
            {
                bulb.Resistance = vals[0];
                bulb.RatedVoltage = vals[1];
            };
            componentNameText.text = bulb.displayName;
            componentInfoText.text = bulb.GetParameterSummary();
        }
        else if (bat != null)
        {
            curTitles[0] = "额定电压 (V)"; curValues[0] = bat.Voltage;
            curTitles[1] = "最大电流 (A)"; curValues[1] = bat.MaxCurrent;
            SetupSlots(2);
            onApply = vals =>
            {
                bat.Voltage = vals[0];
                bat.MaxCurrent = vals[1];
            };
            componentNameText.text = bat.displayName;
            componentInfoText.text = bat.GetParameterSummary();
        }
        else if (res != null)
        {
            curTitles[0] = "电阻 (Ω)"; curValues[0] = res.Resistance;
            SetupSlots(1);
            onApply = vals => { res.Resistance = vals[0]; };
            componentNameText.text = res.displayName;
            componentInfoText.text = res.GetParameterSummary();
        }
        else if (sw != null)
        {
            curTitles[0] = "开/关";
            curValues[0] = sw.isClosed ? 1 : 0;
            SetupSlots(1);
            onApply = vals =>
            {
                sw.ToggleSwitch();
            };
            componentNameText.text = sw.displayName;
            componentInfoText.text = sw.GetParameterSummary();
        }
        else if (am !=null)
        {
            SetupSlots(0);
            componentNameText.text = am.displayName;
            componentInfoText.text = am.GetParameterSummary();
        }
        else if (vm != null)
        {
            SetupSlots(0);
            componentNameText.text = vm.displayName;
            componentInfoText.text = vm.GetParameterSummary();
        }
        else
        {
            SetupSlots(0);
            onApply = null;
            componentNameText.text = go.name;
            componentInfoText.text = "无可编辑参数";
        }
    }

    private void SetupSlots(int activeCount)
    {
        for (int i = 0; i < 4; i++)
        {
            bool show = i < activeCount;
            var slotParent = slotTitles[i].transform.parent;
            slotParent.gameObject.SetActive(show);

            if (!show) continue;

            slotTitles[i].text = curTitles[i];

            // 开关用 Toggle
            if (curTitles[i] == "开/关")
            {
                slotInputs[i].gameObject.SetActive(false);
                var toggle = slotToggles[i];
                toggle.gameObject.SetActive(true);
                toggle.isOn = curValues[i] > 0.5f;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(val =>
                {
                    onApply?.Invoke(new float[] { val ? 1 : 0 });
                    ShowInfo(focusedTarget);   // 立即刷新文本
                });
            }
            else
            {
                slotInputs[i].gameObject.SetActive(true);
                slotToggles[i].gameObject.SetActive(false);
                slotInputs[i].text = curValues[i].ToString("F2");
            }
        }
    }

    private void OnSaveClicked()
    {
        if (onApply == null) return;

        float[] vals = new float[4];
        for (int i = 0; i < 4; i++)
            if (!float.TryParse(slotInputs[i].text, out vals[i]))
                vals[i] = curValues[i];

        onApply(vals);
        ShowInfo(focusedTarget); // 立即刷新 UI
        CircuitController.Instance?.ForceSimulationUpdate();
    }

    public void HideInfo() => infoPanel.SetActive(false);

}