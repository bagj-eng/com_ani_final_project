# 🚀 프로젝트 제목 (Project Title)

"Unity 기반으로 개발한 2D 메트로배니아 액션 플랫포머 게임입니다."

---

## 📌 1. 프로젝트 개요 (Overview)

- **개발 기간**: 2026년 6월3일 ~ 2026년 6월 6일 (3일간 진행)
- **개발 인원**: 2명 (팀 프로젝트 / 팀원 : 정지원 , 박주호)
- **플랫폼**: PC (Windows), Web, Mobile 등
- **주요 기술**: 예시: Unity, C#, Git

---

## ✨ 2. 핵심 기능 및 특징 (Key Features)

### 🔹 주요 기능 1

* **타임라인 기반 패턴 제어 (Coroutine System)**
  * 유니티 코루틴(`IEnumerator`)을 활용하여 **[경고 장판 표시 ➔ 캐스팅 대기 ➔ 데미지 판정 및 이펙트 ➔ 장판 제거]**로 이어지는 일련의 보스 공격 프로세스를 비동기적으로 자연스럽게 제어합니다.

* **정밀한 광역 범위 감지 (Physics Overlap)**
  * `Physics.OverlapSphere`를 사용하여 보스 중심의 3D 구체 범위를 실시간으로 계산합니다. 무대 전체 또는 특정 반경 내의 플레이어를 정확하게 감지합니다.

* **최적화된 타겟팅 및 컴포넌트 참조**
  * 지정된 `LayerMask`(예: Player 레이어)만 필터링하여 불필요한 충돌 연산을 최소화합니다.
  * `TryGetComponent`를 사용하여 가비지 컬렉션(GC) 생성을 줄이고, 컴포넌트 부재로 인한 런타임 Null 에러를 안전하게 방지합니다.

* **디버그용 기즈모(Gizmos) 지원**
  * `OnDrawGizmosSelected`를 탑재하여, 유니티 에디터 상에서 보스를 선택했을 때 전체 공격이 미치는 반경(Radius)을 빨간색 선으로 시각화하여 확인할 수 있습니다. 인게임 테스트 없이도 직관적인 범위 조절이 가능합니다.


## ✨ 특징 (Key Characteristics)

* **높은 모듈성 및 재사용성**
  * 보스 AI 스크립트나 패턴 제어 매니저에서 `TriggerUltimateAttack()` 함수 하나만 호출하면 독립적으로 작동하므로, 어떤 보스 캐릭터나 오브젝트에도 쉽게 부착하여 사용할 수 있습니다.

* **인스펙터 커스터마이징 편의성**
  * 공격력(`damage`), 캐스팅 시간(`castingTime`), 공격 범위(`attackRadius`) 등 핵심 밸런스 데이터와 경고 장판 및 폭발 이펙트 프리팹을 유니티 인스펙터 창에서 직관적으로 수정할 수 있도록 기획되었습니다.

* **유연한 연출 확장성**
  * 경고 장판의 크기가 공격 범위(`attackRadius`)에 맞추어 자동으로 스케일링되므로, 기획 피드백에 따라 수치를 변경해도 이펙트와 실제 판정 범위가 어긋나지 않고 동기화됩니다.

## 🛠 3. 기술 스택 (Tech Stack)

### 🎮 Engine & Environment

- **Engine**: Unity 2022.3 LTS
- **Language**: C#
- **IDE**: Visual Studio 2022 / Rider

### 📦 Library & 아키텍처

- **디자인 패턴**: Singleton Pattern, State Pattern
- **버전 관리**: Git, GitHub

---

## 🕹 4.조작 방법 (Getting Started & Controls)

### ⌨️ 조작 방법 (Controls)

| 키 (Key)              | 행동 (Action)       |
| --------------------- | ------------------- |
|        방향키          | 캐릭터 이동         |
|         space         | 점프 (Jump)         |
|        A S  / F       | 공격 / 인터랙션     |
---
