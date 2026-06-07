using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BossMapCutscene : MonoBehaviour
{
    [Header("카메라 및 대상 설정")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bossTransform;

    [Header("카메라 연출 세팅")]
    [SerializeField] private float closeZoomSize = 3.5f;
    [SerializeField] private float cameraMoveSpeed = 5f;
    [Tooltip("카메라 구도를 위로 올릴 높이 값입니다. (값이 클수록 카메라가 위를 비춥니다)")]
    [SerializeField] private float cameraYOffset = 1.2f;

    [Header("평소 플레이어 추적 카메라 스크립트")]
    [Tooltip("평소에 플레이어를 졸졸 따라다니던 'PlayerFollowCamera' 스크립트를 여기에 넣어주세요.")]
    [SerializeField] private MonoBehaviour playerFollowCamera;

    [Header("보스 감지 범위 오브젝트 설정")]
    [SerializeField] private GameObject bossDetectRangeObject;

    [Header("대사창 UI 설정")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    private TypewriterEffect typewriter;

    [Header("🎵 오디오 세팅 (볼륨 직접 조절)")]
    [SerializeField] private AudioClip cutsceneBgmClip;
    [Range(0f, 1f)][SerializeField] private float cutsceneVolume = 0.1f;

    [Space(10)]
    [SerializeField] private AudioClip battleBgmClip;
    [Range(0f, 1f)][SerializeField] private float battleVolume = 0.8f;

    private AudioSource cutsceneAudioSource;
    private AudioSource battleAudioSource;

    private float originalCameraSize;
    private Vector3 originalCameraPos;
    private Transform cameraTarget;
    private bool isTracking = false;

    // ★ 전역 공유 플래그: 이 값이 true일 때 플레이어의 키보드 입력(Update)이 전면 차단됩니다.
    public static bool IsCutsceneActive { get; private set; } = false;

    private Rigidbody2D playerRigid;
    private Rigidbody2D bossRigid;

    void Awake()
    {
        // 씬 로딩 및 생성 즉시 컷씬 플래그 가동 (플레이어 키보드 즉시 정지)
        IsCutsceneActive = true;

        // 1번째 컷씬용 오디오 소스 설정
        cutsceneAudioSource = gameObject.AddComponent<AudioSource>();
        cutsceneAudioSource.clip = cutsceneBgmClip;
        cutsceneAudioSource.loop = false;
        cutsceneAudioSource.playOnAwake = false;
        cutsceneAudioSource.volume = cutsceneVolume;

        // 2번째 전투용 오디오 소스 설정
        battleAudioSource = gameObject.AddComponent<AudioSource>();
        battleAudioSource.clip = battleBgmClip;
        battleAudioSource.loop = true;
        battleAudioSource.playOnAwake = false;
        battleAudioSource.volume = battleVolume;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraSize = mainCamera.orthographicSize;
            originalCameraPos = mainCamera.transform.position;
        }

        if (dialogueText != null) typewriter = dialogueText.GetComponent<TypewriterEffect>();

        // 물리 컴포넌트 예외 처리 및 속도 초기화 (밀림 방지)
        if (playerTransform != null) playerRigid = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRigid != null) playerRigid.linearVelocity = Vector2.zero;

        if (bossTransform != null)
        {
            bossRigid = bossTransform.GetComponent<Rigidbody2D>();
            if (bossRigid != null) bossRigid.linearVelocity = Vector2.zero;

            Animator bossAnim = bossTransform.GetComponent<Animator>();
            if (bossAnim != null)
            {
                bossAnim.SetFloat("Speed", 0f);
                bossAnim.SetBool("isMove", false);
            }
        }

        if (bossDetectRangeObject != null) bossDetectRangeObject.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 평소 카메라 추적 스크립트 잠시 오프 (연출 방해 방지)
        if (playerFollowCamera != null) playerFollowCamera.enabled = false;

        // 씬 진입과 동시에 컷씬 브금 가동
        if (cutsceneAudioSource.clip != null) cutsceneAudioSource.Play();

        // 14초 정밀 컷씬 코루틴 시작
        StartCoroutine(Strict14SecondsCutsceneRoutine());
    }

    void LateUpdate()
    {
        // ★ 추적 시 targetPos의 y값에 cameraYOffset을 더해 구도를 위로 올립니다.
        if (isTracking && cameraTarget != null && mainCamera != null)
        {
            Vector3 targetPos = new Vector3(cameraTarget.position.x, cameraTarget.position.y + cameraYOffset, originalCameraPos.z);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
        }

        // 컷씬 도중 보스가 물리 충돌이나 다른 스크립트 때문에 미끄러지는 버그 방지
        if (IsCutsceneActive && bossRigid != null)
        {
            bossRigid.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator Strict14SecondsCutsceneRoutine()
    {
        // ================================================================
        // 1. [0초 ~ 2초] 플레이어 클로즈업 및 좌우 두리번거리기 (키보드는 정지 상태)
        // ================================================================
        cameraTarget = playerTransform;
        isTracking = true;
        StartCoroutine(ChangeCameraSize(closeZoomSize, 0.4f));

        Vector3 leftScale = new Vector3(-4, 4, 1);
        Vector3 rightScale = new Vector3(4, 4, 1);

        yield return new WaitForSeconds(0.5f);
        if (playerTransform != null) playerTransform.localScale = leftScale;
        yield return new WaitForSeconds(0.7f);
        if (playerTransform != null) playerTransform.localScale = rightScale;
        yield return new WaitForSeconds(0.8f);

        // ================================================================
        // 2. [2초 ~ 6초] 보스 클로즈업 + 보스의 대사 출력 (4초)
        // ================================================================
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        cameraTarget = bossTransform;
        SetDialogue("제 발로 심장부까지 걸어 들어오다니, 어리석은 놈.");
        yield return new WaitForSeconds(4.0f);

        // ================================================================
        // 3. [6초 ~ 10초] 플레이어 클로즈업 + 플레이어의 대사 출력 (4초)
        // ================================================================
        cameraTarget = playerTransform;
        SetDialogue("쓰러뜨려주지");
        yield return new WaitForSeconds(4.0f);

        // ================================================================
        // 4. [10초 ~ 13초] 대사창 비활성화 + 원래 기본 카메라 구도로 부드러운 복귀 (4초)
        // ================================================================
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        isTracking = false;

        // ★ [오디오 자연 전환] 카메라가 원래대로 돌아오기 시작하는 이 시점부터 페이드 크로스오버를 시작합니다.
        // 카메라 복귀 시간인 2.2초 동안 자연스럽게 1번째 브금은 줄어들고 2번째 브금은 켜지며 커집니다.
        StartCoroutine(FadeAudioSources(2.2f));

        StartCoroutine(ChangeCameraSize(originalCameraSize, 2.2f));
        float returnTime = 0f;
        Vector3 cutsceneEndPos = mainCamera.transform.position;

        while (returnTime < 2.2f)
        {
            returnTime += Time.deltaTime;
            // ★ 복귀할 때도 플레이어 머리 위 구도를 유지하기 위해 cameraYOffset을 반영합니다.
            Vector3 realTimePlayerPos = new Vector3(playerTransform.position.x, playerTransform.position.y + cameraYOffset, originalCameraPos.z);
            mainCamera.transform.position = Vector3.Lerp(cutsceneEndPos, realTimePlayerPos, returnTime / 2.2f);
            yield return null;
        }

        // ================================================================
        // 🛠️ [어색함 완충 보정]: 스크립트 이관 전 카메라 영점 완벽 동기화 (0.3초)
        // ================================================================
        float blendTime = 0f;
        Vector3 preBlendPos = mainCamera.transform.position;
        while (blendTime < 0.3f)
        {
            blendTime += Time.deltaTime;
            // ★ 완충 보정 단계에서도 완벽한 동기화를 위해 동일하게 cameraYOffset을 더해줍니다.
            Vector3 finalFollowTarget = new Vector3(playerTransform.position.x, playerTransform.position.y + cameraYOffset, originalCameraPos.z);
            mainCamera.transform.position = Vector3.Lerp(preBlendPos, finalFollowTarget, blendTime / 0.3f);
            yield return null;
        }

        // ================================================================
        // 🔓 [컷씬 최종 종료 및 제어권 반환]
        // ================================================================
        IsCutsceneActive = false;

        // 보스 AI 감지 범위 기믹 가동
        if (bossDetectRangeObject != null) bossDetectRangeObject.SetActive(true);

        // 평소 플레이어 추적 카메라 스크립트 재가동 (화면 튐 제로)
        if (playerFollowCamera != null)
        {
            playerFollowCamera.enabled = true;
        }

        Debug.Log("⚔️ 모든 보스룸 연출 종료 및 BGM 자연 전환 완료! 플레이어 조작 권한이 정상 복구되었습니다.");
    }

    private void SetDialogue(string text)
    {
        if (dialogueText == null) return;
        if (typewriter != null) typewriter.ChangeTextAndType(text);
        else dialogueText.text = text;
    }

    private IEnumerator ChangeCameraSize(float targetSize, float duration)
    {
        if (mainCamera == null) yield break;
        float startSize = mainCamera.orthographicSize;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, time / duration);
            yield return null;
        }
        mainCamera.orthographicSize = targetSize;
    }

    // ★ [새로 추가된 오디오 크로스페이드 코루틴]
    private IEnumerator FadeAudioSources(float duration)
    {
        float time = 0f;

        // 전투용 오디오 소스를 볼륨 0인 상태에서 먼저 재생을 시작합니다.
        if (battleAudioSource.clip != null)
        {
            battleAudioSource.volume = 0f;
            battleAudioSource.Play();
        }

        float startCutsceneVolume = cutsceneAudioSource.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = time / duration;

            // 1번째 오디오는 설정된 볼륨 값에서 0으로 서서히 줄어듭니다.
            cutsceneAudioSource.volume = Mathf.Lerp(startCutsceneVolume, 0f, progress);

            // 2번째 오디오는 0에서 인스펙터 설정 볼륨 값(battleVolume)까지 서서히 커집니다.
            if (battleAudioSource.clip != null)
            {
                battleAudioSource.volume = Mathf.Lerp(0f, battleVolume, progress);
            }

            yield return null;
        }

        // 완벽히 페이드가 끝나면 1번째 연출용 오디오를 완전히 정지하고 볼륨을 정리합니다.
        cutsceneAudioSource.Stop();
        cutsceneAudioSource.volume = 0f;
        if (battleAudioSource.clip != null) battleAudioSource.volume = battleVolume;
    }
}