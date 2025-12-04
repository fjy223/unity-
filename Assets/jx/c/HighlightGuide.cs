using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HighlightGuide : MonoBehaviour, IPointerClickHandler
{
    [Header("高亮设置")]
    public Image highlightImage;
    public Animator animator;

    private Transform target;
    private Vector2 offset;
    private System.Action onClick;
    private RectTransform rectTransform;
    private Camera mainCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    public void Initialize(Transform targetTransform, Vector2 screenOffset)
    {
        target = targetTransform;
        offset = screenOffset;

        // 开始跟随目标
        StartCoroutine(FollowTarget());
    }

    public void SetClickCallback(System.Action callback)
    {
        onClick = callback;
    }

    private System.Collections.IEnumerator FollowTarget()
    {
        while (target != null)
        {
            // 将世界坐标转换为屏幕坐标
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
            rectTransform.anchoredPosition = screenPos + (Vector3)offset;

            yield return null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}