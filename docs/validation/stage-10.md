# 10단계: 2D Animation 종료 버퍼 경고 제거

검증일: 2026-09-27. 기준: `dev_game`의 `0e49b50a`.

## 수정

9단계 비교 실험에서 종료 시 `GarbageCollector disposing of ComputeBuffer`가 발생한 원인은 2D Animation 10.1.4의 `GpuDeformationSystem` 공용 fallback buffer였다. 패키지의 비공개 `ClearFallbackBuffer`를 게임에서 호출하거나 패키지 캐시를 수정하지 않았다.

2D Animation 10.2.3은 이후 배치 API에서 변형 버퍼 해제를 처리하는 수정이 있으나, 이 프로젝트의 Unity 6000.0.30f1과는 `SpriteRendererDataAccessExtensions.IsSRPBatchingEnabled` API 요구 사항이 맞지 않아 컴파일할 수 없었다. 따라서 해당 업그레이드는 적용하지 않았다.

프로젝트의 C# 코드·씬·프리팹에는 `SpriteSkin`, `SpriteLibrary`, 2D IK, 타일맵 또는 기타 2D Animation 런타임 사용이 없음을 검색으로 확인했다. 체스 말과 보드는 Unity 기본 `SpriteRenderer`를 사용한다. 이에 따라 사용하지 않는 `com.unity.feature.2d` 묶음을 `Packages/manifest.json`에서 제거하고 Unity가 lockfile에서 그 전이 의존성(2D Animation 포함)을 제거하도록 했다. 기본 SpriteRenderer 및 URP 2D 렌더링은 유지된다.

모델·ML-Agents·Sentis 설정은 변경하지 않았다. Unity가 실행 중 생성한 렌더링/프로젝트 설정 변경도 전달 대상에서 제외한다.

## 검증

Unity 6000.0.30f1로 패키지를 다시 해석한 뒤 전체 EditMode 테스트를 실행했다. 결과는 전체 140개 중 138개 통과, 실패 0, 기존 무시 2개이며 실행 시간은 11.446초다. 결과 파일은 `Validation/Stage10/regression.xml`이다.

일반 `Stage0ValidationBuild.Run`으로 Windows x64 Mono 개발 빌드를 만들었다. 결과는 오류 0, 기존 경고 186, 84.849초, 164,773,834바이트다. 경고 수는 8·9단계에서 분류한 Sentis/App UI 경고이며 이번 종료 경고와 별개다. 출력은 `Validation/Stage0/Windows64/Chess.exe`이고 빌드 로그는 `Validation/Stage10/build.log`이다.

새 일반 빌드를 Direct3D 11 그래픽 장치에서 실제 실행하고 창을 정상 종료했다. `Validation/Stage10/player-normal.log`에서 `GarbageCollector disposing of ComputeBuffer`는 0회, `Exception:` 또는 `Error:` 일치는 0회다. 로그에는 기존 App UI 기본 설정 경고가 1회 남으며, 빌드 경고 분류와 동일한 별도 항목이다. 빌드 산출물의 관리 DLL에도 `Unity.2D.Animation.Runtime`과 검증용 `Chess.Validation.Player`는 포함되지 않음을 확인했다.

## 범위와 제한

이 변경은 현재 프로젝트에서 사용되지 않는 2D 기능 묶음을 제거한다. 향후 Sprite Skin, 2D IK, Sprite Library, 타일맵, Sprite Shape, PSD/Aseprite 가져오기 또는 Pixel Perfect Camera가 필요해지면 해당 패키지를 다시 추가하고, Unity 6000.0.30f1과 호환되는 버전 및 종료 동작을 별도로 검증해야 한다.

## 후속 반영 및 최종 대국 검사

2026-09-28 사용자 요청으로 수정 커밋 `43890870`을 원본 `dev_game`에 fast-forward 반영했다. 이후 동일 수정 기반에서 Stage8 검증용 Windows Mono 빌드를 생성했다(오류 0, 기존 경고 186, 18.380초).

자동 플레이 검사는 Passed이며 전체 66.665초 중 연속 대국 60.001초, 사람 측 53수, 종료·재시작 3회를 완료했다. 흑 선공, 일시정지 시계, 설정 유지 재시작, 다섯 해상도에서 승급 체크메이트·재시작, 승급 중 시간초과 무승부, AI와 분석 및 엔진 비활성화/재활성화 검사를 통과했다. 오류 목록은 비어 있고 종료 로그의 ComputeBuffer 경고도 0회다.

증거는 `Validation/Stage10/final-play/report.json`, `final-play.log`, `build-play.log`에 있다. 검증용 플레이어의 숨김 실행 및 UI 이벤트 자동 호출 결과이며, OS 마우스 조작 또는 화면 캡처를 통한 시각 검증으로 세지 않는다. 위 일반 빌드의 정상 종료 검사와 구분한다.
