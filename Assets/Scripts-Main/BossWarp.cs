using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossWarp : MonoBehaviour
{
    [Header("이동할 보스방 씬 이름")]
    [SerializeField] private string bossSceneName = "BossMap";

    private bool isWarping = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isWarping)
        {
            isWarping = true;
            StartCoroutine(WarpSequenceRoutine());
        }
    }

    private IEnumerator WarpSequenceRoutine()
    {
        Debug.Log("🏃 보스방 이동 감지! 'RUN' 씬 전환 준비 중...");

        // 씬이 바뀌어도 스크립트가 파괴되지 않도록 조치
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 1. 중간 연출용 'RUN' 씬을 로드합니다.
        AsyncOperation asyncLoadRun = SceneManager.LoadSceneAsync("RUN");

        // 씬 로딩이 완료될 때까지 대기
        while (!asyncLoadRun.isDone)
        {
            yield return null;
        }

        // ★ [버그 해결 핵심]: 씬 로드가 끝난 직후, 유니티가 RUN 씬의 오브젝트들을 
        // 맵에 배치하고 화면을 한 번 그릴(Render) 수 있도록 최소 1~2프레임의 여유를 강제로 줍니다.
        yield return new WaitForEndOfFrame();
        yield return null;

        Debug.Log("🏃 RUN 씬 화면 표시 완료! 이제 정확히 2초간 대기합니다.");

        // 2. RUN 씬이 완벽히 눈에 보이는 상태에서 정확히 '3초' 동안 대기하며 달리게 합니다.
        yield return new WaitForSecondsRealtime(3f);

        Debug.Log($"🎯 2초 경과! 최종 목적지인 보스방 [{bossSceneName}]으로 이동합니다.");

        // 3. 2초가 확실히 지난 후에 진짜 목적지인 보스방 씬을 불러옵니다.
        AsyncOperation asyncLoadBoss = SceneManager.LoadSceneAsync(bossSceneName);

        while (!asyncLoadBoss.isDone)
        {
            yield return null;
        }

        // 4. 모든 시퀀스가 완벽히 끝났으므로 오브젝트를 정리합니다.
        Debug.Log("🏁 보스방 진입 성공. 워프 오브젝트를 제거합니다.");
        Destroy(gameObject);
    }
}