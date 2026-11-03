# ColorMovePuzzle — Android 런타임 안정화

Unity 6000.6.5f1 / IL2CPP / ARM64 / Portrait / Development 조건을 유지했습니다. 입력 초기화 예외를 수정한 APK의 빌드·승인 후 업데이트 설치·실기기 재검증을 완료했습니다. AssetPack 조회 로그와 원인 미확정 Dex finalizer 로그는 남아 있습니다. 이번 작업의 증거는 `Artifacts/AndroidStabilization/`에 보관합니다.

## 작업 전 복구 지점

Git main에는 아직 커밋이 없고 기존 파일들이 staged/modified/untracked 상태였습니다. 이를 변경하지 않고 `Backup/ProjectBefore.zip`에 Assets·Packages·ProjectSettings·AgentScripts 전체를 백업했습니다. ZIP 563개 항목을 다시 열어 확인했고 기존 APK도 `Backup/ColorMovePuzzle-Before.apk`로 복사했습니다. 기존 README도 별도 보관했습니다. 게임/테스트 파일의 변경 전 SHA-256은 `ContentBefore.json`에 있습니다. 백업은 Assets 밖에 있습니다.

## 1. InputUpdateType.None

실기기 원본 로그에서 최초 18:43:28.376, 재실행 18:47:05.210에 발생했습니다. PID 28451/TID 28471 및 PID 29137/TID 29157로 앱의 UnityMain 스레드입니다. 각 시작 시 예외 헤더 2회이며 각 예외에 `InputSystem.onDeviceChange` 콜백 오류가 뒤따릅니다. 원본 전체 스택은 `Artifacts/AndroidDevice/LogcatFirstLaunch.txt` 및 `LogcatRelaunch.txt`에 있습니다.

호출 경로: `InputManager.OnNativeDeviceDiscovered` → `AddDevice` → `onDeviceChange` → `EnhancedTouchSupport.OnDeviceChange` → `Touch.AddTouchscreen` → `FingerAndTouchState.AddFingers` → `Finger` → `InputStateHistory.updateMask`의 None 거부. 사용자 퍼즐/테스트 코드에는 수동 `InputSystem.Update` 호출이 없습니다.

원인:

1. Input System 1.20.0, Active Input Handling=Input System only, Dynamic Update. 실제 EventSystem 1개와 InputSystemUIInputModule 1개 및 Point/Click/Navigate 참조는 정상입니다.
2. Editor AI Inference Visualizer가 사용 중인 **App UI 2.2.4 설정은 이미 editorOnly=true**입니다. 하지만 Android 심벌은 비어 있고 Standalone에만 `APP_UI_EDITOR_ONLY`가 있어 Android Player에 App UI 어셈블리가 포함됐습니다. [변경 전 설정](Artifacts/AndroidStabilization/ConfigurationBefore.json).
3. App UI `AppUI.RunInitializeInPlayer`가 SubsystemRegistration에서 `AppUIManager.Initialize` → `AppUIInput.Initialize`를 호출합니다. 모바일일 때만 `EnhancedTouchSupport.Enable()`을 실행하여 콜백과 유효 updateMask를 등록합니다.
4. Input System `Touch.InitializeGlobalTouchState`도 같은 SubsystemRegistration 단계에서 GlobalState를 통째로 새 구조체로 바꿉니다. App UI 쪽이 먼저 실행되면 updateMask/enable count는 0이 되지만 기존 device-change 콜백은 남습니다. 이후 실제 Touchscreen 발견 때 None이 Finger에 전달됩니다. 이 순서를 그대로 호출한 재현 테스트가 실제 예외를 발생시킵니다.
5. Desktop Editor는 `Application.isMobilePlatform=false`여서 App UI가 이 Enable 경로에 들어가지 않습니다. 따라서 기존 Editor 입력 테스트만으로 이 Player 초기화 문제를 잡지 못했습니다.

