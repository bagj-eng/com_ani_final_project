using System.Threading;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("monster"))
        {
            Monster monster = other.GetComponent<Monster>();

            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
        }
    }
}