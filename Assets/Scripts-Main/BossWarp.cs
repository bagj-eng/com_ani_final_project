using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossWarp : MonoBehaviour
{
    [Header("이동할 보스방 씬 이름")]
    [SerializeField] private string bossSceneName = "BossMap"; // 유니티 에디터 상의 보스방 씬 이름

    private bool isWarping = false;

    // 플레이어가 포탈(Trigger)에 부딪혔을 때 작동
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 부딪힌 오브젝트가 플레이어이고, 이미 워프가 진행 중이 아닐 때만 실행
        if (collision.CompareTag("Player") && !isWarping)
        {
            isWarping = true;
            StartCoroutine(WarpSequenceRoutine());
        }
    }

    private IEnumerator WarpSequenceRoutine()
    {
        Debug.Log("🏃 보스방 이동 감지! 먼저 'RUN' 씬으로 2초간 이동합니다.");

        // 1. 중간 연출용 'RUN' 씬을 비동기로 불러옵니다.
        AsyncOperation asyncLoadRun = SceneManager.LoadSceneAsync("RUN");

        while (!asyncLoadRun.isDone)
        {
            yield return null;
        }

        // 2. RUN 씬이 켜진 상태에서 정확히 '2초' 동안 대기합니다.
        yield return new WaitForSecondsRealtime(2f);

        Debug.Log($"🎯 2초 경과! 최종 목적지인 보스방 [{bossSceneName}]으로 이동합니다.");

        // 3. 2초가 지나면 진짜 목적지인 보스방 씬을 불러옵니다.
        AsyncOperation asyncLoadBoss = SceneManager.LoadSceneAsync(bossSceneName);

        while (!asyncLoadBoss.isDone)
        {
            yield return null;
        }
    }
}