using System.Collections;
using UnityEngine;

public class MonsterDeathEffect : MonoBehaviour
{
    [Header("피격 깜빡임 설정")]
    [Tooltip("데미지를 입었을 때 바뀔 색상입니다.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [Tooltip("한 번 깜빡일 때 색상이 유지되는 시간입니다.")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("총 몇 번 깜빡일지 지정합니다.")]
    [SerializeField] private int flashCount = 3;

    [Header("사망 사라짐 설정")]
    [Tooltip("몬스터 애니메이션이 멈춘 뒤, 완전히 투명해질 때까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float fadeOutDuration = 2f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rigid;
    private Collider2D monsterCollider;

    private Color originalColor;
    private Coroutine flashCoroutine;
    private bool isEffectStarted = false;

    void Awake()
    {
        // 컴포넌트들을 자동으로 안전하게 찾아서 캐싱합니다.
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        monsterCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// 외부(Monster.cs)에서 데미지를 입었을 때 호출하는 피격 깜빡임 함수
    /// </summary>
    public void PlayHitEffect()
    {
        if (isEffectStarted || spriteRenderer == null) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        flashCoroutine = null;
    }

    /// <summary>
    /// 외부(Monster.cs)에서 체력이 0이 되었을 때 호출하면 애니메이션을 얼리고 투명화시키는 함수
    /// </summary>
    public void PlayDeathEffect()
    {
        if (isEffectStarted) return;
        isEffectStarted = true;

        // 1. 진행 중이던 피격 깜빡임이 있다면 즉시 중지
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        // 2. ★ 핵심: 애니메이션을 현재 프레임에서 즉시 강제 일시정지(Freeze)시킵니다.
        if (animator != null)
        {
            animator.speed = 0f;
        }

        // 3. 물리 및 충돌 차단 (바닥으로 꺼지거나 밀리는 현상 방지)
        if (monsterCollider != null) monsterCollider.enabled = false;
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.bodyType = RigidbodyType2D.Kinematic;
        }

        // 4. 서서히 투명해지며 사라지는 무빙 코루틴 실행
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;

            // 시간에 따라 1에서 0으로 변하는 알파값(투명도) 계산
            float alpha = Mathf.Clamp01(1f - (elapsedTime / fadeOutDuration));

            // 다른 색상 값은 둔 채 투명도만 깎아냅니다.
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // 완전히 투명하게 고정
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }

    // 오브젝트가 리스타트 등으로 꺼질 때 색상 원상복구 안전장치
    void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        if (animator != null)
        {
            animator.speed = 1f; // 애니메이터 속도 복구
        }
        flashCoroutine = null;
        isEffectStarted = false;
    }
}