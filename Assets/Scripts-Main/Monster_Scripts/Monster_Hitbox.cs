using UnityEngine;

public class Monster_Hitbox : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControll player = other.GetComponentInParent<PlayerControll>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }
}