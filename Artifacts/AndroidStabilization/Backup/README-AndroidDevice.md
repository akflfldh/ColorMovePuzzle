# ColorMovePuzzle — Android 실기기 설치 및 ADB 실행 검증

2026-10-27, 사용자 설치 승인(`설치`) 후 Unity 번들 ADB로 실제 USB 기기에서 수행했습니다. **설치·실행·프로세스 재시작과 저장 파일 보존은 확인했지만, 반복되는 런타임 예외가 있어 오류 없는 실행으로 판정하지 않습니다.** 프로젝트나 APK를 수정·재빌드하지 않았습니다.

## 기기와 설치

- Samsung SM-S938N, Android 16 / API 36, ARM64, 화면 1080×2340.
- ADB serial: `R3CY102P6FF`, 연결 상태 `device`.
- 지정 APK: `Builds/Android/ColorMovePuzzle-Development.apk`, 54,300,743 bytes.
- 패키지 `com.colormovepuzzle.game`, versionName 1.0 / versionCode 1, Development/IL2CPP.
- APK SHA-256: `02A5095449C9686BD87B9E23C2A01CFF8A257B21550289BB2A97F80EC35B5193`.
- 설치 전 해당 패키지가 없었습니다. 승인 후 `adb -s R3CY102P6FF install -r APK`를 실행해 **Success**를 확인했습니다. 앱 제거나 데이터 초기화는 수행하지 않았습니다.
- [설치 결과](Artifacts/AndroidDevice/Install.txt), [기기 내 패키지 정보](Artifacts/AndroidDevice/InstalledPackage.txt).

## 실행과 종료·재실행

`am start -W -n com.colormovepuzzle.game/com.unity3d.player.UnityPlayerGameActivity`로 실행했습니다. 최초 및 재실행 모두 `Status: ok`, `LaunchState: COLD`였으며 전면 Activity와 실제 메인 메뉴 화면을 확인했습니다.

1. 최초 PID `28451`에서 앱 실행과 메뉴 렌더링 확인.
2. 해당 앱만 `am force-stop`하여 PID가 사라짐을 확인.
3. 저장 파일을 읽기 전용으로 수집하고 동일 Activity 재실행.
4. 새 PID `29137` 및 메인 메뉴·Continue 표시 확인.
5. 새 프로세스가 살아 있고 앱이 전면에 있는 상태로 남겨두었습니다.

[첫 실행](Artifacts/AndroidDevice/FirstLaunch.txt), [재실행](Artifacts/AndroidDevice/Relaunch.txt), [프로세스/파일 비교](Artifacts/AndroidDevice/Lifecycle.json), [첫 실행 화면](Artifacts/AndroidDevice/FirstLaunch.png), [재실행 화면](Artifacts/AndroidDevice/Relaunch.png)에 기록했습니다. Android 종료 이력은 **USER REQUESTED / FORCE STOP**이며, 관측 구간에 예상치 못한 프로세스 크래시나 ANR 기록은 확인되지 않았습니다. [종료 이력](Artifacts/AndroidDevice/ExitInfo.txt)을 보관했습니다.

## 진행·설정 보존

검증 중 기기에 존재하던 다음 데이터를 실제 앱 전용 저장 폴더에서 읽었습니다.

- `progress-v1.json`: 3×3 스테이지 1 클리어, 마지막 선택 2번.
- `menu-profile-v1.json`: Continue 3×3/2번, BGM 약 70%, SFX 약 80%.
- 위 두 파일의 `.bak` 백업.

앱이 종료된 상태에서의 파일과 새 프로세스 재실행 후 파일을 **기기에서 계산한 SHA-256**으로 비교했습니다. **4개 모두 동일**합니다. 재실행 화면에서도 Continue가 **3×3 / STAGE 02**로 표시됩니다. 저장 파일은 덮어쓰거나 테스트 데이터로 바꾸지 않았습니다.

