using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

#if UNITY_EDITOR
using UnityEditor; // 인스펙터에서 SceneAsset을 사용하기 위해 필수
#endif

public class BossMapPortal : MonoBehaviour
{
    // [인스펙터 노출] 프로젝트 뷰에 있는 이동할 씬 파일(BossMap 등)을 이 칸에 드래그해 넣으세요.
#if UNITY_EDITOR
    [Header("이동할 대상 씬 설정")]
    [SerializeField] private SceneAsset targetSceneAsset;
#endif

    // 실제 빌드 환경에서 사용될 씬 이름 저장 변수
    [HideInInspector][SerializeField] private string targetSceneName = "";

    private bool isPlayerInZone = false;  // 플레이어가 범위 안에 있는지 체크
    private bool isTransitioning = false; // 중복 이동 및 중복 연출 방지용 플래그
    private PlayerControll playerScript;  // 범위 내 플레이어 스크립트 캐싱

    // 인스펙터에서 값이 바뀌면 자동으로 씬 이름을 문자열로 동기화
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (targetSceneAsset != null)
        {
            targetSceneName = targetSceneAsset.name;
        }
        else
        {
            targetSceneName = "";
        }
#endif
    }

    void Update()
    {
        // 플레이어가 영역 안에 있고, F 키를 눌렀을 때 (현재 연출 중이 아닐 때만 작동)
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.F) && !isTransitioning)
        {
            TryEnterSelectedScene();
        }
    }

    private void TryEnterSelectedScene()
    {
        // 1. 현재 활성화된 씬의 이름이 무조건 "Main Town" 일 때만 실행되도록 제약
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName != "Main Town")
        {
            Debug.Log($"현재 맵({currentSceneName})에서는 포탈을 이용할 수 없습니다. Main Town 맵에서만 가능합니다.");
            return;
        }

        // 2. 인스펙터에 이동할 씬이 지정되어 있는지 체크
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("포탈에 이동할 대상 씬(Target Scene Asset)이 지정되지 않았습니다! 인스펙터를 확인해 주세요.");
            return;
        }

        // 3. KeyManager 싱글톤 인스턴스를 통해 두 열쇠(A, B)를 모두 가지고 있는지 체크
        bool hasKey1 = false;
        bool hasKey2 = false;

        if (KeyManager.Instance != null)
        {
            hasKey1 = KeyManager.Instance.HasKeyA;
            hasKey2 = KeyManager.Instance.HasKeyB;
        }
        else
        {
            // 백업용 PlayerPrefs 직접 체크
            hasKey1 = PlayerPrefs.GetInt("Saved_HasKeyA", 0) == 1;
            hasKey2 = PlayerPrefs.GetInt("Saved_HasKeyB", 0) == 1;
        }

        // 4. 조건 충족 시 달리기 연출 코루틴 실행
        if (hasKey1 && hasKey2)
        {
            StartCoroutine(RunAnimationTransitionRoutine());
        }
        else
        {
            Debug.Log($"[{targetSceneName}]에 진입하려면 key1(열쇠A)과 key2(열쇠B)가 모두 필요합니다.");
        }
    }

    // ★ [2초 지연 및 RUN 애니메이션 강제 재생 코루틴]
    private IEnumerator RunAnimationTransitionRoutine()
    {
        isTransitioning = true;
        Debug.Log("🏃 보스방 진입 연출 시작: 2초간 RUN 스테이트 강제 재생");

        if (playerScript != null)
        {
            // 1. 연출 도중 플레이어가 키를 눌러 움직이지 못하게 조작 스크립트 비활성화
            playerScript.enabled = false;

            // 2. 물리적 관성으로 미끄러지는 현상을 막기 위해 X축 속도를 0으로 강제 고정
            Rigidbody2D playerRigid = playerScript.GetComponent<Rigidbody2D>();
            if (playerRigid != null)
            {
                playerRigid.linearVelocity = new Vector2(0f, playerRigid.linearVelocity.y);
            }

            // 3. ★ [수정]: 파라미터(bool) 조작 대신, Animator의 "RUN" 애니메이션 스테이트를 직접 강제 재생합니다.
            Animator playerAnim = playerScript.GetComponent<Animator>();
            if (playerAnim != null)
            {
                // 애니메이터 창(Animator window)에 등록된 달리기 상태의 정확한 이름을 적어주세요.
                // 만약 대소문자가 다르다면 "Run" 등으로 코드를 수정하셔야 합니다.
                playerAnim.Play("RUN");
            }
        }

        // 4. 연출이 유지될 시간인 2초 동안 프레임을 대기합니다.
        yield return new WaitForSeconds(2f);

        // 5. 대기가 끝나면 맵 청소 없이 설정된 씬으로 이동합니다.
        Debug.Log($"2초 연출 완료! [{targetSceneName}] 씬으로 바로 이동합니다.");
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;

            // 플레이어 스크립트 참조 가져오기 및 캐싱
            playerScript = other.GetComponentInParent<PlayerControll>();
            if (playerScript == null)
            {
                playerScript = other.GetComponent<PlayerControll>();
            }

            string displaySceneName = string.IsNullOrEmpty(targetSceneName) ? "보스방" : targetSceneName;
            Debug.Log($"포탈 진입: F키를 누르면 [{displaySceneName}]으로 이동 가능");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            playerScript = null; // 범위를 벗어나면 혹시 모를 오작동 방지를 위해 참조 제거
            Debug.Log("포탈에서 벗어남");
        }
    }
}