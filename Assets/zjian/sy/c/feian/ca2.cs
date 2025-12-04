using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unity 运行时直流电路仿真器（节点-支路法）
/// 主要改动：
/// 1. 灯泡内部自动添加正→负隐形单向支路
/// 2. 采用节点电位法求解，电流方向严格正→负
/// </summary>
public class Ca2 : MonoBehaviour
{
    public static CircuitController Instance { get; private set; }

    [Header("实时刷新")]
    public bool isSimulating = true;

    [Header("组件列表")]
    public List<Battery> batteries = new List<Battery>();
    public List<LightBulb> lightBulbs = new List<LightBulb>();

    /*———————————— 内部容器 ————————————*/
    private readonly List<CircuitComponent> allComponents = new List<CircuitComponent>();

    /*———————————— 新增：灯泡内部隐形支路缓存 ————————————*/
    // 【修改点】Key=灯泡实例，Value=隐形 Branch 对象
    private readonly Dictionary<LightBulb, Branch> internalBranches = new Dictionary<LightBulb, Branch>();

    /*———————————— 数据结构 ————————————*/
    private class Branch
    {
        // 添加组件引用
        public CircuitComponent component;

        public int fromNode;   // 起始节点 id（正极）
        public int toNode;     // 终止节点 id（负极）
        public double resistance; // Ω
        public double emf;        // V（电池电动势，灯泡为 0）
        public double current;    // A（求解结果）
        public double voltage;    // V（求解结果）
    }

    private class Node
    {
        public int id;
        public double potential;  // φ (V)
    }

    /*———————————— 生命周期 ————————————*/
    private void Awake()
    {
       // if (Instance == null) Instance = this;
       // else { Destroy(gameObject); return; }
    }

    private void Start() => RefreshComponents();

    private void Update()
    {
        if (isSimulating) Solve();
    }

    /*———————————— 公共接口 ————————————*/
    public void ForceSimulationUpdate() => Solve();
    public bool IsCircuitComplete() =>
        batteries.TrueForAll(b =>
            b.GetPositiveTerminal()?.isConnected == true &&
            b.GetNegativeTerminal()?.isConnected == true) &&
        lightBulbs.TrueForAll(l =>
            l.GetPositiveTerminal()?.isConnected == true &&
            l.GetNegativeTerminal()?.isConnected == true);

    public void RegisterComponent(CircuitComponent c)
    {
        if (c == null || allComponents.Contains(c)) return;
        allComponents.Add(c);
        if (c is Battery b) batteries.Add(b);
        if (c is LightBulb l)
        {
            lightBulbs.Add(l);
            CreateInternalBranch(l); // 【修改点】
        }
    }

    public void UnregisterComponent(CircuitComponent c)
    {
        allComponents.Remove(c);
        if (c is Battery b) batteries.Remove(b);
        if (c is LightBulb l)
        {
            lightBulbs.Remove(l);
            DestroyInternalBranch(l); // 【修改点】
        }
    }

    /*———————————— 新增：灯泡内部支路管理 ————————————*/
    /// <summary>
    /// 【修改点】为灯泡创建一条隐形单向支路（正极→负极）
    /// </summary>
    private void CreateInternalBranch(LightBulb bulb)
    {
        if (internalBranches.ContainsKey(bulb)) return;

        var pos = bulb.GetPositiveTerminal();
        var neg = bulb.GetNegativeTerminal();
        if (pos == null || neg == null) return;

        internalBranches[bulb] = new Branch
        {
            fromNode = pos.GetHashCode(),
            toNode = neg.GetHashCode(),
            resistance = bulb.resistance,
            emf = 0
        };

        Debug.Log($"[CircuitController] 为灯泡 {bulb.name} 创建内部正→负支路");
    }

    /// <summary>
    /// 【修改点】灯泡被移除时，同步销毁内部支路
    /// </summary>
    private void DestroyInternalBranch(LightBulb bulb)
    {
        if (internalBranches.Remove(bulb))
            Debug.Log($"[CircuitController] 移除灯泡 {bulb.name} 的内部支路");
    }

    /*———————————— 内部刷新 ————————————*/
    private void RefreshComponents()
    {
        allComponents.Clear();
        batteries.Clear();
        lightBulbs.Clear();
        foreach (var c in FindObjectsOfType<CircuitComponent>(true))
            RegisterComponent(c);
    }

    /*———————————— 主求解流程 ————————————*/
    private void Solve()
    {
        var branches = CollectAllBranches(); // 【修改点】
        var nodes = BuildNodeList(branches);
        if (nodes.Count < 2 || branches.Count == 0) return;

        SolveCircuit(nodes, branches);
        ApplyResults(branches);
    }

