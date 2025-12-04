using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 镜头控制
public class Camera_control : MonoBehaviour
{
    [Header("镜头设置")]
    [Tooltip("特写镜头距离")] public float zoomDistance;
    [Tooltip("镜头移动速度")] public float zoomSpeed;
    [Tooltip("特写时视野")] public float zoomFOV;


    [Header("边缘移动设置")]
    [Tooltip("边缘检测阈值")] public float screenEdgeThreshold = 20f;
    [Tooltip("镜头平移速度")] public float panSpeed = 10f;


    [Header("缩放设置")]
    [Tooltip("最小视野")] public float minFOV = 20f;
    [Tooltip("最大视野")] public float maxFOV = 60f;
    [Tooltip("缩放灵敏度")] public float zoomSensitivity = 5f;
    [Tooltip("是否使用位置缩放代替视野缩放")] public bool usePositionZoom = false;
    [Tooltip("最小高度")] public float minHeight = 5f;
    [Tooltip("最大高度")] public float maxHeight = 30f;


    [Header("回原位设置")]
    [Tooltip("回原位速度")] public float returnSpeed = 5f;

    public CircuitBlockPlacer CircuitBlockPlacer1;
    public Material_Conversion Material_conversion;
    //ui 特写ui
    public System.Action<GameObject> OnFocusModeEntered;
    public System.Action OnFocusModeExited;
    public InputManager input;

    // 存储原始相机位置/旋转
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float homeFOV;
    private bool isReturningHome = false;
    private Coroutine returnHomeCoroutine;

    void Start()
    {
        if (Material_conversion == null)
            Material_conversion = FindObjectOfType<Material_Conversion>();
        if (CircuitBlockPlacer1 == null)
            CircuitBlockPlacer1 = FindObjectOfType<CircuitBlockPlacer>();

        // 保存原始相机设置作为"家"的位置
        homePosition = transform.position;
        homeRotation = transform.rotation;
        homeFOV = Camera.main.fieldOfView;
    }

    void Update()
    {
        // 检测空格键回原位
        if (Input.GetKeyDown(input.ReturnToHome))
        {
            ReturnToHomePosition();
        }

        // 在非聚焦模式和非回原位状态下才允许常规镜头控制
        if (!CircuitBlockPlacer1.isZoomed && !isReturningHome)
        {
            HandleCameraPan();
            HandleCameraZoom();
        }
    }

    /// <summary>
    /// 返回初始位置
    /// </summary>
    public void ReturnToHomePosition()
    {
        // 如果已经在回家过程中，停止之前的协程
        if (isReturningHome && returnHomeCoroutine != null)
        {
            StopCoroutine(returnHomeCoroutine);
        }

        // 如果在聚焦模式，先退出聚焦
        if (CircuitBlockPlacer1.isZoomed)
        {
            ExitFocusMode();
        }

        // 启动回原位协程
        returnHomeCoroutine = StartCoroutine(ReturnToHomeCoroutine());
    }

    /// <summary>
    /// 平滑回到初始位置
    /// </summary>
    IEnumerator ReturnToHomeCoroutine()
    {
        isReturningHome = true;

        Transform camTransform = transform;
        Vector3 startPosition = camTransform.position;
        Quaternion startRotation = camTransform.rotation;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * returnSpeed;

            camTransform.position = Vector3.Lerp(startPosition, homePosition, progress);
            camTransform.rotation = Quaternion.Slerp(startRotation, homeRotation, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, homeFOV, progress);

            yield return null;
        }

        // 确保最终位置准确
        camTransform.position = homePosition;
        camTransform.rotation = homeRotation;
        Camera.main.fieldOfView = homeFOV;

