using System.Collections;
using UnityEngine;

public class GameIntroManager : MonoBehaviour
{
    [Header("Typewriter Effect Reference")]
    // 중요: 방금 만드신 TypewriterEffect 스크립트를 인펙터에서 연결합니다.
    public TypewriterEffect typewriter;

    [Header("Character Reference")]
    public Transform characterTransform; // 두리번거릴 캐릭터의 Transform

    [Header("Cutscene Settings")]
    [Tooltip("글자가 완전히 다 찍힌 후 다음 대사로 넘어가기 전까지 대기하는 시간입니다.")]
    public float nextLineDelay = 1.5f;
    public float lookAroundSpeed = 0.2f; // 두리번거리는 속도

    private string[] lines = new string[3] {
        "첫 번째 대사입니다. (평온)",
        "두 번째 대사입니다. 어라? 여기 어디지?", // 이때 캐릭터가 두리번거림
        "세 번째 대사입니다. 정신을 차려야겠어."
    };

    void Start()
    {
        // 껐다 켰을 때만 작동하는 PlayerPrefs 조건문
        int isFirstTime = PlayerPrefs.GetInt("IsFirstTime", 1);

        if (isFirstTime == 1)
        {
            PlayerPrefs.SetInt("IsFirstTime", 0);
            PlayerPrefs.Save();

            StartCoroutine(StartIntroSequence());
        }
        else
        {
            // 이미 실행한 적이 있다면 텍스트 오브젝트 전체를 비활성화
            if (typewriter != null) typewriter.gameObject.SetActive(false);
        }
    }

    IEnumerator StartIntroSequence()
    {
        if (typewriter == null)
        {
            Debug.LogError("Typewriter Effect 스크립트가 연결되지 않았습니다!");
            yield break;
        }

        typewriter.gameObject.SetActive(true);

        // ================================================================
        // 1. 첫 번째 텍스트 출력 (소리 자동 재생)
        // ================================================================
        typewriter.ChangeTextAndType(lines[0]);

        // 글자가 다 타이핑될 때까지의 시간 + 다음 대사 전환 대기시간만큼 기다림
        yield return new WaitForSeconds(GetTypingDuration(lines[0]) + nextLineDelay);


        // ================================================================
        // 2. 두 번째 텍스트 출력 + 캐릭터 두리번거리기
        // ================================================================
        typewriter.ChangeTextAndType(lines[1]);

        // 두리번거리는 연출 시작
        Coroutine lookAroundRoutine = StartCoroutine(LookAroundAnimation());

        // 타이핑 끝날 때까지 대기
        yield return new WaitForSeconds(GetTypingDuration(lines[1]) + nextLineDelay);

        // 두 번째 대사가 끝나면 두리번거림 멈추고 복구
        StopCoroutine(lookAroundRoutine);
        ResetCharacterScale();


        // ================================================================
        // 3. 세 번째 텍스트 출력
        // ================================================================
        typewriter.ChangeTextAndType(lines[2]);
        yield return new WaitForSeconds(GetTypingDuration(lines[2]) + nextLineDelay);


        // 연출 완전히 끝, 텍스트 창 끄기
        typewriter.gameObject.SetActive(false);
    }

    // 대사 글자 수에 따라 코루틴이 기다려야 할 정확한 타이핑 시간을 계산하는 함수
    float GetTypingDuration(string text)
    {
        // TypewriterEffect 내부의 타이핑 속도를 가져와서 계산 (기본값 0.06f 기준)
        // 만약 세팅값이 다르면 리플렉션을 쓰거나 TypewriterEffect의 속도 변수를 public으로 바꾸면 좋지만,
        // 여기서는 안전하게 계산하기 위해 하드코딩 혹은 대략적인 계산을 적용합니다.
        float speed = 0.06f;
        return text.Length * speed;
    }

    // 캐릭터가 좌우로 두리번거리는 코루틴
    IEnumerator LookAroundAnimation()
    {
        if (characterTransform == null) yield break;

        Vector3 localScale = characterTransform.localScale;
        float originalX = Mathf.Abs(localScale.x);

        while (true)
        {
            characterTransform.localScale = new Vector3(-originalX, localScale.y, localScale.z);
            yield return new WaitForSeconds(lookAroundSpeed);

            characterTransform.localScale = new Vector3(originalX, localScale.y, localScale.z);
            yield return new WaitForSeconds(lookAroundSpeed);
        }
    }

    void ResetCharacterScale()
    {
        if (characterTransform != null)
        {
            Vector3 scale = characterTransform.localScale;
            characterTransform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        }
    }

    [ContextMenu("Reset Intro Flag")]
    public void ResetIntroFlag()
    {
        PlayerPrefs.DeleteKey("IsFirstTime");
        Debug.Log("최초 실행 플래그가 초기화되었습니다.");
    }
}