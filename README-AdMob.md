# ColorMovePuzzle — Google AdMob 테스트 광고 연동

이 문서는 9차 구현·검증 기록입니다. 후속 운영 설정 분리·출시 차단·EEA 샘플 UMP 실제 양식 검증과 최신 개발 APK는 **[10차 출시 준비 보고서](README-Release.md)**에 있습니다. 운영 광고 활성화와 스토어 업로드는 하지 않았습니다.

## SDK와 기존 환경

Google Mobile Ads Unity Plugin **11.6.0**을 OpenUPM scoped registry와 Unity Package Manager/Pipeline으로 설치했습니다. 공식 안정 릴리스(2026-10-13, prerelease=false)를 확인했고, 필수 의존성 Unity External Dependency Manager **2.1.0**이 함께 설치됐습니다. Android 선언은 Mobile Ads **25.5.0**, UMP **4.0.0**입니다. [공식 릴리스](https://github.com/googleads/googleads-mobile-unity/releases/tag/v11.6.0), [공식 설치 문서](https://developers.google.com/admob/unity/quick-start).

Unity 6000.6.5f1, Portrait, IL2CPP, ARM64, Input System 1.20.0, Android `APP_UI_EDITOR_ONLY`를 유지했습니다. Player 컴파일 목록에서 Unity.AppUI가 제외된 것도 확인했습니다. 다른 기존 UPM 패키지를 업그레이드하지 않았습니다. 작업 전 Assets/Packages/ProjectSettings/AgentScripts를 `Artifacts/AdMob/Backup/ProjectBefore.zip`에 백업했습니다. 기존 Git 인덱스나 저장 데이터를 초기화하지 않았습니다.

설치 중 Unity의 OpenUPM unsigned package 안내를 닫았고, Android Auto-Resolution 대화상자로 Pipeline이 일시적으로 차단됐습니다. Windows 전경 제한 때문에 자동 클릭을 안전하게 중단한 뒤 사용자가 Resolve를 눌렀습니다. 이후 실제 SDK 설정·의존성 템플릿과 Editor 연결을 확인했습니다. 전역 서명 검사를 끄지 않았습니다.

## 테스트 ID와 출시 차단

| 용도 | 현재 값 |
|---|---|
| Android App ID | `ca-app-pub-3940256099942544~3347511713` |
| 전면 광고 단위 | `ca-app-pub-3940256099942544/1033173712` |
| 보상형 광고 단위 | `ca-app-pub-3940256099942544/5224354917` |

Google 공식 Android 테스트 ID만 사용합니다. 실제 계정/운영 ID는 생성하거나 추측하지 않았습니다. [공식 테스트 광고 표](https://developers.google.com/admob/unity/test-ads).

`Data/AdConfig.asset`은 테스트 모드와 별도의 빈 운영 ID 필드를 관리합니다. App ID는 SDK의 `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`에도 설정합니다. `AdMobBuildValidation`은 두 App ID의 일치, Android 안정화 define, Development/Release 설정을 검사합니다. Development에서 운영 ID 선택은 차단합니다. 현재 테스트 ID가 남은 Release 빌드를 실제 시도하여 **Release build blocked: official TEST ads are still selected.** 오류로 APK 생성이 차단되는 것을 확인했습니다(`ExpectedReleaseBlocked.json`). 테스트 ID를 지우기만 한 미설정 운영 값도 허용하지 않습니다. 운영 설정은 개인정보·대상 연령 검토 플래그와 유효한 운영 App/전면 광고 ID가 모두 있어야 통과합니다.

## 전면 광고 표시 조건

광고 표시 진입점은 **새 클리어 결과 화면의 NEXT 버튼** 하나입니다. 먼저 다음 퍼즐을 열어 놓고 광고를 띄우지 않습니다. 표시가 끝나거나 실패한 뒤 다음 스테이지를 엽니다. 스테이지 선택 카드, Continue, Restart, Settings, Android Back, 앱 실행/메뉴 진입, 복원, Pause/Focus 상실은 광고 표시 진입점이 아닙니다. 마지막 스테이지에서 NEXT가 없으면 이 위치의 전면 광고도 없습니다. [Google Play 광고 정책](https://support.google.com/googleplay/android-developer/answer/9857753).

세 모드에 공유하는 기본 조건은 다음과 같습니다.

- 누적 클리어 **5회 이상** 및 누적 실제 플레이 시간 **180초 이상**이어야 첫 광고를 표시할 수 있습니다.
- 광고를 실제로 연 뒤 새 클리어 **4회 이상**과 실제 플레이 시간 **180초 이상**을 모두 만족해야 다음 광고가 가능합니다.
- 시간은 퍼즐 플레이 중, 전경 상태, 광고/동의 양식이 없고 클리어 전일 때만 누적합니다. 메뉴·설정·클리어 결과·백그라운드·광고 시간은 세지 않아 벽시계 기준보다 보수적입니다. 테스트는 제어 가능한 시간 제공자를 사용합니다.
- 처음에는 기존 세 모드의 클리어 개수를 읽기 전용으로 합산합니다. 이후 정상적인 재플레이 완료도 한 클리어로 셉니다. 각 실행/Restart에 부여된 run ID로 같은 완료 이벤트를 중복 계산하지 않습니다. 복원 직후 이미 목표인 보드를 처리하는 이벤트는 광고 기회로 만들지 않습니다.
- SDK 초기화 완료, UMP `CanRequestAds()`, 유효하고 준비된 광고, 전경 상태, 현재 클리어/애니메이션 완료 여부를 표시 직전에 다시 검사합니다.
- 준비되지 않았으면 그 기회를 소비하고 **즉시 다음 스테이지로 진행**합니다. 늦은 로드 완료가 플레이 중 광고를 띄우지 않습니다.

별도의 `ads-frequency-v1.json`(+tmp/bak)에 카운터·최근 중복 ID·실제 플레이 시간을 저장합니다. 완료/광고 열림/Pause/Focus 상실/Quit 및 30초 간격 체크포인트에서 저장하며 기존 진행·프로필·퍼즐 파일과 분리합니다. 강제 종료 시 마지막 시간 체크포인트 이후 최대 약 30초가 덜 반영될 수 있어 광고가 더 늦어지는 방향입니다.

표시 직전에는 `pendingShow` 예약을 먼저 안전하게 저장합니다. 저장 실패 시 광고 없이 진행하고, 표시 실패 시 예약을 취소합니다. 실제 SDK 열림 콜백에서 쿨다운을 확정합니다. 네이티브 광고가 열린 직후 Unity 콜백 전에 프로세스가 종료되면, 다음 실행에서는 저장된 예약을 기준으로 보수적으로 쿨다운을 적용합니다. 이 예약은 재실행 직후 반복 노출을 막기 위한 장치이며 수익이나 실제 노출 횟수 집계가 아닙니다. 기존 버전 1 광고 빈도 파일에 선택 필드를 추가했으며 퍼즐 진행 파일은 변경하지 않았습니다.

로드는 사전 준비하며 실패 재시도는 30→60→120초로 지연하고 기본 최대 3회(최초 요청 포함 총 4회)로 제한합니다. 30초 로드 시간 초과와 오래된 광고 폐기도 처리합니다. 광고 표시 실패·예외·종료의 continuation은 한 번만 실행하고, 중복 열림/종료 콜백을 차단합니다. 광고 중 투명 UI 차단 영역과 탐색 가드를 함께 사용합니다. 백그라운드에서 닫힘을 받으면 전경 복귀 후 전환을 마칩니다. SDK 어댑터는 공식 Unity update executor로 콜백을 보내 Unity 오브젝트 변경을 메인 스레드에서 수행합니다. [공식 콜백 동기화 지침](https://developers.google.com/admob/unity/global-settings).

## UMP와 보상형 기반

앱 실행마다 UMP consent information update를 한 번 요청합니다. 최신 상태에 따라 필요한 양식은 메인 메뉴/Settings에서만 표시하고, 필요 없는 경우의 공식 form check는 늦게 응답해도 퍼즐 플레이를 가로막지 않습니다. 요청 허용 여부는 항상 SDK의 `CanRequestAds()`를 사용합니다. 앱 자체의 저장 동의 문자열이나 가짜 동의 상태로 대체하지 않습니다. 갱신 실패 시에도 공식 지침대로 SDK가 이전 상태에 근거해 허용하는지 확인하며, 허용하지 않으면 초기화·광고 요청을 하지 않습니다. [공식 UMP 문서](https://developers.google.com/admob/unity/privacy).

Settings의 **Privacy Options** 버튼은 UMP가 진입점을 요구할 때만 표시·활성화합니다. 재표시 중 준비된 광고를 폐기하고, 종료 후 새 SDK 허용 상태로 로드를 판단합니다. Google Mobile Ads 공개 초기화는 프로세스에서 한 번만 수행합니다. 초기화/동의/로드 실패로 게임을 기다리게 하지 않습니다.

보상형은 `IAdRewardConsumer`/`RewardedAdController`만 준비했습니다. 의미 있는 실제 보상 공급자와 사용자 opt-in이 모두 있어야 로드/표시할 수 있고 SDK earned 콜백에서만 한 번 지급합니다. 현재 게임에는 보상 공급자·힌트 버튼이 없어서 보상형 광고는 요청/노출하지 않습니다. 힌트 알고리즘도 추가하지 않았습니다.

## 변경 파일과 UI

- `Core/AdFrequencyController.cs`: 모드 통합 카운터와 실제 플레이 시간 규칙.
- `Runtime/Ads/AdConfig.cs`, `AdContracts.cs`, `AdFrequencyStore.cs`.
- `Runtime/Ads/GoogleAdBackend.cs`, `AdConsentController.cs`, `InterstitialAdController.cs`, `RewardedAdController.cs`, `AdManager.cs`.
- `Runtime/PuzzleCampaignController.cs`: 완료 이벤트, run ID, 안전한 NEXT continuation.
- `Runtime/PuzzleMenuController.cs`: 광고/양식 중 탐색 가드.
- `Editor/AdMobBuildValidation.cs`, `PuzzleAdMobSceneSetup.cs`.
- `Tests/EditMode/AdSystemTests.cs`, `Tests/PlayMode/AdFlowPlayTests.cs`.
- 기존 `PuzzleSessionStoreTests.cs`: 실패 시 저장 오류를 알 수 있도록 assertion 메시지만 보강.
- `Packages/manifest.json`, lock 파일, Google SDK 설정, EDM이 생성한 Android Gradle 템플릿/Resolver 설정.
- 기존 `Scenes/ColorMovePuzzle.unity`: 기존 시스템 오브젝트에 AdManager 추가, Settings에 조건부 Privacy Options, Canvas에 숨겨진 AdInputBlocker 추가 및 참조 연결. 기존 UI 위치·색·애니메이션을 재작성하지 않았습니다.
- 조사/설정/실기기 검증용 `AgentScripts`와 이 보고서.

기존 데이터 파일 193개의 SHA-256이 모두 동일합니다(`DataIntegrity.json`). 보드 이동 로직과 스테이지/난이도 데이터, 기존 저장 포맷을 변경하지 않았습니다. Inspector 수동 연결은 필요하지 않습니다.

## 테스트·빌드·실기기 기록

최종 코드에서 **EditMode 105/105, PlayMode 60/60**(171.82초)이 통과했습니다. 기존 86/46개와 신규 광고 19/14개를 포함하며 실패·스킵은 0입니다. 결과는 [EditModeTests.json](Artifacts/AdMob/EditModeTests.json), [PlayModeTests.json](Artifacts/AdMob/PlayModeTests.json)에 있습니다. 빈도·중복·요청 동의 제한·실패 시 게임 진행·전환·저장 유지·표시 예약 복구는 Mock과 제어 가능한 시계로 검증했습니다. 실제 180초를 기다리는 단위 테스트는 작성하지 않았습니다.

추가 회귀 중 기존 100회 연속 저장 테스트가 한 번 실패했으며 상세 오류가 원래 assertion에 없었습니다. 진단 메시지만 보강한 단독 재실행과 이후 전체 재실행은 통과했습니다. 원인은 재현되지 않아 확정하지 않으며 게임 저장 코드를 임의로 바꾸지 않았습니다. 실패 결과는 `EditMode-WriteFailure.json`에 보관합니다.

중간 전체 실행에서 Mock 광고가 열리지 않은 상태의 콜백을 호출한 테스트가 한 번 실패했습니다. 실제 Editor 포커스라는 외부 상태를 가정했던 테스트에 전경 상태 설정과 표시 시작 assertion을 추가했고 전체를 다시 통과했습니다. 실제 광고 표시 조건을 낮추지 않았습니다(`PlayMode-ForegroundDiagnostic.json`).

Development APK 빌드 중 Google SDK 전처리의 `ResolveSync(true)` 단계에 Android Auto-Resolution 확인 창이 떠 있는 상태로 약 8분간 로그가 진행되지 않았습니다. Editor 창 자체 캡처도 응답하지 않았습니다. 사용자가 판단을 위임한 뒤 현재 파일을 추가 백업하고 해당 Editor만 종료·재실행했습니다. 공식 Resolver의 `promptBeforeAutoResolution=false`를 저장했고 `enableAutoResolution=true`, `autoResolveOnBuild=true`는 유지했습니다. 이후 빌드가 실제 IL2CPP/Gradle 단계를 통과했습니다. SDK/패키지 캐시 코드를 패치하거나 의존성 해결 자체를 생략하지 않았습니다. 일회성 Editor 복구 도구는 `AdMobResolverRecovery.cs`이며 요청 마커가 없으면 아무 작업도 하지 않습니다.

첫 AdMob APK를 승인 후 업데이트 설치하여 저장 파일 해시 보존과 메뉴 실행을 확인했습니다. UMP 네트워크 응답이 왔는데 Unity 콜백이 실행되지 않는 문제를 발견했습니다. 공식 SDK 소스에서 이벤트 실행기는 큐에 넣기만 하며 먼저 `Initialize()`해야 Update가 동작함을 확인했습니다. SDK 초기화는 UMP 뒤에 기다리도록 했기 때문에 실행기가 아직 없던 초기 순서가 원인이었습니다. `GoogleAdBackend` 생성 시 **전달용 실행기만** 준비하도록 수정했습니다. 이것은 광고 초기화나 광고 요청, 동의 우회가 아닙니다. 실제 SDK 실행기로 초기화 이전 큐 전달을 확인하는 네트워크 없는 PlayMode 검사를 추가했습니다. [SDK 실행기 소스](https://github.com/googleads/googleads-mobile-unity/blob/v11.6.0/source/plugin/Assets/GoogleMobileAds/Common/MobileAdsEventExecutor.cs).

이 수정 후 실제 기기에서 UMP 확인 → SDK 초기화 → 테스트 전면 광고 로드가 정상 동작했습니다. 이후 표시 예약 복구 테스트까지 포함해 위의 최종 105/60개 결과를 얻었습니다. 앞선 결과와 실제 기기에서 발견한 초기 순서 문제를 최종 통과 결과와 구분해 보관합니다.

최종 APK는 [ColorMovePuzzle-AdMob-Test-Development.apk](Builds/Android/ColorMovePuzzle-AdMob-Test-Development.apk)입니다. Unity 6000.6.5f1 / Android Portrait / IL2CPP / ARM64 / Development로 **53.852초, 오류 0개·경고 5개**에 빌드했습니다. 파일은 70,935,301 bytes, SHA-256은 `AB39C3177CDECA2B3B1070B7C61987F43369D22C0BB4570D839D72838BAED45B`입니다. 경고는 TMP 큰 메서드 컴파일 안내 3개, 디버그 심볼 권고 1개, 공식 SDK의 iOS xcframework Android 비지원 importer 경고 1개입니다. 이를 숨기기 위해 SDK 캐시를 수정하지 않았습니다.

APK 서명, ARM64 전용 및 네이티브 ELF의 16KB 페이지 호환성, 실제 Google Ads/UMP/Unity 전면 광고 브리지 DEX 클래스 포함을 확인했습니다. Unity.AppUI의 Player 메타데이터 참조는 0입니다. 사용자가 승인한 업데이트 설치는 성공했고 설치 직후 기존 저장 파일 해시가 동일했습니다. 2026-10-28 재개 시에도 실기기의 설치 APK 해시가 최종 파일과 일치하여 재설치·재빌드·자동 테스트를 반복하지 않았습니다. 사용자가 그사이 진행한 5×5 2번 스테이지·2회 이동 상태를 `Device/BeforeResume20261028/`에 읽기 전용으로 새로 백업했습니다.

## 실제 Android 광고·복원 검증 — 2026-10-28

Samsung SM-S938N / Android 16 / USB ADB에서 다음을 직접 실행했습니다. **ADB 자동 입력 검증이며 사람 손가락의 조작감 검증은 아닙니다.** APK 재설치나 게임 코드 수정 없이 남은 검증을 이어서 수행했습니다.

| 확인 항목 | 실제 결과 |
|---|---|
| 기존 Continue | 5×5 2번, 저장된 배치와 이동 횟수 2회 정확히 복원 |
| 5×5 입력 | 유효 스와이프와 역방향 이동 확인, 원래 배치 유지·이동 횟수는 정상적으로 4회 |
| 4×4 입력 | 유효 스와이프와 역방향 이동 확인 |
| 3×3 입력·클리어 | 실제 입력으로 1번 재플레이·클리어 |
| 광고 표시 전 조건 | 누적 클리어 7회, 실제 플레이 시간 867.7895초, 이전 광고 없음. 제한값·저장 파일 조작 없이 충족 |
| 표시 지점 | 클리어 결과 NEXT에서 실제 Google 테스트 전면 광고 표시, 그 전에는 표시 없음 |
| 광고 닫기 | 화면에 나타난 네이티브 X 버튼으로 종료 |
| SDK 이벤트 | 열림·종료 각 1회, 저장된 표시 예약 해제 및 쿨다운 확정 |
| 다음 스테이지 | 3×3 2번 최초 배치로 정확히 한 번 전환 |
| 프로세스 종료·재실행 | PID 24509 → 26252, `am force-stop` 후 새 프로세스 실행 |
| Continue 복원 | 3×3 2번에서 이동 1회 후 종료, 동일한 9개 셀과 이동 횟수 1회 복원 |
| 기존 기록·설정 | 기존 세 모드 클리어 기록 모두 유지, BGM 0.7·효과음 0.8 유지 |
| 광고 빈도 저장 | 재실행 후 hasShown=true, pendingShow=false 유지 |
| Android BACK | 퍼즐 → 선택은 1회 무응답으로 STAGES 버튼 사용, 선택 → 메인은 정상 |

실제 광고 화면은 [test-interstitial.png](Artifacts/AdMob/Device/test-interstitial.png), 닫기 버튼 화면은 [ad-close-current.png](Artifacts/AdMob/Device/ad-close-current.png), 최종 결과는 [Verification.json](Artifacts/AdMob/Device/Verification.json)에 있습니다. 테스트 후 마지막 Continue는 검증에 사용한 **3×3 2번·1회 이동**입니다. 기존 5×5 2번의 배치는 그대로 보존돼 있고 테스트 이동·역이동 두 번이 이동 횟수에 정상 반영됐습니다. 클리어 기록을 삭제하거나 `uninstall`, `pm clear`, 앱 저장 파일 직접 쓰기를 사용하지 않았습니다.

첫 자동 닫기 시도는 닫기 버튼이 나타나기 전 BACK을 전송한 뒤 보드를 너무 일찍 확인하여 실패했습니다. 이는 `ExerciseInitialCloseAttempt.json`에 보관했습니다. 광고가 열린 상태를 유지한 채 버튼을 시각적으로 확인하고 X로 종료한 뒤, 이미 끝난 표시·클리어를 반복하지 않고 복원 검증만 이어갔습니다. 최종 Logcat 대기 중 호스트 연결이 중단돼 저장된 화면·종료 직전 스냅샷·현재 실기기 상태를 읽기 전용으로 대조하여 보고서를 마쳤습니다.

UMP는 이번 실제 지역에서 갱신·form check를 완료하고 광고 요청을 허용했습니다. 강제로 지역·동의 상태를 조작하지 않았습니다. **동의가 필요한 지역의 실제 양식·거절·Privacy Options 재선택은 검증하지 못했습니다.** 실제 AdMob 콘솔 메시지 설정도 수행하지 않았습니다. 인터넷 차단·광고 재고 없음·표시 실패·광고 중 백그라운드 전환은 Mock 자동 테스트 범위이며 이번 실기기에서 각각 재현한 결과는 아닙니다. 보상형은 사용자에게 노출하지 않았습니다.

## 남은 로그와 제한

원본 PID별 로그는 `Device/LogcatFirst.txt`, `LogcatRelaunch.txt`, 집계는 [LogSummary.json](Artifacts/AdMob/Device/LogSummary.json)에 있습니다. 기존 AssetPackManager 선택적 클래스 조회와 Dex finalizer 로그는 다시 관측됐습니다. 기존과 같은 로그를 제거하기 위한 엔진·Gradle 변경은 하지 않았으며 Dex 로더 소유 원인은 여전히 미확정입니다. BACK 무응답도 해결됐다고 판단하지 않습니다.

| PID별 로그 항목 | 최초 실행 24509 | 재실행 26252 |
|---|---:|---:|
| InputUpdateType.None | 0 | 0 |
| AssetPackManager 선택적 조회 | 1 | 1 |
| Dex finalizer 메시지 줄 | 2 | 2 |
| FATAL EXCEPTION / fatal signal / ANR | 0 / 0 / 0 | 0 / 0 / 0 |
| SDK 초기화 / 테스트 광고 로드 | 1 / 2 | 1 / 1 |
| 테스트 광고 열림 / 종료 | 1 / 1 | 0 / 0 |
| FilePhenotypeFlags 진단 | 4 | 4 |

Dex 두 줄은 동일 스택의 중복 출력으로, 기존 검증과 같은 수준입니다. 초기 실행에서 광고를 닫은 뒤 한 번 다시 로드됐고, 재실행 후 메뉴/Continue에서 추가 노출은 없었습니다. 종료 이력도 테스트의 USER REQUESTED/FORCE STOP을 확인했으며, 이번 관측 구간에서 크래시·ANR 종료는 없습니다(`Device/ExitInfo.txt`). 이 결과로 모든 기기에서 오류가 없다고 보장하지는 않습니다.

광고 SDK 추가 후 Google 내부 `FilePhenotypeFlags`의 선언 등록 진단이 출력됐습니다. 광고 로드·표시·닫기와 게임 복원은 완료됐으나 이 진단의 내부 원인은 확정하지 않아 SDK 내부 설정을 임의로 수정하지 않았습니다.

또한 SDK 진단의 I/Unity payload에서 `Could not load the file 'GoogleMobileAdsNative.Common'`이 관측됐습니다. **공식 11.6.0의 `MobileAdsClient.onInitializationComplete`는 초기화 콜백을 먼저 실행한 뒤 선택적 Native 플러그인의 버전 확인을 시도하고, 없으면 catch하여 `ReportTrappedException`에 전달합니다.** 실제 로그의 호출 경로와 일치하며 전면 광고에 필요한 Android 브리지 누락과는 다릅니다. [해당 버전 공식 소스](https://github.com/googleads/googleads-mobile-unity/blob/v11.6.0/source/plugin/Assets/GoogleMobileAds/Platforms/Android/MobileAdsClient.cs). 이 기록을 미처리 게임 예외나 광고 초기화 실패로 집계하지 않았고, 사용하지 않는 Native 광고 플러그인을 추가하지 않았습니다. SDK 자체 진단 payload에는 기기 정보가 포함되므로 원본 로그를 외부에 공유할 때 검토해야 합니다. 자체 광고 이벤트 로그에는 광고 ID나 사용자 식별자를 출력하지 않습니다.

현재 결과는 테스트 광고 개발 빌드의 검증이며 운영 수익화·스토어 출시 승인을 의미하지 않습니다. 사람의 실제 터치·제스처 조작감, 필요한 지역의 UMP 양식, Release AAB·출시 서명 검증은 남아 있습니다.

최종 Unity CLI/Pipeline 확인에서 C# 컴파일 실패 false, Console 오류 0, Play 중지, 게임 씬 저장 완료(`sceneDirty=false`), `runInBackground=false`를 확인했습니다. 이번 재개에서는 게임 소스·패키지·씬을 다시 변경하지 않았고, 검증 도구·결과와 README만 갱신했습니다. 따라서 이미 통과한 최종 자동 테스트와 빌드를 반복하지 않았습니다.

## 사용자가 직접 수행할 출시 작업

현재 APK는 공식 테스트 광고용 개발 빌드이며 실제 수익화 설정은 완료하지 않았습니다.

1. AdMob 계정에서 Android 앱 `com.colormovepuzzle.game`을 등록하고 실제 App ID와 전면 광고 단위를 발급합니다. 향후 실제 보상이 구현될 때 보상형 단위를 별도로 만듭니다.
2. AdMob Privacy & messaging에서 대상 지역에 맞는 메시지를 작성·게시하고 실제 App ID와 연결합니다. 테스트용 기기에서 필요한 지역의 동의 양식·철회·재선택을 검증합니다. 계정 설정을 이 작업에서 임의로 완료했다고 간주하지 마십시오.
3. 개인정보 처리방침 URL을 준비하고 앱/스토어/메시지 설정에 반영합니다. 대상 연령과 아동 대상 여부, UMP under-age 및 광고 요청의 아동/콘텐츠 등급 설정을 확정합니다. 현재 검토 플래그는 미완료 상태입니다.
4. 포함 SDK와 실제 앱 사용에 맞춰 Google Play Data safety, 광고 포함 여부, 광고 ID 사용 관련 신고를 작성합니다. [Google SDK 데이터 공개 지침](https://developers.google.com/admob/unity/privacy/play-data-disclosure)을 확인해야 하며 SDK 추가만으로 신고가 완료되지는 않습니다.
5. 운영 전환을 별도로 리뷰해 실제 App/광고 ID를 AdConfig와 Google SDK 설정에 맞춰 넣고 테스트 선택을 해제합니다. 테스트 ID가 남아 있으면 현재 출시 빌드 검증이 차단합니다. 운영 ID로 시험할 때에는 테스트 기기 등록과 정책 검토를 먼저 수행합니다.
6. 출시용 서명·Play App Signing과 Release AAB를 준비하고 내부 테스트 트랙에서 동의·광고·저장·복귀 흐름을 재검증합니다. 이번 작업은 운영 광고 요청·실제 수익 발생·스토어 업로드를 수행하지 않습니다.
