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

    [Header("UI 설정")]
    [SerializeField] private GameObject deathPanel; // 'DEATH' 패널 오브젝트
    [SerializeField] private Button restartButton; // 'Restart' (MAINTOWN) 버튼 오브젝트
    private Image panelImage; // 패널의 색상/투명도를 조절할 변수

    [Header("사운드 설정")] // ★ 사운드 오디오클립 변수 추가
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
    public BoxCollider2D attackCollider;
    public AttackHitbox attackHitbox;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // ★ 플레이어 오브젝트에 있는 AudioSource 컴포넌트를 자동으로 가져옵니다.
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
            restartButton.gameObject.SetActive(false); // 처음엔 리스타트 버튼을 숨깁니다.
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
                animator.SetBool("isJump", true);
            }
        }

        if (Input.GetKeyDown(KeyCode.A) && attackTimer <= 0f && isGrounded && !isAttacking)
        {
            animator.SetTrigger("Attack");
            attackTimer = attackCooldown;
            isAttacking = true;
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
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
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D platformCollider = currentPlatformCollider;

        if (playerCollider != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            yield return new WaitForSeconds(0.2f);

            if (platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            isGrounded = true;
            animator.SetBool("isJump", false);

            if (collision.gameObject.CompareTag("onewayground")) { currentPlatformCollider = collision.collider; }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("onewayground")) { currentPlatformCollider = collision.collider; }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground") || collision.gameObject.CompareTag("onewayground"))
        {
            isGrounded = false;
            if (collision.collider == currentPlatformCollider) { currentPlatformCollider = null; }
        }
    }

    // ★ 유니티 애니메이션 이벤트: 공격이 시작되는 프레임에 호출
    public void AttackStart()
    {
        attackHitbox.ResetHit();
        attackCollider.enabled = true;

        // ★ 공격 소리가 설정되어 있다면 딱 한 번 재생합니다.
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
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

            // ★ 사망 소리가 설정되어 있다면 딱 한 번 재생합니다.
            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            Die();
            return;
        }

        isAttacking = false;
        attackCollider.enabled = false;

        // ★ 피격 소리가 설정되어 있다면 딱 한 번 재생합니다.
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

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.parent == null && obj.gameObject != this.gameObject)
            {
                Destroy(obj);
            }
        }

        SceneManager.LoadScene("maintown");
    }
}