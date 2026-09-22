# 7단계: 대국 UI 통합과 Windows 배포 검증

검증일: 2026-09-22. Unity 6000.0.30f1.

## 기준 보존

원본 `C:/Users/ychul/GitHub/Chess`의 `dev_game`, HEAD `5e9e992770a12097be2eddaf76eb08a20084930e`에서 시작했습니다. 이 커밋 이후의 대국 설정·시계·색 선택·평가바·잡힌 말·배경 UI 미커밋 파일 26개를 현재 `a612/Chess` 작업 폴더로 복사하고 각 파일의 SHA-256 일치를 확인했습니다. `.meta` GUID를 유지했습니다. 복사 목록과 원본 해시는 `Validation/Stage7/source-manifest.json`에 있습니다. 이전 `e418`, `8258` 폴더는 변경하지 않았습니다.

원본에 남은 과거 결과도 확인했습니다. `Validation/MatchUI-final.xml`은 135 통과/0 실패, `CapturedUI.xml`은 8 통과/0 실패입니다. 이는 이전 실행의 증거이며 이번 최종 결과와 합산하지 않습니다.

## 발견과 수정

`PositionAnalysis`는 다음 Update에서 이전 국면을 무효화했지만, 이동·재시작·일시정지 직후 같은 프레임에 읽으면 이전 점수가 노출됐습니다. 비활성화 시에도 저장된 점수와 깊이를 지우지 않았습니다. `Score`와 `Depth`가 활성 상태, 세션 동일성, Revision, 설정 화면, 일시정지, 종료 여부를 확인한 뒤에만 결과를 제공하도록 수정했습니다. 비활성화와 분석 중단 시 저장값도 정리합니다. 탐색 깊이·예산·평가 방식은 유지합니다.

새 `MatchLifecycleTests`는 실제 MainScene에 진입하고 종료를 UnitySetUp/TearDown으로 관리합니다.

- 분석 결과를 제어한 완료 작업으로 주입해 재시작 및 비활성화 직후의 점수/깊이 무효화를 확인합니다.
- 이전 작업을 미완료 상태로 유지한 채 새 대국으로 전환하여 취소를 확인하고, 늦게 완료한 이전 점수가 새 결과를 덮지 않는지 검사합니다. 일시정지·재개 후에는 실제 엔진의 새 분석 완료를 기다립니다.
- 흑/2분/7초 설정으로 일시정지 후 Restart 버튼을 실행하여 색·시점·시간·증분 유지, 이전 AI 수 거부, New game 버튼 이후 시계 정지와 이동 차단을 확인합니다.
- 프레임 Update 전에 시계 기준 시각을 만료시켜 사람 및 AI 콜백이 시간초과를 우회하지 못하는지 검사합니다.

기존 화면 검사에는 설정 버튼의 화면 내 배치/최상위 클릭 대상, 설정 화면의 보드 입력 차단, 백·흑 양쪽 시점의 기물에 HUD가 간섭하지 않는지 검사를 추가했습니다.

첫 그래픽 실행은 자동 검사 140개가 모두 통과했지만, 캡처 검토에서 평가바가 일시정지 창 제목/버튼 위에 그려지는 문제를 발견했습니다. MatchUI의 Canvas 순서를 대국 중에는 기존 팝업 뒤(-1), 설정 화면에서는 앞(30)으로 변경했습니다. 팝업 앞뒤 순서와 Continue 버튼의 최상위 입력 대상 검사도 보강했습니다. 기존 씬·프리팹은 수정하지 않았습니다.

## 실행 결과

| 실행 | 결과 |
| --- | --- |
| 기준 전체 비그래픽 검사 | 134 통과, 0 실패, 화면 전용 2개 건너뜀 |
| 새 통합 검사 수정 전 | 2 통과, 2 실패: 재시작/일시정지 직후 이전 점수 노출 |
| 평가값 수정 후 전체 실제 그래픽 실행 | 140 통과, 0 실패, 0 건너뜀 (`final.xml`) |
| 팝업 겹침 수정 후 실제 그래픽 재검사 | 2 통과, 0 실패, 0 건너뜀 (`screens-final.xml`) |
| 최종 화면 캡처 | 30장 생성, 그중 설정 3장·잡기/시점 3장·일시정지 5장 직접 검토 |
| Windows x64 Mono 개발 빌드 | 성공, 오류 0, 경고 186, 약 1분 34초 |
| Windows 플레이어 실제 실행 | 1280×720, 사람 이동·AI 응수·잡힌 말·시계 표시 관찰, 응답 상태 정상 |

