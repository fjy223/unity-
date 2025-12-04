using UnityEngine;
using UnityEngine.EventSystems;

public class DeleteMarker : MonoBehaviour
{
    [Header("材质")]
    public Material normalMaterial;
    public Material hoverMaterial;

    private Wire parentWire;
    private Renderer markerRenderer;
    private bool isHovered = false;

    void Awake()
    {
        markerRenderer = GetComponent<Renderer>();
        if (markerRenderer != null && normalMaterial != null)
        {
            markerRenderer.material = normalMaterial;
        }
    }

    /// <summary>
    /// 初始化标记，关联父电线
    /// </summary>
    public void Initialize(Wire wire)
    {
        parentWire = wire;
        // 确保标记在独立层（参考Cude.cs的LayerMask操作）
        gameObject.layer = LayerMask.NameToLayer("DeleteMarker");
    }

    void Update()
    {
        // 只在删除模式下响应
        if (!CircuitBlockPlacer.Instance || !CircuitBlockPlacer.Instance.isDeleteMode)
        {
            if (isHovered) ResetMaterial();
            return;
        }

        // 射线检测（参考Cude.cs的HandleDeleteMode）
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int markerLayerMask = 1 << LayerMask.NameToLayer("DeleteMarker");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, markerLayerMask))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isHovered)
                {
                    isHovered = true;
                    SetMaterial(hoverMaterial);
                }

                // 点击删除（参考Cude.cs的DeleteHoveredObject）
                if (Input.GetMouseButtonDown(0))
                {
                    DeleteParentWire();
                }
            }
            else
            {
                ResetIfHovered();
            }
        }
        else
        {
            ResetIfHovered();
        }
    }

    private void SetMaterial(Material mat)
    {
        if (markerRenderer != null && mat != null)
        {
            markerRenderer.material = mat;
        }
    }

    public void ResetMaterial()
    {
        isHovered = false;
        SetMaterial(normalMaterial);
    }

    private void ResetIfHovered()
    {
        if (isHovered) ResetMaterial();
    }

    /// <summary>
    /// 删除父电线（核心逻辑，参考Cude.cs的DeleteBlock）
    /// </summary>
    private void DeleteParentWire()
    {
        if (parentWire != null)
        {
            // 调用连接管理器删除电线
            CircuitConnectionManager.Instance?.DeleteWire(parentWire);
        }
        else
        {
            Debug.LogWarning("DeleteMarker: 父电线引用为空！");
            // 如果父电线丢失，直接销毁自身
            Destroy(gameObject);
        }
    }
}