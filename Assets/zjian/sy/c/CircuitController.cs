using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

/// <summary>
/// Unity 运行时直流电路仿真器（节点-支路法）
/// 主要改动：
/// 1. 灯泡内部自动添加正→负隐形单向支路
/// 2. 采用节点电位法求解，电流方向严格正→负
/// </summary>
public class CircuitController : MonoBehaviour
{
    public static CircuitController Instance { get; private set; }

    [Header("实时刷新")]
    public bool isSimulating = true;

    [Header("组件列表")]
    public List<Battery> batteries = new List<Battery>();
    public List<LightBulb> lightBulbs = new List<LightBulb>();
    public List<Resistor> resistors = new List<Resistor>();
    public List<Switch> switches = new List<Switch>();
    public List<Ammeter> ammeters = new List<Ammeter>();
    public List<Voltmeter> voltmeters = new List<Voltmeter>();

    /*———————————— 内部容器 ————————————*/
    private readonly List<CircuitComponent> allComponents = new List<CircuitComponent>();
    //点位缓存
    private Dictionary<int, double> nodePotentialCache = new Dictionary<int, double>();


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
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start() => RefreshComponents();


    private void Update()
    {
        if (isSimulating)
        {
            Solve(); // ✅ 只求解，其他不管
        }
    }

    /*———————————— 公共接口 ————————————*/
    public void ForceSimulationUpdate() => Solve();
    public bool IsCircuitComplete()
    {
        if (batteries.Count == 0) return false;

        // 从每个激活的电池出发，DFS遍历整个电路
        foreach (var battery in batteries.Where(b => b.isActive && b.isOn))
        {
            var visited = new HashSet<int>();
            var posNode = battery.GetPositiveTerminal();
            if (posNode == null) continue;

            // 如果从正极能走到负极，说明形成回路
            if (DFSFindPath(posNode.GetHashCode(), battery.GetNegativeTerminal()?.GetHashCode() ?? -1, visited))
            {
                return true; // 至少一个电池形成回路即可
            }
        }
        return false;
    }
    private bool DFSFindPath(int startId, int targetId, HashSet<int> visited)
    {
        if (startId == targetId) return true;
        visited.Add(startId);

        // 遍历所有电线，查找相邻节点
        foreach (var wireObj in CircuitConnectionManager.Instance.allWires)
        {
            var wire = wireObj.GetComponent<Wire>();
            if (wire == null || wire.startNode == null || wire.endNode == null) continue;

            int fromId = wire.startNode.GetHashCode();
            int toId = wire.endNode.GetHashCode();

            if (fromId == startId && !visited.Contains(toId))
            {
                if (DFSFindPath(toId, targetId, visited)) return true;
            }
            else if (toId == startId && !visited.Contains(fromId))
            {
                if (DFSFindPath(fromId, targetId, visited)) return true;
            }
        }
        return false;
    }

    public void RegisterComponent(CircuitComponent c)
    {
        if (c == null || allComponents.Contains(c)) return;
        allComponents.Add(c);
        if (c is Battery b) batteries.Add(b);
        if (c is LightBulb l)
        {
            lightBulbs.Add(l);
        }
        if (c is Resistor r) resistors.Add(r);
        if (c is Switch s) switches.Add(s);
        if (c is Ammeter a) ammeters.Add(a);
        if (c is Voltmeter v) voltmeters.Add(v);

    }

