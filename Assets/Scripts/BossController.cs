using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public int hp = 200; // 보스이므로 체력을 조금 더 높게 설정
    public float moveSpeed = 2f;
    public float attackCooldown = 1.5f;
    public float knockbackDistance = 0.5f; // 보스는 넉백 거리를 조금 줄임

    private bool isDead = false;
    private float originalMoveSpeed;

    [Header("Colliders & Prefabs")]
    public BoxCollider2D attackCollider;
    public GameObject dropItemPrefab;

    private float attackTimer = 0f;
    private float moveDir = 0f; // 1: 오른쪽, -1: 왼쪽, 0: 정지

    // 컴포넌트 캐싱용 변수
    private Rigidbody2D rigid;
    private Animator anim;
    private Collider2D bossCollider;

    [Header("AI Detection Flags")]
    // DetectRange / AttackRange 스크립트에서 값을 넣어줌
    public Transform player;
    public bool playerDetected = false;
    public bool playerInAttackRange = false;

    // 연출 컴포넌트를 담을 변수
    private MonsterDeathEffect deathEffect;

    public void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bossCollider = GetComponent<Collider2D>(); // 내 메인 콜라이더 캐싱

        // 내 몸에 붙은 연출 스크립트 자동 감지 및 연결
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

    // 플레이어의 위치에 따라 좌우 방향 설정 및 시선 전환
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
        if (isDead) return; // 사망 후 물리 무빙 차단
        rigid.linearVelocity = new Vector2(moveDir * moveSpeed, rigid.linearVelocity.y);
    }

    // 애니메이션 이벤트 호출용 메서드들
    public void AttackStart()
    {
        if (attackCollider != null) attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        moveSpeed = originalMoveSpeed;
        if (attackCollider != null) attackCollider.enabled = false;
    }

    /// <summary>
    /// 플레이어에게 공격 받았을 때 대미지 및 연출을 처리하는 메서드
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        moveSpeed = originalMoveSpeed;
        if (attackCollider != null) attackCollider.enabled = false;

        hp -= damage;

        // 넉백 연산
        if (player != null)
        {
            float knockDir = (transform.position.x > player.position.x) ? 1f : -1f;
            transform.position += new Vector3(knockDir * knockbackDistance, 0, 0);
        }

        Debug.Log($"보스 체력 : {hp}");

        // [피격 이펙트]: 빨간 깜빡임 효과 가동
        if (deathEffect != null)
        {
            deathEffect.PlayHitEffect();
        }

        // 사망 조건 검사
        if (hp <= 0)
        {
            isDead = true;
            moveDir = 0f;

            // 사망 즉시 물리 속도를 멈추고 Kinematic으로 변경하여 중력 차단
            if (rigid != null)
            {
                rigid.linearVelocity = Vector2.zero;
                rigid.bodyType = RigidbodyType2D.Kinematic;
            }

            // Kinematic 상태가 되었으므로 콜라이더를 꺼도 바닥 밑으로 추락하지 않음
            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            // 애니메이션 초기화 및 사망 트리거 작동
            anim.SetBool("isRun", false);
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Take_Hit");
            anim.SetTrigger("Death");

            // [사망 이펙트]: 서서히 투명화 연출 시작!
            if (deathEffect != null)
            {
                deathEffect.PlayDeathEffect();
            }

            // 투명화 연출 시간(2.0초)을 고려하여 2.2초 뒤 완전히 오브젝트 파괴 및 아이템 드롭
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