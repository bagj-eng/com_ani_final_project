using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public int hp = 200;
    public float moveSpeed = 2f;
    public float attackCooldown = 1.5f;
    public float knockbackDistance = 0.5f;

    private bool isDead = false;
    private float originalMoveSpeed;

    [Header("Colliders & Prefabs")]
    public BoxCollider2D attackCollider;
    public GameObject dropItemPrefab;

    private float attackTimer = 0f;
    private float moveDir = 0f;

    private Rigidbody2D rigid;
    private Animator anim;
    private Collider2D bossCollider;

    [Header("AI Detection Flags")]
    public Transform player;
    public bool playerDetected = false;
    public bool playerInAttackRange = false;

    private MonsterDeathEffect deathEffect;

    public void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bossCollider = GetComponent<Collider2D>();

        deathEffect = GetComponent<MonsterDeathEffect>();

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }

        originalMoveSpeed = moveSpeed;
    }

    public void Update()
    {
        if (isDead) return;
        attackTimer += Time.deltaTime;

        if (playerInAttackRange && player != null)
        {
            moveDir = 0f;
            anim.SetBool("isRun", false);
            if (attackTimer >= attackCooldown)
            {
                moveDir = 0f;
                moveSpeed = 0f;
                rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);

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
            moveDir = 0f;
            anim.SetBool("isRun", false);
        }
    }

    void MoveToPlayer()
    {
        if (player.position.x > transform.position.x)
        {
            moveDir = 1f;
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            moveDir = -1f;
            transform.localScale = new Vector3(1, 1, 1);
        }

        anim.SetBool("isRun", true);
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        rigid.linearVelocity = new Vector2(moveDir * moveSpeed, rigid.linearVelocity.y);
    }

    public void AttackStart()
    {
        if (attackCollider != null) attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        moveSpeed = originalMoveSpeed;
        if (attackCollider != null) attackCollider.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        moveSpeed = originalMoveSpeed;
        if (attackCollider != null) attackCollider.enabled = false;

        hp -= damage;

        if (player != null)
        {
            float knockDir = (transform.position.x > player.position.x) ? 1f : -1f;
            transform.position += new Vector3(knockDir * knockbackDistance, 0, 0);
        }

        Debug.Log($"보스 체력 : {hp}");

        if (deathEffect != null)
        {
            deathEffect.PlayHitEffect();
        }

        if (hp <= 0)
        {
            isDead = true;
            moveDir = 0f;

            if (rigid != null)
            {
                rigid.linearVelocity = Vector2.zero;
                rigid.bodyType = RigidbodyType2D.Kinematic;
            }

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            anim.SetBool("isRun", false);
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Take_Hit");
            anim.SetTrigger("Death");

            if (deathEffect != null)
            {
                deathEffect.PlayDeathEffect();
            }

            // ================================================================
            // 📢 [확실한 트리거]: 파괴되기 직전 매니저에게 명확하게 사망 신호를 보냄
            // ================================================================
            BossCinematicManager cinematicManager = FindObjectOfType<BossCinematicManager>();
            if (cinematicManager != null)
            {
                cinematicManager.OnTargetMonsterDestroyed(gameObject);
            }

            // 사망 애니메이션 연출 시간을 벌고 완전히 파괴
            Invoke("Die", 2.2f);
            return;
        }

        anim.SetTrigger("Take_Hit");
    }

    public void Die()
    {
        if (dropItemPrefab != null)
        {
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}