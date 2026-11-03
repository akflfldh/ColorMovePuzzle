# ColorMovePuzzle — 퍼즐 진행 자동 저장과 Continue

## 저장 방식

기존 `progress-v1.json`, `progress-4x4-v1.json`, `progress-5x5-v1.json`과 `menu-profile-v1.json`의 스키마·경로를 유지합니다. 기존 클리어·해금·설정 데이터를 마이그레이션하거나 삭제하지 않습니다. 구버전 사용자에게 새 파일이 없으면 Continue는 기존처럼 마지막 스테이지의 최초 배치를 엽니다.

각 모드의 진행 파일 이름 뒤에 `.session-v1.json`을 붙인 별도 파일에 현재 퍼즐을 저장합니다. 예를 들어 5×5는 `progress-5x5-v1.json.session-v1.json`입니다. 실제 경로는 `Application.persistentDataPath/ColorMovePuzzle/` 아래입니다. 각 파일에는 버전, 격자 크기, 스테이지 번호, 시작·목표 배치의 SHA-256 식별값, 현재 셀 배열, 이동 횟수, 진행 여부, 체크섬이 있습니다. `Empty=0`인 셀이 빈칸 위치이므로 빈칸을 중복 저장하지 않습니다. 같은 색 타일과 빈칸의 개별 ID는 없습니다.

`PuzzleGameController.TryMove`가 합법적인 이동을 보드에 적용하고 이동 횟수를 증가시킨 직후 `StateCommitted` 이벤트를 발행합니다. `PuzzleCampaignController`가 이를 동기 저장한 다음 0.15초의 기존 애니메이션을 시작합니다. 저장할 때 애니메이션 중간 좌표를 읽지 않습니다. 이동 중 추가 입력은 기존과 같이 차단합니다. 잘못된 입력은 저장을 발생시키지 않습니다.

저장은 UTF-8 임시 파일 작성 → `Flush(true)` → 기존 파일 교체 순서입니다. 기존 파일은 `.bak`으로 남깁니다. File.Replace를 지원하지 않는 플랫폼에는 기존 저장 코드와 같은 백업 후 복사 대체 경로를 둡니다. 복사 대체 경로는 완전한 원자적 교체를 보장하지 않으므로 손상 검사와 백업 복구를 함께 사용합니다. 저장 실패 시 경고를 남기고 dirty 상태를 유지해 다음 이동·화면 전환·Pause·Focus 상실·Quit에서 재시도합니다. 저장에 성공했다면 보조 콜백은 아무 파일도 쓰지 않습니다. Quit 콜백을 받지 않아도 이미 확정된 이동은 저장되어 있습니다.

## 복원과 화면 이동

- Continue: 프로필의 마지막 모드·스테이지를 선택하고 일치하는 임시 기록을 복원합니다. 셀·이동 횟수·UI 위치를 재생성하고 클리어 여부를 즉시 검사합니다.
- 스테이지를 직접 선택: 기존 동작처럼 최초 배치와 0회로 시작하고 해당 모드의 임시 기록을 교체합니다. 같은 모드에서는 가장 최근 퍼즐 하나를 보관합니다.
- Restart: 진행 중 애니메이션을 중지하고 최초 배치·0회를 즉시 저장합니다.
- 선택 화면/메인 메뉴 복귀: 현재 임시 기록을 유지합니다. 다른 모드를 둘러보는 것만으로 해당 모드 기록을 덮어쓰지 않습니다.
- 다른 모드 플레이: 각 모드의 파일을 사용하고 Continue 대상은 실제 마지막으로 연 퍼즐로 갱신합니다.
- 클리어: 먼저 기존 클리어·해금 파일을 저장한 뒤 임시 기록을 비활성 마커로 바꿉니다. 백업에도 같은 마커를 남겨 완료한 퍼즐이 오래된 백업에서 살아나지 않게 합니다. 다음 스테이지는 별도 최초 배치로 저장합니다. 클리어한 스테이지 재선택/Restart는 재플레이를 시작합니다.
- 마지막 이동 애니메이션 도중 종료: 이미 저장된 목표 배치와 이동 횟수를 복원하고 클리어·해금 처리를 마칩니다. 클리어 기록 저장에 실패했다면 해결된 보드를 유지해 나중에 재시도합니다.

## 손상 검사와 한계

버전·체크섬·격자 크기·스테이지 식별값·셀 수·유효 색상·색상별 개수·빈칸 수·음수 이동 횟수를 검사합니다. 이동 0회라면 반드시 스테이지 최초 배치여야 합니다. 파일 크기도 16KiB로 제한합니다. 주 파일이 손상되면 백업을 읽고, 백업 복원 후의 첫 저장에서는 정상 백업을 손상된 주 파일로 덮어쓰지 않습니다. 둘 다 유효하지 않으면 최초 배치로 돌아가되 클리어·해금 파일은 건드리지 않습니다. 일치하지 않는 다른 스테이지의 정상 기록은 복원하지 않고, 오래된 백업에서 해당 스테이지를 찾아 되살리지도 않습니다.

