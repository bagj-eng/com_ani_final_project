using System.Collections; // 하향 점프 코루틴 및 페이드 연출을 위해 필수
using UnityEngine;
using UnityEngine.UI; // UI 사용을 위해 필수
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class PlayerControll : MonoBehaviour
{
    public int hp = 100;
    public float moveSpeed = 5f;
    public float jumpPower = 7f;

    [Header("공격 쿨타임 설정")]
    [SerializeField] private float attackCooldown = 0.5f;
    private float attackTimer = 0f;

    [Header("★ 발소리 설정")]
    [SerializeField] private AudioClip footstepSound; // 0.5초짜리 걸음 소리 에셋 등록칸
    [SerializeField] private float footstepInterval = 0.5f; // 발소리 재생 주기 (0.5초)
    private float footstepTimer = 0f; // 발소리용 내부 타이머
    private string currentGroundTag = ""; // 현재 딛고 있는 바닥의 태그를 기억할 변수

    [Header("UI 설정")]
    [SerializeField] private GameObject deathPanel; // 'DEATH' 패널 오브젝트
    [SerializeField] private Button restartButton; // 'Restart' (Main Town) 버튼 오브젝트
    private Image panelImage; // 패널의 색상/투명도를 조절할 변수

    [Header("사운드 설정")]
    [SerializeField] private AudioClip attackSound;   // 공격 소리
    [SerializeField] private AudioClip takeHitSound;  // 피격 소리
    [SerializeField] private AudioClip deathSound;    // 사망 소리
    private AudioSource audioSource; // 소리를 재생해줄 컴포넌트

    private Rigidbody2D rigid;
    private Animator animator;

    private float moveInput;
    private bool isGrounded = false;
    private bool isAttacking = false;

    private Collider2D currentPlatformCollider;
    private Collider2D playerCollider; // 플레이어의 메인 물리 콜라이더 캐싱
    public BoxCollider2D attackCollider;
    public AttackHitbox attackHitbox;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>(); // 내 몸체 콜라이더 캐싱

        audioSource = GetComponent<AudioSource>();

        attackCollider.enabled = false;

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
        if (attackTimer > 0f) { attackTimer -= Time.deltaTime; }

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

        // ★ [수정] 발소리 조건 세분화
        // 조건: 1. 방향키 입력 중이고 2. 땅에 닿아 있고 3. 공격 중이 아니며 4. 딛고 있는 바닥 태그가 ground 또는 onewayground일 때만!
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
            // 공중에 뜨거나 다른 태그를 밟거나 멈추면 타이머 초기화
            footstepTimer = footstepInterval;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking)
        {
            if (Input.GetKey(KeyCode.DownArrow) && currentPlatformCollider != null)
            {
                StartCoroutine(DisablePlatformRoutine());
            }
            else
            {
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower);
                isGrounded = false;
                currentGroundTag = ""; // 점프하는 순간 밟고 있는 바닥 태그 비우기
                animator.SetBool("isJump", true);
            }
        }

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
    }

    void FixedUpdate()
    {
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
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);

            currentPlatformCollider = null;
            isGrounded = false;
            currentGroundTag = ""; // 하향 점프 시에도 태그 비우기

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
            currentGroundTag = collision.gameObject.tag; // ★ 착지한 바닥의 태그를 실시간 기록
            animator.SetBool("isJump", false);

            if (collision.gameObject.CompareTag("onewayground")) { currentPlatformCollider = collision.collider; }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            isGrounded = true;
            currentGroundTag = collision.gameObject.tag; // 머물고 있을 때도 바닥 태그 유지

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
                currentGroundTag = ""; // ★ 바닥에서 발이 떨어지면 태그 기억 삭제 (소리 차단)
                currentPlatformCollider = null;
            }
        }
    }

    public void AttackStart()
    {
        attackHitbox.ResetHit();
        attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        attackCollider.enabled = false;
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

        isAttacking = false;
        attackCollider.enabled = false;

        if (audioSource != null && takeHitSound != null)
        {
            audioSource.PlayOneShot(takeHitSound);
        }

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