        isReturningHome = false;
    }

    /// <summary>
    /// 处理镜头平移（边缘移动）
    /// </summary>
    private void HandleCameraPan()
    {
        Vector3 moveDirection = Vector3.zero;
        Vector2 mousePosition = Input.mousePosition;

        // 检测屏幕四个边缘
        if (mousePosition.x < screenEdgeThreshold || Input.GetKey(input.cleft))
            moveDirection.x -= 1;
        if (mousePosition.x > Screen.width - screenEdgeThreshold || Input.GetKey(input.cright))
            moveDirection.x += 1;
        if (mousePosition.y < screenEdgeThreshold || Input.GetKey(input.cdown))
            moveDirection.z -= 1;
        if (mousePosition.y > Screen.height - screenEdgeThreshold || Input.GetKey(input.cup))
            moveDirection.z += 1;

        // 归一化方向并应用速度
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();

            // 计算实际移动方向（考虑相机旋转）
            Vector3 forwardMove = transform.forward * moveDirection.z;
            Vector3 rightMove = transform.right * moveDirection.x;

            // 保持Y轴不变
            forwardMove.y = 0;
            rightMove.y = 0;

            Vector3 combinedMove = (forwardMove + rightMove).normalized * panSpeed * Time.deltaTime;
            transform.position += combinedMove;
        }
    }

    /// <summary>
    /// 处理镜头缩放（滚轮）
    /// </summary>
    private void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (usePositionZoom)
            {
                // 位置缩放模式：沿相机前方移动
                Vector3 zoomDirection = transform.forward * scroll * zoomSensitivity;

                // 计算新位置并限制高度
                Vector3 newPosition = transform.position + zoomDirection;
                newPosition.y = Mathf.Clamp(newPosition.y, minHeight, maxHeight);

                // 应用位置变化
                transform.position = newPosition;
            }
            else
            {
                // FOV缩放模式
                float newFOV = Camera.main.fieldOfView - scroll * zoomSensitivity;
                Camera.main.fieldOfView = Mathf.Clamp(newFOV, minFOV, maxFOV);
            }
        }
    }

    // ======== 聚焦功能 ========

    /// <summary>
    /// 聚焦到特定方块
    /// </summary>
    public void FocusOnBlock(GameObject block)
    {
        // 如果处于删除模式，不执行聚焦
        CircuitBlockPlacer placer = FindObjectOfType<CircuitBlockPlacer>();
        if (placer != null && placer.isDeleteMode)
        {
            return;
        }
        if (CircuitBlockPlacer1.isZoomed)
        {
            ExitFocusMode();
        }

        CircuitBlockPlacer1.focusedBlock = block;
        CircuitBlockPlacer1.isZoomed = true;

        Material_conversion.ApplyOutlineEffect(block);
        StartCoroutine(ZoomToBlock(block));
        // 触发进入特写事件
        OnFocusModeEntered?.Invoke(block);
    }

    /// <summary>
    /// 镜头移动到特写位置
    /// </summary>
    IEnumerator ZoomToBlock(GameObject block)
    {
        Transform camTransform = Camera.main.transform;
        Vector3 directionToBlock = (block.transform.position - camTransform.position).normalized;
        Vector3 targetPosition = block.transform.position - directionToBlock * zoomDistance;
        Quaternion targetRotation = camTransform.rotation;
        float targetFOV = zoomFOV;

        Vector3 startPosition = camTransform.position;
        float startFOV = Camera.main.fieldOfView;
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * zoomSpeed;
            camTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, progress);
            yield return null;
        }

        camTransform.position = targetPosition;
        Camera.main.fieldOfView = targetFOV;
    }

    /// <summary>
    /// 退出特写模式
    /// </summary>
    public void ExitFocusMode()
    {

        if (!CircuitBlockPlacer1.isZoomed) return;

        if (CircuitBlockPlacer1.focusedBlock != null)
        {
            Material_conversion.RemoveOutlineEffect();
        }

        StartCoroutine(ResetCamera());
        CircuitBlockPlacer1.focusedBlock = null;
        CircuitBlockPlacer1.isZoomed = false;

        // 触发退出特写事件
        OnFocusModeExited?.Invoke();
    }

    /// <summary>
    /// 恢复相机到原始位置
    /// </summary>
    IEnumerator ResetCamera()
    {
        Transform camTransform = transform;
        Vector3 startPosition = camTransform.position;
        Quaternion startRotation = camTransform.rotation;
        float startFOV = Camera.main.fieldOfView;

        float progress = 0f;

        while (progress < 1f)
        {
            //这里
            progress += Time.deltaTime * zoomSpeed;

            camTransform.position = Vector3.Lerp(startPosition, homePosition, progress);
            camTransform.rotation = Quaternion.Slerp(startRotation, homeRotation, progress);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, homeFOV, progress);

            yield return null;
        }
    }
}