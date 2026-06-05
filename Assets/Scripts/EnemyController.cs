using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("몬스터 능력치")]
    [SerializeField] private int maxHp = 30;
    private int currentHp;
    private bool isDead = false;

    private Animator animator;
    private Collider2D enemyCollider;
    private Rigidbody2D rigid;
    private SpriteFlashEffect flashEffect;

    void Start()
    {
        currentHp = maxHp;

        // 컴포넌트들을 안전하게 가져옵니다. (없어도 에러 안 나게 안전장치 처리)
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        rigid = GetComponent<Rigidbody2D>();
        flashEffect = GetComponent<SpriteFlashEffect>();
    }

    /// <summary>
    /// 플레이어가 공격했을 때 이 함수를 호출하여 몬스터에게 데미지를 줍니다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return; // 이미 죽은 몬스터면 무시

        currentHp -= damage;
        Debug.Log($"{gameObject.name} 몬스터 피격! 남은 체력: {currentHp}");

        // 1. 피격 애니메이션 트리거가 있다면 실행 (애니메이터에 Take_Hit가 있을 경우)
        if (animator != null)
        {
            animator.SetTrigger("Take_Hit");
        }

        // 2. 피격 빨간색 깜빡임 효과 호출
        if (flashEffect != null)
        {
            flashEffect.Flash();
        }

        // 3. 체력이 0 이하가 되면 사망 처리
        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 몬스터 사망 연출 가동");

        // 사망 애니메이션 실행 (애니메이터에 Death 트리거가 있을 경우)
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // 물리적인 충돌 및 움직임을 차단하여 시체 무시 처리
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.bodyType = RigidbodyType2D.Kinematic; // 땅으로 꺼지거나 안 밀리게 고정
        }

        // 4. 이전에 만든 스크립트를 이용해 서서히 투명해지며 사라지는 연출 가동!
        if (flashEffect != null)
        {
            flashEffect.StartFadeOut();
        }

        // 5. 지정한 시간 뒤에 하이어라키 창에서 몬스터 완전 제거 (예: 2.5초 뒤)
        Destroy(gameObject, 2.5f);
    }
}