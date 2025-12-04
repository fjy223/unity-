using UnityEngine;

public class Wire : MonoBehaviour
{
    public CircuitNode startNode;
    public CircuitNode endNode;

    [Header("外观")]
    public float thickness = 0.05f;
    public int segments = 20;
    public float sag = 0.3f;
    public float minHeight = 11f;

    [Header("箭头")]
    public GameObject arrowObj;      // 拖 Arrow 子物体
    public float arrowSpeed = 3f;

    // ===== 删除标记（新增）=======
    [Header("删除标记")]
    public GameObject deleteMarkerPrefab;  // <--- 在Inspector中拖入预制件
    private GameObject deleteMarkerInstance; // <--- 实例化后的对象
    public float markerHeight = 2f;          // 标记在线上方的高度

    private LineRenderer lr;
    private Vector3[] curve;
    private int arrowIndex = 0;
    private bool currentIsForward = true;

    /* ---------------- 初始化 ---------------- */
    public void Initialize(CircuitNode start, CircuitNode end, float thicknessOverride)
    {
        startNode = start;
        endNode = end;

        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = thicknessOverride;
        lr.material = CircuitConnectionManager.Instance?.validWireMaterial;

        // 简单碰撞器
        var col = gameObject.AddComponent<LineRendererCollider>();
        col.Initialize(lr);

        // 实例化删除标记
        CreateDeleteMarker();

        // 启动箭头
        // if (arrowObj != null) arrowObj.SetActive(true);
    }

    void Start()
    {
        Debug.Log($"Wire {name} arrowObj={arrowObj}");
        if (arrowObj == null) arrowObj = transform.Find("Arrow")?.gameObject;
        if (arrowObj == null) Debug.LogError("没有 Arrow 子物体！");

        // 初始化箭头样式
        LineRenderer lrArrow = arrowObj.GetComponent<LineRenderer>();
        if (lrArrow != null)
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 20f);
            curve.AddKey(10f, 0.0f);
            lrArrow.widthCurve = curve;
            lrArrow.widthMultiplier = 1;
        }
    }

    /* ===== 删除标记相关方法 ===== */
    private void CreateDeleteMarker()
    {
        if (deleteMarkerPrefab == null)
        {
            Debug.LogError("未设置删除标记预制件！请在Wire预制体的Inspector中拖入DeleteMarker预制件。");
            return;
        }

        // 实例化预制件作为子物体
        deleteMarkerInstance = Instantiate(deleteMarkerPrefab, transform);
        deleteMarkerInstance.name = "DeleteMarker";
        deleteMarkerInstance.SetActive(false); // 初始隐藏
    }

    private void UpdateDeleteMarkerPosition()
    {
        if (deleteMarkerInstance == null || startNode == null || endNode == null) return;

        // 计算电线中点
        Vector3 midPoint = (startNode.transform.position + endNode.transform.position) * 0.5f;
        float highestY = Mathf.Max(startNode.transform.position.y, endNode.transform.position.y);

        // 设置标记位置（在电线上方）
        Vector3 markerPos = midPoint;
        markerPos.y = highestY + markerHeight;
        deleteMarkerInstance.transform.position = markerPos;
    }

    public void ShowDeleteMarker()
    {
        if (deleteMarkerInstance != null)
        {
            deleteMarkerInstance.SetActive(true);
            UpdateDeleteMarkerPosition();
        }
    }

    public void HideDeleteMarker()
    {
        if (deleteMarkerInstance != null)
        {
            deleteMarkerInstance.SetActive(false);
        }
    }

    /* ---------------- 每帧更新 ---------------- */
    void Update()
    {
        if (startNode == null || endNode == null) return;

        GenerateCurve();
        UpdateArrow();

        // 如果标记激活，更新位置
        if (deleteMarkerInstance != null && deleteMarkerInstance.activeSelf)
        {
            UpdateDeleteMarkerPosition();
        }
    }

    /* ---------------- 生成曲线 ---------------- */
    void GenerateCurve()
    {
        Vector3 p0 = startNode.transform.position;
        Vector3 p2 = endNode.transform.position;
        Vector3 p1 = (p0 + p2) * 0.5f;
        p1.y = Mathf.Max(p1.y - Vector3.Distance(p0, p2) * sag, minHeight);

        if (curve == null || curve.Length != segments)
            curve = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            curve[i] = Mathf.Pow(1 - t, 2) * p0 +
                       2 * (1 - t) * t * p1 +
                       Mathf.Pow(t, 2) * p2;
        }

        lr.positionCount = segments;
        lr.SetPositions(curve);
    }

    /* ---------------- 箭头 ---------------- */
    void UpdateArrow()
    {
        if (arrowObj == null || curve == null || curve.Length < 2) return;

        int segments = curve.Length;
        float t = (Time.time * arrowSpeed) % 1f;
        int index = (int)(t * (segments - 1));

        Vector3 pos = currentIsForward ? curve[index] : curve[segments - 1 - index];
        Vector3 dir = currentIsForward
            ? (index < segments - 1 ? (curve[index + 1] - pos).normalized : (curve[segments - 1] - curve[segments - 2]).normalized)
            : (index < segments - 1 ? (curve[segments - 2 - index] - pos).normalized : (curve[0] - curve[1]).normalized);

        arrowObj.transform.position = pos;
        arrowObj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    /* ---------------- 右键删除 ---------------- */
    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(1))
            CircuitConnectionManager.Instance?.DeleteWire(this);
    }

    /* ---------------- 电流方向 ---------------- */
    public void SetCurrentDirection(float actualCurrent)
    {
        bool hasCurrent = Mathf.Abs(actualCurrent) > 0.001f;
        bool shouldShow = hasCurrent && FlowToggleUI.Instance != null && FlowToggleUI.Instance.IsShowing();

        if (arrowObj != null)
        {
            arrowObj.SetActive(shouldShow);
            currentIsForward = actualCurrent <= 0;
        }
    }
}

/* ---------------- 简易碰撞体 ---------------- */
[RequireComponent(typeof(MeshCollider))]
public class LineRendererCollider : MonoBehaviour
{
    private LineRenderer lr;
    private MeshCollider col;

    public void Initialize(LineRenderer line)
    {
        lr = line;
        col = GetComponent<MeshCollider>();
    }

    void Update()
    {
        if (lr == null || col == null) return;
        Mesh m = new Mesh();
        lr.BakeMesh(m, useTransform: false);
        col.sharedMesh = m;
    }
}