using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTeleport : MonoBehaviour
{
    [Header("텔레포트 시간 세팅")]
    [SerializeField] private float teleportInterval = 5.0f; // 5초 주기

    // bossTP 태그 오브젝트들을 담아둘 리스트
    private List<Transform> teleportPoints = new List<Transform>();

    // 직전에 순간이동했던 포인트의 인덱스를 기억할 변수 (-1은 시작 상태)
    private int lastPointIndex = -1;
    private Coroutine teleportCoroutine;

    void Start()
    {
        // 1. 씬 내에 배치된 모든 bossTP 투명 포인트를 검색합니다.
        FindAllTeleportPoints();

        // 2. 포인트가 2개 이상 존재해야 '연속 같은 위치 금지' 연산이 의미가 있으므로 검증합니다.
        if (teleportPoints.Count >= 2)
        {
            teleportCoroutine = StartCoroutine(TeleportRoutine());
        }
        else if (teleportPoints.Count == 1)
        {
            Debug.LogWarning("⚠️ [BossPointTeleport]: 'bossTP' 포인트가 1개뿐입니다. 연속 중복 방지 로직이 작동하지 않으며 제자리 텔포만 수행합니다.");
            teleportCoroutine = StartCoroutine(TeleportRoutine());
        }
        else
        {
            Debug.LogError("⚠️ [BossPointTeleport]: 맵에 'bossTP' 태그를 가진 오브젝트가 하나도 없습니다! 하이러키 창에서 태그를 확인해 주세요.");
        }
    }

    // bossTP 태그를 가진 오브젝트들을 탐색하는 함수
    private void FindAllTeleportPoints()
    {
        teleportPoints.Clear();

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("bossTP"))
            {
                teleportPoints.Add(obj.transform);
            }
        }

        Debug.Log($"🎯 [BossPointTeleport]: 'bossTP' 투명 포인트 {teleportPoints.Count}개 로딩 완료!");
    }

    private IEnumerator TeleportRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportInterval);

            if (!this.enabled) yield break;

            ExecuteRandomTeleport();
        }
    }

    private void ExecuteRandomTeleport()
    {
        // ★ [컷씬 전용 락 가동] 
        // 시네마틱 매니저(BossMapCutscene)가 컷씬 상영 중(true)이라고 하면 텔레포트 연산을 통째로 건너뜁니다.
        if (BossMapCutscene.IsCutsceneActive) return;

        if (teleportPoints.Count == 0) return;

        int randomIndex = -1;

        // [중복 방지 로직]
        // 포인트가 2개 이상일 때, 새로 뽑은 번호가 직전 번호(lastPointIndex)와 같다면 계속 다시 뽑습니다.
        if (teleportPoints.Count >= 2)
        {
            do
            {
                randomIndex = Random.Range(0, teleportPoints.Count);
            }
            while (randomIndex == lastPointIndex); // 직전 위치와 같으면 무한 재추첨
        }
        else
        {
            randomIndex = 0;
        }

        // 이번에 최종 확정된 번호를 다음 차례를 위해 기록해 둡니다.
        lastPointIndex = randomIndex;

        Transform targetPoint = teleportPoints[randomIndex];
        if (targetPoint == null) return;

        // 좌표 정밀 이동 처리
        Vector3 targetPosition = targetPoint.position;
        targetPosition.z = transform.position.z; // 2D 레이어 축 유지
        transform.position = targetPosition;

        Debug.Log($"🔮 [보스 텔레포트 완료]: 현재 위치 ➡️ [인덱스 {randomIndex}번] ({targetPoint.name}) 포인트");
    }

    // 외부(예: 보스 사망 스크립트 등)에서 텔레포트를 수동으로 강제 중지시키고 싶을 때 쓰는 함수
    public void StopTeleport()
    {
        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
            teleportCoroutine = null;
        }
    }
}