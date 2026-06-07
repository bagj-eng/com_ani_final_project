using System.Collections;
using UnityEngine;
using TMPro; // 메시지 출력용 TextMesh Pro 필수

public class BossUltimatePattern : MonoBehaviour
{
    [Header("패턴 기본 세팅")]
    [SerializeField] private float patternDuration = 20f;   // 대미지를 넣어야 하는 제한 시간 (20초)
    [SerializeField] private float requiredDamage = 200f;   // 저지하기 위해 필요한 요구 대미지량 (200)
    [SerializeField] private float groggyDuration = 2f;     // 그로기(스턴) 지속 시간 (2초)

    [Header("🕒 주기 시스템 (60초)")]
    [Tooltip("패턴 간의 간격(초)입니다. 정확히 60초로 세팅합니다.")]
    [SerializeField] private float patternInterval = 60f;
    private bool isCooldown = false; // 현재 쿨타임(주기)이 도는 중인지 체크하는 플래그

    [Header("UI 연동 설정")]
    [SerializeField] private GameObject patternNoticePanel; // 화면에 띄울 경고창 패널 전체
    [SerializeField] private TextMeshProUGUI noticeText;    // 경고창 내부의 글자 컴포넌트

    [Header("🔥 전체 공격기 프리팹 설정")]
    [Tooltip("오른쪽에서 왼쪽으로 빠르게 날아갈 거대 사각형 공격기 프리팹 오브젝트를 넣어주세요.")]
    [SerializeField] private GameObject ultimateAttackPrefab;
    [Tooltip("전체 공격기가 화면 오른쪽 바깥에서 생성될 시작 위치 좌표(Transform)")]
    [SerializeField] private Transform spawnPoint;

    // 패턴 제어용 내부 상태 변수
    private float currentAccumulatedDamage = 0f;
    private bool isPatternActive = false;
    private bool isGroggy = false;

    private Rigidbody2D bossRigid;
    private Animator bossAnimator;

    // 외부(예: BossPointTeleport)에서 보스가 멈춰있어야 하는 상태인지 체크하기 위한 플래그
    public bool IsBossStunned => isGroggy || isPatternActive;

    // 코루틴의 중복 방지 및 개별 정지를 위한 참조 변수
    private Coroutine ultimateTimerCoroutine;
    private Coroutine cooldownTimerCoroutine;

    void Start()
    {
        // 컴포넌트 캐싱
        bossRigid = GetComponent<Rigidbody2D>();
        bossAnimator = GetComponent<Animator>();

        if (patternNoticePanel != null) patternNoticePanel.SetActive(false);

        // ⏱️ 씬 진입 후 정확히 30초 뒤에 첫 패턴이 시작되도록 설정합니다.
        Invoke("StartUltimatePattern", 30f);
    }

    /// <summary>
    /// 보스가 광역 전멸기를 캐스팅하기 시작할 때 호출하는 핵심 함수
    /// </summary>
    public void StartUltimatePattern()
    {
        if (BossMapCutscene.IsCutsceneActive) return;

        if (isCooldown)
        {
            Debug.Log("🛡️ [보스 패턴 거부] 아직 전체 공격기 주기가 돌아오지 않았습니다.");
            return;
        }

        if (isPatternActive || isGroggy) return;

        Debug.Log("🔥 [보스 기믹] 전체공격기 준비 시작! 플레이어는 20초 내로 200 대미지를 넣어야 합니다.");
        isPatternActive = true;
        currentAccumulatedDamage = 0f; // 누적 대미지 초기화

        // 알림 메시지 패널 활성화 및 문구 출력
        if (patternNoticePanel != null) patternNoticePanel.SetActive(true);
        if (noticeText != null)
        {
            noticeText.text = "<color=red>경고</color> 보스가 강력한 전체공격기를 준비합니다!\n<color=yellow>(20초 내로 200 대미지를 입혀 저지하세요!)</color>";
        }

        // 🕒 [핵심 추가]: 패턴 시작 경고 메시지도 띄운 지 정확히 3초 뒤 즉시 삭제
        CancelInvoke("CloseNoticePanel");
        Invoke("CloseNoticePanel", 3.0f);

        if (bossAnimator != null) bossAnimator.SetTrigger("isCasting");

        // 10초 타이머 코루틴 가동
        ultimateTimerCoroutine = StartCoroutine(UltimateTimerRoutine());
    }

