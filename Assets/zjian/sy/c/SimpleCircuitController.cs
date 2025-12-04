using UnityEngine;
using System.Collections.Generic;

public class SimpleCircuitController : MonoBehaviour
{
    public static SimpleCircuitController Instance;

    [Header("电路控制")]
    public bool isSimulating = true;

    [Header("组件列表")]
    public List<Battery> batteries = new List<Battery>();
    public List<LightBulb> lightBulbs = new List<LightBulb>();

    public InputManager input;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindCircuitComponents();
    }

    void Update()
    {
        if (isSimulating)
        {
            SimulateCircuit();
        }
        if (InputManager.Instance != null &&
        Input.GetKeyDown(input.CurrentDisplay))
        {
            isSimulating = !isSimulating;
        }
    }

    private void FindCircuitComponents()
    {
        batteries.Clear();
        lightBulbs.Clear();

        batteries.AddRange(FindObjectsOfType<Battery>());
        lightBulbs.AddRange(FindObjectsOfType<LightBulb>());

        Debug.Log($"找到 {batteries.Count} 个电池和 {lightBulbs.Count} 个灯泡");
    }

    private void SimulateCircuit()
    {
        float totalVoltage = 0f;
        foreach (Battery battery in batteries)
        {
            if (battery.isActive && battery.isOn)
            {
                totalVoltage += battery.voltage;
                Debug.Log($"电池电压: {battery.voltage}V, 总电压: {totalVoltage}V");
            }
        }

        foreach (LightBulb bulb in lightBulbs)
        {
            if (bulb.isActive)
            {
                // 简单模拟：假设灯泡正极接电池正极，负极接GND（0V）
                float positiveVoltage = totalVoltage;
                float negativeVoltage = 0f;

                Debug.Log($"更新灯泡 '{bulb.displayName}': 额定={bulb.ratedVoltage}V, 正极={positiveVoltage}V, 负极={negativeVoltage}V");
                bulb.UpdateBulbState(positiveVoltage, negativeVoltage);
            }
        }
    }
}