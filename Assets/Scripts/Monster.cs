using UnityEngine;

public class Monster : MonoBehaviour
{
    public int hp = 100;

    public void TakeDamage(int damage)
    {
        hp -= damage;

        Debug.Log($"몬스터 체력 : {hp}");

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}