체크섬은 비밀 키를 사용하지 않는 손상 감지 장치이며 치트 방지 기능은 아닙니다. 복원 시 대규모 경로 탐색으로 도달 가능성을 재계산하지 않습니다. 실제 저장은 유효한 이동을 거친 보드에서만 생성하며 식별값·체크섬·구성 검증으로 파일 손상을 탐지합니다. 전원 차단·저장 장치 자체 고장까지 무조건 복원을 보장하지는 않습니다.

매 프레임 저장/JSON 생성은 하지 않습니다. 5×5에서도 셀 25개만 복사하며 스테이지 식별값은 캐시합니다. Editor 100회 측정 결과는 `Artifacts/Autosave/EditorWriteMetrics.json`에 있습니다. 이 측정은 Android 디스크 지연이나 메모리 프로파일링 결과를 대신하지 않습니다.

## 변경 파일

- 신규: `Assets/ColorMovePuzzle/Runtime/PuzzleSessionStore.cs`.
- 수정: `PuzzleGameController.cs`, `PuzzleCampaignController.cs`, `PuzzleMenuController.cs`.
- 신규 테스트: `Tests/EditMode/PuzzleSessionStoreTests.cs`, `Tests/PlayMode/PuzzleSessionPlayTests.cs` 및 Unity가 생성한 메타 파일.
- 검증 도구: `AgentScripts/InspectAutosave.cs`, `AgentScripts/VerifyAutosaveDevice.py`.

씬·UI·스테이지 데이터·난이도·이동 시간·기존 저장 포맷은 변경하지 않습니다. 저장 이벤트는 실행 시 기존 컴포넌트 사이에 연결하므로 Inspector 수동 설정은 필요하지 않습니다. 작업 전 Assets/Packages/ProjectSettings/AgentScripts 570개 파일을 `Artifacts/Autosave/Backup/ProjectBefore.zip`에 보관했습니다. 기존 Git 상태를 덮어쓰거나 커밋을 만들지 않았습니다.

## 실제 검증 기록

전체 EditMode **86/86**, 전체 PlayMode **46/46**(172.19초), 실패/스킵 0입니다. 기존 68개/37개에 신규 18개/9개를 추가했습니다. 기존 90개 스테이지의 규칙·해결 경로·UI 클리어 회귀 검사를 함께 실행했습니다. `Artifacts/Autosave/EditModeTests.json`, `PlayModeTests.json`에 실제 결과가 있습니다. 최초 손상 테스트에서 예외 처리 누락과 Unity JSON의 빈 배열 변환을 발견하여 수정한 뒤 통과했습니다. 최초 결과도 `EditMode-FirstAttempt.json`에 보관합니다.

신규 테스트는 세 모드의 셀·이동 횟수·실제 타일 UI 위치 복원, 복원 후 이동·클리어, Restart 중 애니메이션 취소와 즉시 저장, 선택 화면 복귀와 Continue, 직접 스테이지 선택 시 초기화, 모드별 파일 분리, 손상 주 파일의 백업 복구, 잘못된 버전/구성/식별값/체크섬 거부, 기존 진행·설정 보존, 저장 실패 후 Pause 재시도, 마지막 이동과 클리어 처리 사이 종료를 포함합니다.

Editor 저장 100회 측정: 5×5 JSON **256바이트**, 디스크 반영 포함 평균 **5.29ms**, 최대 **13.38ms**. 자동 저장의 Android 디스크 지연과 할당량을 별도 프로파일러로 측정하지는 않았습니다.

## Android 빌드와 승인 후 실기기 검증

[ColorMovePuzzle-Autosave-Development.apk](Builds/Android/ColorMovePuzzle-Autosave-Development.apk): **69,753,193바이트**, SHA-256 `46AA2780EB26D1D5963C5938354B7D3BD0F9C975195A10C1C00FE9E6F273C1F6`.

Unity **6000.6.5f1**, Android Portrait, IL2CPP, ARM64, Development, 패키지 `com.colormovepuzzle.game`, version 1.0/code 1, SDK 26/36을 유지했습니다. 빌드 **성공**, 141.626초, 오류 **0**, 경고 **4**입니다. 경고는 기존 TMP 큰 메서드 컴파일 안내 3개와 Diagnostics 심볼 권고 1개입니다. 이번 증분 빌드에서 이전 AI Inference 셰이더 경고 978개가 재출력되지 않았으며, 해당 패키지나 셰이더를 수정한 결과로 해석하지 않습니다. 기존과 같은 Debug v2 서명과 ARM64 ELF 16KiB 호환을 검사했습니다. `BuildReport.json`, `BuildSummary.json`, `Warnings.json`, `ApkSignature.txt`, `ApkInspection.json`이 근거입니다.

