using UnityEngine;

public class UIFloatingArrow : MonoBehaviour
{
    // ★ 인스펙터 창에서 체크표시로 활성화/비활성화할 변수
    [Header("움직임 가동 여부")]
    public bool isMoving = true;

    [Header("통통 튀는 연출 세팅")]
    [Tooltip("위아래로 움직일 최대 반경(거리)입니다.")]
    [SerializeField] private float moveRange = 20f;

    [Tooltip("위아래로 오르내리는 속도입니다. 수치가 높을수록 방정맞게 통통 튑니다.")]
    [SerializeField] private float moveSpeed = 5f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;

    void Awake()
    {
        // UI 오브젝트의 위치를 제어하기 위해 RectTransform 캐싱
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 게임 시작 시점의 부모 기준 원본 위치를 기억해 둡니다.
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        // 인스펙터에서 체크표시(isMoving)가 켜져 있을 때만 위아래로 움직임
        if (isMoving)
        {
            // Mathf.Sin 함수를 이용하여 -1에서 1 사이를 부드럽게 오가는 파형을 만듭니다.
            float newY = Mathf.Sin(Time.time * moveSpeed) * moveRange;

            // 기존 X 위치는 유지하고, Y 위치만 원래 위치에서 newY만큼 더해 움직입니다.
            rectTransform.anchoredPosition = new Vector2(originalAnchoredPosition.x, originalAnchoredPosition.y + newY);
        }
        else
        {
            // 인스펙터에서 체크를 끄면 즉시 원래 얌전한 처음 위치로 복구시킵니다.
            if (rectTransform.anchoredPosition != originalAnchoredPosition)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }
        }
    }

    // 씬이 도중에 꺼지거나 오브젝트가 비활성화될 때를 대비한 안전 복구
    void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }
}