# 8단계: Windows 실사용 최종 검증

검증일: 2026-09-23~24. 기준: 원본 `dev_game`의 `598f6504`.

## 범위와 실행 방식

원본은 변경 없는 상태로 시작했습니다. 이전 `a612/Chess` 작업 폴더의 7단계 구현과 캐시를 재사용하며 원본 파일의 초기 SHA-256을 `Validation/Stage8/source-manifest.json`에 저장했습니다. 이번 범위는 Windows 플레이어의 대국 반복·승급·시간초과·재시작·작은 화면과 기존 경고의 영향 분류입니다. 신규 게임 기능이나 포팅은 포함하지 않습니다.

`Stage8ValidationBuild.Run`은 MainScene의 임시 복사본에 별도 `Chess.Validation.Player` 도구를 연결한 Windows Mono 개발 빌드를 `Validation/Stage8/Windows64`에 생성합니다. 원본 MainScene은 저장하지 않고 임시 씬은 빌드 후 제거합니다. `STAGE8_PLAYER_VALIDATION`을 빌드에만 전달하고 프로젝트의 전역 define은 바꾸지 않습니다. 도구 assembly는 Editor 또는 검증 define에서만 컴파일됩니다. 일반 플레이어의 assembly 및 초기화 목록에서 제외 여부도 확인합니다.

검증 전용 플레이어를 실행하면 실제 GameManager/EngineManager/UI와 그래픽 장치를 사용해 자동 검사하고 실행 파일 옆 `Reports/`에 report.json 및 PNG를 남깁니다. 기본 연속 실행은 600초이며 `-stage8-output <절대경로> -stage8-seconds 600`으로 경로와 시간을 지정할 수도 있습니다. UI 버튼은 실제 raycast 최상위 대상과 화면 경계를 검사한 후 pointerClick 핸들러를 실행합니다. 이는 OS 마우스 입력 자동화와 구분합니다. 승급 검사는 명시적인 테스트 국면을 주입하고, 연속 실행은 초기 배치에서 실제 엔진 응수와 고정 시드의 합법적인 사람 측 수로 진행합니다. 자동 실행은 백그라운드에서도 진행하도록 설정합니다.

준비 과정에서 독립 assembly의 자동 초기화가 누락된 빌드가 있었습니다(`build-before.log`, `build-bootstrap.log`, `build-linked.log`). 최종 도구는 임시 씬에 명시적으로 연결하여 이 의존성을 없앴습니다. 첫 씬 빌드 후 비어 있는 Editor 씬 구성을 복원하는 정리 오류도 수정했습니다(`build-scene.log`). 숨김 실행의 검은 PNG는 유효한 화면 증거로 세지 않았습니다(`player-scenes/`). 이후 computer-use로 실제 창을 띄운 실행부터 화면 증거로 사용합니다.

## 발견과 수정

실제 Windows 검사에서 기존 일시정지 제목 `Restart Game?`가 TMP의 지정된 글자 영역을 넘는 것으로 판정됐습니다(`player-visible-before/report.json`). 캡처상 제목 전체는 보였으므로 이를 잘린 글자가 있었다는 증거로 과장하지 않습니다. 기존 팝업의 50px 글자 영역에 최대 55px 폰트를 사용하고 있었고 결과 사유는 더 길어질 수 있어, 공통 BaseUI 초기화에서 기존 크기를 상한으로 글자 자동 맞춤을 적용했습니다.

또한 고정 550px 폭의 일시정지/결과 창이 작은 창에서도 화면 안에 들어오도록 BaseUI가 화면 크기에 맞춰 축소합니다. 정상 크기에서는 원래 크기를 유지합니다. 씬·프리팹·GUID는 변경하지 않습니다. 검증 도구는 1280×720, 800×600, 640×480, 540×960, 1920×1080에서 실제 창 크기를 바꾸고 일시정지·백/흑 승급·체크메이트 결과·재시작을 확인합니다.

