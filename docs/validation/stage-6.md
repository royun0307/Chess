# 6단계: 실제 화면과 AI 수명주기 검증

검증일: 2026-09-14. 기준: `dev_game`, `d15e2132`.

## 시작점과 범위

새 작업 폴더는 기본 브랜치의 분리 HEAD였으며 5단계와 fast-forward 관계가 아니었습니다. 사용자의 `dev_game` 기준 진행 지시에 따라 깨끗한 작업 폴더의 분리 HEAD를 해당 브랜치의 `d15e2132`로 전환했습니다. 원본 브랜치와 이전 `8258` 작업 폴더는 이 전환으로 변경하지 않았습니다.

이번 범위는 Windows 화면 배치·가독성·입력 간섭 및 실행 중 AI의 재시작·비활성화·늦은 완료 검증입니다. 새 기능, WebGL/모바일 포팅, 탐색 강도 개선은 포함하지 않습니다.

## 발견과 수정

- 기존 Windows 플레이어의 1280×720 실제 화면에서 일시정지 버튼이 보이지 않았습니다. 기존 씬의 화면 중앙 기준 `(−300, 555)` 위치가 화면 밖으로 나가는 것이 원인입니다. `PauseUI.Init`에서 좌상단 12px 여백, 40×40px 버튼으로 배치합니다. 씬과 프리팹의 GUID 및 참조는 유지합니다.
- 상단 상태 문구는 Retry가 숨겨져도 오른쪽 90의 여백을 남겨 왼쪽으로 치우쳐 있었습니다. 오류일 때만 Retry 공간을 확보하고 정상 상태는 양쪽 12의 여백으로 가운데 정렬합니다.
- 실행 중 `EngineManager`를 비활성화하면 탐색은 취소됐지만, 다시 활성화했을 때 흑 차례가 멈췄습니다. `OnEnable`에서 기존 `EngineMove`의 세션·차례·중복 검사에 따라 새 탐색을 요청합니다.

## 테스트 설계

`EngineLifecycleTests`는 실제 MainScene과 작업 스레드에서 다음 네 상황을 검사합니다. 엔진의 시작·해제를 게이트로 제어하여 탐색 속도에 의존하지 않습니다.

1. 실행 중 재시작: 취소 토큰 전달, 작업 종료, 초기 보드/백 차례 복원.
2. 실행 중 비활성화: 취소 후 재활성화하면 응수 한 번만 적용.
3. 구형 엔진이 새 대국의 새 탐색 중 뒤늦게 합법 수 반환: 이전 수 폐기, 새 작업과 Revision 보존, 새 응수만 적용.
4. 비활성화한 구형 엔진의 늦은 예외: 대국 변경·Retry 오류 표시·예상 밖 Unity 로그 없음.

`EngineStatusLayoutTests`는 그래픽 장치를 사용한 Game view에서 다섯 해상도와 정상/일시정지/오류 상태를 캡처합니다. 화면 안의 상태바·버튼, 텍스트 넘침, 보드 64칸의 UI 입력 간섭, Pause/Retry 버튼의 UI raycast를 검사합니다. 이 검사는 `-nographics`에서는 명시적으로 건너뛰며 실제 화면 검증의 성공으로 세지 않습니다. 스크린샷은 사람이 별도로 검토해야 합니다.

테스트의 Game view 크기는 검증 중 임시로 추가하고 종료 시 기존 선택을 복원합니다. 화면 캡처와 로그는 Git에서 제외한 `Validation/Stage6/`에 보관합니다.

## 검증 결과

| 항목 | 결과 |
| --- | --- |
| 전체 비그래픽 EditMode 검사 | 128 통과, 실패 0, 화면 전용 1건 건너뜀 |
| 실제 그래픽 화면 검사 | 1/1 통과, 5개 해상도 × 3개 상태 |
| 화면 캡처 직접 검토 | 15장, 상태 문구·Retry 잘림/겹침 없음 |
| 보드 및 UI 입력 배치 | 64칸에서 UI 간섭 없음, Pause/Retry raycast 확인 |
| 최종 Windows x64 Mono 개발 빌드 | 성공, 오류 0, 경고 186, 약 12.5초 |

전체 회귀 결과는 `tests.xml`/`tests.log`, 실제 화면 결과는 `screens-retry.xml`/`screens-retry.log`입니다. 두 실행을 합쳐 129개 고유 테스트가 통과했으며, 비그래픽 실행의 건너뜀을 화면 검증 성공으로 간주하지 않았습니다. 화면 캡처는 `screens/<해상도>-turn.png`, `-paused.png`, `-retry.png`입니다.

