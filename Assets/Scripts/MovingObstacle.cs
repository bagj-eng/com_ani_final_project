using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    private Vector3 targetPosition;
    private float moveSpeed;
    private bool isMoving = false;

    void Update()
    {
        if (isMoving)
        {
            // 목표 위치로 부드럽게 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // 목표 위치에 거의 도달하면 정지
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    // 외부(KeyManager)에서 이 함수를 호출해 나무를 움직입니다.
    public void MoveTo(Vector3 destination, float speed)
    {
        targetPosition = destination;
        moveSpeed = speed;
        isMoving = true;
    }
}