첫 축소 구현과 검사에는 작은 상위 RectTransform만 기준으로 삼은 공백이 있었습니다. 캡처에서 540px 창의 실제 배경 패널이 화면 밖으로 나가는 것을 확인한 뒤 검증 도구가 모든 실제 Graphic의 경계를 검사하도록 보강했습니다. 배경 크기만 반영한 중간 수정본에서도 640×480의 승자 문구 영역이 오른쪽으로 약 12px 나가는 실패를 확인했습니다(`player-text-bounds/report.json`). 최종 축소 기준은 자식 문구까지 포함한 전체 영역입니다.

9월 23일의 전체 회귀 140개 통과 결과는 `regression.xml`에 있으나 마지막 패널 수정 전입니다. 당시 연속 실행은 report가 Running인 채 360초/사람 측 316수/18회 종료·재시작까지만 남아 있어 완료로 세지 않습니다(`player-incomplete-sep23/`). 자동 승인 검토의 사용량 한도로 중단 명령이 실행되지 않았으며 9월 24일 재개 시 해당 프로세스는 없었습니다.

## 빌드 경고 분류

7단계 전체 빌드 로그의 186개 경고를 다음과 같이 분류했습니다.

| 분류 | 수 | 영향과 남은 제한 |
| --- | ---: | --- |
| Sentis에서 지원하지 않는 연산 커널 조합 | 160 | 패키지 추론 셰이더 변형의 경고. 현재 MainScene의 체스 탐색은 C# SimpleChessEngine이며 해당 추론 경로를 사용하지 않음 |
| Sentis 부호 있는/없는 정수 변환 | 15 | 추론 셰이더 경고. 학습/추론 정확성까지 검증한 것은 아님 |
| Sentis 정수 나머지·나눗셈 성능 | 9 | 셰이더 성능 경고. 현 대국 기능의 빌드 오류는 아님 |
| Sentis 반복문 변수 이름 중복 | 1 | 패키지 셰이더 경고. 이번에 패키지 코드를 수정하지 않음 |
| App UI 설정이 저장 자산이 아님 | 1 | 사전 로드에서 제외하고 기본 설정 사용. 체스 UI는 Unity UI/TextMeshPro이며 실행 결과로 영향 확인 |

Training assembly와 ML-Agents/Sentis 패키지는 기존 구조를 유지합니다. 위 분류는 경고를 해결했다거나 학습 기능 전체가 안전하다는 뜻이 아닙니다.

## 결과와 제한

| 검사 | 결과 | 증거 (`Validation/Stage8/` 기준) |
| --- | --- | --- |
| 9월 24일 전체 EditMode 회귀 | 140개 통과, 실패·누락 0. 마지막 자식 영역 축소 수정 전 | `regression-final.xml` |
| 마지막 수정 후 실제 화면 검사 | EngineStatusLayoutTests 2개 통과, 실패·누락 0 | `screens-final.xml` |
| 최종 검증 전용 Windows 빌드 | Mono, 오류 0, 기존 경고 186, 13.553초 | `build-summary.txt`, `build-content-bounds.log` |
| 최종 일반 Windows 빌드 | Mono, 오류 0, 기존 경고 186, 19.719초 | `build-normal-summary.txt`, `build-normal.log` |
| 일반 플레이어에서 검증 코드 제외 | Chess.Core/Engine/Runtime/Training만 존재. Validation/Tests/nunit/UnityEditor DLL과 검증 초기화 없음. 임시 씬 정리 확인 | `normal-assembly-check.json` |
| 최종 Windows 연속 실행 | Passed. 연속 대국 600.006초, 전체 606.624초, 사람 측 525수, 대국 종료·재시작 31회, 검사 오류 0 | `player-final/report.json`, `player-final/Player.log`, PNG 28장 |
| 일반 실행 파일 직접 확인 | 시작 설정 화면 표시 확인. 실제 마우스 클릭 검사는 미완료 | `player-normal.log`, 작업 대화의 computer-use 화면 |

빌드 요약의 Options 문자열에는 Unity가 중복된 enum 값을 문자열로 변환하면서 `Il2CPP`도 표시하지만, 설정 조회 결과는 `Mono2x`이고 출력에 `MonoBleedingEdge` 및 관리 DLL이 존재합니다. 이 검증을 IL2CPP 빌드 성공으로 해석하지 않습니다.

