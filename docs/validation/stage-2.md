# Stage 2 — 규칙 검증

검증일: 2026-09-10  
Unity: 6000.0.30f1 (62b05ba0686a)

## 시작점과 범위

새 작업 트리의 초기 HEAD는 `10c55cae`였습니다. 깨끗한 작업 상태와 로컬 완료 커밋을 확인하고, 이 작업 트리의 분리 HEAD만 `a51b18d4`로 이동했습니다. 그 부모인 `7eb9f5c1`도 포함됩니다. 원본 `dev_game` 브랜치와 기존 커밋은 변경하지 않았으며, 이번 변경은 커밋·푸시하지 않았습니다.

이번 범위는 특수 행마, 이력/반수 카운터, 보드 복사, perft 및 일반/학습 상태 전이 공통화입니다. 엔진 평가·기물 매핑·좌표 방향·Quiescence·탐색 말단 판정은 3단계에 남깁니다.

## 결과

| 항목 | 결과 |
| --- | --- |
| 수정 전 첫 테스트 실행 | 56개 중 49 통과, 7 실패 |
| 수정 후 전체 EditMode 실행 | 68/68 통과, 실패/건너뜀 0 |
| 기존 규칙/진행 회귀/실제 씬 통합 | 기존 19개 모두 통과 |
| 새 2단계 테스트 | 49개 통과 (매개변수별 테스트 케이스 기준) |
| Windows x64 Mono 개발 빌드 | 성공 (약 1분 31초) |
| 빌드 오류 / 경고 | 0 / 186 |

빌드는 `MainScene`만 포함하며, 요약의 `Backend: Mono2x`와 결과물의 `MonoBleedingEdge` 및 `Chess_Data/Managed/Assembly-CSharp.dll`을 확인했습니다. 테스트와 빌드 종료 후 각각 Unity 프로세스가 완전히 종료됐습니다. 경고는 기존 단계와 같은 수이며, 이를 경고가 해결됐다는 의미로 해석하지 않습니다.

첫 실행에서는 백/흑 × 양쪽 캐슬링의 잘못된 시작 랭크 4개, 백/흑 폰의 잘못된 시작 랭크 2개, 룩·폰 엔드게임 perft 1개가 실패했습니다. 이후 오류를 고치고 만료 표시·부적절한 앙파상·캐슬링 권리 관련 회귀 사례를 추가했습니다. 첫 실행 이후 추가된 12개는 수정 전 실행 결과에 포함되지 않습니다.

## 수정 내용

- `GameState.MakeMove`와 `MakeMoveForTraining`은 `ApplyMove`로 보드 실행, 반수 카운터, 비가역 수 이후 이력 초기화, 턴 변경을 공유합니다. 응수가 끝나면 상대의 앙파상 표시도 즉시 만료합니다. 일반 경로만 반복 문자열 기록과 종료 판정을 수행하며, 학습 경로의 기존 생략 최적화는 유지합니다.
- 폰 공격 판정을 앙파상 이동 후보와 분리했습니다. 이전 skip 칸에 같은 편 킹이 놓여도 자기 킹을 공격한다고 오판하지 않습니다. 폰의 2칸 이동에는 시작 랭크 조건을 추가했습니다.
- 앙파상 합법 판정은 올바른 출발/도착 랭크, 대각선 한 칸 이동, 빈 도착 칸, 상대 skip 표시, 실제 인접 상대 폰과 자기 킹의 안전을 확인합니다. 오래된 skip 칸에 상대 기물이 있으면 일반 잡기 후보를 생성합니다.
- 캐슬링 후보는 킹의 원래 시작 칸과 같은 색의 미이동 룩을 요구합니다. 반복 문자열에 쓰이는 캐슬링 권리도 해당 플레이어의 킹·룩 색을 확인합니다.
- `Board.Copy`는 기물별 깊은 복사와 독립된 skip 사전을 이미 구현하고 있어 변경하지 않았습니다. 모든 기물 타입/색/이동 플래그 및 양방향 변경 독립성을 테스트했습니다. `Position`은 읽기 전용 좌표이므로 공유해도 가변 상태가 공유되지 않습니다.

## 테스트 범위와 독립 기대값

`Assets/Tests/EditMode/Editor/Stage2RuleTests.cs`에 테스트용 FEN 파서와 perft가 있습니다. FEN 입력과 기대 노드 수는 생산 코드의 `StateString`이나 현재 엔진 출력에서 만들지 않았습니다. 반수/이력 내부 값 확인에는 테스트 내부에서만 reflection을 사용합니다.

