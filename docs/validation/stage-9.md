# 9단계: Windows 직접 조작과 종료 경고 원인 확인

검증일: 2026-09-26. 원본 기준: `dev_game`의 `fb1220a4`.

## 실제 Windows 조작

8단계의 일반 Mono 개발 빌드 `Validation/Stage0/Windows64/Chess.exe`를 사용했습니다. 검증용 씬이나 이벤트 핸들러 직접 호출 없이 computer-use의 실제 마우스 입력으로 검사했습니다. 창의 클라이언트 영역은 1280×720입니다.

첫 실행에서는 사용자 입력으로 백 대국이 시작된 것을 감지해 조작을 멈췄습니다. 사용자가 검사용 조작을 허용한 뒤 기존 창이 종료된 상태임을 확인하고 다시 실행했습니다. 이후 다음 항목을 직접 확인했습니다.

| 항목 | 관찰 결과 |
| --- | --- |
| 흑 선택과 시작 | BLACK 선택, 흑이 아래쪽에 배치되고 백 AI가 먼저 폰을 이동 |
| 사람 이동과 AI 응수 | 흑 e7 폰을 선택하면 e6/e5 표시. e5로 이동 후 AI가 나이트를 이동하고 흑 차례로 전환 |
| 일시정지 | Paused 표시. 두 번의 관찰에서 백 05:04, 흑 04:41로 시계 유지 |
| 계속하기 | 팝업이 닫히고 흑 차례 복귀. 이후 흑 시계가 04:35로 진행 |
| 일시정지 창 재시작 | 초기 배치와 양쪽 05:00 복원, 흑 선택 유지, 백 AI 탐색 시작 |
| 새 대국 설정 | New game으로 설정 표시. 시간 감소 버튼 4회로 5분→1분, WHITE로 변경 |
| 시간초과 | 사람의 수 없이 백 00:00에서 WINNER Black / Timeout 결과 표시 |
| 결과 창 재시작 | 초기 배치, 양쪽 01:00, Your turn (White) 복원 |
| 결과 창 종료 | 재시작 후 두 번째 시간초과에서 EXIT 클릭, 이후 창과 프로세스 종료 확인 |

직접 화면 증거는 작업 대화의 computer-use 캡처이며 일반 플레이어 로그는 `Validation/Stage9/player-normal.log`입니다. 오류/예외는 검색되지 않았고 기존 종료 버퍼 경고는 1회 재현됐습니다. 8단계의 자동 승급 검사와 이번 OS 마우스 검사는 구분하며, 이번에 실제 입력으로 승급까지 재현한 것은 아닙니다.

## 종료 경고의 비교 실험

설치된 `com.unity.2d.animation`은 10.1.4입니다. 로컬 소스 `Runtime/BatchedDeformation/GpuDeformationSystem.cs`의 `CreateFallbackBuffer`는 AfterSceneLoad 초기화에서 `s_FallbackBuffer`를 생성합니다. 해제는 `ClearFallbackBuffer`에 있고 GPU 변형 시스템의 `Cleanup`에서 호출합니다. 따라서 체스 코드가 직접 ComputeBuffer를 만들지 않아도 패키지 초기화만으로 버퍼가 존재할 수 있습니다.

`Stage9DiagnosticBuild.Run`은 MainScene을 저장하지 않고 임시 씬 복사본에 `Stage9BufferProbe`를 연결한 별도 실행 파일을 생성합니다. 기존 `Chess.Validation.Player` assembly와 검증 define을 사용하므로 일반 플레이어에는 진단 코드가 포함되지 않습니다. 진단 실행은 3초 뒤 버퍼 상태를 확인하고 종료합니다. 동일한 실행 파일을 다음 두 방식으로 실행했습니다.

| 실행 | 변경한 조건 | 종료 결과 |
| --- | --- | --- |
| baseline | 패키지 버퍼를 그대로 둠 | 버퍼 유효, count=64/stride=64. 기존 ComputeBuffer 경고 1회 |
| release | 패키지의 ClearFallbackBuffer만 호출 | 버퍼 해제·필드 null 확인. 같은 경고 0회 |

