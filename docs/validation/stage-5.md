# 5단계: 시간 제한 탐색과 화면 응답성

검증일: 2026-09-13. 4단계 기준 커밋: `a6e5c5ec`.

## 변경

- 보드 복사본을 작업 스레드에서 탐색하고 메인 스레드에서 결과 검증·적용.
- 반복 심화, 기본 1초/깊이 6 제한, 완료 깊이·노드 수·경과 시간 기록.
- 시간 소진 시 마지막 완료 결과 또는 합법 수 fallback. 취소 시 결과 폐기.
- 재시작·일시정지·컴포넌트 비활성화 시 취소 및 작업 자원 정리.
- 화면 상단 차례/진행 상태와 오류 시 Retry 버튼. 기존 씬·프리팹 변경 없음.

## 검증 범위

- 작업 폴더 전체 테스트: **124/124 통과**, 실패 0.
- GitHub Desktop 원본 `C:/Users/ychul/GitHub/Chess`, `dev_game` 반영 후: **124/124 통과**, 실패 0 (`Validation/Stage5/handoff.xml`).
- Windows x64 Mono 개발 빌드: **성공**, 오류 0, 경고 186, 소요 약 19.4초.
- 초기 배치 30ms 예산 검사: 완료 깊이 0, 8노드, 약 32.7ms. 합법 fallback 반환을 확인한 단일 실행 수치이며 일반 성능 벤치마크는 아닙니다.

기존 117개 테스트에 예산·취소·동기 결과 호환성 테스트와 실제 MainScene 작업 스레드/프레임 진행/일시정지 취소/오류 후 버튼 재시도 테스트를 추가했습니다.

결과 파일: `Validation/Stage5/tests-complete.xml`, `Validation/Stage5/tests-complete.log`.
Windows 개발 빌드 로그: `Validation/Stage5/build.log`.
빌드 요약 및 실행 파일: 기존 `Validation/Stage0/` 출력 경로.

시간 제한은 협력적이며 엄격한 실시간 보장이 아닙니다. 구형 IChessEngine 구현은 실행 중 강제 취소할 수 없지만 결과 적용은 차단합니다. Windows Mono가 대상이며 WebGL 및 모바일 성능·다양한 화면 비율의 시각 검증은 별도입니다.

초기 123개 검사는 통과했습니다. 이후 추가한 오류/재시도 검사에서 Unity Editor Error Pause가 UI 갱신을 멈추는 현상을 확인했습니다. 복구 가능한 AI 오류를 예외 로그 대신 상세 경고로 기록하도록 수정했습니다. 실패 기록은 `tests-final.xml`, `tests-verified.xml`, 원인 확인은 `retry-debug.xml`에 남겼습니다.
