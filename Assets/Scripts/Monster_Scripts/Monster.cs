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
    private Collider2D monsterCollider; // ★ 시체 충돌판정 제거용 변수 추가

    // DetectRange / AttackRange 스크립트에서 값을 넣어줌
    public Transform player;
    public bool playerDetected = false;
    public bool playerInAttackRange = false;

    // ★ 새 연출 컴포넌트를 담을 변수
    private MonsterDeathEffect deathEffect;

    public void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        monsterCollider = GetComponent<Collider2D>(); // ★ 내 메인 콜라이더 캐싱

        // ★ 내 몸에 붙은 연출 스크립트 자동 감지 및 연결
        deathEffect = GetComponent<MonsterDeathEffect>();

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
        if (isDead) return; // ★ 사망 후 물리 무빙 차단
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

        // 넉백
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

        // ★ [추가]: 피격 시 마다 새로 만든 스크립트의 빨간 깜빡임 효과 가동
        if (deathEffect != null)
        {
            deathEffect.PlayHitEffect();
        }

        if (hp <= 0)
        {
            isDead = true;
            moveDir = 0f;

            // ★ 사망 즉시 물리 충돌 및 속도 완전 리셋 (안전장치)
            if (rigid != null)
            {
                rigid.linearVelocity = Vector2.zero;
            }
            if (monsterCollider != null)
            {
                monsterCollider.enabled = false;
            }

            anim.SetBool("isRun", false);
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Take_Hit");
            anim.SetTrigger("Death");

            // ★ [추가]: 죽는 순간 애니메이션을 얼리고 부드럽게 투명화 연출 시작!
            if (deathEffect != null)
            {
                deathEffect.PlayDeathEffect();
            }

            // ★ [수정]: 기존에는 즉시 Die()를 불러 삭제했으나, 
            // 이제 투명화 소멸 연출이 끝나는 타이밍(예: 2.2초 뒤)에 맞춰 지연 실행하도록 변경합니다.
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