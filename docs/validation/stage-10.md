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

커밋과 push는 수행하지 않았다.
