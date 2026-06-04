using UnityEngine;

public class Monster : MonoBehaviour
{
    public int hp = 100;
    public float moveSpeed = 2f;
    public float attackCooldown = 1f;
    public float knockbackDistance = 1f;

    private bool isDead = false;
    private float originalMoveSpeed;

    public BoxCollider2D attackCollider;


    public GameObject dropItemPrefab;

    private float attackTimer = 0f;

    // 1이면 오른쪽 이동, -1이면 왼쪽 이동, 0이면 정지
    private float moveDir = 0f;

    private Rigidbody2D rigid;
    private Animator anim;

    // DetectRange / AttackRange 스크립트에서 값을 넣어줌
    public Transform player;
    public bool playerDetected = false;
    public bool playerInAttackRange = false;

    public void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        attackCollider.enabled = false;

        originalMoveSpeed = moveSpeed;
    }

    public void Update()
    {
        if (isDead) return;
        attackTimer += Time.deltaTime;
        // 플레이어가 공격 범위 안에 있으면 멈추고 공격
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
        // 플레이어가 감지 범위 안에 있으면 따라가기
        else if (playerDetected && player != null)
        {
            MoveToPlayer();
        }
        // 아무것도 감지 안 되면 정지
        else
        {
            moveDir = 0f;
            anim.SetBool("isRun", false);
        }
    }

    // 몬스터가 플레이어 오른쪽에 있으면 왼쪽을 바라보고,
    // 플레이어가 몬스터 오른쪽에 있으면 오른쪽을 바라보며 따라오도록 설정
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
        rigid.linearVelocity = new Vector2(moveDir * moveSpeed, rigid.linearVelocity.y);
        
    }
    public void AttackStart()
    {
        

        attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        moveSpeed = originalMoveSpeed;
        attackCollider.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        moveSpeed = originalMoveSpeed;
        attackCollider.enabled = false;

        hp -= damage;
        //넉백
        if (player != null)
        {
            float knockDir;

            if (transform.position.x > player.position.x)
            {
                knockDir = 1f;
            }
            else
            {
                knockDir = -1f;
            }

            transform.position += new Vector3(knockDir * knockbackDistance, 0, 0);
        }

        Debug.Log($"몬스터 체력 : {hp}");

        if (hp <= 0)
        {
            isDead = true;
            moveDir = 0f;
            anim.SetBool("isRun", false);

            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Take_Hit");
            anim.SetTrigger("Death");
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