첫 Windows 기준 빌드는 성공했습니다(오류 0, 경고 186). 첫 수명주기 테스트의 4개 실패는 테스트의 PlayMode 전환과 지역 변수 캡처 문제였으며, 재활성화 문제의 증거로 사용하지 않습니다. 테스트 진입을 UnitySetUp으로 분리한 후 재실행에서 재활성화 정지 실패를 확인했습니다. 상세 기록은 `lifecycle-before.*`, `lifecycle-baseline.*`입니다.

수명주기 기준 재실행은 2건 통과/2건 실패였으며, 재활성화 정지 외의 실패는 테스트 정리 시 null 접근이었습니다. 정리 가드를 보강한 최종 전체 검사에서 네 수명주기 사례가 모두 통과했습니다. 첫 화면 검사는 EditMode의 `WaitForEndOfFrame` 미지원 로그 때문에 실패했습니다(`screens.*`). 캡처를 요청한 뒤 실제 PNG의 갱신 시간을 확인하는 null 프레임 대기로 변경한 재실행이 위 최종 화면 결과입니다.

## 재현

프로젝트 루트에서 Unity 6000.0.30f1을 사용합니다. 매 실행의 프로세스 종료를 확인한 뒤 다음 실행을 시작합니다. 로컬 라이선스 사용을 위해 승인된 사용자 환경에서 실행했습니다.

```powershell
# 규칙·엔진·수명주기 전체 검사 (화면 검사는 명시적 건너뜀)
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -runTests -testPlatform EditMode -projectPath "$PWD" -testResults "$PWD\Validation\Stage6\tests.xml" -logFile "$PWD\Validation\Stage6\tests.log"

# 실제 렌더링 및 화면 캡처: batchmode/nographics를 사용하지 않음
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -runTests -testPlatform EditMode -testFilter EngineStatusLayoutTests -projectPath "$PWD" -testResults "$PWD\Validation\Stage6\screens-retry.xml" -logFile "$PWD\Validation\Stage6\screens-retry.log"

# Windows x64 Mono 개발 빌드
& 'C:\Program Files\Unity\Hub\Editor\6000.0.30f1\Editor\Unity.exe' -batchmode -nographics -executeMethod Stage0ValidationBuild.Run -projectPath "$PWD" -logFile "$PWD\Validation\Stage6\build.log"
```

빌드 출력은 기존 도구의 `Validation/Stage0/Windows64/Chess.exe`와 `Validation/Stage0/build-summary.txt`입니다. 이 경로의 기준 빌드는 최종 빌드로 갱신됩니다.

최종 빌드의 Mono2x 백엔드, MainScene 단독 포함, Core/Engine/Runtime/Training DLL과 Editor/테스트 DLL 부재를 확인했습니다. 기존 경고 186개는 해결된 것으로 간주하지 않습니다. 최종 Windows 플레이어 1280×720 화면에서도 수정된 일시정지 버튼, 실제 말 이동 및 `AI thinking...`/사람 차례 전환을 관찰했습니다. 이 플레이어의 실제 입력은 사용자 조작 중이었으며 자동 클릭 성공으로 세지 않습니다. Pause/Retry 클릭 대상과 Pause/Continue 동작은 별도의 그래픽 테스트에서 검증했습니다.

## 제한과 인계

시각 검증은 위 다섯 데스크톱 해상도에 한정하며 Windows DPI 배율 전 조합, 모바일 화면 및 장시간 대국을 의미하지 않습니다. 오류 상태는 테스트 엔진으로 재현했습니다. 구형 엔진을 강제로 종료하는 기능은 추가하지 않았으며, 늦은 완료·실패가 현재 대국에 영향을 주지 않는 것을 검증했습니다. 앞 단계의 탐색 예산·Quiescence·이력 전달 제한도 유지합니다.

원본 `C:/Users/ychul/GitHub/Chess`의 `dev_game`과 `d15e2132`, 깨끗한 상태 및 기존 네 파일의 해시를 확인한 뒤 코드 3개·테스트 2개와 메타데이터 2개·문서 2개, 총 9개 파일을 인계합니다. 인계 후 작업 폴더와 원본 파일의 SHA-256 일치를 확인합니다. 커밋·push는 수행하지 않습니다.

검증 중 Unity가 변경한 렌더링 asset/ProjectSettings의 대부분은 내용 차이 없는 줄바꿈 변경입니다. 최종 빌드가 작업 폴더의 Standalone 정의에 추가한 `SENTIS_ANALYTICS_ENABLED`는 자동 생성 변경이며, 이번 9개 인계 파일에 포함하지 않습니다. 원본 씬·프리팹·설정 파일과 기존 GUID는 보존합니다. 원본에서 테스트를 중복 실행한 결과는 아니며, 위 검증은 현재 작업 폴더에서 수행했습니다.
