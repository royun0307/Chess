# Chess

Unity로 제작한 2D 체스 게임 프로젝트입니다.  
기본 체스 규칙, 특수 행마, 게임 종료 판정, Alpha-Beta 기반 체스 AI, Unity ML-Agents 기반 강화학습 실험, EditMode 테스트를 구현했습니다.

## 개발 환경

- Unity 6000.0.30f1
- C#
- Universal Render Pipeline 2D
- Unity Input System
- Unity Test Framework
- Unity ML-Agents

## 주요 기능

### 체스 규칙

- 기물별 이동 규칙
- 턴 관리
- 기물 잡기
- 체크 판정
- 체크메이트 판정
- 스테일메이트 판정

### 특수 행마

- 캐슬링
- 앙파상
- 프로모션

### 무승부 판정

- 스테일메이트
- 기물 부족
- 50수 룰
- 3회 반복

### 체스 AI

- Alpha-Beta Pruning 기반 탐색
- Quiescence Search
- 기물 가치 평가
- Piece-Square Table 기반 위치 평가
- 이동성 평가
- 폰 구조 평가
- 킹 안전성 평가

### 강화학습 실험

Unity ML-Agents를 활용하여 체스 에이전트 학습 구조를 실험했습니다.

- `ChessAgent` 구현
- `CollectObservations()`를 통한 보드 상태 관측
- `OnActionReceived()`를 통한 행동 처리
- 보상 / 패널티 기반 학습 구조 설계
- 에피소드 단위 학습 흐름 구성
- 탐색 기반 AI와 강화학습 기반 AI를 비교할 수 있는 구조 마련

### 테스트

Unity Test Framework 기반 EditMode 테스트를 통해 주요 체스 규칙을 검증했습니다.

- 초기 보드에서 백의 합법 수 20개 검증
- 앙파상 실행 시 잡힌 폰 제거 검증
- 킹사이드 캐슬링 가능 조건 검증
- 프로모션 시 퀸으로 변경 검증
- 체크 판정 검증
- 체크메이트 판정 검증
- 스테일메이트 판정 검증

## 프로젝트 구조

```text
Assets/
├─ Scripts/
│  ├─ Board      # 보드 데이터, 위치, 이동 표시
│  ├─ Piece      # Pawn, Knight, Bishop, Rook, Queen, King
│  ├─ Move       # NormalMove, Castle, Enpassant, Promotion 등
│  ├─ Game       # GameState, Result, 게임 진행 상태
│  ├─ Engine     # 체스 AI, 평가 함수, ChessAgent
│  ├─ Manager    # GameManager, BoardManager, EngineManager, UIManager
│  └─ UI         # 결과창, 프로모션창, 일시정지 UI
│
└─ Tests/
   └─ EditMode/
      └─ Editor/
         └─ ChessRuleTests.cs
```

## 아키텍처

이 프로젝트는 체스 규칙 로직과 Unity 화면 표시 로직을 분리하는 방향으로 설계했습니다.

```text
UI Layer
├─ Result UI
├─ Promotion UI
└─ Pause UI
        ↓
Manager Layer
├─ GameManager
├─ BoardManager
├─ EngineManager
└─ UIManager
        ↓
Core Game Logic
├─ Board
├─ Piece
├─ Move
├─ GameState
└─ Position
        ↓
AI / RL Layer
├─ Search Engine
│  ├─ SimpleChessEngine
│  ├─ Alpha-Beta Search
│  ├─ Quiescence Search
│  └─ Evaluation
│
└─ Reinforcement Learning
   ├─ ChessAgent
   ├─ ML-Agents
   ├─ CollectObservations
   ├─ OnActionReceived
   ├─ Rewards / Episodes
   └─ Training / Inference
        ↓
Tests
├─ EditMode Tests
└─ ChessRuleTests
```

## 핵심 설계

### 1. 보드 로직과 Unity 표시 분리

체스 규칙은 `Board`, `Piece`, `Move`, `GameState`에서 처리하고,  
Unity 화면 표시와 입력 처리는 `BoardManager`, `GameManager`에서 담당하도록 분리했습니다.

### 2. 기물별 이동 규칙 분리

각 체스 기물은 `Piece`를 기반으로 하며,  
`Pawn`, `Knight`, `Bishop`, `Rook`, `Queen`, `King` 클래스에서 자신의 이동 후보를 생성합니다.

### 3. 특수 행마를 Move 클래스로 분리

일반 이동, 캐슬링, 앙파상, 프로모션을 각각 별도 Move 클래스로 나누어  
특수 규칙이 `GameState`에 몰리지 않도록 구성했습니다.

### 4. 합법 수 검증

기물이 생성한 이동 후보는 `Move.IsLegal()`을 통해 검증되며,  
이동 후 자신의 킹이 체크 상태가 되는 수는 제외됩니다.

### 5. 탐색 기반 AI와 강화학습 구조 분리

`SimpleChessEngine`은 Alpha-Beta 기반 탐색 AI를 담당하고,  
`ChessAgent`는 Unity ML-Agents 기반 강화학습 실험을 담당하도록 분리했습니다.

이를 통해 기존 탐색 알고리즘 기반 AI와 강화학습 기반 AI를 비교하거나 확장할 수 있는 구조를 만들었습니다.

## 강화학습 구조

강화학습 구조는 다음 흐름을 기준으로 설계했습니다.

```text
Board State
    ↓
CollectObservations()
    ↓
Policy / Agent
    ↓
OnActionReceived()
    ↓
Move Selection
    ↓
Reward / Penalty
    ↓
Episode Update
```

### 관측

`ChessAgent`는 현재 체스판 상태를 관측하여 에이전트가 판단할 수 있는 입력값으로 변환합니다.

주요 관측 대상은 다음과 같습니다.

- 각 칸의 기물 존재 여부
- 기물 종류
- 기물 색상
- 현재 턴
- 게임 진행 상태

### 행동

에이전트는 가능한 수 중 하나를 선택하고,  
선택한 행동은 체스 규칙에 따라 실제 이동으로 변환됩니다.

### 보상

학습을 위해 다음과 같은 보상 구조를 사용할 수 있습니다.

- 합법 수 선택 시 보상
- 불법 수 선택 시 패널티
- 기물 획득 시 보상
- 체크 유도 시 보상
- 체크메이트 승리 시 큰 보상
- 패배 시 큰 패널티
- 무승부 시 중립 또는 작은 패널티

## 향후 개선 계획

- 편의성 기능과 UI 개선
- 플레이 화면 GIF 또는 스크린샷 추가
- 체스 AI 난이도 분리
- Chess960 구현
- King of the Hill 변형 체스 구현
- 테스트 케이스 추가
- ML-Agents 학습 환경 개선
- 탐색 기반 AI와 강화학습 AI 성능 비교

## 목표

이 프로젝트는 단순한 체스판 구현이 아니라,  
체스 규칙을 객체지향적으로 모델링하고, 탐색 기반 AI와 강화학습 실험, 테스트 코드까지 포함한 체스 시스템을 직접 구현하는 것을 목표로 합니다.
