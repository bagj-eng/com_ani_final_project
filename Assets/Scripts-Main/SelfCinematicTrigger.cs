using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SelfCinematicTrigger : MonoBehaviour
{
    [Header("🎬 카메라 및 타겟 설정")]
    [Tooltip("Hierarchy 창의 실제 Main Camera를 넣어주세요.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("플레이어 캐릭터 오브젝트를 넣어주세요.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("맵 끝에 배치한 흑막의 '위치 좌표(부모 빈 오브젝트)'를 넣어주세요.")]
    [SerializeField] private Transform hiddenCharacterTransform;

    [Header("💬 UI 및 연출 오브젝트")]
    [Tooltip("대사 자막을 띄울 TextMesh Pro 텍스트 컴포넌트")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("맵 끝에 배치했으나 인스펙터 체크를 꺼둔 흑막 캐릭터 오브젝트")]
    [SerializeField] private GameObject hiddenCharacter;
    [Tooltip("암전용 블랙스크린 이미지에 추가한 Canvas Group 컴포넌트")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    private float originalLensSize;
    private static bool isCinematicPlaying = false;

    void Start()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (blackScreenCanvasGroup != null) blackScreenCanvasGroup.alpha = 0f;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) originalLensSize = mainCamera.orthographicSize;
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        if (isCinematicPlaying) return;

        isCinematicPlaying = true;

        GameObject cinematicRunner = new GameObject("CinematicRunner");
        DontDestroyOnLoad(cinematicRunner);
        cinematicRunner.AddComponent<CinematicExecutor>().Setup(
            mainCamera, playerTransform, hiddenCharacterTransform,
            dialogueText, hiddenCharacter, blackScreenCanvasGroup, originalLensSize
        );
    }
}

public class CinematicExecutor : MonoBehaviour
{
    private Camera mainCamera;
    private Transform playerTransform;
    private Transform hiddenCharacterTransform;
    private TextMeshProUGUI dialogueText;
    private GameObject hiddenCharacter;
    private CanvasGroup blackScreenCanvasGroup;
    private float originalLensSize;

    public void Setup(Camera cam, Transform player, Transform hiddenTarget, TextMeshProUGUI text, GameObject hiddenChar, CanvasGroup blackGroup, float lensSize)
    {
        mainCamera = cam;
        playerTransform = player;
        hiddenCharacterTransform = hiddenTarget;
        dialogueText = text;
        hiddenCharacter = hiddenChar;
        blackScreenCanvasGroup = blackGroup;
        originalLensSize = lensSize;

        StartCoroutine(CinematicRoutine());
    }

    private IEnumerator CinematicRoutine()
    {
        // ================================================================
        // 🔒 1. [즉시 작동] 키보드 조작 및 인풋 시스템 물리적 완전 전원 차단
        // ================================================================
        if (playerTransform != null)
        {
            Rigidbody2D playerRigid = playerTransform.GetComponent<Rigidbody2D>();
            if (playerRigid != null)
            {
                playerRigid.linearVelocity = Vector2.zero;
                playerRigid.bodyType = RigidbodyType2D.Static;
            }

            var playerInput = playerTransform.GetComponent("PlayerInput");
            if (playerInput != null)
            {
                (playerInput as MonoBehaviour).enabled = false;
            }

            MonoBehaviour[] scripts = playerTransform.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this && !(script is SpriteRenderer) && !(script is Animator))
                {
                    script.enabled = false;
                }
            }
        }

        if (mainCamera != null)
        {
            var brain = mainCamera.GetComponent("CinemachineBrain");
            if (brain != null)
            {
                (brain as MonoBehaviour).enabled = false;
            }
        }

        // ================================================================
        // 🎵 2. [즉시 작동] BGM 음량 3초 동안 서서히 줄인 뒤 끄기 호출
        // ================================================================
        AudioSource bgmSource = FindObjectOfType<AudioSource>(); // 씬 내의 오디오 소스를 자동 검색
        if (bgmSource != null)
        {
            StartCoroutine(FadeOutBGM(bgmSource, 3.0f));
        }

        // ================================================================
        // 🕒 3. [보스 사망 후 3초 대기] 카메라와 대사 없이 정적 유지 (슬로우 모션 포함)
        // ================================================================
        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 현실 시간(Realtime) 기준으로 정확히 3.0초 동안 가만히 대기합니다.
        yield return new WaitForSecondsRealtime(3.0f);

        // 정적이 끝났으므로 게임 속도를 다시 정상으로 돌려놓습니다.
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // ================================================================
        // 💬 4. [대사 연출] "이제 끝인가.." 출력 및 텍스트 속도 조절
        // ================================================================
        // 카메라를 움직이지 않고 제자리에서 대사를 띄웁니다.
        if (dialogueText != null) dialogueText.text = "\"이제 끝인가..\"";

        // 플레이어가 글을 충분히 읽을 수 있도록 대사 유지 시간을 3.0초로 증가시켰습니다.
        yield return new WaitForSeconds(3.0f);
        if (dialogueText != null) dialogueText.text = "";

        // 🔥 [조건 추가]: 말을 다 하고 정확히 1초 뒤에 카메라가 움직이기 시작합니다.
        yield return new WaitForSeconds(1.0f);

        // ================================================================
        // 👥 5. [흑막 등장] 카메라 이동 및 대사 가독성 템포 조절
        // ================================================================
        if (hiddenCharacter != null) hiddenCharacter.SetActive(true);

        // 흑막에게로 부드럽게 카메라 워킹 이동 (1.5초 소요)
        yield return StartCoroutine(KeepTrackingTargetRealtime(hiddenCharacterTransform, 1.5f));

        // 말이 너무 빨라 지나가지 않도록 각 대사당 대기 시간을 3.5초로 넉넉히 늘렸습니다.
        if (dialogueText != null) dialogueText.text = "\"실험작이 죽어 아쉽군\"";
        yield return new WaitForSeconds(3.5f);

        if (dialogueText != null) dialogueText.text = "\"다음에 보자고\"";
        yield return new WaitForSeconds(3.5f);

        // ================================================================
        // 🔍 6. [종막] 플레이어 강제 클로즈업 줌인 + 암전 페이드
        // ================================================================
        if (mainCamera != null && playerTransform != null)
        {
            mainCamera.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            mainCamera.orthographicSize = originalLensSize * 0.4f;
        }
        if (dialogueText != null) dialogueText.text = "";

        float fadeDuration = 0.7f; // 암전도 분위기 있게 조금 더 서서히 진행
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (blackScreenCanvasGroup != null)
            {
                blackScreenCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            }
            yield return null;
        }
        if (blackScreenCanvasGroup != null) blackScreenCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
        SceneManager.LoadScene("Ending");
    }

    // 현실 시간 기준으로 부드럽게 카메라를 이동시키는 함수
    private IEnumerator KeepTrackingTargetRealtime(Transform target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (target != null && mainCamera != null)
            {
                Vector3 targetPos = new Vector3(target.position.x, target.position.y, -10f);
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.unscaledDeltaTime * 5f);
            }
            yield return null;
        }
    }

    // 🎵 볼륨을 3초 동안 선형 보간으로 줄여주는 서브 코루틴
    private IEnumerator FadeOutBGM(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 슬로우 모션 영향 없이 현실 시간 기준 연산
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop(); // 완전히 정지
    }
}