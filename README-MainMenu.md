# COLOR MOVE — 메인 메뉴와 설정

5차 개발에서 3×3/4×4/5×5 모두 실제 30개 콘텐츠에 연결했습니다. 최신 데이터·뷰·저장 확장은 [README-FiveByFive.md](README-FiveByFive.md)를 참고하십시오. 아래 Coming Soon 설명과 43/21 테스트 수는 3차 개발 당시의 기록입니다.

현재 씬: `Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity`

Unity에서 Play를 누르면 COLOR MOVE 메인 메뉴로 시작합니다. 기존 씬과 3×3 캠페인은 그대로 사용하며 추가 씬은 없습니다. 메인 메뉴와 설정을 담당하는 컨트롤러를 기존 PuzzleGame 루트에 추가했습니다. Inspector 수동 설정은 필요 없습니다.

## 화면과 이동

```text
MainMenu
  ├─ 3×3 → 기존 StageSelection → Puzzle / ClearOverlay
  │          └─ MENU → MainMenu       └─ STAGES / 뒤로 가기 → StageSelection
  ├─ Continue → 마지막으로 플레이한 격자/스테이지
  ├─ 4×4, 5×5 → Coming Soon / 선택 불가
  └─ Settings → BGM / 효과음 볼륨 → DONE / 뒤로 가기 → MainMenu
```

메인 메뉴에는 색상 Image 타일 장식, COLOR MOVE 제목, 이어하기, 데이터 기반 격자 카드 3개와 설정 버튼이 있습니다. 기존 Canvas Scaler와 안전 영역/균일 축소 방식을 재사용하여 기준 1080×1920 세로 화면에 배치했습니다. 장식과 작은 모드 아이콘은 비조작 uGUI Image이며 4×4/5×5 퍼즐 콘텐츠는 생성하지 않았습니다.

현재 화면은 `PuzzleScreen`으로 구분합니다. Android 뒤로 가기/Escape는 설정 → 메뉴, 스테이지 선택 → 메뉴, 플레이 또는 클리어 → 스테이지 선택으로 한 단계씩 이동합니다. 메인 메뉴에서 뒤로 가기는 Android 플레이어에서만 앱 종료를 요청하며 에디터는 종료하지 않습니다.

## 기존 시스템과의 연결

`PuzzleCampaignController`의 스테이지 해금·클리어·저장 동작을 유지했습니다. 준비 완료, 선택 화면 표시, 스테이지 진입 이벤트를 추가했고 메뉴가 시작 화면을 소유하도록 설정했습니다. 기존 STAGES/NEXT STAGE 버튼과 직접 `ShowSelection`/`PlayStage`를 사용하는 경로도 메뉴 상태에 반영됩니다.

`Data/Modes.asset`에는 격자 크기, 구현 여부, 스테이지 카탈로그, 해당 모드의 진행 파일 이름이 있습니다. 현재 3×3만 기존 30개 카탈로그에 연결됩니다. 4×4/5×5는 구현 여부가 false이고 카탈로그도 없습니다. 데이터뿐 아니라 실제 보드/선택 뷰가 콘텐츠를 표현할 수 있는지도 검사하므로 가짜 콘텐츠에 진입할 수 없습니다. 향후 실제 콘텐츠와 해당 크기용 뷰/카탈로그를 연결한 뒤 같은 메뉴 카드와 선택 경로를 사용할 수 있습니다.

## 이어하기와 저장 호환성

기존 `Application.persistentDataPath/ColorMovePuzzle/progress-v1.json`은 계속 3×3 해금·클리어를 담당합니다. 스키마를 변경하거나 메뉴 설정으로 덮어쓰지 않습니다.

새 `Application.persistentDataPath/ColorMovePuzzle/menu-profile-v1.json`에는 다음 정보를 별도로 저장합니다.

- 실제 스테이지에 진입한 기록이 있는지
- 마지막 격자 크기와 스테이지 번호
- BGM 및 효과음 볼륨(0~1)

처음 실행하거나 설정만 조작한 상태에서는 Continue가 비활성화됩니다. 잠긴 스테이지나 아직 구현되지 않은 모드를 가리키는 기록도 Continue로 열 수 없습니다. 단순히 스테이지 선택 화면을 보는 것만으로 플레이 기록을 생성하지 않습니다.

이전 버전의 정상 진행 파일이 있고 새 메뉴 프로필이 없으면, 기존 `lastPlayed`를 읽어 이어하기에 활용합니다. 시작할 때 기존 진행 파일이나 새 프로필을 강제로 다시 쓰지 않으며, 실제 스테이지 진입/설정 변경 시 새 프로필을 저장합니다.