    public void TakeDamageInPattern(float damageAmount)
    {
        if (!isPatternActive) return;

        currentAccumulatedDamage += damageAmount;
        Debug.Log($"💥 [전멸기 패턴 딜링 체크]: {damageAmount} 피격 완료! (현재 누적: {currentAccumulatedDamage} / 200)");

        if (currentAccumulatedDamage >= requiredDamage)
        {
            SuccessToInterrupt();
        }
    }

    // [성공 결과]: 10초 안에 200 대미지를 달성하여 저지 성공
    private void SuccessToInterrupt()
    {
        if (ultimateTimerCoroutine != null)
        {
            StopCoroutine(ultimateTimerCoroutine);
        }

        isPatternActive = false;

        // 패턴이 종료되었으므로 즉시 60초 주기 타이머 작동
        cooldownTimerCoroutine = StartCoroutine(CooldownTimerRoutine());

        // 2초 그로기 코루틴 가동
        StartCoroutine(GroggyRoutine());
    }

    // 10초 시간제한 감시 루틴
    private IEnumerator UltimateTimerRoutine()
    {
        yield return new WaitForSeconds(patternDuration);

        isPatternActive = false;

        // 패턴이 끝났으므로 즉시 60초 주기 타이머 작동
        cooldownTimerCoroutine = StartCoroutine(CooldownTimerRoutine());

        ExecuteScreenUltimateAttack();
    }

    // [실패 결과]: 거대 공격기 프리팹 스폰
    private void ExecuteScreenUltimateAttack()
    {
        Debug.LogError("💀 [보스 기믹 실패] 거대 전체공격기를 발동합니다!");

        if (noticeText != null)
        {
            noticeText.text = "<color=red>💥 전멸기 발동! 거대한 어둠의 벽이 엄습합니다!</color>";
        }

        if (ultimateAttackPrefab != null && spawnPoint != null)
        {
            Instantiate(ultimateAttackPrefab, spawnPoint.position, Quaternion.identity);
        }

        // 🕒 실패 문구를 띄운 후 정확히 3.0초 뒤 패널 삭제
        CancelInvoke("CloseNoticePanel");
        Invoke("CloseNoticePanel", 3.0f);
    }

    // 딜찍누 성공 시 보스가 완전히 얼어붙는 그로기 코루틴
    private IEnumerator GroggyRoutine()
    {
        isGroggy = true;
        if (bossRigid != null) bossRigid.linearVelocity = Vector2.zero;
        if (bossAnimator != null) bossAnimator.SetTrigger("isGroggy");

        if (noticeText != null)
        {
            noticeText.text = "<color=green>💫 그로기 상태입니다!</color>\n보스가 2초 동안 무력화됩니다!";
        }

        // 🕒 성공(그로기) 문구를 띄운 후 정확히 3.0초 뒤 패널 삭제
        CancelInvoke("CloseNoticePanel");
        Invoke("CloseNoticePanel", 3.0f);

        yield return new WaitForSeconds(groggyDuration);
        isGroggy = false;
    }

    private IEnumerator CooldownTimerRoutine()
    {
        isCooldown = true;
        Debug.Log($"⏳ [보스 쿨타임] 다음 패턴까지 {patternInterval}초 대기 시작.");

        yield return new WaitForSeconds(patternInterval);

        isCooldown = false;
        Debug.Log("🔓 [보스 쿨타임 해제] 60초가 지나 다음 전체 공격기를 실행합니다.");

        StartUltimatePattern();
    }

    private void CloseNoticePanel()
    {
        if (patternNoticePanel != null) patternNoticePanel.SetActive(false);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (isPatternActive && Input.GetKeyDown(KeyCode.G))
        {
            TakeDamageInPattern(50f);
        }
    }
#endif
}