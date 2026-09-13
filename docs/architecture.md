# 체스 프로젝트 assembly 구조

4단계부터 프로젝트 코드는 다음 여섯 assembly로 컴파일합니다. 클래스 이름과 namespace는 유지하며, 씬에 연결된 스크립트 이동 시 `.meta` GUID도 유지합니다.

| Assembly | 담당 코드 | 직접 프로젝트 의존성 |
| --- | --- | --- |
| `Chess.Core` | Board, GameState/GameSession, Position, Piece, Move, 결과·이력 | 없음 |
| `Chess.Engine` | IChessEngine, SimpleChessEngine, SimplePST | Core |
| `Chess.Runtime` | Manager, UI, Chessman, MovePlate | Core, Engine |
| `Chess.Training` | ChessAgent | Core |
| `Chess.Editor` | Editor 빌드 검증 도구 | 없음 |
| `Chess.Tests.EditMode` | 규칙·엔진·진행·씬·assembly 테스트 | Core, Engine, Runtime, Training |

Runtime은 Unity UI/TextMeshPro를, Training은 ML-Agents를 참조합니다. Runtime에서 Training이나 ML-Agents로 향하는 직접 참조는 없습니다. Training은 기존 플레이어 기반 학습 가능성을 유지하기 위해 Editor 전용으로 제한하지 않았습니다. 배포용 활성 씬은 계속 MainScene이고 TrainingScene은 제외됩니다. 이 분리는 ML-Agents 패키지나 모든 관련 배포 파일을 제거한다는 의미가 아닙니다.

## 파일 배치 규칙

- `Assets/Scripts/Chess.Core.asmdef`가 기본 경계입니다. `Board/`, `Game/`, `Piece/`, `Move/`의 순수 C# 규칙과 세션 코드가 여기에 속합니다.
- `Assets/Scripts/Engine/`은 `Chess.Engine.asmdef`로 분리합니다. 인터페이스도 Unity 타입을 사용하지 않습니다.
- `Assets/Scripts/Manager/Chess.Runtime.asmdef`가 화면·진행 코드를 담당합니다. `Assets/Scripts/UI/Chess.Runtime.asmref`는 UI 폴더를 같은 assembly에 포함시킵니다. 새 MonoBehaviour는 규칙 폴더가 아니라 Runtime 경계 안에 둡니다.
- `Chessman.cs`는 `Game/`에서 `Manager/`로, `MovePlate.cs`는 `Board/`에서 `Manager/`로 이동했습니다. `ChessAgent.cs`는 `Engine/`에서 `Training/`으로 이동했습니다.
- `Assets/Editor/`와 `Assets/Tests/EditMode/Editor/`는 각각 별도의 Editor 전용 assembly입니다. 테스트 assembly에는 TestAssemblies 설정을 명시합니다.

## 의존성 제한

Core와 Engine은 `noEngineReferences: true`로 컴파일하여 UnityEngine/UnityEditor 사용을 차단합니다. 모든 프로젝트 assembly는 자동 assembly 참조를 끄고 의존성을 명시합니다. 사전 컴파일된 플러그인 참조도 기본적으로 받지 않으며, 테스트만 NUnit을 명시합니다.

`GameSession`은 진행 상태 정책을 담당하지만 Unity 화면이나 싱글톤을 참조하지 않습니다. Runtime이 Core와 Engine을 조합합니다. UIManager의 Editor 종료 분기는 기존 `UNITY_EDITOR` 조건부 컴파일을 유지하므로 플레이어 코드에는 Editor API가 포함되지 않습니다.

## 검증 및 이동 시 주의점

`Stage4AssemblyTests`는 실제 컴파일된 assembly 소속/참조, 플레이어에서 Editor·테스트 제외, 기존 GUID, 모든 체스 프리팹 및 MainScene/TrainingScene의 누락 스크립트를 검사합니다. 기존 1단계 실제 MainScene 플레이 모드 테스트도 유지합니다.

이전 Library가 있는 프로젝트에 파일 이동과 assembly 정의를 동시에 반영하면 첫 실행에서 이전 파일 경로의 컴파일 오류 또는 일시적인 Missing Script가 나타날 수 있습니다. 이번 검증에서는 Unity를 완전히 종료하고 다시 실행한 뒤 정상화됐습니다. 그런 상태에서 씬이나 프리팹을 저장하지 말고 재시작해 확인합니다. 문제가 지속되면 Unity를 닫고 생성 캐시 `Library`를 별도 위치에 보관한 뒤 재생성하여 확인할 수 있습니다. `.meta` 파일이나 GUID를 새로 만들지 않습니다.

## 5단계 비동기 탐색

EngineManager는 메인 스레드에서 보드를 복사하고 작업 스레드에서 탐색합니다. Unity 객체와 게임 세션은 작업 스레드에 전달하지 않습니다. 완료 결과는 메인 스레드에서 세션과 Revision을 다시 확인한 뒤 적용합니다.

IBudgetedChessEngine은 반복 심화와 협력적 취소를 지원합니다. 기본 제한은 1초/최대 깊이 6이며 완료한 깊이의 결과만 채택합니다. 첫 깊이도 끝내지 못하면 합법 수를 사용합니다. 제한 확인 사이의 수 생성·평가 시간 때문에 실제 경과 시간은 예산을 약간 넘을 수 있습니다. 기존 IChessEngine 동기 API는 호환성을 유지하며 시간 제한이 없습니다.

일시정지·재시작·비활성화는 대기 코루틴과 탐색 토큰을 취소합니다. 취소된 작업의 오류도 관찰하고 종료 후 토큰 자원을 해제합니다. 구형 엔진은 강제 중단할 수 없지만 완료 결과를 폐기합니다. 화면 상단 상태 표시와 오류 시 Retry 버튼은 Runtime UI가 담당합니다. WebGL처럼 작업 스레드가 제한된 플랫폼은 이번 검증 범위가 아닙니다.