이어하기는 저장된 **격자 크기와 스테이지**에 입장하는 기능입니다. 기존 스테이지 재입장 규칙대로 최초 배치와 이동 횟수 0에서 시작하며, 중간 칸 배치나 진행 중 애니메이션을 저장하지 않습니다.

새 프로필도 임시 파일 교체와 `.bak` 백업/복구를 사용합니다. 잘못된 JSON, 지원하지 않는 버전, 범위를 벗어난 볼륨/기록을 처리합니다. 자동 테스트와 미리보기는 사용자 파일과 분리된 경로를 사용합니다.

## 설정과 향후 오디오 연결

설정 패널에는 0~100%를 표시하는 BGM/효과음 Slider가 있습니다. 변경 즉시 새 프로필에 저장하고 패널/씬을 다시 열면 복원합니다. 기본값은 BGM 70%, 효과음 80%입니다.

오디오 리소스, AudioSource, AudioMixer를 추가하지 않았습니다. 향후 오디오 시스템은 `PuzzleMenuController.Profile.BgmVolume`, `.SfxVolume`을 읽고 `VolumesChanged(float bgm, float sfx)` 이벤트를 구독해 실제 볼륨에 적용할 수 있습니다.

## 생성/변경 파일

모든 C# 경로는 `Assets/ColorMovePuzzle/` 기준입니다.

| 파일 | 역할 |
| --- | --- |
| `Core/PuzzleMenuProfile.cs` | 플레이 기록과 정규화된 두 볼륨의 순수 데이터 |
| `Runtime/PuzzleMenuProfileStore.cs` | 별도 메뉴 프로필 저장/백업/복원 |
| `Runtime/PuzzleModeCatalog.cs` | 격자 크기별 콘텐츠/가용성/저장 이름 |
| `Runtime/PuzzleMenuController.cs` | 메뉴·설정·이어하기·뒤로 가기와 캠페인 연결 |
| `Runtime/PuzzleMainMenuView.cs` | 메뉴 버튼, 이어하기 상태, 모드 카드 갱신 |
| `Runtime/PuzzleModeButtonView.cs` | 격자 카드와 Coming Soon 입력 차단 |
| `Runtime/PuzzleSettingsView.cs` | 슬라이더·백분율·설정 저장 콜백 |
| `Runtime/PuzzleCampaignController.cs` | 기존 동작에 화면 이벤트/메뉴 시작/모드 연결 경로 추가 |
| `Editor/PuzzleMenuSceneBuilder.cs` | 같은 씬에 UI 생성, 모드 에셋 생성, 참조 연결 및 저장 |
| `Tests/EditMode/PuzzleMenuTests.cs` | 프로필 왕복, 이전 파일 보존, 백업, 모드 데이터 테스트 |
| `Tests/PlayMode/PuzzleMenuPlayTests.cs` | 시작 화면, 이동, Continue, 설정 복원, 이전 데이터, Escape 입력 |
| 기존 두 PlayMode 테스트 파일 | 메뉴 프로필도 임시 저장 경로로 격리 |

추가 데이터: `Data/Modes.asset`. 변경 씬: `Scenes/ColorMovePuzzle.unity`. 기존 BFS/30개 배치 에셋과 퍼즐 이동 코드는 변경하지 않았습니다.

## 검증과 한계

보고서는 `Artifacts/MainMenu/`, 화면 캡처는 `Assets/Artifacts/MainMenu/`에 저장합니다. 기존 30단계 연속 클리어/보드/입력/저장 테스트를 포함해 실행합니다. Android 실제 기기/시스템 제스처와 APK/AAB 빌드는 수행하지 않았으므로, 에디터의 Escape 입력 검증과 구분해야 합니다.

Unity Test Runner 실제 실행 결과: **EditMode 43/43 통과, PlayMode 21/21 통과**, 실패/건너뛰기 0. PlayMode 전체 실행은 약 58초였으며 기존 30단계 연속 클리어도 포함됩니다. 설정만 변경해도 이어하기 기록이 생기지 않는지, 새 씬 세션에서 마지막 스테이지와 두 볼륨이 복원되는지, 가상 키보드 Escape가 한 화면씩 복귀하는지 검사했습니다.

실제 InputSystem의 가상 Touchscreen으로 Coming Soon 차단, 설정 버튼, 두 Slider 드래그, 3×3/스테이지 선택, 두 복귀 버튼과 Continue까지 추가 확인하여 통과했습니다. UI 준비를 기다린 뒤 입력했고 테스트 동안 변경한 포커스 정책과 장치는 종료 시 복원했습니다.

1080×1920, 720×1280, 1080×2400에서 메뉴와 설정 콘텐츠의 안전 영역 내 배치도 확인했습니다. 화면 검수 후 Slider 손잡이를 48×48로 다듬고 터치 검증을 다시 통과했습니다.
