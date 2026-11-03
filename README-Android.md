# ColorMovePuzzle — Android 빌드 및 모바일 검증

11차 비공개 테스트용 **[서명된 Release AAB](Builds/Android/ColorMovePuzzle-ClosedTest-v1.0-code1.aab)**가 생성됐습니다. UMP 유지·광고 요청 차단·업로드 키 및 검증 결과는 [비공개 테스트 보고서](README-ClosedTest.md)에 있습니다. 기존 스마트폰 앱/저장 데이터는 변경하지 않았습니다.

10차 최신 개발 APK와 API 36 고정, Release AAB 차단·서명·개인정보·콘솔 준비 및 UMP 실기기 검증은 **[출시 준비 보고서](README-Release.md)**에 있습니다. 최신 APK는 [ColorMovePuzzle-ReleasePreparation-Development.apk](Builds/Android/ColorMovePuzzle-ReleasePreparation-Development.apk)입니다. 아래는 이전 단계의 기록입니다.

9차 테스트 광고 APK는 **[ColorMovePuzzle-AdMob-Test-Development.apk](Builds/Android/ColorMovePuzzle-AdMob-Test-Development.apk)**입니다. 빌드·업데이트 설치·실제 광고 표시와 Continue 복원 결과는 **[README-AdMob.md](README-AdMob.md)**에 있습니다.

이전 자동 저장 APK와 실기기 업데이트·Continue 복원 결과는 **[README-Autosave.md](README-Autosave.md)**에 있습니다. 해당 파일은 [ColorMovePuzzle-Autosave-Development.apk](Builds/Android/ColorMovePuzzle-Autosave-Development.apk)입니다.

7차 안정화 APK는 [ColorMovePuzzle-Stabilized-Development.apk](Builds/Android/ColorMovePuzzle-Stabilized-Development.apk)입니다. 입력 초기화 예외 수정 결과는 [7차 안정화 보고서](README-AndroidStabilization.md)에 있습니다. 아래는 최초 빌드 당시 기록입니다.

후속 USB 실기기 설치·실행·재실행 검증은 **[README-AndroidDevice.md](README-AndroidDevice.md)** 에 있습니다. SM-S938N에서 메뉴 및 저장 보존을 확인했지만 입력 초기화 등 런타임 예외가 발견됐습니다. 아래의 실기기 미실시 설명은 APK를 생성한 6차 개발 시점의 기록입니다.

실제 빌드 결과와 테스트 기록은 `Artifacts/Android/`에 보관합니다. 게임의 90개 스테이지, 난이도 및 UI 디자인을 유지했습니다.

## 환경과 설정

Unity 6000.6.5f1, Unity CLI 1.0.0-beta.14, Unity Pipeline 0.9.1-exp.1을 사용합니다. Android Build Support가 설치되어 있으며 Unity 번들 SDK·NDK·OpenJDK를 사용합니다. `Toolchain.json`과 `EnvironmentBefore.json`에 도구 경로와 변경 전 설정을 기록했습니다.

- NDK r27c / 27.2.12479018
- OpenJDK Temurin 17.0.18+8
- SDK Platforms: android-34, android-36, android-37.0
- SDK Build Tools 36.0.0, Platform Tools 37.0.1
- Android 활성 플랫폼, Portrait, IL2CPP, ARM64, Input System only 유지
- Package name: `com.colormovepuzzle.game`
- 최소 SDK 26 및 자동 Target SDK 기존 설정 유지
- Development APK, 앱 번들/Gradle 프로젝트 내보내기 해제
- 스크립트 디버깅 및 자동 Profiler 연결 해제
- 시작 씬: `Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity`

기존 SampleScene과 에셋은 삭제하지 않습니다. 이번 APK의 빌드 씬 목록은 퍼즐 씬 하나로 명시했습니다. 프로젝트 Build Settings의 첫 씬도 퍼즐 씬으로 유지합니다.