사용자의 **업데이트 설치** 승인 후 연결된 Samsung SM-S938N / Android 16 / serial R3CY102P6FF에서 `adb install -r`가 **Success**였습니다. uninstall/pm clear/저장 JSON 직접 덮어쓰기는 사용하지 않았습니다. 앱을 중지하고 기존 저장 파일 6개를 원본 바이트로 백업했으며, 설치 후 첫 실행 전에 파일 6개 모두 SHA-256이 같았습니다. 실제 저장 경로는 `/sdcard/Android/data/com.colormovepuzzle.game/files/ColorMovePuzzle`입니다. 처음 검증 도구가 내부 경로를 가정해 실패하여 경로를 확인한 뒤 도구만 수정했습니다.

실기기 입력은 **ADB tap/swipe/keyevent**이며 사람의 손가락 조작감 검증은 아닙니다. Editor 테스트 결과를 실기기 테스트로 대신 보고하지 않습니다.

1. 메인 메뉴에서 5×5 / 스테이지 1 진입 후 세 번 실제 이동.
2. 각 이동마다 캡처한 25셀 배치와 저장 JSON의 배치·이동 횟수 일치 확인. 두 번째 이동 뒤 홈으로 갔다 복귀해 동일 프로세스·보드 유지 확인.
3. 세 번째 이동의 셀 배열·이동 횟수 **3**을 기록하고 앱을 force-stop.
4. PID **13728 → 14488**로 새 프로세스 실행, 메인 메뉴에서 Continue 선택.
5. 종료 전과 복원 후 **25개 셀 전부 일치**, 이동 횟수 **3** 유지. 실제 UI의 MOVES 3과 타일 배치도 두 스크린샷에서 직접 확인.
6. 복원 후 추가 이동과 일반적인 역방향 이동을 수행해 입력 및 저장 동작 확인. 두 번 모두 이동 횟수를 증가시켰으므로 최종 배치는 복원한 배치이고 최종 횟수는 **5**입니다. 되돌리기 기능을 추가한 것이 아닙니다.
7. 기존 세 모드의 클리어 기록과 BGM/SFX 설정이 동일함을 비교. Continue 대상은 실제 마지막 플레이에 따라 **5×5/1번**으로 정상 갱신됐습니다.

기기는 마지막으로 5×5 / 1번 / 5회 상태로 남겼습니다. `Device/Verification.json`, `BeforeTermination.json`, `AfterVerification.json`, `SavesBeforeUpdate/`, `HashesBeforeUpdate.json`과 화면 PNG가 실제 증거입니다. 종료 전 [화면](Artifacts/Autosave/Device/move-3.png)과 복원 후 [화면](Artifacts/Autosave/Device/continue-restored.png)을 비교할 수 있습니다.

## 남아 있는 문제와 미검증 항목

두 실제 실행 모두 입력 None 예외 **0**, 자동 저장 실패 로그 **0**, FATAL/ANR 로그 **0**이었고 관측 종료 시 새 프로세스가 살아 있었습니다. 앱 PID 로그는 `Device/LogcatFirst.txt`, `LogcatRelaunch.txt`, 집계는 `Device/LogSummary.json`에 있습니다.

- AssetPackManager 선택적 조회는 각 실행 1회, Dex finalizer 로그는 각 실행 2줄(동일 예외 중복 출력)로 기존처럼 재발했습니다. 이전 안정화 보고서의 분류를 유지하며 이번 저장 기능과 무관한 엔진/Gradle 변경은 하지 않았습니다.
- 실제 ADB BACK으로 퍼즐 → 선택 화면 전환 한 번이 응답하지 않아 STAGES 버튼으로 복귀했습니다. 이후 선택 → 메인 메뉴 BACK은 동작했습니다. `WindowOnBackDispatcher`의 callback 비활성 경고도 기록했습니다. 이 경고가 무응답의 원인이라는 인과관계는 검증하지 않았고, 기존 간헐적 문제를 해결됐다고 보고하지 않습니다.
- Android에서 이번 단계의 직접 저장·복원 검증은 5×5/1번으로 수행했습니다. 3×3·4×4의 복원은 실제 Editor PlayMode에서 검증했으며 이번 APK로 두 모드의 실기기 복원을 별도 반복하지는 않았습니다.
- 실제 메모리 압박에 의한 OS 강제 종료·전원 차단·장기간 사용·다른 Android 기기·사람의 터치감/제스처 뒤로 가기는 미검증입니다. force-stop에 의한 새 프로세스 복원과 Home 복귀만 위 절차로 확인했습니다.

데이터/씬 195개 파일의 SHA-256이 모두 작업 전과 같고, 기존 Core 코드·패키지·Android 설정을 유지했습니다. `ContentIntegrity.json`, `ChangedExistingFiles.json`에 대조 기록을 보관합니다. Unity Editor는 Play 중지 및 테스트용 백그라운드 실행 설정 해제 상태로 복원했습니다. Inspector에서 직접 연결할 항목은 없습니다.
