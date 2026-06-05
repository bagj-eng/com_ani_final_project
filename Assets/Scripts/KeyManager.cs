using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance { get; private set; }

    [Header("열쇠 UI 설정")]
    [SerializeField] private GameObject keyAImage;
    [SerializeField] private GameObject keyBImage;

    [Header("나무(타일맵) 오브젝트 설정")]
    [SerializeField] private MovingObstacle leftTree;
    [SerializeField] private MovingObstacle rightTree;
    [SerializeField] private float treeMoveDistance = 3f;
    [SerializeField] private float treeMoveSpeed = 2f;

    [Header("카메라 연출 설정")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform treeCenterTransform;

    [Header("시네마틱 영화 연출 UI 설정")]
    [SerializeField] private GameObject cinematicPanels;
    [SerializeField] private GameObject exclamationMark;

    [Header("시네마틱 패널 설정")]
    [SerializeField] private RectTransform topPanelRect;
    [SerializeField] private RectTransform bottomPanelRect;
    [Tooltip("패널이 들어오거나 나갈 때 걸리는 시간 (초)")]
    [SerializeField] private float panelMoveDuration = 1.2f;

    [Header("시네마틱 사운드 교체 설정")]
    [Tooltip("기존 마을 배경음이 재생되고 있는 BGM_Manager 오브젝트를 드래그해서 넣어주세요.")]
    [SerializeField] private AudioSource townBgmAudioSource;
    [Tooltip("나무가 움직일 때(10초 시점) 새로 재생할 컷씬용 오디오 클립을 넣어주세요.")]
    [SerializeField] private AudioClip cinematicNewBgmClip;
    [Tooltip("새로 바뀔 브금의 볼륨 크기 (0.0 ~ 1.0)")]
    [SerializeField] private float cinematicBgmVolume = 0.4f;

    [Header("카메라 화면 이탈 방지 한계선")]
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    [Header("화면 흔들림(Shake) 세팅")]
    [SerializeField] private float baseShakeMagnitude = 0.08f;

    private float originalCameraSize;
    private bool isCutscenePlayed = false;

    // 지정해주신 정확한 Y 좌표 상수 값 정의
    private const float TOP_START_Y = 770f;
    private const float TOP_TARGET_Y = 620f;
    private const float BOTTOM_START_Y = -770f;
    private const float BOTTOM_TARGET_Y = -635f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ClearKeysOnGameStart()
    {
        PlayerPrefs.SetInt("Saved_HasKeyA", 0);
        PlayerPrefs.SetInt("Saved_HasKeyB", 0);
        PlayerPrefs.Save();
    }

    public bool HasKeyA
    {
        get { return PlayerPrefs.GetInt("Saved_HasKeyA", 0) == 1; }
        private set { PlayerPrefs.SetInt("Saved_HasKeyA", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public bool HasKeyB
    {
        get { return PlayerPrefs.GetInt("Saved_HasKeyB", 0) == 1; }
        private set { PlayerPrefs.SetInt("Saved_HasKeyB", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) originalCameraSize = mainCamera.orthographicSize;

        if (cinematicPanels != null) cinematicPanels.SetActive(false);
        if (exclamationMark != null) exclamationMark.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (keyAImage != null) keyAImage.SetActive(HasKeyA);
        if (keyBImage != null) keyBImage.SetActive(HasKeyB);
    }

    public void GetKey(string keyType)
    {
        if (keyType == "A" || keyType == "a") HasKeyA = true;
        else if (keyType == "B" || keyType == "b") HasKeyB = true;

        RefreshUI();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && HasKeyA && HasKeyB && !isCutscenePlayed &&
            SceneManager.GetActiveScene().name == "Main Town")
        {
            if (gameObject.CompareTag("key1") || gameObject.CompareTag("key2")) return;

            isCutscenePlayed = true;
            PlayerControll playerScript = collision.GetComponent<PlayerControll>();

            if (playerScript != null)
            {
                StartCoroutine(CinematicCutsceneRoutine(playerScript));
            }
        }
    }

    private Vector3 ClampCameraPosition(Vector3 targetPos)
    {
        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);
        return new Vector3(clampedX, clampedY, targetPos.z);
    }

    // 패널 등장 애니메이션 코루틴 (화면 밖 -> 화면 안)
    private IEnumerator SlidePanelsIn()
    {
        if (topPanelRect == null || bottomPanelRect == null) yield break;

        Vector2 topStartPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_START_Y);
        Vector2 bottomStartPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_START_Y);

        Vector2 topTargetPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_TARGET_Y);
        Vector2 bottomTargetPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_TARGET_Y);

        topPanelRect.anchoredPosition = topStartPos;
        bottomPanelRect.anchoredPosition = bottomStartPos;

        float elapsedTime = 0f;
        while (elapsedTime < panelMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panelMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            topPanelRect.anchoredPosition = Vector2.Lerp(topStartPos, topTargetPos, t);
            bottomPanelRect.anchoredPosition = Vector2.Lerp(bottomStartPos, bottomTargetPos, t);
            yield return null;
        }

        topPanelRect.anchoredPosition = topTargetPos;
        bottomPanelRect.anchoredPosition = bottomTargetPos;
    }

    // ★ [추가] 패널 퇴장 애니메이션 코루틴 (화면 안 -> 화면 밖 원상복구)
    private IEnumerator SlidePanelsOut()
    {
        if (topPanelRect == null || bottomPanelRect == null) yield break;

        // 현재 들어와 있는 중간 위치가 시작점이 됩니다.
        Vector2 topStartPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_TARGET_Y);
        Vector2 bottomStartPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_TARGET_Y);

        // 다시 화면 허공 밖으로 치워버릴 최종 목적지 세팅
        Vector2 topTargetPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_START_Y);
        Vector2 bottomTargetPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_START_Y);

        float elapsedTime = 0f;
        while (elapsedTime < panelMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / panelMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t); // 나갈 때도 부드럽게 무빙

            topPanelRect.anchoredPosition = Vector2.Lerp(topStartPos, topTargetPos, t);
            bottomPanelRect.anchoredPosition = Vector2.Lerp(bottomStartPos, bottomTargetPos, t);
            yield return null;
        }

        topPanelRect.anchoredPosition = topTargetPos;
        bottomPanelRect.anchoredPosition = bottomTargetPos;
    }

    private IEnumerator CinematicCutsceneRoutine(PlayerControll player)
    {
        Debug.Log("🎬 [타임라인] 13초 영화 연출 시작 (종료 후 패널 퇴장 및 복구 포함)");

        if (townBgmAudioSource != null)
        {
            townBgmAudioSource.Stop();
        }

        if (player != null) player.enabled = false;
        if (cinematicPanels != null) cinematicPanels.SetActive(true);
        StartCoroutine(SlidePanelsIn()); // 1. 검은색 레터박스 부드럽게 등장 (1번 -> 2번)

        Vector3 originalCamPos = mainCamera.transform.position;
        Vector3 defaultLeftScale = new Vector3(-4, 4, 1);
        Vector3 defaultRightScale = new Vector3(4, 4, 1);

        // ----------------------------------------------------
        // 구간 1 (0초 ~ 3초): 전체화면 그대로 + [약한 흔들림]
        // ----------------------------------------------------
        float timePhase1 = 0f;
        while (timePhase1 < 3.0f)
        {
            timePhase1 += Time.deltaTime;
            if (playerTransform != null)
            {
                Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
                Vector3 randomShake = Random.insideUnitSphere * baseShakeMagnitude;
                randomShake.z = 0;
                mainCamera.transform.position = ClampCameraPosition(targetPos + randomShake);
            }
            yield return null;
        }

        // ----------------------------------------------------
        // 구간 2 (3초 ~ 6초): 플레이어 클로즈업 + [중간 흔들림] + [좌우 두리번반전]
        // ----------------------------------------------------
        float timePhase2 = 0f;
        float closeZoomSize = originalCameraSize * 0.5f;

        while (timePhase2 < 3.0f)
        {
            timePhase2 += Time.deltaTime;
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = Mathf.Lerp(originalCameraSize, closeZoomSize, timePhase2 / 3.0f);
            }

            if (playerTransform != null)
            {
                Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
                Vector3 randomShake = Random.insideUnitSphere * (baseShakeMagnitude * 2f);
                randomShake.z = 0;
                mainCamera.transform.position = ClampCameraPosition(targetPos + randomShake);

                if (timePhase2 < 0.7f) playerTransform.localScale = defaultRightScale;
                else if (timePhase2 >= 0.7f && timePhase2 < 1.5f) playerTransform.localScale = defaultLeftScale;
                else if (timePhase2 >= 1.5f && timePhase2 < 2.2f) playerTransform.localScale = defaultRightScale;
                else playerTransform.localScale = defaultLeftScale;
            }
            yield return null;
        }
        if (mainCamera != null) mainCamera.orthographicSize = closeZoomSize;

        // ----------------------------------------------------
        // 구간 3 (6초 ~ 7초): 클로즈업 고정 + '!' 등장 + [강한 흔들림]
        // ----------------------------------------------------
        if (playerTransform != null) playerTransform.localScale = defaultRightScale;
        if (exclamationMark != null) exclamationMark.SetActive(true);

        float timePhase3 = 0f;
        while (timePhase3 < 1.0f)
        {
            timePhase3 += Time.deltaTime;
            if (playerTransform != null)
            {
                Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
                Vector3 randomShake = Random.insideUnitSphere * (baseShakeMagnitude * 4.5f);
                randomShake.z = 0;
                mainCamera.transform.position = ClampCameraPosition(targetPos + randomShake);
            }
            yield return null;
        }
        if (exclamationMark != null) exclamationMark.SetActive(false);

        // ----------------------------------------------------
        // 구간 4 (7초 ~ 10초): 약간 확대 구도로 카메라 무빙 + [약한 흔들림]
        // ----------------------------------------------------
        float timePhase4 = 0f;
        float slightZoomSize = originalCameraSize * 0.85f;

        while (timePhase4 < 3.0f)
        {
            timePhase4 += Time.deltaTime;
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = Mathf.Lerp(closeZoomSize, slightZoomSize, timePhase4 / 3.0f);
            }

            if (treeCenterTransform != null)
            {
                Vector3 targetPos = new Vector3(treeCenterTransform.position.x, treeCenterTransform.position.y, originalCamPos.z);
                Vector3 currentCamPos = Vector3.Lerp(mainCamera.transform.position, targetPos, timePhase4 / 3.0f);
                Vector3 randomShake = Random.insideUnitSphere * baseShakeMagnitude;
                randomShake.z = 0;
                mainCamera.transform.position = ClampCameraPosition(currentCamPos + randomShake);
            }
            yield return null;
        }
        if (mainCamera != null) mainCamera.orthographicSize = slightZoomSize;

        // ----------------------------------------------------
        // 구간 5 (10초 ~ 13초): 구도 고정 + [나무 작동 및 사운드 가동] + [우르릉 흔들림]
        // ----------------------------------------------------
        if (townBgmAudioSource != null && cinematicNewBgmClip != null)
        {
            townBgmAudioSource.clip = cinematicNewBgmClip;
            townBgmAudioSource.volume = cinematicBgmVolume;
            townBgmAudioSource.loop = true;
            townBgmAudioSource.Play();
        }

        if (leftTree != null)
        {
            Vector3 targetPos = leftTree.transform.position + Vector3.left * treeMoveDistance;
            leftTree.MoveTo(targetPos, treeMoveSpeed);
        }
        if (rightTree != null)
        {
            Vector3 targetPos = rightTree.transform.position + Vector3.right * treeMoveDistance;
            rightTree.MoveTo(targetPos, treeMoveSpeed);
        }

        float timePhase5 = 0f;
        while (timePhase5 < 3.0f)
        {
            timePhase5 += Time.deltaTime;
            if (treeCenterTransform != null)
            {
                Vector3 targetPos = new Vector3(treeCenterTransform.position.x, treeCenterTransform.position.y, originalCamPos.z);
                Vector3 randomShake = Random.insideUnitSphere * (baseShakeMagnitude * 3.5f);
                randomShake.z = 0;
                mainCamera.transform.position = ClampCameraPosition(targetPos + randomShake);
            }
            yield return null;
        }

        // ----------------------------------------------------
        // [복귀 및 정리 단계]: 원래 앵글로 1초간 복귀 + ★[패널 퇴장 시작]
        // ----------------------------------------------------
        // 카메라인 복귀 연출이 시작됨과 동시에 검은색 패널도 바깥으로 되돌아갑니다. (2번 -> 1번)
        StartCoroutine(SlidePanelsOut());

        float timeReturn = 0f;
        Vector3 cutsceneEndPos = mainCamera.transform.position;

        while (timeReturn < 1.0f)
        {
            timeReturn += Time.deltaTime;
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = Mathf.Lerp(slightZoomSize, originalCameraSize, timeReturn / 1.0f);
            }

            if (playerTransform != null)
            {
                Vector3 finalPlayerPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
                mainCamera.transform.position = ClampCameraPosition(Vector3.Lerp(cutsceneEndPos, finalPlayerPos, timeReturn / 1.0f));
            }
            yield return null;
        }

        // 최종 데이터 리셋 및 오차 교정 원상복구
        if (mainCamera != null) mainCamera.orthographicSize = originalCameraSize;
        if (playerTransform != null)
        {
            Vector3 finalPlayerPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
            mainCamera.transform.position = ClampCameraPosition(finalPlayerPos);
        }

        // 패널들이 완전히 화면 밖으로 퇴장했으므로 캔버스 최적화를 위해 오브젝트를 꺼줍니다.
        if (cinematicPanels != null) cinematicPanels.SetActive(false);
        if (keyAImage != null) keyAImage.SetActive(false);
        if (keyBImage != null) keyBImage.SetActive(false);

        if (player != null) player.enabled = true;
        Debug.Log("🔓 연출 완료: 카메라가 원래대로 돌아오고 패널도 완전히 퇴장하여 평소 화면으로 복구되었습니다.");
    }

    public void ResetKeys()
    {
        HasKeyA = false;
        HasKeyB = false;
        RefreshUI();
    }
}