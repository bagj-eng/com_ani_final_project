using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public enum SpawnDirection { Left, Right }

    // ====== 모든 포탈이 공유하는 정적(Static) 변수 ======
    private static float globalCooldownTimer = 0f;       // 전역 쿨타임 타이머
    private static string targetSceneNameString = "";     // 최종 목적지 씬 이름을 공유하기 위한 변수
    private static bool isSequenceRunning = false;        // 씬 전환 프로세스 중복 실행 방지

    [Header("이 포탈을 타면 이동할 다음 씬 이름")]
    [SerializeField] private string nextSceneName;

    [Header("★ 이 포탈을 타고 마을로 가면 어디서 스폰될지 선택")]
    [SerializeField] private SpawnDirection portalType;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string spawnPointTag = "SpawnPoint"; // 스폰포인트 식별용 태그
    [SerializeField] private float cooldownDuration = 3f; // 쿨타임 3초

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (globalCooldownTimer > 0f || isSequenceRunning) return;

        if (other.CompareTag(playerTag))
        {
            isSequenceRunning = true;
            globalCooldownTimer = cooldownDuration;

            // ★ [수정] 씬 이름 대신, 이 포탈 오브젝트에 설정된 방향(Left 또는 Right)을 저장소에 기록합니다.
            PlayerPrefs.SetString("Portal_Spawn_Direction", portalType.ToString());
            PlayerPrefs.Save();

            targetSceneNameString = nextSceneName;

            HidePortalVisuals();
            StartCoroutine(SceneSequenceRoutine());
        }
    }

    private IEnumerator SceneSequenceRoutine()
    {
        DontDestroyOnLoad(gameObject);

        // 1. 중간 연출용 RUN 씬으로 이동
        SceneManager.LoadScene("RUN");
        yield return new WaitForSeconds(2f);

        // 2. 최종 목적지 씬으로 이동
        SceneManager.LoadScene(nextSceneName);
        yield return null;

        isSequenceRunning = false;
        Destroy(gameObject);
    }

    // 새로운 씬이 완전히 켜지면 실행되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 최종 목적지 씬 이름(Main Town)과 일치할 때만 작동
        if (scene.name == targetSceneNameString)
        {
            GameObject player = GameObject.FindWithTag(playerTag);

            // 저장소에서 방향(Left 또는 Right)을 읽어옵니다. 기본값은 Left
            string direction = PlayerPrefs.GetString("Portal_Spawn_Direction", "Left");
            string targetSpawnPointName = "";

            // 목적지가 Main Town일 때만 방향 분기 처리
            if (scene.name == "Main Town")
            {
                if (direction == "Left")
                {
                    targetSpawnPointName = "spawnleft";
                }
                else if (direction == "Right")
                {
                    targetSpawnPointName = "spawnright";
                }
            }
            else
            {
                // 다른 던전용 씬 등으로 갈 때 스폰 포인트가 없다면 기본 처리를 위한 예외 방지용
                targetSpawnPointName = "spawnleft";
            }

            Debug.Log($"[포탈 시스템] 읽어온 방향: {direction} -> 목적지 스폰포인트 오브젝트: {targetSpawnPointName}");

            // 씬에 배치된 "SpawnPoint" 태그를 가진 오브젝트들 중에서 알맞은 이름을 가진 녀석을 정밀 검색
            GameObject targetSpawnPoint = null;
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);

            foreach (GameObject sp in spawnPoints)
            {
                if (sp.name == targetSpawnPointName)
                {
                    targetSpawnPoint = sp;
                    break;
                }
            }

            // 순간이동 실행
            if (player != null && targetSpawnPoint != null)
            {
                player.transform.position = targetSpawnPoint.transform.position;
                Debug.Log($"[스폰 완료] 플레이어를 {targetSpawnPointName} 위치로 정확히 이동시켰습니다.");
            }
            else
            {
                if (player == null) Debug.LogError("[오류] 'Player' 태그를 가진 플레이어 오브젝트를 씬에서 찾을 수 없습니다.");
                if (targetSpawnPoint == null) Debug.LogError($"[오류] 씬에서 태그가 '{spawnPointTag}'이고 이름이 '{targetSpawnPointName}'인 스폰 포인트를 찾지 못했습니다.");
            }

            // 데이터 초기화
            targetSceneNameString = "";
        }
    }

    private void HidePortalVisuals()
    {
        if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.enabled = false;
        if (TryGetComponent<Collider2D>(out var collider)) collider.enabled = false;
    }
}