[재실행 전 데이터](Artifacts/AndroidDevice/SavesBeforeRelaunch.json), [재실행 후 데이터](Artifacts/AndroidDevice/SavesAfterRelaunch.json), [비교 결과](Artifacts/AndroidDevice/Lifecycle.json)를 참고하십시오. 4×4·5×5 진행 파일은 이 시점에 없어 해당 모드의 실제 기기 저장 복원을 검증했다고 보고하지 않습니다. 볼륨은 현재 저장된 70%/80%가 보존된 것을 확인했으며 변경한 값의 저장 시험은 수행하지 않았습니다.

## Logcat 결과와 남은 문제

기존 Logcat을 지우지 않고 두 앱 PID의 main/system/crash 로그를 각각 수집했습니다. [최초 실행 로그](Artifacts/AndroidDevice/LogcatFirstLaunch.txt), [재실행 로그](Artifacts/AndroidDevice/LogcatRelaunch.txt), [집계](Artifacts/AndroidDevice/LogSummary.json)를 보관했습니다.

두 실행에서 모두 다음 문제가 재현됐습니다.

1. **입력 초기화 예외**: `ArgumentException: 'InputUpdateType.None' is not a valid update mask`. 각 실행에서 예외 헤더 2회. 스택은 `InputStateHistory.set_updateMask` → `EnhancedTouch.Finger` → `Touch.AddTouchscreen` → `InputManager.AddDevice`이며 `InputSystem.onDeviceChange` 콜백 예외도 기록됩니다. 화면 하단 Development Console에 표시됩니다. 입력 관련 호환성 문제를 별도로 수정·재검증해야 합니다.
2. **AssetPack 클래스 조회 실패**: `java.lang.ClassNotFoundException` for `com.google.android.play.core.assetpacks.AssetPackManager`. 각 실행 초기화 시 기록됩니다. 앱은 그 뒤 메뉴를 표시했으나 원인과 전체 영향은 확정하지 않았습니다.
3. **Dex finalizer 오류**: `java.lang.AssertionError: Failed to close dex file in finalizer.` 두 실행에서 기록됐습니다. 동일 예외가 중복 출력되므로 로그 줄 수를 별도 오류 발생 횟수로 단정하지 않습니다.

이외 ashmem 폐기 예정 API 및 제조사 그래픽 라이브러리/서비스 관련 E 로그도 있습니다. 앱 PID별 E 로그 줄 수는 각 75줄이며 대부분 예외 스택을 포함하므로 75개의 독립 오류라는 뜻은 아닙니다. 수집 로그에서 `FATAL EXCEPTION`, `Fatal signal`, `ANR in`은 발견하지 못했고 앱은 실행 상태를 유지했습니다. **크래시가 없다는 사실과 런타임 오류가 없다는 판단은 구분합니다.**

이번 요청은 기존 APK 설치·실행 검증이므로 소스/패키지 설정을 변경하거나 다른 APK로 교체하지 않았습니다. 남은 예외의 수정과 수정 APK의 실기기 회귀 검증은 수행하지 않았습니다.

## 검증 범위와 보존

에이전트는 ADB 설치·Activity 실행·강제 종료·재실행·로그/화면/저장 파일 조회만 수행했습니다. **ADB 탭/스와이프 입력을 주입하거나 직접 퍼즐을 플레이하지 않았습니다.** 3×3·4×4·5×5의 실제 조작감, 멀티터치, 스와이프 정확도와 장시간 플레이는 사용자가 직접 확인할 항목입니다.

[ProjectIntegrity.json](Artifacts/AndroidDevice/ProjectIntegrity.json)에서 기존 Core/Runtime 코드·스테이지 에셋·씬 117개와 APK가 이전 빌드와 동일함을 확인했습니다. 이번에 추가·갱신한 것은 검증 로그·스크린샷·기기 저장 파일의 로컬 사본과 보고서입니다. 기기의 저장 파일을 수정하거나 Logcat을 초기화하지 않았습니다.
