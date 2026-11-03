# COLOR MOVE — 5×5 구현 및 검증

기존 `Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity`를 열고 Play를 누르면 메인 메뉴에서 3×3/4×4/5×5를 선택할 수 있습니다. 각 30개, 총 90개입니다. 추가 씬이나 수동 Inspector 설정은 필요 없습니다. 5×5는 빨강·파랑·노랑·초록·보라 각 4개, 검정 2개, 빈칸 3개입니다. 기존 두 모드의 빈칸은 각각 2개로 유지했습니다.

Unity 6000.6.5f1, Unity CLI 1.0.0-beta.14, Unity Pipeline 0.9.1-exp.1 및 공식 Unity 플러그인의 unity-cli/ui-ugui 스킬로 작업했습니다. 씬/에셋은 실행 중인 Editor의 API로 생성·연결·저장했습니다.

## 구현 파일과 UI

새 C# 파일:

- `Editor/PuzzleFiveByFiveGenerator.cs`: 제한된 개발용 생성 및 평가, CSV 출력.
- `Editor/PuzzleFiveSceneBuilder.cs`: 기존 씬에 5×5 뷰, 목표 확대 패널, 메뉴 연결.
- `Runtime/PuzzleTargetZoom.cs`: 읽기 전용 목표 확대와 닫기.
- `Tests/EditMode/PuzzleFiveTests.cs`: 구성·규칙·동일 색상 매칭·30개 경로·난이도·인코딩·저장 호환성.
- `Tests/PlayMode/PuzzleFivePrototypeTests.cs`, `PuzzleFiveCampaignTests.cs`: 실제 씬과 UI·애니메이션·해금·재시작·이어하기·뒤로 가기.

수정 C# 파일:

- `Core/PuzzleBoard.cs`: 기존 색상 번호를 유지하면서 Purple=6을 끝에 추가.
- `Runtime/PuzzlePalette.cs`: 보라색과 P 표시.
- `Runtime/PuzzleStageData.cs`: 보라색 인벤토리 검증 및 생성/평가 메타데이터.
- `Runtime/PuzzleGameController.cs`: 빈칸 개수를 특정하지 않는 안내 문구.
- `Runtime/PuzzleMenuController.cs`: 목표 확대를 먼저 닫는 뒤로 가기 처리.
- `Editor/PuzzleMatchingHeuristic.cs`: 동일 색상 최소 매칭에 보라색 포함.
- 기존 `PuzzleMenuTests`, `PuzzleMenuPlayTests`, `PuzzleFourCampaignTests`: 구현된 5×5를 선택할 수 있다는 새 요구사항을 반영. 알 수 없는 모드와 잠긴 스테이지 거부 검사는 유지. `PuzzlePlayTests`의 기존 좌표 검사에도 0.01 UI 단위 부동소수점 허용 오차를 적용.

`PuzzleBoardView`, `PuzzleTileView`, `PuzzleCampaignController`, 진행/프로필/볼륨 저장 및 핵심 이동 규칙은 공통 시스템을 그대로 사용합니다. 셀과 색상별 개수는 스테이지의 시작/목표 배열에서 결정됩니다. 런타임에 생성기나 난이도 탐색을 실행하지 않습니다.

씬에 추가한 UI:

- `PuzzleCanvas/SafeArea/Content/PuzzleArea/CurrentBoard5`: 25셀, 22개의 2D Image 타일.
- `.../TargetArea/TargetGrid5`: 25셀 읽기 전용 목표. 배경 Button을 눌러 확대.
- `PuzzleCanvas/TargetZoom/ZoomSafeArea/ZoomContent`: 800×800 확대 목표와 BACK TO PUZZLE 버튼.
- 기존 메뉴의 5×5 카드 활성화. 기존 30개 스크롤 스테이지 버튼과 진행 컨트롤러를 재사용.
- 1080×1920 CanvasScaler 및 기존 Safe Area/균일 배율 시스템 유지. 본 보드 840×840, 셀 151.2×151.2, 간격 10. 확대는 입력 차단용 배경 Image를 사용하며 타일은 조작되지 않음.

데이터: `Data/FiveByFive.asset`, `Data/FiveByFive/Stage01.asset`~`Stage30.asset`, 기존 `Data/Modes.asset`의 5×5 연결. 기존 3×3/4×4 스테이지 데이터는 재생성하지 않았습니다.

추가 자동화 C#은 `AgentScripts/` 아래 `BuildFivePrototype`, `BuildFiveCampaign`, `FixFiveZoomHint`, `FivePreview`, `PlayFivePreview`, `VerifyFiveTouch`, `InspectFiveScene`, `InspectFiveLayout`, `OpenFiveTarget`, `CloseFiveTarget`, `ShowFiveSelection`, `InspectLiveFiveProgress`, `InspectPreviewPaths`, `OpenSavedPuzzleScene`입니다. Assets 밖에 두어 Editor의 임시 실행/진단만 담당하고 Player 빌드에는 들어가지 않습니다.