자동 검사는 다섯 해상도에서 모든 팝업 Graphic의 화면 경계, TMP 글자 넘침, 사용 버튼의 최상위 raycast 대상을 확인합니다. 실제 PNG도 540×960 일시정지/흑 결과, 640×480 백 결과, 1920×1080 흑 승급을 열어 배경·문구·버튼을 확인했습니다. 승급 체크메이트는 백과 흑 양쪽, 승급 중 시간초과는 상대가 왕만 가진 무승부 국면입니다.

연속 실행 전후 강제 GC 뒤 관리 힙은 6,447,104 → 6,811,648바이트(약 6.15 → 6.50 MiB)였습니다. 사람 측 525수는 엔진 응수까지 합친 총 반수 수치가 아닙니다. 30초마다 엔진을 비활성화했다가 다음 프레임에 활성화하며 취소와 재개도 검사했습니다.

일반 플레이어를 computer-use로 실행해 시작 화면을 확인했습니다. 흑 선택을 클릭하려던 시점에 사용자 입력 감지가 보고되어 화면을 다시 확인했고, 다음 입력에서는 `foreground window did not report a process id`가 반환됐습니다. 대상 창을 다시 찾았으나 창과 프로세스가 모두 종료되어 실제 클릭 성공으로 세지 않습니다. 시작·흑 선공·일시정지·재시작·시간초과는 검증 전용 플레이어의 자동 검사 결과이며 OS 마우스로 검증한 결과가 아닙니다.

두 최종 플레이어의 종료 로그에는 `GarbageCollector disposing of ComputeBuffer` 경고가 각각 1회 남았습니다. 실행 중 수집한 오류 목록은 비어 있지만, 종료 시 버퍼 해제 경고까지 해결했다고 주장하지 않습니다. 해당 로그에는 원인 스택이 없어 이번에는 발생 위치를 단정하지 않았습니다. 이 경고의 원인 확인과 일반 플레이어의 OS 입력 검사는 후속 확인 사항입니다.

검증용 플레이어가 실행되는 동안 같은 컴퓨터에서 Editor 화면 검사와 일반 빌드를 수행했으므로 단독 성능 벤치마크가 아닙니다. 10분 연속 실행은 수시간·수일 내구성 보증을 대신하지 않으며, GC 관리 힙 크기만으로 메모리 누수 부재를 판정하지 않습니다. 관찰된 Screen.dpi는 96입니다. Windows DPI 설정은 변경하지 않았으며 실제 OS 배율 전 조합과 다른 컴퓨터 검증은 별도입니다.

## 재현

Unity Editor 프로세스가 끝난 뒤 다음 빌드를 실행합니다. 실제 플레이어 검사는 창이 보이는 상태에서 수행합니다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.0.30f1/Editor/Unity.exe' -batchmode -nographics -executeMethod Stage8ValidationBuild.Run -projectPath "$PWD" -logFile "$PWD/Validation/Stage8/build.log"
& "$PWD/Validation/Stage8/Windows64/Chess.exe" -stage8-seconds 600
```

일반 배포 내용 검사는 기존 `Stage0ValidationBuild.Run`으로 별도 빌드합니다. 실행 파일은 `Validation/Stage0/Windows64/Chess.exe`입니다. 상세 로그·JSON·PNG와 실행 파일은 Git에서 제외된 로컬 Validation 폴더에 보관합니다.

## 원본 반영

원본 `dev_game`의 기준 HEAD와 전체 추적 파일 SHA-256이 시작 시점과 같은지 확인한 뒤, `BaseUI.cs`, `Stage8ValidationBuild.cs`와 meta, `Assets/Tests/Player.meta`, 검증 assembly와 실행 도구의 각 파일·meta, 이 문서의 9개 파일만 반영합니다. 기존 BaseUI는 로컬 Validation에 백업하고 복사 후 해시 일치 및 나머지 추적 파일 불변을 확인합니다. 인계 기록은 `Validation/Stage8/handoff.json`입니다. Unity가 작업 폴더에서 변경한 프로젝트 설정·렌더링 설정과 줄바꿈 차이는 인계하지 않습니다. 테스트와 빌드는 작업 폴더에서 수행했습니다.

커밋·push는 이번 요청에 포함하지 않습니다.
