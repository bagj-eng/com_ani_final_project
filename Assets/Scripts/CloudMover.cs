using UnityEngine;

public class CloudMover : MonoBehaviour
{
    [Header("구름 이동 속도")]
    [SerializeField] private float moveSpeed = 0.5f;

    [Header("★ 구름 리셋 위치 설정 (X 좌표)")]
    [Tooltip("구름이 오른쪽 어디까지 가면 왼쪽으로 되돌아갈지 정하는 한계선입니다.")]
    [SerializeField] private float endX = 20f;

    [Tooltip("오른쪽 끝에 도달한 구름이 다시 태어날 왼쪽 시작 위치입니다.")]
    [SerializeField] private float startX = -20f;

    void Update()
    {
        // 1. 매 프레임마다 구름을 오른쪽(Vector3.right) 방향으로 이동시킵니다.
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);

        // 2. 구름의 현재 X 좌표가 지정한 오른쪽 한계선(endX)을 넘어섰는지 체크합니다.
        if (transform.position.x >= endX)
        {
            // 3. 한계선을 넘었다면 Y좌표와 Z좌표는 그대로 유지한 채, X좌표만 왼쪽 시작점(startX)으로 순간이동 시킵니다.
            transform.position = new Vector3(startX, transform.position.y, transform.position.z);
        }
    }
}