    public void UnregisterComponent(CircuitComponent c)
    {
        allComponents.Remove(c);
        if (c is Battery b) batteries.Remove(b);
        if (c is LightBulb l)
        {
            lightBulbs.Remove(l);
        }
        if (c is Resistor r) resistors.Remove(r);
        if (c is Switch s) switches.Remove(s);
        if (c is Ammeter a) ammeters.Add(a);
        if (c is Voltmeter v) voltmeters.Add(v);
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
        // 检查电路是否联通
        bool isCircuitComplete = IsCircuitComplete();


        /* 1. 收集全局所有支路 */
        var allBranches = CollectAllBranches();
        if (!allBranches.Any()) return;

        /* 2. 枚举每个电池，把它当一棵独立树的根 */
        foreach (var bat in batteries.Where(b => b.isActive && b.isOn))
        {
            /* 2-1 找到这个电池的正极根节点 */
            var posNode = bat.GetPositiveTerminal();
            var negNode = bat.GetNegativeTerminal();
            if (posNode == null || negNode == null) continue;
            int rootId = posNode.GetHashCode();

            /* 2-2 只拿与这个根连通的支路 */
            var connectedIds = FindConnectedNodes(rootId, allBranches);
            var branches = allBranches
                .Where(b => connectedIds.Contains(b.fromNode) &&
                            connectedIds.Contains(b.toNode))
                .ToList();

            var nodes = BuildNodeList(branches);
            if (nodes.Count < 2) continue;

            /* 2-3 独立求解这个子电路 */
            SolveCircuit(nodes, branches);
            ApplyResults(branches, nodePotentialCache);
        }
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

        /* 1. 真实导线 */
        foreach (var w in FindObjectsOfType<Wire>())
        {
            if (w.startNode == null || w.endNode == null) continue;

            list.Add(new Branch
            {
                fromNode = w.startNode.GetHashCode(),
                toNode = w.endNode.GetHashCode(),
                resistance = 0,   // 导线电阻
                emf = 0,
                component = null      // 导线不属于某个组件
            });
        }

        /* 2. 电池支路：正极→负极 */
        foreach (var b in batteries.Where(b => b.isActive && b.isOn))
        {
            var pos = b.GetPositiveTerminal();
            var neg = b.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            list.Add(new Branch
            {
                fromNode = pos.GetHashCode(),
                toNode = neg.GetHashCode(),
                resistance = 0,
                emf = b.voltage,
                component = b
            });
        }

        /* 3. 灯泡支路：正极→负极 */
        foreach (var bulb in lightBulbs)
        {
            var pos = bulb.GetPositiveTerminal();
            var neg = bulb.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            list.Add(new Branch
            {
                fromNode = neg.GetHashCode(),
                toNode = pos.GetHashCode(),
                resistance = bulb.resistance,
                emf = 0,
                component = bulb
            });
        }

        /* 4. 电阻支路 */
        foreach (var r in resistors.Where(r => r.isActive))
        {
            var pos = r.GetPositiveTerminal();
            var neg = r.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            list.Add(new Branch
            {
                fromNode = pos.GetHashCode(),
                toNode = neg.GetHashCode(),
                resistance = r.resistance,
                emf = 0,
                component = r
            });
        }

        /* 5. 开关支路 */
        foreach (var s in switches.Where(s => s.isActive))
        {
            var pos = s.GetPositiveTerminal();
            var neg = s.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            list.Add(new Branch
            {
                fromNode = pos.GetHashCode(),
                toNode = neg.GetHashCode(),
                resistance = s.CurrentResistance,
                emf = 0,
                component = s
            });
        }
        /* 6. 电流表支路（视为导线） */
        foreach (var am in ammeters.Where(a => a.isActive))
        {
            var pos = am.GetPositiveTerminal();
            var neg = am.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            list.Add(new Branch
            {
                fromNode = neg.GetHashCode(),
                toNode = pos.GetHashCode(),
                resistance = 0.01f,
                emf = 0,
                component = am
            });
        }

        /* 7. 电压表支路（视为开路，不参与电流） */
        foreach (var vm in voltmeters.Where(v => v.isActive))
        {
            var pos = vm.GetPositiveTerminal();
            var neg = vm.GetNegativeTerminal();
            if (pos == null || neg == null) continue;
            list.Add(new Branch
            {
                fromNode = neg.GetHashCode(),
                toNode = pos.GetHashCode(),
                resistance = 999999f,
                emf = 0,
                component = vm
            });
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
    /// <summary>
    /// 返回与给定根节点连通的所有节点 id
    /// </summary>
    private HashSet<int> FindConnectedNodes(int rootId, List<Branch> branches)
    {
        var adj = new Dictionary<int, List<int>>();
        foreach (var b in branches)
        {
            if (!adj.ContainsKey(b.fromNode)) adj[b.fromNode] = new List<int>();
            if (!adj.ContainsKey(b.toNode)) adj[b.toNode] = new List<int>();
            adj[b.fromNode].Add(b.toNode);
            adj[b.toNode].Add(b.fromNode);
        }

        var visited = new HashSet<int>();
        var q = new Queue<int>();
        q.Enqueue(rootId);
        visited.Add(rootId);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (!adj.ContainsKey(cur)) continue;
            foreach (var nxt in adj[cur])
                if (!visited.Contains(nxt))
                {
                    visited.Add(nxt);
                    q.Enqueue(nxt);
                }
        }
        return visited;
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
        /* 9. 计算完电流后，缓存节点电位 */
        nodePotentialCache.Clear();
        foreach (var n in nodes)
            nodePotentialCache[n.id] = n.potential;
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
    private void ApplyResults(List<Branch> branches, Dictionary<int, double> nodePot)
    {

        foreach (var wireObj in CircuitConnectionManager.Instance.allWires)
        {
            Wire wire = wireObj.GetComponent<Wire>();
            if (wire == null || wire.startNode == null || wire.endNode == null) continue;

            int fromId = wire.startNode.GetHashCode();
            int toId = wire.endNode.GetHashCode();

            foreach (var b in branches)
            {
                if ((b.fromNode == fromId && b.toNode == toId))
                {
                    wire.SetCurrentDirection((float)b.current);
                    break;
                }
                else if ((b.fromNode == toId && b.toNode == fromId))
                {
                    wire.SetCurrentDirection(-(float)b.current);
                    break;
                }
            }
        }
        // 2. 计算并写回电压表
        foreach (var vm in voltmeters)
        {
            var pos = vm.GetPositiveTerminal();
            var neg = vm.GetNegativeTerminal();
            if (pos == null || neg == null) continue;

            int posId = pos.GetHashCode();
            int negId = neg.GetHashCode();

            if (nodePot.TryGetValue(posId, out var pPos) &&
                nodePot.TryGetValue(negId, out var pNeg))
            {
                vm.UpdateReading((float)(pPos - pNeg));
            }
        }
        //电阻
        foreach (var branch in branches)
        {
            if (branch.component is LightBulb bulb)
            {
                bulb.current = (float)branch.current;
                bulb.voltage = (float)branch.voltage;
                bulb.UpdateState();
            }
            else if (branch.component is Resistor resistor)
            {
                resistor.UpdateElectricalState((float)branch.current, (float)branch.voltage);
            }
        }
        // 电流表：Branch 已经关联到 Ammeter 实例
        foreach (var b in branches)
        {
            if (b.component is Ammeter am)
                am.UpdateReading((float)b.current);
        }
    }
}