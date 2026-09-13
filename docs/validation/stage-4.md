# Stage 4 — 구조·assembly 검증

검증일: 2026-09-12, 원본 최종 재검증 2026-09-13

Unity: 6000.0.30f1 (62b05ba0686a)

## 기준과 변경

사용자 요청에 따라 먼저 3단계를 원본 `dev_game`의 `94af4890` (`Fix : 엔진 평가 및 탐색 말단 판정 개선`)으로 커밋했습니다. 그 상태를 기준으로 4단계를 진행합니다. 푸시는 하지 않았습니다.

규칙, 엔진, 화면/진행, 학습, Editor 도구, 테스트를 여섯 assembly로 분리했습니다. 상세 소유권과 참조 관계는 [assembly 구조](../architecture.md)에 정리합니다. 규칙·엔진 동작은 변경하지 않았습니다.

- Core와 Engine은 Unity 참조 없이 컴파일합니다.
- Runtime은 Core/Engine을, Training은 Core/ML-Agents를 참조합니다. UI 폴더는 Runtime assembly reference로 연결합니다.
- Editor 도구 및 테스트는 플레이어 컴파일에서 제외합니다. Training의 플랫폼 지원은 유지합니다.
- `Chessman`, `MovePlate`, `ChessAgent`는 담당 폴더로 `.meta`와 함께 이동했습니다. 기존 GUID를 기대값으로 고정한 테스트가 있습니다.
- IChessEngine의 불필요한 UnityEngine using, MovePlate의 VisualScripting using, UIManager의 미사용 SocialPlatforms using을 제거했습니다.

## 검증 결과

| 항목 | 결과 |
| --- | --- |
| 기존 캐시에서 첫 마이그레이션 실행 | 117개 중 111 통과, 6 실패 |
| Unity 완전 종료 후 재실행 | 117/117 통과 |
| 기존 테스트 | 106개 모두 통과 |
| 신규 assembly/asset 테스트 | 11개 모두 통과 |
| Library 없는 신규 import 테스트 | 117/117 통과, 실패/건너뜀 0 |
| 원본 dev_game 최종 재실행 | 117/117 통과, 실패/건너뜀 0 |
| Windows x64 Mono 개발 빌드 | 성공 (약 1분 31초) |
| 빌드 오류 / 경고 | 0 / 186 |

빌드 요약의 `Backend: Mono2x`, `StandaloneWindows64`, MainScene 단독 포함을 확인했습니다. Managed 폴더에는 `Chess.Core.dll`, `Chess.Engine.dll`, `Chess.Runtime.dll`, `Chess.Training.dll`이 있으며 `Chess.Editor.dll`, `Chess.Tests.EditMode.dll`, `nunit.framework.dll`은 없습니다. 학습 assembly의 플레이어 지원은 유지한 결과이며 배포에서 학습 패키지 전체가 제거됐다는 뜻은 아닙니다. 빌드용 Unity의 완전 종료도 확인했습니다.

첫 실행은 기존 Library가 이전 세 스크립트 경로와 Assembly-CSharp를 참조하여 시작 시 CS2001을 기록했습니다. 새 assembly 컴파일은 성공했지만 이동한 MonoScript asset 로딩이 일시적으로 실패해 GUID/프리팹/학습 씬 검사와 기존 MainScene 통합까지 총 6개가 실패했습니다. 코드·GUID·씬을 추가로 변경하지 않고 Unity를 종료/재실행한 결과 117개가 모두 통과했습니다. 첫 실패를 숨기거나 최종 성공에 포함시키지 않았습니다.

캐시에만 의존한 통과인지 확인하기 위해 Unity 종료 후 기존 `Library`를 삭제하지 않고 `Validation/Stage4/Library-before-clean-import`에 보관했습니다. 새 Library의 첫 실행에서 117개가 모두 통과했습니다. 씬·프리팹 관련 41개 파일은 원본과 해시 비교로 내용 보존을 확인했습니다.

## 구조 및 연결 검사

새 `Assets/Tests/EditMode/Editor/Stage4AssemblyTests.cs`는 다음을 확인합니다.