기본 회사명 패키지 `com.DefaultCompany.ColorMovePuzzle`에서 위 이름으로 변경했습니다. 저장 JSON 버전·파일명·직렬화 구조는 그대로입니다. **다른 패키지 이름의 과거 Android APK가 있다면 Android는 별도 앱으로 취급하므로 그 앱의 저장 폴더가 자동 이전되지는 않습니다.** 향후 이 패키지 이름과 서명을 유지한 업데이트 설치를 사용해야 합니다. Editor의 기존 실제 사용자 진행 데이터는 수정하지 않습니다.

## 빌드

**빌드 성공**: [ColorMovePuzzle-Development.apk](Builds/Android/ColorMovePuzzle-Development.apk), 54,300,743 bytes(54.3MB / 약 51.8MiB). Unity BuildPipeline/IL2CPP/Gradle이 완료한 실제 파일이며 빌드 시간은 약 289.3초입니다. [BuildSummary.json](Artifacts/Android/BuildSummary.json), [전체 BuildReport](Artifacts/Android/BuildReport.json)을 보관했습니다. BuildReport의 totalSizeBytes는 여러 생성 산출물의 합계이며 APK 파일 크기가 아닙니다.

APK 내부 검사:

- `com.colormovepuzzle.game`, versionName 1.0 / versionCode 1
- 최소 SDK 26, 실제 대상/컴파일 SDK **36**
- Development/debuggable, ARM64(`arm64-v8a`)만 포함, `libil2cpp.so` 존재
- 기본 Android Debug 인증서의 APK Signature Scheme v2 검증 통과
- ZIP의 네이티브 라이브러리 16KiB 정렬 및 ELF LOAD 세그먼트 16KiB 호환 정렬 검사 통과. 실제 16KiB 페이지 기기 실행을 검증한 것은 아님
- Portrait Activity, UnityPlayerGameActivity 시작
- SHA-256: `02A5095449C9686BD87B9E23C2A01CFF8A257B21550289BB2A97F80EC35B5193`

[ApkBadging.txt](Artifacts/Android/ApkBadging.txt), [ApkSignature.txt](Artifacts/Android/ApkSignature.txt), [ApkInspection.json](Artifacts/Android/ApkInspection.json), [ApkAlignment.json](Artifacts/Android/ApkAlignment.json)에 실제 도구 결과를 보관했습니다. 배포/스토어 제출용 Release 빌드는 수행하지 않았습니다.

빌드 오류 **0**, 경고 **982**:

- 기존 `com.unity.ai.inference`의 Sentis/ConvGeneric 셰이더 경고 978개: 정수 나눗셈·나머지 성능, signed/unsigned, 지원하지 않는 일부 커널 변형에 대한 경고.
- TextMeshPro IL2CPP의 큰 메서드를 별도 C++ 파일로 분리했다는 컴파일 안내 3개.
- Diagnostics Data를 위한 Debug Symbols 설정 권고 1개. 네이티브 크래시 진단용 SymbolTable/Full 설정은 이번 빌드에서 추가하지 않았습니다.

[WarningCategories.json](Artifacts/Android/WarningCategories.json), [중복을 묶은 경고](Artifacts/Android/WarningSummary.json)와 전체 BuildReport에 기록했습니다. 기존 패키지나 셰이더를 제거/변경하여 경고를 감추지 않았으며, 퍼즐 C#의 빌드 오류는 없습니다.

빌드 요청 전 PowerShell 배열 인자 오류와 설정 반영 중 Pipeline 연결 종료가 한 차례 있었습니다. Editor 준비 상태를 확인하고 인자를 수정한 뒤 실제 빌드는 한 번 실행하여 성공했습니다. 추가 설치가 필요한 SDK/NDK/JDK 구성 요소는 발견하지 못했습니다.

## 검증 범위

