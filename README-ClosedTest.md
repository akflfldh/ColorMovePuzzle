# Google Play 비공개 테스트 AAB — 2026-10-28

업로드 키 생성에 대한 사용자 승인 후 서명된 Release AAB를 만들고 로컬 검증을 완료했습니다. Google Play 업로드·테스트 버전 게시·기존 스마트폰 앱 설치/삭제/데이터 초기화는 수행하지 않았습니다.

## 최종 파일

| 항목 | 결과 |
|---|---|
| AAB | `C:\Users\dongd\ColorMovePuzzle\Builds\Android\ColorMovePuzzle-ClosedTest-v1.0-code1.aab` |
| 파일 크기 | 45,367,787 bytes (45.37 MB / 43.27 MiB) |
| SHA-256 | `D7E2BF9BD852B5E5D11B4A27A771547400660A2EBED93C3D0D08BC62C55662AC` |
| Package Name | `com.colormovepuzzle.game` |
| Version Name / Code | `1.0` / `1` (사용자가 첫 업로드임을 확인) |
| Unity / 플랫폼 | 6000.6.5f1 / Android |
| Target / Minimum API | 36 / 26 |
| 스크립트 / CPU / 방향 | IL2CPP / ARM64 only / Portrait |
| 빌드 | Release managed variant, Development=false, debuggable=false, profiler/script debugging 없음 |
| 채널 | `ClosedTestNoAds` |
| 광고 | 앱의 광고 SDK 초기화·전면/보상형 요청·표시 차단 |
| UMP | 사용자 제공 실제 App ID로 동의 갱신/양식/Settings Privacy Options 유지 |

[서명된 최종 AAB](Builds/Android/ColorMovePuzzle-ClosedTest-v1.0-code1.aab)를 사용하십시오. `Builds/Android/Intermediate/`의 unsigned 파일은 업로드 대상이 아닙니다.

## 광고와 개인정보 처리

`Assets/ColorMovePuzzle/Editor/ClosedTestReleaseSettings.asset`의 `consentAppId`에 `ca-app-pub-9556737105305150~6563007441`을 저장했습니다. 이 값은 App ID이며 광고 단위 ID가 아닙니다. 실제 광고 단위 ID는 추가하지 않았습니다.

전용 Editor 메뉴 `Color Move Puzzle → Closed Test → Build Release AAB (No Ads)`가 해당 빌드에만 `COLORMOVE_CLOSED_TEST_NO_ADS`를 전달합니다. AdManager는 UMP를 계속 실행하고 광고 빈도 저장 파일을 읽거나 수정하지 않습니다. GoogleAdBackend의 명시적인 MobileAds.Initialize/InterstitialAd.Load/RewardedAd.Load 호출은 이 채널의 컴파일 출력에서 제거됐습니다. 광고가 없어도 클리어·다음 스테이지·Restart·Continue가 진행됩니다.

빌드 완료 후 원래 Development APK 토글과 SDK 소스 App ID가 복원됐습니다. 전역 define에는 비공개 테스트 define이 남지 않았고 Editor 채널은 Standard입니다. 일반 운영 Release는 기존 테스트 ID 선택 상태에서 계속 차단됩니다. 사용자 제공 App ID가 일반 운영 광고를 활성화하지 않습니다.

