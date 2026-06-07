using System.Collections; // 하향 점프 코루틴 및 페이드 연출을 위해 필수
using UnityEngine;
using UnityEngine.UI; // UI 사용을 위해 필수
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class PlayerControll : MonoBehaviour
{
    public int hp = 100;
    public float moveSpeed = 5f;
    public float jumpPower = 7f;

    [Header("첫 번째 공격(A키) 설정")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private int attackDamage = 10;
    private float attackTimer = 0f;

    [Header("두 번째 공격(S키) 설정")]
    [SerializeField] private float attack2Cooldown = 0.7f;
    [SerializeField] private int attack2Damage = 15;
    private float attack2Timer = 0f;

    [Header("★ 발소리 설정")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.5f;
    private float footstepTimer = 0f;
    private string currentGroundTag = "";

    [Header("UI 설정")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;
    private Image panelImage;

    // ────────────────────────────────────────────────────────────────
    // 사운드 설정
    [SerializeField] private AudioClip attackSound;    // 첫 번째 공격(A키) 소리
    [SerializeField] private AudioClip attack2Sound;   // 두 번째 공격(S키) 소리
    [SerializeField] private AudioClip jumpSound;      // ★ [추가] 점프 시 재생할 소리 에셋 등록칸
    [SerializeField] private AudioClip takeHitSound;   // 피격 소리
    [SerializeField] private AudioClip deathSound;     // 사망 소리
    private AudioSource audioSource;
    // ────────────────────────────────────────────────────────────────

    private Rigidbody2D rigid;
    private Animator animator;

    private float moveInput;
    private bool isGrounded = false;
    private bool isAttacking = false;

    private Collider2D currentPlatformCollider;
    private Collider2D playerCollider;
    private DashAbility dashAbility;

    [Header("공격 1 물리 콜라이더 및 히트박스 연결")]
    public BoxCollider2D attackCollider;
    public AttackHitbox attackHitbox;

    [Header("공격 2 물리 콜라이더 및 히트박스 연결")]
    public BoxCollider2D attack2Collider;
    public AttackHitbox attack2Hitbox;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        dashAbility = GetComponent<DashAbility>();
        audioSource = GetComponent<AudioSource>();

        if (attackCollider != null) attackCollider.enabled = false;
        if (attack2Collider != null) attack2Collider.enabled = false;

        if (deathPanel != null)
        {
            panelImage = deathPanel.GetComponent<Image>();
            deathPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            restartButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // ★ [핵심 보정 추가]: 보스 룸 연출(대화, 워킹) 도중에는 플레이어의 키보드 조작을 전면 차단합니다.
        if (BossMapCutscene.IsCutsceneActive)
        {
            moveInput = 0f;
            if (rigid != null)
            {
                // 움직이던 관성을 지우고 물리 속도를 완전히 묶어 바닥에 고정시킵니다.
                rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
            }
            if (animator != null)
            {
                animator.SetBool("isRun", false);
            }

            // 아래의 키보드 매핑 입력 검사문(Space, A, S, Arrow)을 실행하지 않고 패스합니다.
            return;
        }

        if (attackTimer > 0f) { attackTimer -= Time.deltaTime; }
        if (attack2Timer > 0f) { attack2Timer -= Time.deltaTime; }

        moveInput = 0f;

        if (!isAttacking)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                moveInput = -1f;
                transform.localScale = new Vector3(-4, 4, 1);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                moveInput = 1f;
                transform.localScale = new Vector3(4, 4, 1);
            }
        }

        animator.SetBool("isRun", moveInput != 0f && !isAttacking);

        // 발소리 시스템
        if (moveInput != 0f && isGrounded && !isAttacking &&
            (currentGroundTag == "ground" || currentGroundTag == "onewayground"))
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                if (audioSource != null && footstepSound != null)
                {
                    audioSource.PlayOneShot(footstepSound);
                }
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = footstepInterval;
        }

        // 점프 및 하향 점프 제어
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking)
        {
            // 밑방향키와 같이 누른 하향 점프일 때 (소리 나지 않음)
            if (Input.GetKey(KeyCode.DownArrow) && currentPlatformCollider != null)
            {
                StartCoroutine(DisablePlatformRoutine());
            }
            // 일반 위로 뛰는 점프일 때
            else
            {
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower);
                isGrounded = false;
                currentGroundTag = "";
                animator.SetBool("isJump", true);

                // ★ 일반 점프 도약 타이밍에 지정된 오디오 소리를 원샷 재생합니다.
                if (audioSource != null && jumpSound != null)
                {
                    audioSource.PlayOneShot(jumpSound);
                }
            }
        }

        // 첫 번째 공격 (A키)
        if (Input.GetKeyDown(KeyCode.A) && attackTimer <= 0f && isGrounded && !isAttacking)
        {
            animator.SetTrigger("Attack");
            attackTimer = attackCooldown;
            isAttacking = true;
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);

            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
        }

        // 두 번째 공격 (S키)
        if (Input.GetKeyDown(KeyCode.S) && attack2Timer <= 0f && isGrounded && !isAttacking)
        {
            animator.SetTrigger("Attack2");
            attack2Timer = attack2Cooldown;
            isAttacking = true;
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);

            if (audioSource != null)
            {
                if (attack2Sound != null)
                    audioSource.PlayOneShot(attack2Sound);
                else if (attackSound != null)
                    audioSource.PlayOneShot(attackSound);
            }
        }
    }

    void FixedUpdate()
    {
        // 🛠️ 대화/워킹 연출 시 물리 연산에 의해 캐릭터가 미끄러지거나 밀리는 왜곡 방지
        if (BossMapCutscene.IsCutsceneActive)
        {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
            return;
        }

        if (dashAbility != null && dashAbility.IsDashing())
        {
            return;
        }

        if (isAttacking)
        {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
        }
        else
        {
            rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
        }
    }

    private IEnumerator DisablePlatformRoutine()
    {
        Collider2D platformCollider = currentPlatformCollider;

        if (playerCollider != null && platformCollider != null)
        {
            animator.SetBool("Isfall", true);

            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);

            currentPlatformCollider = null;
            isGrounded = false;
            currentGroundTag = "";

            yield return new WaitForSeconds(0.2f);

            while (platformCollider != null &&
                   (rigid.linearVelocity.y < 0f || playerCollider.bounds.min.y > platformCollider.bounds.max.y))
            {
                yield return null;
            }

            if (platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("key1"))
        {
            if (KeyManager.Instance != null)
            {
                KeyManager.Instance.GetKey("A");
            }
            Debug.Log("플레이어가 key1 태그 오브젝트와 충돌하여 획득했습니다.");
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("key2"))
        {
            if (KeyManager.Instance != null)
            {
                KeyManager.Instance.GetKey("B");
            }
            Debug.Log("플레이어가 key2 태그 오브젝트와 충돌하여 획득했습니다.");
            Destroy(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            isGrounded = true;
            currentGroundTag = collision.gameObject.tag;
            animator.SetBool("isJump", false);
            animator.SetBool("Isfall", false);

            if (collision.gameObject.CompareTag("onewayground")) { currentPlatformCollider = collision.collider; }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            isGrounded = true;
            currentGroundTag = collision.gameObject.tag;

            if (collision.gameObject.CompareTag("onewayground")) { currentPlatformCollider = collision.collider; }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            if (collision.collider == currentPlatformCollider || currentPlatformCollider == null)
            {
                isGrounded = false;
                currentGroundTag = "";
                currentPlatformCollider = null;
            }
        }
    }

    public void AttackStart()
    {
        if (attackHitbox != null)
        {
            attackHitbox.ResetHit();
        }
        if (attackCollider != null) attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        if (attackCollider != null) attackCollider.enabled = false;
        isAttacking = false;
    }

    public void Attack2Start()
    {
        isAttacking = true;
        if (attack2Hitbox != null)
        {
            attack2Hitbox.ResetHit();
        }
        if (attack2Collider != null) attack2Collider.enabled = true;
        rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
    }

    public void Attack2End()
    {
        if (attack2Collider != null) attack2Collider.enabled = false;
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log($"플레이어 체력 : {hp}");

        if (hp <= 0)
        {
            isAttacking = true;
            moveInput = 0f;
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);

            animator.SetBool("isRun", false);
            animator.SetTrigger("Death");

            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            Die();
            return;
        }

        if (audioSource != null && takeHitSound != null)
        {
            audioSource.PlayOneShot(takeHitSound);
        }

        if (isAttacking)
        {
            Debug.Log("공격 중 피격당함: 공격 모션을 유지합니다.");
            return;
        }

        isAttacking = false;

        if (attackCollider != null) attackCollider.enabled = false;
        if (attack2Collider != null) attack2Collider.enabled = false;

        animator.SetTrigger("Take_Hit");
    }

    public void Die()
    {
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (deathPanel != null && panelImage != null)
        {
            deathPanel.SetActive(true);
            panelImage.color = new Color(1f, 0f, 0f, 0f);
        }

        Time.timeScale = 0f;

        float duration = 5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);

            if (panelImage != null)
            {
                panelImage.color = new Color(1f, 0f, 0f, alpha);
            }

            yield return null;
        }

        if (panelImage != null) panelImage.color = new Color(1f, 0f, 0f, 1f);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        PlayerPrefs.DeleteKey("Saved_HasKeyA");
        PlayerPrefs.DeleteKey("Saved_HasKeyB");
        PlayerPrefs.Save();

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.parent == null && obj.gameObject != this.gameObject)
            {
                Destroy(obj);
            }
        }

        SceneManager.LoadScene("Main Town");
    }
}