## 생성과 난이도

[Stages.csv](Artifacts/FiveByFive/Stages.csv)에 시작·목표, 생성 이동 수, 매칭 하한, 해결 경로 상한, 등급 및 제한 탐색 평가를 기록했습니다. [Generation.json](Artifacts/FiveByFive/Generation.json)에 실제 생성 결과와 자원 제한이 있습니다.

첫 단일 5×5 스테이지를 씬에 구성한 뒤 PlayMode 테스트 2/2를 통과시켰고, 그 스테이지를 유지해 30개로 확장했습니다. 첫 스테이지의 검증 범위는 [PrototypePlayModeTests.json](Artifacts/FiveByFive/PrototypePlayModeTests.json)에 보관했습니다.

| 구간 | 추정 등급 | 이동 하한 범위 |
| --- | --- | --- |
| 1~5 | 입문 | 12~16 |
| 6~10 | 쉬움 | 17~22 |
| 11~15 | 보통 | 23~27 |
| 16~20 | 어려움 | 29~33 |
| 21~25 | 매우 어려움 | 34~39 |
| 26~30 | 전문가 | 40~45 |

고정 시드로 목표부터 실제 유효 이동을 수행합니다. 각 경로는 방문 상태를 기록하므로 즉시 왕복 및 반복 상태가 없습니다. 25개 셀 전부를 보존하는 문자열 키를 사용합니다. 5×5를 64비트에 3비트씩 넣는 인코딩은 75비트가 필요해 충돌하므로 사용하지 않습니다. 최대 512번의 걷기, 경로당 180회, 후보 10,000개, 걷기 탐색 12초 제한입니다. 모든 후보는 실제 경로에서 나왔으며 선택 단계에서 중복 시작을 배제합니다.

색상별 현재 위치와 목표 위치 사이의 최소 일대일 배정 맨해튼 거리를 비트마스크 DP로 계산합니다. 색상 타일 비용 합을 Hc, 빈칸 매칭 비용을 He라고 할 때 H=max(Hc,He)를 저장합니다. 한 이동은 색상 타일 하나와 빈칸 하나를 각각 한 칸 움직이므로 H는 최단 거리의 하한입니다. 빈칸 비용을 색상 비용에 더하지 않아 이중 계산하지 않습니다. 목표에서의 실제 경로를 역으로 검증한 U는 상한입니다. 따라서 H ≤ 실제 최단 거리 ≤ U입니다. 생성 횟수를 최단 거리로 표시하지 않습니다.

배치 선택은 목표 하한을 상승시키면서 기존 선택과 다른 셀 수를 늘리고 같은 하한에서 짧은 증명 경로를 선호합니다. 선택된 모든 문제에는 추가로 깊이 6, 폭 24, 최대 1,024개 후속 상태/100ms의 제한 빔 탐색을 수행합니다. CSV의 probe_best_residual은 이 탐색 중 관측한 가장 작은 H이며 탐색 난항의 참고값입니다. 빔 탐색은 상태를 버리므로 최단 거리의 증명이 아닙니다. 전체 상태 BFS를 수행하지 않습니다.

등급은 설계한 진행 순서와 수치에 따른 추정값입니다. 하한 상승이나 짧은 탐색만으로 플레이어의 체감 난이도를 증명할 수 없습니다. 사람을 대상으로 한 난이도/소요 시간 측정은 수행하지 않았습니다. 알려진 해결 경로는 최적화되지 않을 수 있으며 CSV의 상한은 플레이어에게 요구하는 이동 수가 아닙니다. 게임에는 RATING/ESTIMATED DIFFICULTY로 표시합니다.

## 저장과 탐색

기존 저장 디렉터리 `Application.persistentDataPath/ColorMovePuzzle/`에서 3×3 `progress-v1.json`, 4×4 `progress-4x4-v1.json`을 유지하고 5×5 `progress-5x5-v1.json`만 추가합니다. 새 파일이 없으면 스테이지 1만 해금합니다. 기존 프로필 파일에 이미 있는 격자 크기/스테이지 필드로 Continue를 복원하며 볼륨 저장은 변경하지 않습니다. 테스트는 별도 임시 폴더를 사용합니다.

메인 → 해당 모드 선택 → 퍼즐 구조를 유지합니다. 확대 목표를 연 상태에서 뒤로 가면 확대만 닫히고, 다음 뒤로 가기는 선택 화면, 그 다음은 메인으로 돌아갑니다. 마지막 스테이지를 클리어하면 전체 완료 문구가 표시되고 다음 스테이지 버튼이 숨겨집니다.

## 테스트 및 확인

EditMode는 기존 항목을 포함해 66/66 통과했습니다. [EditModeTests.json](Artifacts/FiveByFive/EditModeTests.json)에 실제 결과가 있습니다. 시작/목표 구성과 전 경로, 중복, 단계별 하한, 같은 색상의 최적 매칭을 별도의 순열 완전 계산과 비교하는 검증을 포함합니다.