- 앙파상: 한 번의 응수 후 만료(일반/학습), 양쪽 색 캡처, 수직 핀과 수평 발견 체크로 자기 킹이 노출되는 금지 수, 상대 킹에 발견 체크, 빈 목표 칸·실제 상대 폰 조건.
- 캐슬링: 양쪽 색/방향, 킹·룩 이동 이력, 룩 부재/다른 색, 경로 막힘, 시작 체크/통과 칸/도착 칸 공격, 잘못된 시작 칸. 퀸사이드 b열과 룩만 공격받는 경우의 허용, 왕/룩의 왕복 후 권리 소멸도 확인합니다.
- 승급: 양쪽 색의 전진/잡기에서 나이트·비숍·룩·퀸 네 선택과 새 기물의 색/이동 상태. 승급 체크메이트와 콜백 단일 적용은 기존 실제 씬 테스트까지 통과했습니다.
- 이력/카운터: 초기 국면을 포함한 실제 3회 반복, 차례/캐슬링 권리/합법 앙파상 유무의 구별, 폰 이동·잡기의 카운터 초기화, 99/100 반수 경계. 기존의 3회 반복/50수 자동 무승부 정책을 유지합니다.
- 학습: 일반 경로와 특수 행마 뒤 보드·턴·카운터 일치, 반복 기록 및 체크메이트 자동 판정 생략 유지.

perft는 무승부 판정에 따른 조기 종료 없이 합법 수 트리의 잎을 셉니다. 기대값 출처: [Chess Programming Wiki — Perft Results](https://www.chessprogramming.org/Perft_Results).

| 국면 | 깊이 | 기대값 = 실제값 |
| --- | --- | --- |
| 초기 배치 | 3 | 8,902 |
| Kiwipete (Position 2) | 3 | 97,862 |
| 룩·폰 엔드게임 (Position 3) | 4 | 43,238 |
| 승급·캐슬링 국면 (Position 4) | 3 | 9,467 |

룩·폰 국면은 수정 전 42,995였습니다. 로컬 `python-chess` 1.999 (`chess` 1.11.2)와 수순별로 대조해 `g2-g4, Kh4-g3` 이후 백의 합법 수가 14개 중 2개만 남는 문제로 좁혔습니다. 흑 f4 폰이 이전 g3 skip 칸의 흑 킹을 잡을 수 있다고 오판하여 백이 체크 상태라고 판정한 것이 원인입니다. 수정 후 해당 국면의 모든 루트별 깊이 4 노드 수가 독립 라이브러리와 일치합니다. 진단 도구와 의존성은 `Validation/Stage2/` 안에만 있습니다.

## 재현과 산출물

프로젝트 루트에서 다음을 실행합니다. 테스트 뒤에는 해당 Unity 프로세스의 종료를 확인하고 빌드를 시작해야 합니다.

```powershell
New-Item -ItemType Directory -Force Validation/Stage2 | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -runTests -testPlatform EditMode -testResults "$PWD\Validation\Stage2\after.xml" -logFile "$PWD\Validation\Stage2\after.log" -projectPath "$PWD"
```

빌드는 기존 `Assets/Editor/Stage0ValidationBuild.cs`의 `Run`을 사용합니다. 이 도구의 출력 위치는 `Validation/Stage0/Windows64/Chess.exe`, 요약은 `Validation/Stage0/build-summary.txt`로 고정되어 있습니다. 이번 빌드의 상세 로그는 `Validation/Stage2/build.log`에 저장합니다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -executeMethod Stage0ValidationBuild.Run -logFile "$PWD\Validation\Stage2\build.log" -projectPath "$PWD"
```

새 작업 트리에서 기존 `Library` 없이 시작했습니다. 샌드박스에서는 Unity 라이선스를 찾지 못했으므로 정식 권한 상승으로 실행했습니다. 수정 전/후 테스트 XML 및 로그는 각각 `Validation/Stage2/before.*`, `after.*`입니다. 이 폴더는 Git에서 제외되며 Unity 실행 파일의 셸 반환값만으로 성공을 판단하지 않고 XML·빌드 요약 및 프로세스 종료를 확인했습니다.

Unity가 자동 변경한 설정은 규칙 코드 변경과 별도로 보존합니다. 최종 의미 있는 자동 변경은 `Assets/Settings/UniversalRP.asset`의 셰이더 사전 필터/알파 출력 직렬화와 `Assets/UniversalRenderPipelineGlobalSettings.asset`의 런타임 설정 목록입니다. 테스트 중 비워졌던 `SENTIS_ANALYTICS_ENABLED` 정의와 빌드 중 잠시 바뀐 Graphics 설정은 Unity 종료 후 원래 내용으로 돌아왔습니다. `ProjectSettings`의 네 asset 파일은 최종 내용 차이 없이 줄바꿈 변화가 작업 상태에 표시됩니다. 최초 작업 트리는 깨끗했으며, 이 자동 변경을 수동 기능 수정으로 분류하지 않습니다.

## 남은 제한

perft는 위 네 국면과 명시된 깊이까지만 검증했습니다. 전체 가능한 체스 국면, 더 깊은 perft, 장시간 실제 대국 및 빌드 실행 화면 QA를 의미하지 않습니다. 기존 MainScene 통합 테스트는 `EnterPlayMode` 이후 실제 프레임을 기다리며 AI 예약 취소, 중복 요청, 일시정지/재개, 승급 대기 및 승급 체크메이트를 검사합니다. 별도 학습 장기 실행이나 모델 품질, 탐색 성능은 이번 검증에 포함하지 않습니다.