    /*———————————— 收集所有支路 ————————————*/
    /// <summary>
    /// 【修改点】把三类对象统一转为 Branch：
    /// 1. Wire
    /// 2. Battery（内部正→负）
    /// 3. LightBulb（内部正→负）
    /// </summary>
    private List<Branch> CollectAllBranches()
    {
        var list = new List<Branch>();

        // 1. 电线支路
        foreach (var w in FindObjectsOfType<Wire>())
        {
            int a = w.startNode.GetHashCode();
            int b = w.endNode.GetHashCode();
            list.Add(new Branch
            {
                fromNode = a,
                toNode = b,
                resistance = 0.01,
                component = null  // 电线没有特定组件
            });
        }

        // 2. 电池支路
        foreach (var b in batteries.Where(b => b.isActive && b.isOn))
        {
            int p = b.GetPositiveTerminal().GetHashCode();
            int n = b.GetNegativeTerminal().GetHashCode();
            list.Add(new Branch
            {
                fromNode = p,
                toNode = n,
                emf = b.voltage,
                resistance = 0,
                component = b  // 关联电池组件
            });
        }

        // 3. 灯泡支路 - 使用当前节点信息
        foreach (var bulb in lightBulbs)
        {
            var pos = bulb.GetPositiveTerminal();
            var neg = bulb.GetNegativeTerminal();
            if (pos != null && neg != null)
            {
                list.Add(new Branch
                {
                    fromNode = pos.GetHashCode(),
                    toNode = neg.GetHashCode(),
                    resistance = bulb.resistance,
                    emf = 0,
                    component = bulb  // 关联灯泡组件
                });
            }
        }

        return list;
    }

    /*———————————— 构造节点列表 ————————————*/
    private List<Node> BuildNodeList(List<Branch> branches)
    {
        var ids = new HashSet<int>();
        foreach (var b in branches)
        {
            ids.Add(b.fromNode);
            ids.Add(b.toNode);
        }
        return ids.Select(id => new Node { id = id }).ToList();
    }

    /*———————————— 节点-支路法求解 ————————————*/
    private void SolveCircuit(List<Node> nodes, List<Branch> branches)
    {
        int nNode = nodes.Count;
        int nBranch = branches.Count;

        /* 1. id -> index 映射 */
        var id2idx = new Dictionary<int, int>();
        for (int i = 0; i < nodes.Count; i++) id2idx[nodes[i].id] = i;

        /* 2. 关联矩阵 A (nNode × nBranch) */
        double[,] A = new double[nNode, nBranch];
        for (int b = 0; b < nBranch; b++)
        {
            int i = id2idx[branches[b].fromNode];
            int j = id2idx[branches[b].toNode];
            A[i, b] = 1;
            A[j, b] = -1;
        }

        /* 3. 去掉参考节点 (φ=0) */
        int refIdx = 0;
        double[,] Ared = new double[nNode - 1, nBranch];
        for (int r = 0, rr = 0; r < nNode; r++)
        {
            if (r == refIdx) continue;
            for (int c = 0; c < nBranch; c++) Ared[rr, c] = A[r, c];
            rr++;
        }

        /* 4. 阻抗对角阵 Z⁻¹ 与右端向量 rhs */
        double[] invZ = new double[nBranch];
        double[] rhs = new double[nNode - 1];
        for (int b = 0; b < nBranch; b++)
        {
            invZ[b] = 1.0 / Math.Max(branches[b].resistance, 1e-6);
            double rhsVal = branches[b].emf * invZ[b];
            for (int r = 0; r < nNode - 1; r++)
                rhs[r] += Ared[r, b] * rhsVal;
        }

        /* 5. 电导矩阵 G = Ared * Z⁻¹ * Aredᵀ */
        double[,] G = new double[nNode - 1, nNode - 1];
        for (int i = 0; i < nNode - 1; i++)
            for (int j = 0; j < nNode - 1; j++)
                for (int k = 0; k < nBranch; k++)
                    G[i, j] += Ared[i, k] * invZ[k] * Ared[j, k];

        /* 6. 解线性方程组 G φ = rhs */
        double[] phi = SolveLinear(G, rhs);

        /* 7. 回写节点电位 */
        for (int i = 0, ii = 0; i < nNode; i++)
        {
            if (i == refIdx) nodes[i].potential = 0;
            else nodes[i].potential = phi[ii++];
        }

        /* 8. 计算支路电流 I = (E + Δφ)/Z */
        foreach (var b in branches)
        {
            double dPhi = nodes[id2idx[b.toNode]].potential -
                          nodes[id2idx[b.fromNode]].potential;
            b.current = (b.emf + dPhi) / Math.Max(b.resistance, 1e-6);
            b.voltage = b.current * b.resistance;
        }
    }

    /*———————————— 简易高斯消元 ————————————*/
    private static double[] SolveLinear(double[,] A, double[] b)
    {
        int n = b.Length;
        double[,] M = new double[n, n + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++) M[i, j] = A[i, j];
            M[i, n] = b[i];
        }

        for (int col = 0; col < n; col++)
        {
            /* 选主元 */
            int max = col;
            for (int r = col + 1; r < n; r++)
                if (Math.Abs(M[r, col]) > Math.Abs(M[max, col])) max = r;
            if (max != col)
                for (int c = col; c <= n; c++)
                    (M[col, c], M[max, c]) = (M[max, c], M[col, c]);

            /* 消元 */
            for (int r = col + 1; r < n; r++)
            {
                double f = M[r, col] / M[col, col];
                for (int c = col; c <= n; c++) M[r, c] -= M[col, c] * f;
            }
        }

        /* 回代 */
        double[] x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            x[i] = M[i, n];
            for (int j = i + 1; j < n; j++) x[i] -= M[i, j] * x[j];
            x[i] /= M[i, i];
        }
        return x;
    }

    /*———————————— 结果回写 ————————————*/
    private void ApplyResults(List<Branch> branches)
    {
        foreach (var branch in branches)
        {
            // 只处理灯泡组件
            if (branch.component is LightBulb bulb)
            {
                bulb.current = (float)branch.current;
                bulb.voltage = (float)branch.voltage;
                bulb.UpdateState();
            }
        }
    }
}