Editor에서 가상 Touchscreen/Keyboard, 실제 Canvas UI, 파일 저장 복원을 검증합니다. 실제 Android 프로세스 실행이나 실기기 터치 검증과 구분합니다. ADB `devices -l` 조회 시 연결된 기기가 없어 기기 설치·실행·Logcat 검증은 수행하지 않습니다.

진행·이어하기는 모드별 스테이지 번호와 해금/클리어 기록을 저장합니다. 플레이 도중 보드 위치와 이동 수는 저장하지 않으며, 이어하기 시 해당 스테이지의 최초 배치와 0회부터 시작하는 기존 동작을 유지합니다. 현재 보드 상태 저장은 향후 개선 사항입니다.

Android 활성 플랫폼에서 전체 EditMode **66/66 통과**(6.16초), 전체 PlayMode **37/37 통과**(140.49초), 실패/스킵 0입니다. [EditModeTests.json](Artifacts/Android/EditModeTests.json), [PlayModeTests.json](Artifacts/Android/PlayModeTests.json)에 실제 결과를 보관했습니다. 기존 3×3/4×4 회귀, 메뉴·설정·저장·뒤로 가기, 5×5 전체 30개 UI 클리어를 포함합니다. 5×5 1·15·30번은 기본 애니메이션 속도, 나머지는 테스트 중에만 빠르게 실행했습니다. [해당 UI 결과 CSV](Artifacts/Android/FiveAllStagesUIPlay.csv)도 보관했습니다. 자동 테스트 파일 저장은 임시 폴더에서만 수행합니다.

[MobileLayout.json](Artifacts/Android/MobileLayout.json)의 **9/9 경우**를 실제 Game View에서 검증했습니다. 720×1280, 1080×1920, 1080×2400마다 3×3/4×4/5×5의 셀·타일·빈칸 구성, 정사각형 비율과 현재 보고된 Safe Area 내부 배치를 확인했습니다. 720×1280에서 5×5 목표 셀은 약 35.2px이므로 기존 목표 확대 기능을 사용할 수 있습니다. 실제 카메라 홀/노치 및 제스처 영역을 포함한 기기 화면은 검증하지 않았습니다.

[MobileTouch.json](Artifacts/Android/MobileTouch.json)에서 Input System 가상 Touchscreen/Keyboard 검증 통과를 확인했습니다. 세 모드에서 스테이지 선택 스크롤, 단일 빈칸 탭, 대각선 거부, 재시작, 스테이지 1 해결 경로 전체의 네이티브 스와이프(각 2/4/44회), 클리어·다음 해금, 뒤로 가기를 수행했습니다. 여러 목적지가 있는 타일에 대한 탭 14회가 임의 이동하지 않는지 검사하고 방향 스와이프를 적용했습니다. 볼륨은 실제 슬라이더 드래그로 20%/55%를 설정해 저장했으며, 목표 확대와 Continue(5×5/2번)도 확인했습니다.

이후 Play mode를 종료하고 도메인을 재로드한 다음 새 Play 세션에서 같은 파일들을 다시 불러왔습니다. [EditorRestartPersistence.json](Artifacts/Android/EditorRestartPersistence.json)에서 **세 모드의 독립 클리어/해금·마지막 스테이지, Continue(5×5/2번), 볼륨 20%/55% 복원 통과**를 기록했습니다. 종료 전 1회 이동했던 퍼즐은 최초 배치와 0회로 시작함을 확인했습니다. 이것은 Editor 세션 재시작 검증이며 **Android 앱 프로세스 종료/재실행 검증은 수행하지 않았습니다.** [Editor 메인 메뉴](Assets/Artifacts/Android/EditorMainMenu.png), [복원된 5×5 최초 배치](Assets/Artifacts/Android/EditorRestoredFive.png)도 실제 캡처로 보관했습니다.

최종적으로 Editor는 Android 플랫폼과 저장된 게임 씬을 유지하고 Play mode를 종료했습니다. 테스트용 Run In Background 재정의는 원래 값으로 복원했습니다. [EnvironmentAfter.json](Artifacts/Android/EnvironmentAfter.json)에 최종 설정이 있습니다.

