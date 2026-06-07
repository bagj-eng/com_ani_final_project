using UnityEngine;

public class MonsterAttackRange : MonoBehaviour
{
    private Monster monster;

    void Start()
    {
        monster = GetComponentInParent<Monster>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 공격 범위 안에 들어오면 공격 상태로 전환
        if (other.CompareTag("Player"))
        {
            monster.playerInAttackRange = true;
            monster.player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어가 공격 범위 밖으로 나가면 다시 추적 가능
        if (other.CompareTag("Player"))
        {
            monster.playerInAttackRange = false;
        }
    }
}