처음 샌드박스 실행은 라이선스를 찾지 못해 테스트 전에 종료했습니다(`baseline.log`). 정식 사용자 라이선스 환경에서 재실행했고 새 Library를 생성한 기준 결과는 `baseline-licensed.xml`/`.log`입니다. 수정 전 새 검사는 `lifecycle-before.xml`/`.log`에 기록했습니다.

전체 실행의 화면 전용 두 테스트는 이후 팝업 수정본으로 재실행했습니다. 따라서 고유 테스트는 140개이며 반복 실행을 더해 142개로 세지 않습니다. 수정 후 다섯 데스크톱 해상도의 일시정지 창에서 평가바가 제목/버튼을 가리지 않는 것을 직접 확인했습니다. 설정과 양쪽 시점/잡기는 1280×720, 800×600, 540×960에서 자동 검사하며, 일반/일시정지/Retry는 800×600, 1280×720, 1920×1080, 1024×768, 2560×1080에서 검사합니다. 최종 캡처는 `screens/`, 수정 전 캡처는 `screens-before-layer-fix/`에 있습니다. 캡처 30장 전체의 수동 검토를 의미하지 않습니다.

빌드 요약의 `Backend: Mono2x`, MainScene 단독 포함, 실제 `MonoBleedingEdge` 폴더와 Core/Engine/Runtime/Training DLL을 확인했습니다. Editor/테스트/NUnit DLL은 배포 폴더에 없습니다. 경고 186개는 해결된 것으로 간주하지 않습니다.

Windows 플레이어의 첫 숨김 실행 로그는 `player.log`입니다. 해당 검사용 프로세스만 종료한 뒤 computer-use로 다시 실행했습니다. 실제 창에서 사용자 입력을 감지하여 자동 클릭이나 대국 초기화를 하지 않았습니다. 화면에서 사람의 이동, `AI thinking...`에서 `Your Turn (White)`로 전환, 여러 수 진행과 양쪽 잡힌 말 표시를 관찰했습니다. 이 관찰을 자동 입력 성공으로 세지 않습니다. 자동 버튼/흑 선택/재시작/시간초과 검증은 위 Editor 실제 씬 검사 결과입니다. 실행 중 로그 사본은 `player-visible.log`이며 검사 시점에 오류/예외가 없고 기존 App UI 기본 설정 경고가 있습니다. 사용 중인 플레이어는 종료하지 않았습니다.

## 원본 반영

반영 대상은 `PositionAnalysis.cs`, `MatchUI.cs`, `EngineStatusLayoutTests.cs`, 신규 `MatchLifecycleTests.cs`와 `.meta`, `docs/match-ui.md`, 이 문서의 7개 파일입니다. 반영 직전에 원본 HEAD/브랜치와 초기 변경 파일 26개의 해시를 재확인하고 신규 대상 경로가 없는지도 확인했습니다. 기존 파일을 로컬 Validation에 백업한 뒤 7개 파일을 반영하고 SHA-256 일치를 확인했습니다. 나머지 원본 변경 파일 22개는 초기 해시와 일치합니다. 반영 결과는 `Validation/Stage7/handoff.json`에 기록했습니다.

검증 중 Unity가 작업 폴더의 `ProjectSettings.asset`에 추가한 `SENTIS_ANALYTICS_ENABLED`와 내용 차이 없는 줄바꿈 변경은 인계하지 않습니다. 씬·프리팹에는 변경이 없습니다. 테스트와 빌드는 작업 폴더에서 수행했으며 원본에서 다시 실행한 결과가 아닙니다.

## 재현 및 제한

각 Unity 프로세스가 종료된 뒤 다음 실행을 시작합니다. 실제 화면 검사는 `-batchmode`, `-nographics` 없이 실행합니다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.0.30f1/Editor/Unity.exe' -runTests -testPlatform EditMode -projectPath "$PWD" -testResults "$PWD/Validation/Stage7/final.xml" -logFile "$PWD/Validation/Stage7/final.log"
& 'C:/Program Files/Unity/Hub/Editor/6000.0.30f1/Editor/Unity.exe' -batchmode -nographics -executeMethod Stage0ValidationBuild.Run -projectPath "$PWD" -logFile "$PWD/Validation/Stage7/build.log"
```

빌드 출력은 기존 `Validation/Stage0/Windows64/Chess.exe` 및 `Validation/Stage0/build-summary.txt`입니다. 상세 결과와 화면은 Git에서 제외된 Validation 아래에 보관합니다. Windows Mono 개발 빌드와 명시한 해상도/대국 흐름에 한정한 검증이며, 장시간 대국·모든 DPI 배율·모바일/WebGL·엔진 강도 또는 모든 시간패 무승부 국면을 보증하지 않습니다. 이전 단계의 제한된 기물 부족 판정과 협력적 탐색 예산은 유지합니다. 커밋·push는 하지 않습니다.
