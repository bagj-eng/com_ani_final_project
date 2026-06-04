using UnityEngine;

public class MonsterDetectRange : MonoBehaviour
{
    private Monster monster;

    void Start()
    {
        monster = GetComponentInParent<Monster>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 감지 범위 안에 들어오면 추적 시작
        if (other.CompareTag("Player"))
        {
            monster.playerDetected = true;
            monster.player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어가 감지 범위 밖으로 나가면 추적 중지
        if (other.CompareTag("Player"))
        {
            monster.playerDetected = false;
            monster.player = null;
        }
    }
}