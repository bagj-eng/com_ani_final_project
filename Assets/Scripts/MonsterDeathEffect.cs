using System.Collections;
using UnityEngine;

public class MonsterDeathEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine hitCoroutine;
    private bool isDead = false;

    [Header("피격 연출 세팅")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitDuration = 0.1f;

    [Header("사망 페이드아웃 세팅")]
    // ★ 무조건 10초 동안 스르륵 사라지도록 고정했습니다.
    private float fadeDuration = 10.0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void PlayHitEffect()
    {
        if (isDead || spriteRenderer == null) return;

        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        hitCoroutine = StartCoroutine(HitFlashRoutine());
    }

    public void PlayDeathEffect()
    {
        if (isDead || spriteRenderer == null) return;
        isDead = true;

        if (hitCoroutine != null) StopCoroutine(hitCoroutine);

        // 상위 부모 오브젝트가 지워지면서 연출이 끊기는 걸 방지하기 위해 독립시킵니다.
        transform.SetParent(null);

        // 몬스터의 애니메이션, 리지드바디, 콜라이더, AI 스크립트를 그 자리에서 즉시 얼림
        FreezeMonsterTotally();

        // 방해받지 않는 독립적인 10초 페이드아웃 가동
        StartCoroutine(AbsoluteFadeOutRoutine());
    }

    private void FreezeMonsterTotally()
    {
        // 1. 애니메이션 즉시 정지 (굳어버림)
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.speed = 0f;
        }

        // 2. 물리 속도 제로 고정 및 키네마틱 전환 (외력 차단)
        Rigidbody2D rigid = GetComponent<Rigidbody2D>();
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.bodyType = RigidbodyType2D.Kinematic;
        }

        // 3. 충돌체(Collider) 전부 비활성화 (플레이어가 통과 가능)
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D coll in colliders)
        {
            coll.enabled = false;
        }

        // 4. 나 자신(Effect)을 제외한 부착된 모든 컴포넌트(AI, 이동 등) 비활성화
        MonoBehaviour[] behaviors = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behavior in behaviors)
        {
            if (behavior != this)
            {
                behavior.enabled = false;
            }
        }
    }

    private IEnumerator AbsoluteFadeOutRoutine()
    {
        Color startColor = spriteRenderer.color;
        float elapsedTime = 0f;

        // 시스템 타임을 기준으로 정확히 10초 동안 루프를 돕니다.
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 10초에 걸쳐 알파값(투명도)을 1에서 0으로 부드럽게 보간
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);

            yield return null;
        }

        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // 10초 연출이 완벽히 마감되면 씬에서 삭제
        Destroy(gameObject);
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitDuration);
        spriteRenderer.color = originalColor;
        hitCoroutine = null;
    }
}