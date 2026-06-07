using UnityEngine;

public class Displayselector : MonoBehaviour
{
    void Awake()
    {
        // 1. 게임 실행 시 화면 해상도를 1920x1080 창모드(false)로 강제 고정
        // 맨 뒤 파라미터는 주사율(Refresh Rate)이며 0으로 두면 모니터 기본 설정을 따릅니다.
        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed, 0);

        // 2. 윈도우 OS의 OS 배율(DPI Scaling) 간섭을 무시하고 1.0 정배율로 고정
        // 고해상도 모니터에서 화면이 흐려지거나 픽셀이 뭉개지는 현상을 방지합니다.
        QualitySettings.vSyncCount = 1; // 화면 찢어짐 방지 (선택 사항)
    }
}