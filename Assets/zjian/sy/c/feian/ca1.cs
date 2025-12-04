using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI; // 添加UI命名空间


//镜头控制
public class ca1 : MonoBehaviour
{


    [Header("镜头设置")]
    [Tooltip("特写镜头距离")] public float zoomDistance;
    [Tooltip("镜头移动速度")] public float zoomSpeed;
    [Tooltip("特写时视野")] public float zoomFOV;



    public CircuitBlockPlacer CircuitBlockPlacer1;
    public Material_Conversion Material_conversion;


    // Start is called before the first frame update
    void Start()
    {
        if (Material_conversion == null)
            Material_conversion = FindObjectOfType<Material_Conversion>();
        if (CircuitBlockPlacer1 == null)
            CircuitBlockPlacer1 = FindObjectOfType<CircuitBlockPlacer>();

    }

    // Update is called once per frame
    void Update()
    {

    }
    // ======== 新增的聚焦功能 ========

    /// <summary>
    /// 聚焦到特定方块
    /// </summary>
    public void FocusOnBlock(GameObject block)
    {

        // 如果已经在聚焦状态，先退出
        if (CircuitBlockPlacer1.isZoomed)
        {
            ExitFocusMode();
        }

        // 设置聚焦方块
        CircuitBlockPlacer1.focusedBlock = block;
        CircuitBlockPlacer1.isZoomed = true;

        // 保存原始材质并应用描边效果
        Material_conversion.ApplyOutlineEffect(block);

        // 移动相机到特写位置
        CircuitBlockPlacer1.StartCoroutine(ZoomToBlock(block));

        // TODO: 这里可以显示属性菜单
        // UIManager.Instance.ShowPropertyMenu(block);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object other)
    {
        return base.Equals(other);
    }

    public override string ToString()
    {
        return base.ToString();
    }
    /// <summary>
    /// 镜头移动到特写位置（只改变位置和视野，不改变旋转）
    /// </summary>
    System.Collections.IEnumerator ZoomToBlock(GameObject block)
    {
        Transform camTransform = Camera.main.transform;

        // 计算目标位置：在方块前方一定距离，但保持当前相机角度
        Vector3 directionToBlock = (block.transform.position - camTransform.position).normalized;
        Vector3 targetPosition = block.transform.position - directionToBlock * zoomDistance;

        // 保持当前相机旋转
        Quaternion targetRotation = camTransform.rotation;

        // 目标视野
        float targetFOV = zoomFOV;

        Vector3 startPosition = camTransform.position;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * zoomSpeed;

            // 只插值位置和视野，保持旋转不变
            camTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, progress);

            yield return null;
        }

        // 确保最终位置和视野准确
        camTransform.position = targetPosition;
        Camera.main.fieldOfView = targetFOV;
    }

    /// <summary>
    /// 退出特写模式
    /// </summary>
    public void ExitFocusMode()
    {
        if (!CircuitBlockPlacer1.isZoomed) return;

        // 移除描边效果前先检查
        if (CircuitBlockPlacer1.focusedBlock != null)
        {
            Material_conversion.RemoveOutlineEffect();
        }

        // 恢复相机位置
        StartCoroutine(ResetCamera());

        // 重置状态
        CircuitBlockPlacer1.focusedBlock = null;
        CircuitBlockPlacer1.isZoomed = false;

        // TODO: 这里可以隐藏属性菜单
        // UIManager.Instance.HidePropertyMenu();
    }

    /// <summary>
    /// 恢复相机到原始位置
    /// </summary>
    System.Collections.IEnumerator ResetCamera()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 startPosition = camTransform.position;
        Quaternion startRotation = camTransform.rotation;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * zoomSpeed;

            camTransform.position = Vector3.Lerp(startPosition, CircuitBlockPlacer1.originalCameraPosition, progress);
            camTransform.rotation = Quaternion.Slerp(startRotation, CircuitBlockPlacer1.originalCameraRotation, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, CircuitBlockPlacer1.originalFOV, progress);

            yield return null;
        }
    }
}