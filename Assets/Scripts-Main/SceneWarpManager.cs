using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneWarpManager : MonoBehaviour
{
    public static SceneWarpManager Instance { get; private set; }

    private string reservedTargetScene = "";
    private bool isWarping = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeSceneWithRun(string targetSceneName)
    {
        if (isWarping || targetSceneName == "RUN") return;

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        isWarping = true;
        reservedTargetScene = targetSceneName;
        StartCoroutine(WarpSequenceRoutine());
    }

    private IEnumerator WarpSequenceRoutine()
    {
        Debug.Log($"🏃 [WarpManager] 'RUN' 씬으로 전환합니다. (최종 목적지: {reservedTargetScene})");

        // 1. RUN 씬을 불러옵니다.
        AsyncOperation asyncLoadRun = SceneManager.LoadSceneAsync("RUN");
        while (!asyncLoadRun.isDone)
        {
            yield return null;
        }

        // 화면 렌더링이 완전히 정착할 시간을 강제로 제공합니다.
        yield return new WaitForEndOfFrame();
        yield return null;

        Debug.Log("🏃 [WarpManager] RUN 씬 정착 완료! 이제 최종 목적지 로딩을 백그라운드에서 미리 시작합니다.");

        // 2. ★ [핵심 해결책] 진짜 목적지(보스방 등)를 미리 로드하되, 화면 전환에 락(Lock)을 겁니다.
        AsyncOperation asyncLoadTarget = SceneManager.LoadSceneAsync(reservedTargetScene);

        // allowSceneActivation을 false로 두면, 로딩이 100% 끝나도 화면이 멋대로 바뀌지 않고 대기합니다.
        asyncLoadTarget.allowSceneActivation = false;

        // 3. RUN 씬을 눈으로 확인하며 정확히 2초간 달리는 시간을 보장합니다.
        float timer = 0f;
        while (timer < 2f)
        {
            timer += Time.unscaledDeltaTime; // 실시간 2초 계산
            yield return null;
        }

        Debug.Log("🎯 [WarpManager] RUN 씬 체류 시간 2초 완료! 최종 목적지 로딩 상태를 확인합니다.");

        // 4. 유니티 비동기 로딩은 allowSceneActivation이 false일 때 progress가 0.9f에서 멈춥니다. (90% 완료 = 내부적 완공)
        // 만약 컴퓨터가 느려서 2초 동안 로딩이 90%에 도달하지 못했다면, 도달할 때까지 추가 대기합니다.
        while (asyncLoadTarget.progress < 0.9f)
        {
            yield return null;
        }

        Debug.Log($"🏁 [WarpManager] 로딩 완공 완료! 걸어두었던 락을 해제하고 [{reservedTargetScene}]으로 화면을 전환합니다.");

        // 5. 락을 해제하여 최종 목적지 씬 화면을 띄웁니다.
        asyncLoadTarget.allowSceneActivation = true;

        // 최종 씬 전환이 완전히 끝날 때까지 대기
        while (!asyncLoadTarget.isDone)
        {
            yield return null;
        }

        // 6. 모든 상태 초기화 후 시퀀스 종료
        isWarping = false;
        reservedTargetScene = "";
        Debug.Log("🎉 [WarpManager] 최종 맵 안착 완료.");
    }
}