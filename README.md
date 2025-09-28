# 2025순천AI게임잼 - 두루두루 클린업!


## 프로젝트 개요

- **게임 장르**: 2D 탑뷰 액션 게임
- **개발 엔진**: Unity 6000.2.5f1
- **개발 기간**: 2025/09/26 - 2025/09/28
- **게임 컨셉**: 플레이어가 캐릭터를 조작하여 맵의 오염과 쓰레기를 청소하고, 적들을 물리치며 스테이지를 클리어하는 환경 정화 게임

## 게임 플레이

### 핵심 시스템
- **스테이지 진행**: 여러 스테이지를 순차적으로 클리어
- **청소 시스템**: 그리드 기반 맵에서 오염(Pollution)과 쓰레기(Trash) 제거
- **적 처치**: 다양한 순천만 생물 적들과의 전투
- **캐릭터 성장**: 랜덤한 버프 시스템 중 사용자 선택 옵션으로 캐릭터 스탯 강화
- **보상 시스템**: 스테이지 클리어 시 카드 기반 보상 선택

### 조작법
- **이동**: WASD
- **정화**: 스페이스 바
- **공격**: 마우스 클릭
- **UI 조작**: 마우스 및 키보드

## 실행 방법

### 1. 환경 요구사항
- **Unity**: Unity 6000.2.5f1 이상
- **OS**: Windows 10/11 (개발 환경 기준)
- **.NET**: Unity 내장 버전 사용

### 2. 프로젝트 실행
1. **Unity Hub에서 프로젝트 열기**
   ```
   Unity Hub > 프로젝트 추가 > 프로젝트 폴더 선택
   ```

2. **Unity Editor에서 플레이**
   - `Scenes/MainMenu.unity` 또는 `Scenes/InGame.unity` 열기
   - Play 버튼 클릭 또는 `Ctrl+P`

3. **빌드 실행**
   - `File > Build Settings` (`Ctrl+Shift+B`)
   - 플랫폼 선택 (Windows Standalone 권장)
   - `Build and Run` 클릭

### 3. 테스트 씬 실행
개발자별 테스트 씬들이 Personal 폴더에 있습니다:
- `Assets/Project/Scripts/Personal/mskim2/TestScene/` - 주요 게임 시스템 테스트
- `Assets/Project/Scripts/Personal/haewon.shon/` - 적 AI 테스트

## 게임 진행 순서

1. **MainMenu**: 게임 시작 화면, 기본 조작 방법 설명
2. **Prologue**: 게임 스토리 설명
3. **InGame**: 메인 게임플레이
   - Stage 기반 진행
   - 정화도 100% 달성 시 보스 소환
   - 보스 처치 후 다음 스테이지로 진행
   - 스테이지 클리어 시 보상 카드 선택
4. **Game Over/Clear**: 결과 화면

## 주요 기능

### 캐릭터 시스템
- **이동 시스템**: 부드러운 2D 이동, 선택적 4방향 이동 제한
- **청소 시스템**: 주변 영역 청소, 반경 조절 가능
- **버프 시스템**: 공격력, 이동속도, 청소 반경 등 다양한 능력치 강화

### 적 시스템
- **State Machine**: Idle, Move, Attack, Skill, Dead 상태 관리
- **Roamer AI**: 맵을 돌아다니며 오염 흔적 생성
- **체력 시스템**: HP 표시 UI와 함께 체력 관리

### UI 시스템
- **애니메이션**: DOTween 기반 부드러운 UI 애니메이션
- **보상 UI**: 카드 선택 방식의 직관적인 보상 시스템
- **진행도 UI**: 청소율, 스테이지 진행도 실시간 표시

## 개발자 정보

### 팀 구성
- **mskim2**: 핵심 게임 시스템, UI, 맵 관리
- **haewon.shon**: 적 AI 시스템, 게임플레이 밸런싱

### 개발 구조
```
Assets/Project/Scripts/
├── Core/                    # 핵심 시스템 (Singleton, EventBus, Pool)
├── Gameplay/               # 게임플레이 로직 (Character, Stage, Map)
├── Personal/               # 개발자별 개인 작업 폴더
│   ├── mskim2/            # 주요 시스템 개발
│   └── haewon.shon/       # 적 시스템 개발
└── ScriptableObjects/      # 설정 데이터
```

## 사용된 에셋 및 패키지

### 외부 에셋
- **DOTween Pro**: 애니메이션 시스템
- **Feel v5.7**: 게임 피드백 시스템
- **Odin Inspector**: 에디터 도구
- **Febucci Text Animator**: 텍스트 애니메이션

### Unity 패키지
- Universal Render Pipeline (URP) 17.2.0
- Unity Input System 1.14.2
- Addressables 2.7.3
- Cinemachine 3.1.4

## 아키텍처 특징

### 디자인 패턴
- **Singleton**: 전역 매니저 관리
- **Event-Driven**: EventBus를 통한 느슨한 결합
- **Object Pooling**: 성능 최적화
- **ScriptableObject**: 데이터 기반 설정

### 핵심 시스템
- **StageManager**: 스테이지 진행 관리
- **Character**: 플레이어 캐릭터 및 버프 시스템
- **Enemy**: 적 상태 머신
- **RewardSystem**: 카드 기반 보상 선택

## 문제 해결

### 일반적인 문제
1. **Unity 버전 호환성**: Unity 6000.2.5f1 사용 권장
2. **패키지 누락**: Package Manager에서 누락된 패키지 설치
3. **빌드 오류**: TextMeshPro 폰트 에셋 확인

### 디버그 기능
- **MapManager Inspector**: 런타임에서 맵 상태 확인 및 수정
- **Gizmos**: Scene View에서 그리드 및 디버그 정보 표시
- **Debug 모드**: 각 시스템별 로그 및 상태 확인

## 라이선스

2025 순천 AI 게임잼 프로젝트 - 교육 및 비상업적 목적
