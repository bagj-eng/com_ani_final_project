using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Image 및 CanvasScaler 참조용

public class EndingCredits : MonoBehaviour
{
    [Header("🎬 크레딧 설정")]
    [Tooltip("크레딧 TextMeshPro의 RectTransform을 넣어주세요.")]
    [SerializeField] private RectTransform creditsTextRect;
    [Tooltip("글자가 올라가는 속도입니다.")]
    [SerializeField] private float scrollSpeed = 40f;
    [Tooltip("모든 요소가 화면 위로 완전히 사라진 후 다음 씬으로 넘어가기 전 대기 시간")]
    [SerializeField] private float endDelay = 2.0f;

    [Header("🖼️ 사진 페이드 인 설정")]
    [Tooltip("텍스트와 함께 올라가며 서서히 밝아질 UI Image를 넣어주세요.")]
    [SerializeField] private Image creditImage;
    [Tooltip("사진이 완전히 밝아지는 데 걸리는 시간(초)")]
    [SerializeField] private float imageFadeDuration = 2.0f;

    [Header("🔄 다음 목적지")]
    [Tooltip("엔딩 크레딧이 끝나면 이동할 메인 메뉴 씬의 이름을 적어주세요.")]
    [SerializeField] private string nextSceneName = "MainMenu";

    private bool isScrolling = false;
    private float targetY;

    void Start()
    {
        if (creditsTextRect != null)
        {
            CanvasScaler scaler = creditsTextRect.GetComponentInParent<CanvasScaler>();
            float referenceHeight = (scaler != null) ? scaler.referenceResolution.y : Screen.height;

            // 1. 시작 위치: 즉시 보이도록 화면 맨 아래 경계선에 딱 맞춤
            creditsTextRect.anchoredPosition = new Vector2(0, -referenceHeight);

            // 💡 [핵심 고침]: 자식 오브젝트(사진)가 부모 텍스트 밑으로 빠져나간 거리까지 자동으로 계산합니다.
            float extraBottomSpace = 0f;
            if (creditImage != null)
            {
                RectTransform imageRect = creditImage.rectTransform;
                // 사진의 하단 경계선 좌표를 계산하여 부모 텍스트보다 얼마나 밑에 있는지 구합니다.
                float imageBottomY = Mathf.Abs(imageRect.anchoredPosition.y) + (imageRect.sizeDelta.y * imageRect.pivot.y);
                if (imageBottomY > creditsTextRect.sizeDelta.y)
                {
                    extraBottomSpace = imageBottomY - creditsTextRect.sizeDelta.y;
                }
            }

            // 💡 최종 목적지: 텍스트 높이 + 화면 높이 + 사진이 밑으로 삐져나간 길이까지 모두 더해 
            // 씬 안의 모든 요소가 상단 화면 바깥으로 100% 완전하게 스쳐 지나갈 때까지 좌표를 확장합니다.
            targetY = creditsTextRect.sizeDelta.y + referenceHeight + extraBottomSpace + 100f;
        }

        // 사진 초기 투명도 설정 및 페이드 인 시작
        if (creditImage != null)
        {
            Color c = creditImage.color;
            c.a = 0f;
            creditImage.color = c;
            StartCoroutine(FadeInImageRoutine());
        }

        // 즉시 스크롤 가동
        isScrolling = true;
    }

    void Update()
    {
        if (!isScrolling || creditsTextRect == null) return;

        // 멈추지 않고 아래에서 위로 계속 이동
        creditsTextRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        // 💡 모든 크레딧 요소가 화면 위로 완벽히 증발했는지 체크
        if (creditsTextRect.anchoredPosition.y >= targetY)
        {
            isScrolling = false;
            StartCoroutine(FinishCreditsRoutine());
        }

        // 스킵 기능 (Space, ESC, 마우스 클릭)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            isScrolling = false;
            GoToNextScene();
        }
    }

    private IEnumerator FadeInImageRoutine()
    {
        float timer = 0f;
        Color startColor = creditImage.color;

        while (timer < imageFadeDuration)
        {
            timer += Time.deltaTime;
            startColor.a = Mathf.Lerp(0f, 1f, timer / imageFadeDuration);
            creditImage.color = startColor;
            yield return null;
        }

        startColor.a = 1f;
        creditImage.color = startColor;
    }

    private IEnumerator FinishCreditsRoutine()
    {
        yield return new WaitForSeconds(endDelay);
        GoToNextScene();
    }

    private void GoToNextScene()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(nextSceneName);
    }
}