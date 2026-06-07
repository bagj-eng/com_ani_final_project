using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 제어 필수

public class BossHPBar : MonoBehaviour
{
    [Header("슬라이더 UI 연동")]
    [SerializeField] private Slider mainHpSlider;     // 실제 체력바 (빨간색)
    [SerializeField] private Slider effectHpSlider;   // 잔상 체력바 (노란색)

    [Header("연출 세팅")]
    [Tooltip("잔상이 실제 체력을 따라가는 속도입니다. 수치가 높을수록 빠르게 쫓아갑니다.")]
    [SerializeField] private float lirpSpeed = 2f;

    [Tooltip("체력이 깎인 후 잔상이 줄어들기 시작할 때까지 대기하는 시간(초)입니다.")]
    [SerializeField] private float delayTime = 0.5f;

    private float targetHpRatio = 1f; // 목표 체력 비율 (0 ~ 1)
    private float currentEffectRatio = 1f;
    private float currentDelayTimer = 0f;

    /// <summary>
    /// 보스의 체력이 변할 때 외부(보스 스크립트 등)에서 호출해주는 함수입니다.
    /// </summary>
    /// <param name="currentHp">보스의 현재 체력</param>
    /// <param name="maxHp">보스의 최대 체력</param>
    public void UpdateHP(float currentHp, float maxHp)
    {
        // 0~1 사이의 비율로 전환
        targetHpRatio = Mathf.Clamp01(currentHp / maxHp);

        // 실제 체력바는 피격 즉시 '툭' 하고 먼저 깎여서 타격감을 줍니다.
        if (mainHpSlider != null)
        {
            mainHpSlider.value = targetHpRatio;
        }

        // 데미지를 입었으므로 잔상 대기 타이머를 리셋하여 딜레이를 줍니다.
        currentDelayTimer = delayTime;
    }

    void Update()
    {
        if (effectHpSlider == null) return;

        // 체력이 줄어든 후 딜레이 타이머 계산
        if (currentDelayTimer > 0f)
        {
            currentDelayTimer -= Time.deltaTime;
        }
        else
        {
            // 딜레이 시간이 지나면 잔상(노란색)이 실제 체력(targetHpRatio)을 향해 부드럽게 쫓아갑니다.
            currentEffectRatio = Mathf.Lerp(currentEffectRatio, targetHpRatio, Time.deltaTime * lirpSpeed);
            effectHpSlider.value = currentEffectRatio;
        }
    }

    // 테스트용 치트키: 스페이스바를 누르면 체력이 20%씩 깎여서 연출을 확인해볼 수 있습니다.
    // 실제 보스 대미지 시스템을 연결한 후에는 이 Update 안의 테스트 코드를 지워주세요!
#if UNITY_EDITOR
    private float testMaxHp = 100f;
    private float testCurHp = 100f;
    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
        {
            testCurHp -= 20f;
            if (testCurHp < 0f) testCurHp = testMaxHp; // 0 이하가 되면 다시 풀피
            UpdateHP(testCurHp, testMaxHp);
        }
    }
#endif
}