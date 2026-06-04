using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int damage = 10;
    private bool alreadyHit = false;

    public void ResetHit()
    {
        alreadyHit = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (alreadyHit) return;

        if (other.CompareTag("monster"))
        {
            Monster monster = other.GetComponent<Monster>();

            if (monster != null)
            {
                alreadyHit = true;
                monster.TakeDamage(damage);
            }
        }
    }
}