최종 전체 PlayMode는 **37/37 통과, 실패·스킵 0**, 142.30초입니다. [PlayModeTests.json](Artifacts/FiveByFive/PlayModeTests.json)에 실제 최종 결과를 보관했습니다. 기존 3×3 전체 캠페인, 4×4 대표 스테이지 및 저장, 메뉴·설정·뒤로 가기 회귀 테스트와 신규 5×5 테스트가 모두 포함됩니다. [Compilation.json](Artifacts/FiveByFive/Compilation.json)에서 컴파일 완료·오류 없음도 확인했습니다. 최종 Editor는 Play mode를 종료하고 저장된 메인 메뉴 씬을 연 상태이며, 테스트용 Run In Background 재정의는 원래 값으로 복원했습니다.

[AllStagesUIPlay.csv](Artifacts/FiveByFive/AllStagesUIPlay.csv)에 5×5 전체 30개를 실제 씬의 PuzzleTileView 포인터/드래그 이벤트로 연속 클리어한 결과를 기록했습니다. 내부 모델만 바꾸는 테스트가 아닙니다. 모든 이동에서 UI 타일, 애니메이션 완료, 최종 좌표, 카운트, 클리어와 다음 해금을 검사합니다. 1·15·30번은 기본 0.15초 애니메이션을 사용하고 나머지 27개는 해당 테스트 실행 중에만 리플렉션으로 0.001초로 줄였습니다. 종료 시 복원하며 저장된 씬의 기본 값은 0.15초입니다. UI 이벤트 자동화와 실제 하드웨어 조작은 구분해야 합니다.

첫 전체 PlayMode 실행에서는 36/37 통과했고, 좌표의 완전 일치 비교 하나가 미세한 부동소수점 오차로 실패했습니다. 좌표 검사에 0.01 UI 단위 허용 오차를 적용했습니다. 직후 재실행은 Unity Test Framework 내부 PlayModeRunTask 예외로 시작에 실패하여 취소했으며 결과에 포함하지 않습니다. [중단 로그](Artifacts/FiveByFive/TestRunnerInterrupted.log)를 보관하고 컴파일과 도메인 재로드 완료 후 다시 실행했습니다.

다음 완료 실행에서 신규 5×5 UI 전체 클리어는 통과했으나 기존 3×3 좌표 검사 한 곳에서도 동일한 완전 일치 오차가 발생했습니다. 기존 두 좌표 검사도 같은 허용 오차를 적용했습니다. 이동 규칙·카운트·애니메이션 잠금·raycast 검사는 유지했습니다. 각 완료 실행의 실패 기록도 별도 JSON으로 보관했습니다.

별도의 실제 Input System 가상 Touchscreen 검증도 [NativeTouch.json](Artifacts/FiveByFive/NativeTouch.json)에 통과 결과를 기록했습니다. 메뉴/스테이지 선택, 목표 확대와 닫기, 단일 경로 탭, 재시작, 1번의 해결 경로 44회 전체 네이티브 스와이프, 클리어·다음 단계, 5×5 Continue, 기존 두 저장 파일 불변을 확인했습니다. 테스트용 저장 폴더 외 사용자의 실제 진행 파일은 수정하지 않았습니다.

720×1280, 1080×1920, 1080×2400 실제 Game View에서 25셀/22타일/빈칸 3개, 정사각형 비율 및 보드/목표의 Screen.safeArea 내부 배치를 확인했습니다. [레이아웃 수치](Artifacts/FiveByFive/Layout-1080x1920.json), [플레이 화면](Assets/Artifacts/FiveByFive/Play-1080x1920.png), [확대 목표](Assets/Artifacts/FiveByFive/TargetZoom.png), [스테이지 선택](Assets/Artifacts/FiveByFive/StageSelection.png)을 참고하십시오. 실제 노치 기기의 Safe Area 검증은 수행하지 않았습니다.

[SavedScene.json](Artifacts/FiveByFive/SavedScene.json)에서 저장 완료, 90개 스테이지, 세 모드의 보드/목표 뷰, 누락 참조 0, EventSystem 1개, MeshRenderer/Rigidbody/Collider 0, 메인 메뉴 초기 활성화를 확인했습니다.

Android APK 빌드 및 실제 Android 기기 검증은 아직 수행하지 않았습니다. 가상 Touchscreen/Keyboard와 Unity Editor 검증은 Android 하드웨어 검증과 다릅니다. 광고, 인앱 결제, 별점, 힌트, 새 오디오 및 추가 격자는 구현하지 않았습니다.

Editor에서 직접 확인할 항목은 Play → 5×5 → 1번에서의 탭/방향 스와이프 감각, 목표를 눌렀을 때 확대 가독성, 재시작과 뒤로 가기, 클리어 후 다음 단계와 Continue입니다. 필수 수동 설정은 없습니다. 난이도 등급의 체감 검증과 Android 빌드·실기기 입력/노치 검증은 남아 있습니다.