- 규칙·세션 타입의 Core 소속, Core/Engine의 Unity 및 화면·학습 참조 부재.
- 화면/매니저 타입의 Runtime 소속, Runtime에서 학습·ML-Agents로 향하는 직접 참조 부재.
- ChessAgent의 Training 소속과 Core/ML-Agents 참조, 화면/엔진 참조 부재.
- Unity 플레이어 컴파일 목록에서 Editor/테스트 assembly 및 테스트 소스 제외.
- 이동한 세 스크립트의 기존 GUID와 로딩되는 실제 클래스 일치.
- `Assets/Prefabs`의 모든 프리팹, MainScene 및 TrainingScene의 누락 스크립트 부재.
- TrainingScene의 ChessAgent가 초기 보드를 만들고 백의 합법 수 20개를 생성하며, 배포 활성 씬에 TrainingScene이 포함되지 않음.

기존 MainScene 통합은 `EnterPlayMode` 이후 실제 프레임을 기다려 AI 예약 취소/중복 방지, 일시정지·재개, 승급 대기·단일 적용·체크메이트 결과창을 검증합니다. 규칙 perft와 엔진 평가·Quiescence 회귀도 함께 실행합니다.

## 재현 및 산출물

프로젝트 루트에서 실행하며, 테스트와 빌드 사이에는 Unity 프로세스 종료를 확인합니다. 라이선스는 이전 단계와 동일하게 정식 권한 상승으로 사용자 환경에서 확인했습니다.

```powershell
New-Item -ItemType Directory -Force Validation/Stage4 | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -runTests -testPlatform EditMode -testResults "$PWD\Validation\Stage4\tests-clean.xml" -logFile "$PWD\Validation\Stage4\tests-clean.log" -projectPath "$PWD"
```

첫 마이그레이션 결과는 `tests.xml`/`tests.log`, 재실행은 `tests-reload.xml`/`tests-reload.log`, 새 Library 검증은 `tests-clean.xml`/`tests-clean.log`에 저장합니다.

빌드는 기존 `Stage0ValidationBuild.Run`을 사용하며 이제 이 도구는 `Chess.Editor`에 속합니다. 출력은 기존 고정 경로 `Validation/Stage0/Windows64/Chess.exe`, 요약은 `Validation/Stage0/build-summary.txt`입니다. 빌드가 실행되면 이전 단계의 해당 고정 경로 산출물은 갱신됩니다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -executeMethod Stage0ValidationBuild.Run -logFile "$PWD\Validation\Stage4\build.log" -projectPath "$PWD"
```

상세 로그/결과 XML/캐시/빌드는 Git에서 제외된 `Validation/`에만 보관합니다. 새 assembly 정의와 메타데이터, 테스트, 문서는 버전 관리 대상입니다.

Unity 종료 후 렌더링 asset과 ProjectSettings에는 `94af4890` 대비 의미 있는 내용 차이가 없음을 확인했습니다. 기존 자동 생성 설정은 보존했습니다. 4단계 변경은 원본 `dev_game`에 옮겨 GitHub Desktop에서 확인할 수 있게 하며, 새 4단계 커밋·푸시는 수행하지 않습니다.

원본으로 인계한 후에도 기존 Library의 첫 실행에서 동일한 6개 실패가 재현됐습니다. 원본 Unity를 완전히 종료하고 다시 실행한 최종 결과는 117/117 통과입니다. 원본의 로그/XML은 `C:\Users\ychul\GitHub\Chess\Validation\Stage4\handoff.*` 및 `handoff-retry.*`에 있습니다. 최종 원본 테스트 과정에서 `ProjectSettings/ProjectSettings.asset`의 `SENTIS_ANALYTICS_ENABLED` 정의가 비워진 자동 변경은 보존했습니다. 이는 앞서 빌드까지 완료한 검증 작업 트리의 설정 점검 이후, 원본 테스트에서 발생한 변경이며 assembly 기능 수정과 구분합니다.

## 남은 제한

학습 검증은 씬 연결과 초기 보드 생성까지이며, 모델 추론 정확성이나 장기 학습·플레이어 학습 실행 검증은 아닙니다. ML-Agents와 관련 패키지의 설치·배포 용량을 줄이는 작업도 포함하지 않습니다. 이번 구조 분리는 장시간 실제 대국 및 수동 화면 QA를 대신하지 않습니다.

다음 단계는 5단계 성능/UI입니다. 3단계 문서의 유한 Quiescence 및 탐색 API의 대국 반복/반수 이력 미전달 제한도 유지됩니다.
