using UnityEngine;

public class Batmonster : MonoBehaviour
{
    public int hp = 1;
    public float moveSpeed = 3f;
    public float attackCooldown = 1f;

    public BoxCollider2D attackCollider;

    public Transform player;
    public bool playerDetected = false;
    public bool playerInAttackRange = false;

    private Rigidbody2D rigid;
    private Animator anim;
    private float attackTimer = 0f;
    private bool isDead = false;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rigid.gravityScale = 0f;
        attackCollider.enabled = false;
    }

    void Update()
    {
        if (isDead) return;

        attackTimer += Time.deltaTime;

        if (playerInAttackRange && player != null)
        {
            rigid.linearVelocity = Vector2.zero;

            if (attackTimer >= attackCooldown)
            {
                anim.SetTrigger("Attack");
                attackTimer = 0f;
            }
        }
        else if (playerDetected && player != null)
        {
            MoveToPlayer();
        }
        else
        {
            rigid.linearVelocity = Vector2.zero;
        }
    }

    void MoveToPlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rigid.linearVelocity = dir * moveSpeed;

        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void AttackStart()
    {
        attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        attackCollider.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        isDead = true;
        rigid.linearVelocity = Vector2.zero;
        attackCollider.enabled = false;

        anim.ResetTrigger("Attack");
        anim.SetTrigger("Death");
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}