Google Mobile Ads Unity Plugin 11.6.0 및 UMP SDK는 그대로 포함됩니다. `MobileAdsInitProvider`, AD_ID/AdServices/인터넷 권한도 Manifest에 남아 있습니다. UMP는 네트워크 통신을 할 수 있고 기존 Unity Diagnostics 설정도 유지됩니다. 따라서 광고 요청 차단만으로 '개인정보 수집 없음' 또는 'SDK 제거'라고 단정하지 않습니다. [Google SDK 데이터 공개 문서](https://developers.google.com/admob/unity/privacy/play-data-disclosure), [Google Play 앱 콘텐츠 신고 안내](https://support.google.com/googleplay/android-developer/answer/9859455)를 기준으로 현재 앱 전체와 배포 트랙의 실제 동작에 맞게 개인정보 처리방침·Data Safety·Advertising ID·광고 포함 여부를 검토해야 합니다. 기존 Console 신고는 자동 변경하지 않았습니다.

사용자가 게시했다고 알려 준 GDPR/미국 개인정보 메시지를 콘솔에서 변경하지 않았습니다. UMP와 Privacy Options의 코드 및 컴파일 결과는 확인했고 Mock PlayMode 테스트도 통과했습니다. 이번 서명 AAB를 기기에 설치해 게시된 실제 동의 양식을 다시 확인한 것은 아닙니다.

## 서명과 키 보관

- Keystore: `C:\Users\dongd\UnitySigning\ColorMovePuzzle\colormove-upload.keystore`.
- PKCS12 / RSA 3072 / SHA256withRSA / alias `colormove-upload`.
- 인증서: `CN=ColorMovePuzzle Upload`, 2026-10-28 ~ 2054-03-15.
- SHA-256: `3C:E1:68:06:B2:C0:3C:99:0E:19:F1:B7:81:31:3A:48:60:A4:BB:77:B8:35:A9:DF:E4:6E:58:35:4B:5C:C3:E5`.
- SHA-1: `38:DF:98:2A:E9:01:27:5F:F8:62:66:71:01:82:6A:43:4A:05:E5:80`.
- 공개 인증서: [UploadCertificate.pem](Artifacts/ClosedTest/UploadCertificate.pem).

생성 전 스크립트를 검토했습니다. 로컬 keytool만 실행하며 외부 전송과 비밀번호 출력은 없습니다. 기존 키/암호/인증서/메타데이터가 있으면 중단합니다. 비밀번호는 난수로 생성해 프로젝트 밖 DPAPI 파일에 보관하며, Unity/Gradle/명령줄/Git에 평문을 저장하지 않았습니다. Windows 실행 정책에 막힌 최초 호출은 키 생성 전에 중단됐고, 이후 해당 PowerShell 프로세스에만 Bypass를 적용해 실행했습니다. 시스템 실행 정책은 바꾸지 않았습니다.

**사용자에게 남은 보관 작업:** keystore를 암호화한 별도 저장소/오프라인 매체에 백업하고, 비밀번호를 별도의 암호 관리자에 기록하십시오. DPAPI만으로 다른 PC의 복구가 보장되지는 않습니다. 로컬에서만 비밀번호를 확인하는 명령과 다른 PC의 복구·재서명 절차는 [키 보관 및 재사용 안내](Docs/Release/ClosedTestSigning.md)에 있습니다. 이 백업 작업은 아직 사용자가 수행해야 합니다. 비밀번호를 채팅으로 보내지 마십시오.

Play App Signing에서 Google이 배포 APK에 사용하는 앱 서명 키와, 이번 AAB를 서명한 업로드 키의 역할은 다릅니다. 앞으로도 같은 업로드 키를 재사용하고 Version Code를 올려 빌드합니다. [Android 공식 서명 문서](https://developer.android.com/studio/publish/app-signing).

## 실제 수행한 검증

| 검사 | 결과 / 기록 |
|---|---|
| EditMode 전체 | 117/117 통과, 실패·스킵 0, 3.57초 — [결과](Artifacts/ClosedTest/EditModeTests.json) |
| PlayMode 전체 | 62/62 통과, 실패·스킵 0, 177.32초 — [결과](Artifacts/ClosedTest/PlayModeTests.json) |
| 새 광고 차단 PlayMode | UMP/Privacy Options 유지, SDK 광고 init/load/show 0회, 세 모드 클리어→다음 스테이지·Restart·Continue 및 광고 카운터 파일 보존 |
| Release 빌드 | 성공, 486.19초, 오류 0, 경고 980 — [BuildReport](Artifacts/ClosedTest/BuildReport.json) |
| Bundletool | unsigned 및 signed AAB validate 성공 |
| Manifest | 패키지/버전/API/Portrait/소유자 UMP App ID/비디버그 확인 |
| IL2CPP 실제 출력 | 광고 요청 코드 제거, UMP와 Privacy Options 유지, 개발용 UMP 강제 지역/초기화 기능 없음 — [기록](Artifacts/ClosedTest/CompiledAdPolicy.txt) |
| 네이티브 | ARM64만 포함, ELF LOAD 정렬 16KB 이상 |
| 서명 | jarsigner `jar verified`, 공개 업로드 인증서와 일치, 서명 전후 앱 payload 동일 — [종합 검증](Artifacts/ClosedTest/SignedVerification.json) |
| 기존 데이터 보존 | 스테이지 관련 193개 파일 해시 일치. 씬·핵심 로직·스테이지·패키지·SDK 소스 설정 유지 — [무결성](Artifacts/ClosedTest/Integrity.json) |
| Editor 최종 상태 | 원래 게임 씬이 저장된 채 정지, 컴파일 중 아님, 일반 운영 안전장치 유지, 비밀번호 설정 없음 — [확인](Artifacts/ClosedTest/EditorFinalState.json) |

첫 빌드에서는 기존 Target API가 Auto(0)로 되어 있어 안전 검사에서 중단됐습니다. 요청한 API 36으로 고정한 뒤 빌드에 성공했습니다. 해당 실패 기록도 보존했습니다. `WritePlayDependencies`에 긴 대기가 있었지만 강제 종료나 엔진 변경 없이 완료됐으며, 대기 원인은 확정하지 않았습니다.

경고 980개의 분류: 기존 Inference/Sentis 셰이더 970, DEVELOPMENT_BUILD 조건의 Unity UAC0009 4, TMP의 큰 메서드 C++ 분리 3, 성능 테스트 리소스 재작성 1, Diagnostics 심볼 권고 1, SDK의 Android 비지원 iOS 플러그인 경고 1. 관련 패키지/디자인/게임 데이터를 바꾸지 않았습니다. JDK는 자체 서명 인증서·신뢰 체인·타임스탬프 부재 경고를 냈습니다. 서명 자체는 검증됐으며 인증서 만료일도 기록했습니다. [JDK 검증 로그](Artifacts/ClosedTest/JarsignerVerification.txt).

## 사용자가 직접 Google Play에서 진행할 순서

1. 먼저 위 키/비밀번호를 별도로 안전하게 백업합니다.
2. Play Console의 해당 앱에서 패키지가 `com.colormovepuzzle.game`인지 확인하고 Play App Signing을 설정합니다. 일반적인 Google 관리 앱 서명 키를 선택하면 이번 인증서는 업로드 인증서 역할을 합니다. 기존 콘솔에 키가 이미 지정돼 있다면 업로드 인증서 지문을 비교하십시오.
3. 테스트 및 출시 → 테스트 → 비공개 테스트에서 트랙을 선택하고 새 버전을 만듭니다. 이메일/Google 그룹 등 테스터와 배포 국가 설정을 확인합니다. [공식 테스트 트랙 안내](https://support.google.com/googleplay/android-developer/answer/9845334).
4. **최종 서명된** `ColorMovePuzzle-ClosedTest-v1.0-code1.aab`를 첨부하고 표시되는 패키지·Version Code 1·서명 정보를 확인합니다. 이미 Version Code 1을 사용했다면 올린 코드로 재빌드해야 합니다.
5. 앱 콘텐츠/개인정보 처리방침/Data Safety/광고 ID/대상 연령/스토어 등록 등 Console의 미완료 항목과 경고를 검토합니다. 광고가 비활성화됐다는 이유로 기존 신고를 임의로 '수집 없음'으로 바꾸지 마십시오.
6. 출시 노트를 작성하고 검토 화면에서 비공개 테스트 배포 범위를 확인합니다. **실제 업로드·검토 제출·트랙 게시 버튼은 사용자가 직접 결정하고 실행합니다.** 이 작업에서는 수행하지 않았습니다.

## 남은 검증

Play Console 수락/심사 결과와 Google Play에서 생성한 APK의 실기기 검증은 미수행입니다. 기존 USB 개발 APK와 Play 앱은 서명이 다를 수 있으므로 기존 폰의 앱/저장 데이터를 삭제하지 말고 별도 테스트 기기 또는 데이터 보존 계획을 사용하십시오. 기존 Dex finalizer와 ADB 뒤로 가기 문제의 재발 여부도 이번에는 재측정하지 않았습니다. 개인정보 메시지의 실제 배포 동작과 Play 설치 후 Continue는 사용자 비공개 테스트에서 확인해야 합니다.

## 주요 생성·수정 파일

신규: `Runtime/Ads/AdBuildPolicy.cs`, `Editor/ClosedTestReleaseSettings.cs/.asset`, `Editor/ClosedTestReleaseBuild.cs`, `Editor/ClosedTestGradleProject.cs` (모두 `Assets/ColorMovePuzzle/` 아래), 서명·Bundletool·무결성 검사 도구(`AgentScripts/`), 이 문서와 키 보관 안내.

수정: `Runtime/Ads/AdManager.cs`, `Runtime/Ads/GoogleAdBackend.cs`, `Editor/AdMobBuildValidation.cs`, EditMode `ReleasePreparationTests.cs`, PlayMode `AdFlowPlayTests.cs`, `.gitignore`(DPAPI 제외), Android Target API 설정. 기존 90개 스테이지·난이도·UI 씬·퍼즐 이동/저장 코드는 변경하지 않았습니다.
