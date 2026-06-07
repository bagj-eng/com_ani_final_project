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

    /// <summary>
    /// 보스가 죽는 순간 직접 신호를 받아 연출을 트리거하는 안전 장치 함수
    /// </summary>
    public void OnTargetMonsterDestroyed(GameObject deadMonster)
    {
        if (isCinematicStarted) return;

        // 인스펙터에 지정된 몬스터가 죽은 게 맞다면 즉시 100% 가동
        if (targetMonster != null && deadMonster == targetMonster)
        {
            isCinematicStarted = true;
            StartCoroutine(CinematicRoutine());
        }
    }

    private IEnumerator CinematicRoutine()
    {
        // 1. 막타 타임 슬로우
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(0.4f);
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 2. 플레이어 카메라 추적 및 대사
        StartTracking(playerTransform);
        if (dialogueText != null) dialogueText.text = "플레이어: \"이제 끝인가..\"";
        yield return new WaitForSeconds(2.0f);
        if (dialogueText != null) dialogueText.text = "";

        // 3. 흑막 캐릭터 등장 및 카메라 이동
        if (hiddenCharacter != null) hiddenCharacter.SetActive(true);
        StartTracking(hiddenCharacterTransform);
        yield return new WaitForSeconds(1.5f);

        if (dialogueText != null) dialogueText.text = "???: \"실험작이 죽어 아쉽군\"";
        yield return new WaitForSeconds(2.0f);
        if (dialogueText != null) dialogueText.text = "???: \"다음에 보자고\"";
        yield return new WaitForSeconds(1.5f);

        // 4. 플레이어 포커싱 줌인 + 암전 페이드
        StopTracking();
        if (mainCamera != null && playerTransform != null)
        {
            mainCamera.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            mainCamera.orthographicSize = originalLensSize * 0.5f;
        }
        if (dialogueText != null) dialogueText.text = "";

        float fadeDuration = 0.5f;
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

        // 5. 엔딩 씬으로 전환
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