증거는 `Validation/Stage9/probe-baseline.log`, `probe-release.log`, `probe-comparison.json`입니다. 둘 다 probe passed 이후 정상 종료 정리 로그가 있고 예외는 없습니다. 이 비교로 관찰된 경고의 원인을 2D Animation의 공용 fallback 버퍼 해제 누락으로 좁혔습니다. 버퍼 데이터 용량은 64×64=4,096바이트이며 드라이버/객체 부대 비용이나 전체 메모리 사용량을 뜻하지 않습니다. 장기 누적 누수 여부를 검증한 실험도 아닙니다.

비공개 메서드를 호출하는 reflection은 진단 도구에만 있습니다. 일반 게임에 패키지 내부 구현 의존성을 추가하지 않았으며, 일반 빌드의 경고 자체를 수정한 단계는 아닙니다. 패키지 업데이트 또는 패키지 측 생명주기 수정은 별도 수정·호환성 검증이 필요합니다. [공식 2D Animation 변경 기록](https://docs.unity3d.com/Packages/com.unity.2d.animation@10.2/changelog/CHANGELOG.html)도 확인했지만, 이번 경고에 해당하는 수정 버전을 변경 기록만으로 확정하지 않았습니다.

## 검증과 재현

첫 진단 빌드는 Windows Mono, 오류 0, 기존 경고 186, 23.018초였으며 이 실행 파일로 비교 실험을 했습니다. 이후 빌더의 출력 폴더 생성 경로를 Stage9로 바로잡고 최종 빌드도 성공했습니다(오류 0, 경고 186, 8.854초). `Validation/Stage9/build-initial-summary.txt`, `build-summary.txt`, `build.log`, `build-final.log`에 기록했습니다. 임시 씬이 제거된 것도 확인했습니다. 일반 플레이어와 별도 경로에 빌드하며, 진단 플레이어 로그도 `-logFile`로 분리합니다. 숨김 실행은 경고 비교에만 사용하며 화면 증거로 사용하지 않습니다.

전체 EditMode 회귀는 140개 통과, 실패·누락 0입니다(`regression.xml`, 테스트 실행 11.111초). 일반 플레이어 컴파일에서 Editor/Tests 코드가 제외되는 검사도 통과했습니다. 최종 수정은 빌더의 출력 폴더 경로 한 줄이며 이후 진단 빌드로 확인했습니다. 실제 조작에 사용한 일반 플레이어는 8단계 빌드이고 이번 단계에서 일반 게임 코드는 바꾸지 않았습니다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.0.30f1/Editor/Unity.exe' -batchmode -nographics -executeMethod Stage9DiagnosticBuild.Run -projectPath "$PWD" -logFile "$PWD/Validation/Stage9/build.log"
& "$PWD/Validation/Stage9/Windows64/Chess.exe" -logFile "$PWD/Validation/Stage9/probe-baseline.log"
& "$PWD/Validation/Stage9/Windows64/Chess.exe" -stage9-release-buffer -logFile "$PWD/Validation/Stage9/probe-release.log"
```

각 명령은 앞선 프로세스가 종료된 후 실행합니다. 그래픽 장치가 필요한 비교이므로 플레이어에 `-nographics`를 넣지 않습니다.

## 원본 반영

원본 HEAD와 변경 없는 작업 상태, 시작 시 저장한 전체 추적 파일 SHA-256을 확인한 뒤 아래 신규 5개 파일만 반영합니다. 복사 후 해시 일치와 기존 추적 파일 불변을 확인하고 `Validation/Stage9/handoff.json`에 기록합니다. 원본에서 테스트를 재실행한 것은 아니며 Unity가 검증 작업 폴더에서 변경한 설정은 인계하지 않습니다.

이번 변경은 진단 빌더와 meta, 버퍼 진단 도구와 meta, 이 문서입니다. 패키지·씬·프리팹·게임 코드는 변경하지 않습니다. 커밋과 push는 수행하지 않습니다.
