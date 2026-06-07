using UnityEngine;

public class BatAttackRange : MonoBehaviour
{
    private Batmonster monster;

    void Start()
    {
        monster = GetComponentInParent<Batmonster>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            monster.playerInAttackRange = true;
            monster.player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            monster.playerInAttackRange = false;
        }
    }
}