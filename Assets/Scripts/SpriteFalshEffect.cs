using System.Collections;
using UnityEngine;

public class SpriteFlashEffect : MonoBehaviour
{
    [Header("피격 깜빡임 연출 설정")]
    [Tooltip("데미지를 입었을 때 바뀔 색상입니다.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [Tooltip("한 번 깜빡일 때 색상이 유지되는 시간입니다.")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("총 몇 번 깜빡일지 지정합니다.")]
    [SerializeField] private int flashCount = 3;

    [Header("★ 사망 사라짐 연출 설정")]
    [Tooltip("체력이 0이 되어 캐릭터가 완전히 투명해질 때까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float fadeOutDuration = 2f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// 외부(PlayerControll 등)에서 데미지를 받았을 때 호출하는 피격 깜빡임 함수
    /// </summary>
    public void Flash()
    {
        if (spriteRenderer == null) return;

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
    /// ★ 외부에서 체력이 0이 되었을 때 호출하면 캐릭터를 서서히 투명하게 소멸시키는 함수
    /// </summary>
    public void StartFadeOut()
    {
        if (spriteRenderer == null) return;

        // 혹시 작동 중이던 피격 깜빡임 코루틴이 있다면 강제로 정지시킵니다.
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsedTime = 0f;
        // 현재 스프라이트의 색상값(알파 포함)을 시작 지점으로 설정합니다.
        Color startColor = spriteRenderer.color;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;

            // 시간에 따라 1에서 0으로 변하는 비율을 계산합니다.
            float alpha = Mathf.Clamp01(1f - (elapsedTime / fadeOutDuration));

            // 다른 RGB 색상 필터는 유지한 채 Alpha(투명도)만 줄여나갑니다.
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // 오차 보정 및 확실하게 완전 투명화 고정
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Debug.Log($"{gameObject.name} 캐릭터가 완전히 투명해져 소멸했습니다.");
    }

    void OnDisable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        flashCoroutine = null;
    }
}