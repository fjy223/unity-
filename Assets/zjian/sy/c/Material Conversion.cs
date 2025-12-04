using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Material_Conversion : MonoBehaviour
{
    [Header("节点材质")]
    [Tooltip("正极节点材质")] public Material positiveNodeMaterial;
    [Tooltip("负极节点材质")] public Material negativeNodeMaterial;
    [Tooltip("普通节点材质")] public Material normalNodeMaterial;
    [Tooltip("连接中材质")] public Material connectingMaterial;

    [Header("方块材质")]
    [Tooltip("选中材质")] public Material selectedMaterial;
    [Tooltip("有效放置材质")] public Material validPlacementMaterial;
    [Tooltip("无效放置材质")] public Material invalidPlacementMaterial;
    [Tooltip("描边材质")] public Material outlineMaterial;
    [Tooltip("描边宽度")] public float outlineWidth = 0.05f;

    // 存储每个组件的原始材质
    private Dictionary<GameObject, Material[]> originalMaterials = new Dictionary<GameObject, Material[]>();

    public CircuitBlockPlacer CircuitBlockPlacer1;
    public Camera_control camera_control;

    void Start()
    {
        if (camera_control == null)
            camera_control = FindObjectOfType<Camera_control>();
        if (CircuitBlockPlacer1 == null)
            CircuitBlockPlacer1 = FindObjectOfType<CircuitBlockPlacer>();
    }

    /// <summary>
    /// 获取组件的原始材质
    /// </summary>
    public Material[] GetOriginalMaterials(GameObject obj)
    {
        if (originalMaterials.ContainsKey(obj))
        {
            return originalMaterials[obj];
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterials[obj] = renderer.materials;
            return renderer.materials;
        }

        return null;
    }

    /// <summary>
    /// 恢复组件的默认材质（从组件自身读取）
    /// </summary>
    public void RestoreOriginalMaterials(GameObject obj)
    {
        CircuitComponent component = obj.GetComponent<CircuitComponent>();
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null || component == null) return;

        // 如果是灯泡，使用动态默认材质
        if (component is LightBulb bulb)
        {
            renderer.material = bulb.GetCurrentDefaultMaterial();
        }
        else
        {
            // 其他组件使用自身 defaultMaterial
            if (component.defaultMaterial != null)
            {
                renderer.material = component.defaultMaterial;
            }
        }

        // 子物体也恢复（可选）
        foreach (Transform child in obj.transform)
        {
            CircuitComponent childComponent = child.GetComponent<CircuitComponent>();
            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null && childComponent != null)
            {
                if (childComponent is LightBulb childBulb)
                {
                    childRenderer.material = childBulb.GetCurrentDefaultMaterial();
                }
                else if (childComponent.defaultMaterial != null)
                {
                    childRenderer.material = childComponent.defaultMaterial;
                }
            }
        }
    }

    /// <summary>
    /// 更新方块视觉效果（保持不变）
    /// </summary>
    public void UpdateBlockVisuals()
    {
        if (CircuitBlockPlacer1.selectedBlock != null)
        {
            bool isValidPosition = CircuitBlockPlacer1.CheckPlacementValid(CircuitBlockPlacer1.selectedBlock);
            Renderer renderer = CircuitBlockPlacer1.selectedBlock.GetComponent<Renderer>();
            if (renderer == null) return;

            if (CircuitBlockPlacer1.isCreatingNewBlock)
            {
                renderer.material = isValidPosition ? validPlacementMaterial : invalidPlacementMaterial;
            }
            else if (CircuitBlockPlacer1.isDragging)
            {
                renderer.material = isValidPosition ? selectedMaterial : invalidPlacementMaterial;
            }

            if (isValidPosition)
            {
                renderer.material.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f));
            }
            else
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    /// <summary>
    /// 应用描边效果（保持不变）
    /// </summary>
    public void ApplyOutlineEffect(GameObject block)
    {
        //当前功能不需要了
    }
    /// <summary>
    /// 恢复组件的当前默认材质
    /// </summary>
    public void RestoreCurrentDefaultMaterials(GameObject obj)
    {
        CircuitComponent component = obj.GetComponent<CircuitComponent>();
        Renderer renderer = obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material currentDefault = component?.GetCurrentDefaultMaterial();
            if (currentDefault != null)
            {
                renderer.material = currentDefault;
            }
        }
    }

    /// <summary>
    /// 移除描边效果（保持不变）
    /// </summary>
    public void RemoveOutlineEffect()
    {
        //当前功能不需要了
    }
}