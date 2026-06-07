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

    [Header("컷씬 종료 후 나타날 상호작용 화살표")]
    [SerializeField] private GameObject guideArrowObject;

    // ★ [추가] 컷씬 종료 즉시 활성화할 양 옆 막이 이미지/콜라이더 부모 오브젝트 슬롯
    [Header("컷씬 종료 후 활성화될 맵 경계선 장벽")]
    [SerializeField] private GameObject mapBoundaryObject;

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

    private bool isPlayerInPortalZone = false;
    private PlayerControll cachedPlayerScript;

    private const float TOP_START_Y = 770f;
    private const float TOP_TARGET_Y = 620f;
    private const float BOTTOM_START_Y = -770f;
    private const float BOTTOM_TARGET_Y = -635f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ClearKeysOnGameStart()
    {
        // 게임을 아예 처음 켰을 때만 데이터 초기화
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
        // ★ [중요] 싱글톤 갱신 로직: 어느 씬에서든 새로 깨어난 KeyManager가 Instance 주도권을 잡게 함
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) originalCameraSize = mainCamera.orthographicSize;

        if (cinematicPanels != null) cinematicPanels.SetActive(false);
        if (exclamationMark != null) exclamationMark.SetActive(false);
        if (guideArrowObject != null) guideArrowObject.SetActive(false);

        // ★ [추가] 처음 맵에 진입했을 때는 양옆 이미지장벽이 켜지지 않도록 초기 비활성화
        if (mapBoundaryObject != null) mapBoundaryObject.SetActive(false);

        // ★ 메인 타운으로 돌아오거나 다른 씬에서 시작할 때, 저장된 PlayerPrefs 기반으로 즉시 UI 새로고침
        RefreshUI();
    }

    void Update()
    {
        if (isPlayerInPortalZone && Input.GetKeyDown(KeyCode.F))
        {
            TryOpenBossMap();
        }
    }

    public void RefreshUI()
    {
        // UI 오브젝트들이 현재 씬 인스펙터에 등록되어 있을 때만 끄고 켭니다. (Right/Left 맵에 UI가 없어도 에러 우회)
        if (keyAImage != null) keyAImage.SetActive(HasKeyA);
        if (keyBImage != null) keyBImage.SetActive(HasKeyB);
    }

    // ★ [핵심 수정] 어떤 씬에서 호출되더라도 데이터를 안전하게 저장하고 로그를 무조건 출력함
    public void GetKey(string keyType)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (keyType == "A" || keyType == "a")
        {
            HasKeyA = true;
            Debug.Log($"📢 [{currentScene}] 씬에서 [열쇠 A] 획득 완료! (데이터 로컬 저장 성공)");
        }
        else if (keyType == "B" || keyType == "b")
        {
            HasKeyB = true;
            Debug.Log($"📢 [{currentScene}] 씬에서 [열쇠 B] 획득 완료! (데이터 로컬 저장 성공)");
        }

        // 현재 씬에 UI 오브젝트가 존재한다면 실시간 반영
        RefreshUI();
    }

    private void TryOpenBossMap()
    {
        if (SceneManager.GetActiveScene().name != "Main Town")
        {
            Debug.Log("이곳은 Main Town이 아니므로 보스방 포탈이 작동하지 않습니다.");
            return;
        }

        if (HasKeyA && HasKeyB)
        {
            Debug.Log("조건 충족! 즉시 BossMap으로 안전하게 이동합니다.");
            SceneManager.LoadScene("BossMap");
        }
        else
        {
            Debug.Log("열쇠(key1, key2)가 부족하여 BossMap 포탈을 열 수 없습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (gameObject.CompareTag("key1") || gameObject.CompareTag("key2")) return;

            cachedPlayerScript = collision.GetComponent<PlayerControll>();
            isPlayerInPortalZone = true;

            if (HasKeyA && HasKeyB && !isCutscenePlayed && SceneManager.GetActiveScene().name == "Main Town")
            {
                isCutscenePlayed = true;
                if (cachedPlayerScript != null)
                {
                    StartCoroutine(CinematicCutsceneRoutine(cachedPlayerScript));
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInPortalZone = false;
            cachedPlayerScript = null;
        }
    }

    private Vector3 ClampCameraPosition(Vector3 targetPos)
    {
        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);
        return new Vector3(clampedX, clampedY, targetPos.z);
    }

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

    private IEnumerator SlidePanelsOut()
    {
        if (topPanelRect == null || bottomPanelRect == null) yield break;

        Vector2 topStartPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_TARGET_Y);
        Vector2 bottomStartPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_TARGET_Y);
        Vector2 topTargetPos = new Vector2(topPanelRect.anchoredPosition.x, TOP_START_Y);
        Vector2 bottomTargetPos = new Vector2(bottomPanelRect.anchoredPosition.x, BOTTOM_START_Y);

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
        bottomPanelRect.anchoredPosition = bottomStartPos;
    }

    private IEnumerator CinematicCutsceneRoutine(PlayerControll player)
    {
        if (townBgmAudioSource != null) townBgmAudioSource.Stop();
        if (player != null) player.enabled = false;
        if (cinematicPanels != null) cinematicPanels.SetActive(true);
        StartCoroutine(SlidePanelsIn());

        Vector3 originalCamPos = mainCamera.transform.position;
        Vector3 defaultLeftScale = new Vector3(-4, 4, 1);
        Vector3 defaultRightScale = new Vector3(4, 4, 1);

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

        float timePhase2 = 0f;
        float closeZoomSize = originalCameraSize * 0.5f;
        while (timePhase2 < 3.0f)
        {
            timePhase2 += Time.deltaTime;
            if (mainCamera != null) mainCamera.orthographicSize = Mathf.Lerp(originalCameraSize, closeZoomSize, timePhase2 / 3.0f);

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

        float timePhase4 = 0f;
        float slightZoomSize = originalCameraSize * 0.85f;
        while (timePhase4 < 3.0f)
        {
            timePhase4 += Time.deltaTime;
            if (mainCamera != null) mainCamera.orthographicSize = Mathf.Lerp(closeZoomSize, slightZoomSize, timePhase4 / 3.0f);

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

        if (townBgmAudioSource != null && cinematicNewBgmClip != null)
        {
            townBgmAudioSource.clip = cinematicNewBgmClip;
            townBgmAudioSource.volume = cinematicBgmVolume;
            townBgmAudioSource.loop = true;
            townBgmAudioSource.Play();
        }

        if (leftTree != null) { Vector3 targetPos = leftTree.transform.position + Vector3.left * treeMoveDistance; leftTree.MoveTo(targetPos, treeMoveSpeed); }
        if (rightTree != null) { Vector3 targetPos = rightTree.transform.position + Vector3.right * treeMoveDistance; rightTree.MoveTo(targetPos, treeMoveSpeed); }

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

        StartCoroutine(SlidePanelsOut());

        float timeReturn = 0f;
        Vector3 cutsceneEndPos = mainCamera.transform.position;

        while (timeReturn < 1.0f)
        {
            timeReturn += Time.deltaTime;
            if (mainCamera != null) mainCamera.orthographicSize = Mathf.Lerp(slightZoomSize, originalCameraSize, timeReturn / 1.0f);

            if (playerTransform != null)
            {
                Vector3 finalPlayerPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z);
                mainCamera.transform.position = ClampCameraPosition(Vector3.Lerp(cutsceneEndPos, finalPlayerPos, timeReturn / 1.0f));
            }
            yield return null;
        }

        if (mainCamera != null) mainCamera.orthographicSize = originalCameraSize;
        if (playerTransform != null) { Vector3 finalPlayerPos = new Vector3(playerTransform.position.x, playerTransform.position.y, originalCamPos.z); mainCamera.transform.position = ClampCameraPosition(finalPlayerPos); }

        if (cinematicPanels != null) cinematicPanels.SetActive(false);
        if (keyAImage != null) keyAImage.SetActive(false);
        if (keyBImage != null) keyBImage.SetActive(false);

        if (player != null) player.enabled = true;

        if (guideArrowObject != null) guideArrowObject.SetActive(true);

        // =================================────────────────===============
        // ★ [핵심 추가] 카메라 복귀 연출이 100% 종료되는 즉시 양옆 이미지 장벽 활성화
        // ================================================================
        if (mapBoundaryObject != null)
        {
            mapBoundaryObject.SetActive(true);
            Debug.Log("🚧 [경계선 장벽 가동] 카메라 복귀 연출 종료 즉시 맵 양옆 이탈 방지 장벽 및 감지 트리거가 가동되었습니다.");
        }
    }

    public void ResetKeys()
    {
        HasKeyA = false;
        HasKeyB = false;
        RefreshUI();
    }
}