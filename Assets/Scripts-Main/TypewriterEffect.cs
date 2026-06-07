using System.Collections;
using UnityEngine;
using TMPro; // TextMesh Pro 사용 필수

[RequireComponent(typeof(TextMeshProUGUI))]
public class TypewriterEffect : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private string originalText;
    private Coroutine typingCoroutine;

    [Header("타이핑 속도 세팅")]
    [Tooltip("한 글자가 출력되는 데 걸리는 시간(초)입니다. 도트 폰트는 0.05~0.08초가 가장 맛이 삽니다.")]
    [SerializeField] private float typingSpeed = 0.06f;

    [Header("도트 타자기 사운드 세팅 (단발성)")]
    [Tooltip("글자가 한 글자씩 찍힐 때마다 재생할 짧은 도트 효과음 에셋을 넣어주세요.")]
    [SerializeField] private AudioClip typeSound;

    // ================================================================
    // 🎵 [새로 추가된 글씨 쓰기 루프 브금 세팅]
    // ================================================================
    [Header("글씨 쓰기 배경음 세팅 (반복 재생)")]
    [Tooltip("글씨가 타이핑되는 동안 계속 반복(Loop)해서 흘러나올 배경 음악/효과음 에셋을 넣어주세요.")]
    [SerializeField] private AudioClip typingLoopBgm;

    [Range(0f, 1f)]
    [Tooltip("글씨 쓰는 루프 브금의 볼륨을 조절합니다.")]
    [SerializeField] private float loopBgmVolume = 0.5f;

    private AudioSource audioSource;       // 단발성 틱 사운드용 오디오 소스
    private AudioSource loopBgmAudioSource; // ★ 루프 브금 전용 오디오 소스

    void Awake()
    {
        // 컴포넌트 캐싱
        textComponent = GetComponent<TextMeshProUGUI>();

        // 1. 단발성 타자기 소리용 오디오 소스 세팅
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && typeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 2. ★ [추가] 글씨 쓰는 루프 브금용 전용 오디오 소스 개별 생성 (소리가 섞이지 않게 분리)
        loopBgmAudioSource = gameObject.AddComponent<AudioSource>();
        loopBgmAudioSource.clip = typingLoopBgm;
        loopBgmAudioSource.loop = true;          // 무한 반복 설정
        loopBgmAudioSource.playOnAwake = false;
        loopBgmAudioSource.volume = loopBgmVolume;
    }

    void OnEnable()
    {
        if (textComponent == null) return;

        originalText = textComponent.text;
        textComponent.text = "";

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            // 혹시 이전 루프 브금이 돌고 있었다면 강제 정지
            if (loopBgmAudioSource != null) loopBgmAudioSource.Stop();
        }

        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    private IEnumerator TypeTextRoutine()
    {
        // 🎵 ★ [루프 브금 시작]: 글씨 타이핑 연출이 시작되자마자 브금 On!
        if (loopBgmAudioSource != null && typingLoopBgm != null && originalText.Length > 0)
        {
            loopBgmAudioSource.volume = loopBgmVolume; // 실시간 볼륨 수치 동기화
            loopBgmAudioSource.Play();
        }

        // 텍스트 컴포넌트에 한 글자 한 글자 누적하여 출력
        for (int i = 0; i <= originalText.Length; i++)
        {
            textComponent.text = originalText.Substring(0, i);

            // 단발성 도트 소리 틱! 틱! 내기
            if (i > 0 && i < originalText.Length && originalText[i - 1] != ' ')
            {
                if (audioSource != null && typeSound != null)
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(typeSound);
                }
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        // 🎵 ★ [루프 브금 종료]: 모든 글자가 다 써지면 즉시 음악을 칼같이 정지합니다.
        if (loopBgmAudioSource != null && loopBgmAudioSource.isPlaying)
        {
            loopBgmAudioSource.Stop();
        }

        typingCoroutine = null;
    }

    public void ChangeTextAndType(string newText)
    {
        originalText = newText;
        if (gameObject.activeInHierarchy)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                if (loopBgmAudioSource != null) loopBgmAudioSource.Stop();
            }
            typingCoroutine = StartCoroutine(TypeTextRoutine());
        }
    }

    // 오브젝트가 갑자기 비활성화되거나 씬이 넘어갈 때 루프 소리가 남아있는 버그 방지 예외 처리
    void OnDisable()
    {
        if (loopBgmAudioSource != null && loopBgmAudioSource.isPlaying)
        {
            loopBgmAudioSource.Stop();
        }
    }
}