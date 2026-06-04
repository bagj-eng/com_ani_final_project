using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    // ====== 모든 포탈이 공유하는 정적(Static) 변수 ======
    private static float globalCooldownTimer = 0f;       // 전역 쿨타임 타이머
    private static string nextSpawnPointName = "";        // 이동 후 태어날 스폰포젝트 이름
    private static bool isSequenceRunning = false;        // 씬 전환 프로세스 중복 실행 방지

    [Header("이 포탈을 타면 이동할 다음 씬 이름")]
    [SerializeField] private string nextSceneName;

    [Header("다음 씬에 도착했을 때 플레이어가 태어날 SpawnPoint 오브젝트의 '이름'")]
    [SerializeField] private string targetSpawnPointObjectName;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float cooldownDuration = 10f; // 쿨타임 10초

    private void Awake()
    {
        // 씬이 로드될 때마다 자동으로 실행될 함수(OnSceneLoaded)를 유니티 시스템에 등록
        // 이 스크립트가 새로 컴포넌트로 켜질 때마다 등록됩니다.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 등록을 해제하여 메모리 누수를 방지합니다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // 전역 쿨타임 타이머 감소 (매 프레임 하나의 타이머만 깎임)
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 글로벌 쿨타임 중이면 무시
        if (globalCooldownTimer > 0f)
        {
            Debug.Log($"포탈 쿨타임 중! 남은 시간: {globalCooldownTimer:F1}초");
            return;
        }

        // 2. 이미 누군가 포탈을 타서 씬 전환 중이면 무시
        if (isSequenceRunning) return;

        // 3. 플레이어 충돌 확인
        if (other.CompareTag(playerTag))
        {
            isSequenceRunning = true;
            globalCooldownTimer = cooldownDuration; // 즉시 10초 쿨타임 가동
            nextSpawnPointName = targetSpawnPointObjectName; // 스폰할 위치 이름 기록

            HidePortalVisuals();
            StartCoroutine(SceneSequenceRoutine());
        }
    }

    private IEnumerator SceneSequenceRoutine()
    {
        // 이 오브젝트는 RUN 씬까지만 살아남아서 코루틴을 이어갑니다.
        DontDestroyOnLoad(gameObject);

        // 1. RUN 씬으로 이동
        SceneManager.LoadScene("RUN");
        yield return new WaitForSeconds(2f);

        // 2. 최종 목적지 씬으로 이동
        SceneManager.LoadScene(nextSceneName);
        yield return null; // 씬 로드 안정화를 위해 한 프레임 대기

        // 3. 목적지 씬 이동이 끝났으므로 코루틴을 돌리던 유령 포탈 삭제
        isSequenceRunning = false;
        Destroy(gameObject);
    }

    // 새로운 씬이 완전히 켜지면 유니티가 자동으로 실행해 주는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 기억해둔 스폰 포인트 이름이 있을 때만 플레이어를 순간이동 시킵니다.
        if (!string.IsNullOrEmpty(nextSpawnPointName))
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            GameObject spawnPoint = GameObject.Find(nextSpawnPointName);

            if (player != null && spawnPoint != null)
            {
                player.transform.position = spawnPoint.transform.position;
                Debug.Log($"플레이어를 {nextSpawnPointName} 위치로 이동시켰습니다.");
            }

            // 순간이동이 완료되었으므로 스폰 포인트 이름 초기화
            nextSpawnPointName = "";
        }
    }

    private void HidePortalVisuals()
    {
        if (TryGetComponent<SpriteRenderer>(out var renderer)) renderer.enabled = false;
        if (TryGetComponent<Collider2D>(out var collider)) collider.enabled = false;
    }
}