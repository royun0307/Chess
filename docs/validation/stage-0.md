# Stage 0 — 배포 복구 검증

검증일: 2026-09-09
Unity: 6000.0.30f1 (62b05ba0686a)

현재 수정본을 새 로컬 checkout에 반영하고, 기존 `Library` 없이 Unity를 시작해 검증했습니다.

| 항목 | 결과 |
| --- | --- |
| EditMode 테스트 | 7/7 통과 |
| Windows x64 Mono 개발 빌드 | 성공 |
| 빌드 오류 | 0 |
| 빌드 경고 | 186 |
| 초기 실행 | 20초 동안 응답 상태 유지, 로그상 예외 없음 |

빌드에는 `Assets/Scenes/MainScene.unity`만 포함됐고, 학습용 `ChessAgent`는 빌드 대상에서 제외된 `TrainingScene`에 분리되어 있음을 확인했습니다. `StateString.cs`에는 `UnityEditor` 참조가 없으며, `UIManager`의 Editor API 사용은 `UNITY_EDITOR` 조건부 컴파일 안에 있습니다.

경고에는 Sentis 셰이더 관련 메시지와 App UI Settings 기본값 생성 메시지가 포함됐습니다. 빌드는 성공했지만, 대국의 상세 입력과 화면 QA는 이 단계에서 수행하지 않았습니다.

로컬 상세 로그, XML 결과, 실행 파일, 검증 checkout은 저장소에 포함하지 않습니다. 모두 `Validation/`의 로컬 산출물입니다.