[ContentIntegrity.json](Artifacts/Android/ContentIntegrity.json)은 Core/Runtime C#·스테이지 에셋·게임 씬 117개의 SHA-256을 변경 전과 비교하여 모두 동일함을 확인한 기록입니다. 저장 JSON의 버전 1 및 세 모드의 파일명도 유지했습니다. 광고·인앱 결제·힌트·새 게임 기능은 추가하지 않았습니다.

변경한 작업 파일은 Android Player/Editor 빌드 설정과 `AgentScripts/InspectAndroidEnvironment.cs`, `ConfigureAndroidBuild.cs`, `AndroidPreview.cs`, `VerifyMobileLayout.cs`, `VerifyMobileTouch.cs`, `RestoreMobilePreview.cs`, `InspectApk.ps1`입니다. 이 스크립트들은 Assets 밖의 검사/자동화 도구이며 게임 실행 코드에 포함되지 않습니다.

## 실기기 설치 및 확인 방법

ARM64 Android 8.0(API 26) 이상 기기를 USB로 연결하고 개발자 옵션에서 USB 디버깅을 활성화한 뒤 PC 연결을 승인합니다. Unity 번들 ADB 경로는 다음과 같습니다.

```powershell
$adb = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
& $adb devices -l
& $adb install -r 'C:/Users/dongd/ColorMovePuzzle/Builds/Android/ColorMovePuzzle-Development.apk'
& $adb shell monkey -p com.colormovepuzzle.game -c android.intent.category.LAUNCHER 1
```

여러 기기가 표시되면 `-s SERIAL`로 대상 기기를 지정합니다. 서명이 다른 기존 앱이 있으면 `install -r`이 실패할 수 있습니다. 기존 데이터를 보존하려면 앱을 임의로 삭제하지 말고 같은 서명으로 빌드해야 합니다. APK를 휴대폰으로 복사해 Android 파일 앱에서 설치할 수도 있습니다.

실기기에서 확인할 사항:

1. 메인 메뉴 표시, 각 모드 30개 및 3×3/4×4/5×5의 1번 플레이.
2. 타일 비율, 목표 글자/색상 가독성, 5×5 목표 확대, 카메라 홀·노치·제스처 영역과 UI 겹침.
3. 단일 빈칸 탭, 여러 빈칸에 대한 방향 스와이프, 대각선 거부, 이동 중 추가 입력, 재시작.
4. 스테이지 선택 터치 스크롤 및 볼륨 슬라이더.
5. 클리어→다음 해금, 세 모드의 독립 진행, 다시 플레이.
6. 앱을 종료/재실행하여 Continue와 볼륨이 복원되는지 확인. 퍼즐은 최초 배치로 복원되어야 함.
7. Android 뒤로 가기: 확대→퍼즐→선택→메인, 설정→메인, 메인에서 종료. Android 제스처/예측 뒤로 가기도 확인.
8. 백그라운드 전환/복귀, 화면 잠금/복귀와 실제 장시간 플레이의 프레임·메모리.

프로세스 종료 후 복원 시험은 다음과 같이 앱 데이터를 지우지 않고 수행할 수 있습니다.

```powershell
& $adb shell am force-stop com.colormovepuzzle.game
& $adb shell monkey -p com.colormovepuzzle.game -c android.intent.category.LAUNCHER 1
```

Logcat은 앱 실행 후 프로세스 ID를 받아 해당 앱 로그를 보관합니다.

```powershell
$appProcessId = (& $adb shell pidof -s com.colormovepuzzle.game).Trim()
& $adb logcat --pid=$appProcessId -v threadtime
```

특히 `AndroidRuntime`, `FATAL EXCEPTION`, `Unity` 예외, 저장 오류를 확인하십시오. PID는 앱 재실행마다 다시 조회해야 합니다. 실제 기기가 연결되면 설치는 사용자 승인 후에만 수행합니다.
