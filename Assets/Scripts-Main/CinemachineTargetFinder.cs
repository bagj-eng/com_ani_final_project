using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine; // ★ 최신 유니티 버전에 맞춘 시네머신 네임스페이스

public class CinemachineTargetFinder : MonoBehaviour
{
    // 최신 버전 규격인 CinemachineCamera 컴포넌트를 사용합니다.
    private CinemachineCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();

        // 씬이 변경되거나 새로 로드될 때마다 자동으로 타겟을 새로 고치는 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // 게임이 처음 시작될 때도 플레이어를 한 번 찾습니다.
        FindAndTrackPlayer();
    }

    void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 켜지자마자 플레이어 태그를 다시 락온합니다.
        FindAndTrackPlayer();
    }

    private void FindAndTrackPlayer()
    {
        // Hierarchy 창에서 "Player" 태그를 가진 오브젝트를 검색합니다.
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null && vcam != null)
        {
            // 최신 시네머신에서 타겟을 설정하는 프로퍼티 구조입니다.
            vcam.Follow = player.transform; // 카메라가 플레이어를 추적함
            vcam.LookAt = player.transform; // 카메라가 플레이어를 중심점으로 바라봄
            Debug.Log($"[시네머신] '{SceneManager.GetActiveScene().name}' 씬에서 Player 타겟을 성공적으로 잡았습니다!");
        }
        else if (player == null)
        {
            Debug.LogWarning("[시네머신] 현재 씬에 'Player' 태그를 가진 오브젝트가 없어 추적에 실패했습니다.");
        }
    }
}