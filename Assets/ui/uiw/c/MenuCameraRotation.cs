using UnityEngine;

public class MenuCameraRotation : MonoBehaviour
{
    [Header("旋转设置")]
    public Transform rotationTarget;           // 在Inspector中拖入要围绕旋转的组件
    public float rotationSpeed = 10f;          // 旋转速度
    public float height = 15f;                 // 相机高度
    public float radius = 20f;                 // 旋转半径

    [Header("视角设置")]
    public Vector3 lookAtOffset = Vector3.zero; // 视角偏移

    private float currentAngle = 0f;

    void Start()
    {
        // 如果没有指定目标，尝试自动寻找
        if (rotationTarget == null)
        {
            // 可以按标签或名称查找
            GameObject targetObj = GameObject.FindWithTag("RotationTarget");
            if (targetObj != null)
            {
                rotationTarget = targetObj.transform;
            }
            else
            {
                Debug.LogWarning("MenuCameraRotation: 没有指定旋转目标，请在Inspector中设置rotationTarget");
            }
        }

        // 初始位置
        UpdateCameraPosition();
    }

    void Update()
    {
        // 更新角度
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        // 更新相机位置
        UpdateCameraPosition();

        // 让相机始终看向目标
        if (rotationTarget != null)
        {
            Vector3 lookAtPosition = rotationTarget.position + lookAtOffset;
            transform.LookAt(lookAtPosition);
        }
    }

    void UpdateCameraPosition()
    {
        if (rotationTarget == null) return;

        // 计算圆形路径上的位置
        float x = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius;

        Vector3 targetPosition = rotationTarget.position;
        Vector3 newPosition = new Vector3(targetPosition.x + x, targetPosition.y + height, targetPosition.z + z);
        transform.position = newPosition;
    }

    // 在Scene视图中绘制调试信息
    void OnDrawGizmosSelected()
    {
        if (rotationTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(rotationTarget.position, radius);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rotationTarget.position, transform.position);
        }
    }
}