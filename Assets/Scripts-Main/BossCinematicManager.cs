using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BossCinematicManager : MonoBehaviour
{
    [Header("🎯 시네마틱 트리거 대상 설정")]
    [Tooltip("누가 죽을 때 연출을 시작할지 Hierarchy의 보스 오브젝트를 여기에 드래그해서 넣어주세요.")]
    [SerializeField] private GameObject targetMonster;

    [Header("🎬 카메라 및 타겟 설정")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform hiddenCharacterTransform;

    [Header("💬 UI 및 연출 오브젝트")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject hiddenCharacter;
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    private Coroutine cameraTrackingCoroutine;
    private float originalLensSize;
    private bool isCinematicStarted = false;


    void Start()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (blackScreenCanvasGroup != null) blackScreenCanvasGroup.alpha = 0f;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) originalLensSize = mainCamera.orthographicSize;
    }

    public void OnTargetMonsterDestroyed(GameObject deadMonster)
    {
        if (isCinematicStarted) return;

        if (targetMonster != null && deadMonster == targetMonster)
        {
            isCinematicStarted = true;
            StartCoroutine(CinematicRoutine());
        }
    }

    private IEnumerator CinematicRoutine()
    {
        // ================================================================
        // 1. 막타 타임 슬로우
        // ================================================================
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(0.4f);
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // ================================================================
        // 2. 플레이어 카메라 추적 및 대사
        // ================================================================
        StartTracking(playerTransform);
        if (dialogueText != null) dialogueText.text = "플레이어: \"이제 끝인가..\"";
        yield return new WaitForSeconds(2.0f);
        if (dialogueText != null) dialogueText.text = "";

        // ================================================================
        // 3. 흑막 캐릭터 등장 및 카메라 이동 (기본 거리에서 부드럽게 추적)
        // ================================================================
        if (hiddenCharacter != null) hiddenCharacter.SetActive(true);
        StartTracking(hiddenCharacterTransform);
        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null) dialogueText.text = "???: \"실험작이 죽어 아쉽군\"";
        yield return new WaitForSeconds(2.5f); // 첫 대사를 읽을 시간 제공

        // ================================================================
        // 4. ★ "다음에 보자고" 출력과 동시에 히든 캐릭터 강렬하게 줌인 포커싱
        // ================================================================
        // 부드러운 Lerp 추적을 멈추고, 히든 캐릭터의 정중앙으로 카메라를 꽂아버립니다.
        StopTracking();

        if (mainCamera != null && hiddenCharacterTransform != null)
        {
            mainCamera.transform.position = new Vector3(hiddenCharacterTransform.position.x, hiddenCharacterTransform.position.y, -10f);
            mainCamera.orthographicSize = originalLensSize * 0.5f; // 원래 크기의 반으로 확 줌인
        }

        // 줌인되어 화면 가득 히든 캐릭터가 보이는 순간 마지막 대사 출력!
        if (dialogueText != null) dialogueText.text = "???: \"다음에 보자고\"";
        yield return new WaitForSeconds(2.0f); // 충격적인 클로즈업 상태 유지

        if (dialogueText != null) dialogueText.text = "";

        // ================================================================
        // 5. 암전 페이드 아웃 (여전히 히든 캐릭터를 바라본 상태로 어두워짐)
        // ================================================================
        float fadeDuration = 2f;
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

        yield return new WaitForSeconds(1.0f);

        // ================================================================
        // 6. 엔딩 씬으로 전환
        // ================================================================
        SceneManager.LoadScene("Ending");
    }

    private void StartTracking(Transform target)
    {
        StopTracking();
        if (mainCamera != null && target != null)
        {
            cameraTrackingCoroutine = StartCoroutine(KeepTrackingTarget(target));
        }
    }

    private void StopTracking()
    {
        if (cameraTrackingCoroutine != null) StopCoroutine(cameraTrackingCoroutine);
    }

    private IEnumerator KeepTrackingTarget(Transform target)
    {
        while (target != null && mainCamera != null)
        {
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, -10f);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * 5f);
            yield return null;
        }
    }
}