수정: **기존 Editor Only 설정을 존중하도록 Android에 `APP_UI_EDITOR_ONLY` 심벌을 추가**했습니다. App UI의 공식 asmdef 조건 `UNITY_EDITOR || !APP_UI_EDITOR_ONLY`에 따라 Player에서 제외하고 Editor 도구는 유지합니다. 퍼즐은 Canvas/uGUI와 InputSystemUIInputModule을 쓰며 App UI에 의존하지 않습니다. 패키지 버전을 바꾸거나 PackageCache 코드를 수정하지 않았습니다. 예외를 잡아 숨기거나 입력 시스템을 수동 업데이트하지 않습니다.

공식 [AppUISettings.editorOnly 문서](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.2/api/Unity.AppUI.Core.AppUISettings.html#Unity_AppUI_Core_AppUISettings_editorOnly)와 설치된 패키지 소스를 대조했습니다. 테스트의 의도적 패키지 상태 초기화·예상 로그 검사는 Editor 재현용이며 Player에 포함되지 않습니다.

## 2. AssetPackManager 조회

최초 18:43:26.063 및 재실행 18:47:02.952, 각 1회, 앱 PID의 UnityMain에서 `java.lang.ClassNotFoundException`이 E/Unity로 출력됐습니다. 이는 다른 앱의 시스템 로그가 아닙니다. 전체 문자열은 `com.google.android.play.core.assetpacks.AssetPackManager`가 APK의 DexPathList에 없다는 내용입니다. Java 전체 스택은 원래 로그에 출력되지 않았습니다.

현재 APK는 Split Application Binary=false, App Bundle=false이며 Gradle 모듈은 launcher/unityLibrary입니다. Play Asset Delivery 패키지, asset packs, 커스텀 Android 플러그인이나 PAD 호출은 없습니다. 모든 게임 데이터를 APK에 넣습니다.

정확한 출력 지점은 설치된 Unity 엔진 `classes.jar`의 `PlayAssetDeliveryUnityWrapper` 생성자입니다. JDK javap로 확인한 [bytecode](Artifacts/AndroidStabilization/AssetPackWrapper.javap.txt)는 선택적 클래스 loadClass와 getPackStates 반환형을 검사하고 ClassNotFoundException/NoSuchMethodException/SecurityException을 **엔진 내부에서 catch하여 logError(toString) 후 반환**합니다. `playCoreApiMissing()`은 구현이 없을 때 true를 반환합니다. 이것이 Java 스택 없이 오류 한 줄만 찍힌 이유입니다.

판정: 실제 PAD 다운로드 실패가 아니라, PAD를 포함하지 않는 일반 APK에서 엔진의 선택적 기능 탐색이 오류 수준으로 출력된 것입니다. 현재 게임의 자산 로드/메뉴 실행을 막지 않았습니다. **PAD 라이브러리를 추가하거나 엔진 클래스를 교체하지 않았습니다.** 새 APK의 두 차례 cold start에서도 각각 한 번 남는 것을 확인했습니다.

## 3. Dex finalizer

최초 18:43:58.994 PID 28451/TID 28459, 재실행 18:47:35.734 PID 29137/TID 29145, 앱 프로세스의 ART FinalizerDaemon에서 발생했습니다. 원문은 `java.lang.AssertionError: Failed to close dex file in finalizer.`이며 `DexFile.finalize` → `Daemons$FinalizerDaemon.doFinalize/processReference/runInternal` → `Daemon.run` → `Thread.run`입니다. 동일 스택이 두 번 출력되므로 두 개의 별도 실패로 계산하지 않습니다.

Android ART가 DEX 리소스를 닫는 단계의 실패입니다. 게임 C# 스택이 아니지만 **앱 PID 안에서 발생했으므로 앱과 무관한 다른 시스템 프로세스의 로그로 분류하지 않습니다.** 앞서 SwappyDisplayManager의 InMemoryDexClassLoader가 libgame.so를 찾지 못했다는 기록은 있으나, 이것만으로 해당 DEX의 소유자나 finalizer 실패와의 인과관계를 확정할 수 없습니다. Unity/App UI/Swappy 중 누가 최종 원인인지는 미확인입니다.

기존 실행은 수십 초 이후에도 살아 있었고 Android 종료 이력은 검증을 위한 USER REQUESTED/FORCE STOP뿐입니다. FATAL EXCEPTION, fatal signal, ANR은 관측되지 않았습니다. 원인 검증 없이 frame pacing을 끄거나 라이브러리·Java 클래스를 교체하지 않습니다. 새 APK의 최소 45초 이상 실행과 재실행에서 재발 여부를 관찰합니다.

## 현재 변경과 테스트

- ProjectSettings: Android `APP_UI_EDITOR_ONLY` 심벌 1개 추가.
- `Tests/EditMode/PuzzleAndroidStartupTests.cs`: 초기화 순서 재현 및 Editor-only 어셈블리 제외 검증.
- EditMode asmdef: 위 검사에 필요한 Input System/App UI Editor 참조.
- AgentScripts: 설정 확인·수정 도구, APK 검사 도구의 출력 경로 파라미터.
- 게임 Runtime/Core 코드·90개 스테이지·씬·UI·애니메이션·저장 포맷은 변경하지 않았습니다.

전체 EditMode **68/68 통과**(기존 66개+신규 2개), 전체 PlayMode **37/37 통과**(141.12초), 스킵/실패 0입니다. 처음 재현 테스트의 정리 코드가 가상 터치스크린 제거 콜백을 잘못 호출해 실패했고, 정리 순서를 수정한 뒤 전체가 통과했습니다. 실패 기록도 별도 보관했습니다. 실제 Player에서 예외를 숨기는 처리는 없습니다. `EditModeTests.json`, `PlayModeTests.json`에 실제 결과가 있습니다.

## APK와 바이너리 검증

새 파일: [ColorMovePuzzle-Stabilized-Development.apk](Builds/Android/ColorMovePuzzle-Stabilized-Development.apk), **54,300,631 bytes**, SHA-256 `45D14C1388506EEF7AF90887F94C232B44F6CCDBA408A4A9A1E7C7672C8364D8`.

빌드 성공, 약 184.87초, 오류 0·경고 982개로 이전과 같은 개수입니다. 경고는 기존 AI Inference 셰이더 978개, IL2CPP의 TMP 큰 메서드 컴파일 안내 3개, Diagnostics Symbols 권고 1개입니다. `BuildReport.json`, `BuildSummary.json`, `WarningSummary.json`에 기록했습니다. Unity 버전·Portrait·IL2CPP·ARM64·Development·API 26/36·패키지명과 Android Debug 서명을 유지합니다. APK v2 서명과 ARM64 ELF의 16KiB 정렬도 통과했습니다.

`ConfigurationAfter.json`뿐 아니라 **완성 APK 내부의 ScriptingAssemblies, RuntimeInitializeOnLoads, IL2CPP global-metadata**를 직접 대조했습니다. 이전 APK에는 App UI 메타데이터가 있었고 새 APK에는 Unity.AppUI 어셈블리·런타임 초기화·메타데이터가 없습니다. Unity.InputSystem은 남아 있습니다. `PlayerBinaryProof.json`을 참고하십시오. 단순히 설정 UI만 바뀐 것으로 수정 완료를 판단하지 않았습니다.

## 실기기 업데이트

사용자의 `업데이트 설치 승인` 후 같은 SM-S938N에 **install -r 업데이트 성공**했습니다. uninstall/pm clear는 사용하지 않았습니다. 설치 전에 앱을 종료하고 실제 기기 JSON과 백업을 로컬에 보관했습니다. 처음 해시 명령의 잘못된 인자가 있어 해당 시도에는 해시가 기록되지 않았으며, 설치 전 읽어 둔 UTF-8 텍스트에서 다시 계산한 해시와 설치 후 기기의 sha256sum을 대조했습니다. **4개 파일의 텍스트와 해시가 모두 동일**합니다. `Device/UpdateDataComparison.json`과 `DeviceSavesBeforeUpdate.json`에 내용이 있습니다.

첫 실행의 입력 None 예외는 재발하지 않았고, AssetPackManager 조회 및 Dex finalizer 로그는 남아 있습니다.

### ADB 자동 입력과 실제 화면 검증

`Device/UiVerification.json`은 세 모드의 실제 Android 화면을 캡처해 스테이지 데이터와 픽셀 배치를 대조한 결과입니다. 에이전트의 입력은 ADB `input tap/swipe/keyevent`이며 사람의 손가락 조작감 검증이 아닙니다. 프로젝트/기기의 게임 상태를 직접 주입하는 테스트도 아닙니다.

- 3×3/4×4/5×5 각각 1번 진입 및 실제 시작 배치 일치.
- 인접 빈칸이 하나인 타일의 탭 이동 → 정확히 한 칸 이동한 보드 확인.
- 역방향 스와이프 → 원래 보드로 복원 확인.
- 5×5의 여러 빈칸에 인접한 타일: 탭만으로 움직이지 않음, 지정 방향 스와이프에 따라 정확한 빈칸으로 이동, 역방향으로 복원 확인.
- 최종 실행에서 각 화면의 Android BACK/화면 전환 확인.
- 기존 3×3 마지막 선택 2번을 UI로 다시 선택하여 기존 프로필 값 복원.
- Settings 패널에서 BGM 70%/SFX 80% 표시 확인. 볼륨 자체는 변경하지 않음.
- Continue를 눌러 3×3/2번의 최초 보드와 일치함을 확인.

이미지를 편집하지 않고 PNG를 읽어 비교했습니다. 처음 검사기의 색상 완전 일치는 화면 색 변환의 1~3 채널 차이 때문에 실패해, 타일 색 간격보다 작은 RGB 허용 오차를 적용했습니다. 샘플 스크린샷에서 25셀과 색상을 먼저 확인한 뒤 실제 입력 검증에 사용했습니다.

첫 자동 입력 시도에서는 3×3 이동 검증 뒤 ADB BACK 한 번이 화면을 바꾸지 않았습니다. `Device/UiVerification-KeyBackAttempt.json`과 당시 스크린샷을 보관했습니다. UI 버튼으로 복귀한 후 전체를 다시 실행하자 각 모드·Continue의 BACK 요청이 모두 응답했습니다(최종 `adbBackFailures=[]`). **처음 한 번 무응답한 원인은 미확인이며 반복 검증·실제 제스처 뒤로 가기를 추가로 확인해야 합니다.** 실패를 감추기 위한 게임 코드 변경은 하지 않았습니다.

### 저장 기록 유지와 재실행

자동 입력은 실제 UI를 통해 테스트 스테이지를 열고 이동만 되돌렸으며 새 클리어 기록을 추가하지 않았습니다. 테스트 종료 후 원래 3×3 클리어 1번·마지막 선택 2번과 프로필 Continue 3×3/2번·70%/80%가 설치 전과 같음을 `Device/OriginalDataAfterUi.json`에서 확인했습니다. 원래 없던 4×4/5×5 진행 파일은 각 모드 첫 진입으로 정상 생성됐고 클리어 기록은 없습니다. `.bak`은 게임의 정상 저장 동작에 따라 갱신될 수 있으며 업데이트 전 원본은 로컬 백업에 보관했습니다.

앱을 force-stop한 뒤 PID **7582 → 9785**의 새 프로세스로 cold start했습니다. 이때 존재하는 6개 저장/백업 파일은 기기 sha256sum으로 재실행 전후 **모두 동일**했습니다. `Device/RestartPersistence.json`에 결과를 보관했습니다. 입력 테스트 중 원본 저장 JSON을 직접 덮어쓰지 않았고 uninstall/pm clear는 실행하지 않았습니다.

## 수정 전후 Logcat 최종 비교

각 행은 앱 PID로 수집한 실제 한 번의 실행 구간입니다. 재실행 후 새 PID는 1분 45초 이상 관측했습니다. 원본과 새 버전은 관측 시간·UI 조작이 다르므로 전체 오류 줄 수만으로 품질을 평가하지 않습니다. [LogComparison.json](Artifacts/AndroidStabilization/LogComparison.json)에 집계를 보관했습니다.

| 로그 | 수정 전 첫 실행 / 재실행 | 수정 후 첫 실행 / 재실행 |
|---|---:|---:|
| InputUpdateType.None 예외 | 2 / 2 | 0 / 0 |
| 입력 장치 콜백 오류 | 2 / 2 | 0 / 0 |
| AssetPackManager 조회 | 1 / 1 | 1 / 1 |
| Dex finalizer 출력 줄 | 2 / 2 | 2 / 2 |
| FATAL EXCEPTION·fatal signal·ANR | 0 / 0 | 0 / 0 |

Dex의 두 줄은 동일 예외의 중복 출력이며 별도 실패 두 건으로 세지 않습니다. 새 APK에서도 발생했으므로 App UI 제외가 이 로그까지 해결했다고 판단하지 않습니다. 엔진 내부의 선택적 AssetPack 클래스 조회는 유지하고, Dex를 소유한 로더/네이티브 컴포넌트는 미확정으로 남겼습니다.

## 변경 파일과 남은 확인 사항

- `ProjectSettings/ProjectSettings.asset`: Android 전용 `APP_UI_EDITOR_ONLY` 추가.
- `Assets/ColorMovePuzzle/Tests/EditMode/PuzzleAndroidStartupTests.cs` 및 메타 파일: 초기화 순서 재현·Player 포함 범위 회귀 검사 2개 추가.
- `Assets/ColorMovePuzzle/Tests/EditMode/ColorMovePuzzle.EditModeTests.asmdef`: 위 Editor 테스트용 참조 추가.
- `AgentScripts/InspectStabilization.cs`, `FixAndroidAppUiScope.cs`, `InspectBackApi.cs`, `ExportDeviceModels.cs`, `VerifyStabilizedDevice.py`: 조사·설정 적용·기기 검증 도구 추가.
- `AgentScripts/InspectApk.ps1`: 다른 APK 경로와 보고서 경로를 지정할 수 있도록 확장.
- 이 보고서와 기존 Android/프로젝트 README에 최신 결과 링크 추가.

게임 Runtime/Core·90개 스테이지 데이터·씬을 기존 144개 파일의 SHA-256과 대조했으며 변경하지 않았습니다. 기존 파일 중 변경된 것은 테스트 asmdef뿐입니다. `ContentIntegrity.json`에 대조 결과가 있고, `PackageIntegrity.json`에서 manifest/lock 파일도 백업과 같음을 확인했습니다. 패키지 업그레이드와 Gradle 의존성 추가는 하지 않았습니다.

Unity Editor는 Play 중지·컴파일 완료·runInBackground=false 상태로 복원했습니다. Inspector 수동 설정은 필요하지 않습니다. 실기기의 사람 손가락 탭·스와이프 조작감과 제스처 뒤로 가기는 아직 검증하지 않았습니다. 세 모드 플레이와 반복 뒤로 가기를 직접 확인해 주십시오. 자동 검증 첫 시도의 BACK 한 번 무응답 및 Dex finalizer의 정확한 소유 원인은 후속 조사 대상으로 남습니다. 현재 퍼즐 상태를 저장하지 않고 Continue 시 스테이지 최초 배치로 시작하는 기존 동작은 유지했습니다.
