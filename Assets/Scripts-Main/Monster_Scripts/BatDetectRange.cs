using UnityEngine;

public class BatDetectRange : MonoBehaviour
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
            
            monster.playerDetected = true;
            monster.player = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            monster.playerDetected = false;
            monster.player = null;
        }
    }
}