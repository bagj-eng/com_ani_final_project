using System.Collections.Generic;
using UnityEngine;

public class BossUltimateObject : MonoBehaviour
{
    [Header("이동 및 대미지 수치")]
    [SerializeField] private float moveSpeed = 15f; // 왼쪽으로 날아가는 속도 (빠르게)
    [SerializeField] private int damage = 50;        // 까일 대미지 수치 (50)

    // 이미 대미지를 준 오브젝트들을 기억하는 리스트 (중복 대미지 방지 및 단 1번만 타격 보장)
    private HashSet<Collider2D> hitObjects = new HashSet<Collider2D>();

    void Update()
    {
        // 매 프레임마다 왼쪽(-X 방향)으로 빠르게 돌진합니다.
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // 화면 밖으로 멀리 완전히 벗어나면 메모리 최적화를 위해 오브젝트를 삭제합니다.
        // (X 좌표가 -30 이하로 내려가면 삭제, 맵 크기에 따라 수치 조절 가능)
        if (transform.position.x < -30f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 부딪힌 대상이 "Player" 태그를 가졌는지 체크
        // 2. 그리고 이번 전체공격 장벽에 아직 한 번도 대미지를 입지 않은 대상인지 검증
        if (collision.CompareTag("Player") && !hitObjects.Contains(collision))
        {
            // 중복 히트 방지 목록에 즉시 추가 (장벽이 몸을 완전히 통과할 때까지 연사 대미지 방지)
            hitObjects.Add(collision);

            // 컴포넌트에서 플레이어 컨트롤러 본체를 링크해옵니다.
            PlayerControll player = collision.GetComponent<PlayerControll>();
            if (player != null)
            {
                // ★ [연동 완료]: 플레이어 컨트롤러 내부의 핵심 피격 처리 시스템을 트리거합니다.
                // 이 함수가 실행되면 플레이어 스크립트 내부에 짜두신:
                // hp 차감 (50) -> 피격 사운드 재생 -> Take_Hit 애니메이션 작동이 한 번에 스르륵 실행됩니다!
                player.TakeDamage(damage);

                Debug.Log($"🟥 [전체공격 적중] Player 캐릭터가 {damage} 대미지를 입고 Take Hit 모션이 작동되었습니다!");
            }
        }
    }
}