# ColorMovePuzzle — Google Play 출시 준비

11차 최신 결과: 사용자 제공 실제 UMP App ID를 유지하고 광고 요청을 모두 차단한 **서명된 비공개 테스트 Release AAB**를 생성했습니다. [최종 파일·서명·테스트·업로드 안내](README-ClosedTest.md)를 확인하십시오. 아래는 실제 App ID와 업로드 키 제공 전인 10차 기록입니다.

작성일 2026-10-28. **출시 준비·로컬 검증만 수행합니다. Google Play 업로드, 공개 출시, 운영 광고 활성화는 수행하지 않았습니다.** 실제 AdMob ID와 게시된 개인정보 메시지는 사용자가 아직 준비하지 않았다고 확인했습니다.

## 준비 상태

| 항목 | 확인 결과 |
|---|---|
| Google Mobile Ads Unity Plugin | 11.6.0 유지, 기존 패키지 업그레이드 없음 |
| 운영 App / 전면 광고 ID | 미제공, 필드 비어 있음 |
| 테스트·운영 구분 | Development는 공식 테스트 광고, Release는 실제 ID·소유자 검토 필요. 잘못된 조합은 빌드 차단 |
| UMP | 기존 SDK 흐름 재검토, Release에 포함되지 않는 개발 지역 진단 추가 |
| 패키지 | `com.colormovepuzzle.game` 유지 |
| 버전 | Name 1.0 / Code 1 유지. 콘솔 업로드 이력 확인 전 임의 증가 안 함 |
| API | Minimum 26 유지, Target 자동 선택에서 명시적 36으로 고정 |
| 플랫폼 | Unity 6000.6.5f1 / IL2CPP / ARM64 / Portrait 유지 |
| 시작 씬 | `Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity` |
| Release 런타임 Pipeline | 비활성 유지 |
| 업로드 키 | 설정 안 됨. 사용자 생성·안전한 보관 필요 |
| 아이콘 | Android 사용자 지정 아이콘 참조 없음. 출시용 파일 준비 필요 |
| 개인정보 URL·앱 내부 링크 | URL 미제공. 게시·앱 내부 연결은 출시 전 남은 작업 |

현재 Target API 36 요구는 [Google Play 공식 정책](https://support.google.com/googleplay/android-developer/answer/11926878)을 기준으로 확인했습니다. 실제 제출일에 다시 확인해야 합니다.

## 설정과 소유자 작업 문서

- [AdMob 등록·ID 입력 위치·테스트 기기·UMP 검증](Docs/Release/AdMobAndUmp.md)
- [실제 데이터 처리 근거와 Data Safety 작성표·개인정보 정책 준비](Docs/Release/PrivacyAndDataSafety.md)
- [서명 키 보관·AAB 빌드·Play Console 작업](Docs/Release/SigningAndPlayConsole.md)
- [앱 이름·소개 문구 초안과 스토어 이미지 목록](Docs/Release/StoreListing.ko.md)

## 코드 및 설정 변경

- `Runtime/Ads/AdConfig.cs`: App ID/광고 단위 구분, ASCII·줄바꿈·공식 테스트 publisher 검사, 선택적 운영 보상형 ID 검사 보강. 빈 운영 ID에 테스트 ID로 fallback하지 않음. 별도의 Development Consent App Id로 실제 UMP 메시지를 시험해도 광고 단위는 공식 테스트용을 유지하도록 분리. 현재 해당 필드는 비어 있음.
- `Runtime/Ads/GoogleAdBackend.cs`: 개발 빌드에서만 UMP 진단 요청·상태 로그 사용. 기존 공식 SDK 동의 판단과 초기화 순서 유지.
- `Runtime/Ads/UmpDevelopmentDiagnostics.cs`: Development 전용 실행 Intent에서만 유효한 테스트 기기와 지역을 받아 공식 UMP debug API 사용. Reset은 명시 요청한 경우 UMP에만 적용. Release에서는 타입과 호출부 전체 제외.
- `Editor/AdMobBuildValidation.cs`: 기존 광고 차단 유지, Release AAB·업로드 키·서명 비밀번호 검사 추가.
- `Editor/PuzzleReleasePreparation.cs`: `Color Move Puzzle → Release → Check Readiness` 메뉴에서 ID·플랫폼·키 설정 문제 확인. 정책 승인·계정 소유·콘솔 버전 이력을 자동 보장하는 도구는 아님.
- `Tests/EditMode/ReleasePreparationTests.cs`: ID 혼동·부적절한 ID·운영 미설정·소유자 검토·개발 운영 광고 금지·UMP 재선택·동의용 App ID와 테스트 광고 단위 분리 검사 추가.
- `ProjectSettings/ProjectSettings.asset`: Android Target API 36 고정. 기존 Input System·APP_UI_EDITOR_ONLY·비율·방향·빈도 설정 유지.
- `.gitignore`: 키스토어·개인 키·로컬 비밀 파일 및 원본 UMP QA 로그 제외 규칙.
- `AgentScripts/`: 설정 스냅샷·무결성·개발 기기 진단·빌드 설정 복원 도구.

작업 전 변경 상태를 확인하고 `Artifacts/ReleasePreparation/BeforeChanges.zip`에 수정 대상의 텍스트 파일과 씬·설정·메타를 백업했습니다. 기존 Git 인덱스나 사용자 파일을 초기화하지 않았습니다. 기존 193개 스테이지/데이터 파일, 핵심 보드 코드, 씬, 패키지 manifest/lock의 불변 여부는 [Integrity.json](Artifacts/ReleasePreparation/Integrity.json)에 기록합니다. 디자인·난이도·이동·저장·광고 빈도는 변경하지 않았습니다.

## 자동 테스트

최종 전체 실행은 **EditMode 113/113, PlayMode 60/60** 통과이며 실패·스킵 0입니다. 기존 105/60개와 신규 EditMode 8개를 포함하고 실행 시간은 3.80초 / 180.51초입니다. SDK 네트워크 없이 동의 요청 1회, 개인정보 옵션 중복 방지와 재선택 후 SDK 요청 가능 상태를 따르는 것, 동의용 App ID가 테스트 단위를 운영 단위로 바꾸지 않는 것을 확인했습니다. 이는 실제 지역 양식 수락·거부 검증을 대신하지 않습니다.

결과 파일: [EditModeTests.json](Artifacts/ReleasePreparation/EditModeTests.json), [PlayModeTests.json](Artifacts/ReleasePreparation/PlayModeTests.json).

Pipeline의 과거 콘솔 기록에는 기존 `PuzzleAndroidStartupTests`가 `LogAssert.Expect`로 의도적으로 재현한 InputUpdateType.None/입력 callback 오류와 Release 차단 오류가 남습니다. 이를 실기기 재발이나 C# 컴파일 실패로 분류하지 않습니다. 최종 Editor 확인에서 컴파일 실패 false, Play 중지, 씬 dirty=false, `runInBackground=false`, 공식 테스트 App/전면 광고 단위 선택을 확인했습니다. 진단 기록을 지우거나 오류 로그를 감추지 않았습니다.

## Release AAB 실제 빌드 시도

Pipeline으로 Android / AAB / Development 해제 / DetailedBuildReport 조건의 최종 코드 빌드를 실제 요청했습니다. **0.732초 후 예상한 정책 차단으로 실패**했습니다. 메시지는 `Release build blocked: official TEST ads are still selected.`이며 같은 원인이 빌드 오류 두 줄로 기록됐습니다. 경고는 0개입니다. [실제 차단 결과](Artifacts/ReleasePreparation/ReleaseAabBlocked.json).

따라서 **Release AAB 파일은 생성되지 않았습니다.** 테스트 ID를 바꿔 끼우거나 임의 운영 ID·디버그 서명으로 우회하지 않았습니다. 준비 상태 검사에는 실제 ID 미설정, 업로드 keystore 미설정, alias 미설정도 남아 있습니다. Release 시도 후 기존 Development/APK 빌드 토글로 복구했습니다. 향후 실제 ID·UMP·정책·키가 준비되고 운영 활성화를 승인한 뒤에 서명된 AAB 생성 및 최종 Manifest/권한/16KB/서명/QA 코드 제외를 검증해야 합니다.

## Android Development 및 UMP 실기기 결과

최종 Development APK는 [ColorMovePuzzle-ReleasePreparation-Development.apk](Builds/Android/ColorMovePuzzle-ReleasePreparation-Development.apk)입니다. **44.772초, 오류 0개·경고 979개**로 성공했습니다. 71,000,813 bytes이며 SHA-256은 `25E1EDC11AEAD17F7562BACF4F45B5CC8F897352280998762DE2B19CCFEF4096`입니다. APK v2 서명은 기존 디버그 인증서와 같고 ARM64 전용·네이티브 ELF 16KB 페이지 호환·Google Ads/UMP/Unity 브리지 DEX 포함·Unity.AppUI Player 메타데이터 참조 0을 확인했습니다.

경고는 Shader 관련 970개, TMP 큰 메서드 안내 3개, Diagnostics Data/심볼 권고 1개, 공식 SDK iOS xcframework의 Android 비지원 안내 1개, 새 개발 진단의 `DEVELOPMENT_BUILD` 전처리 기호 폐기 예정 경고 4개입니다. 경고를 숨기지 않았습니다. 이 기호는 현재 사용 중인 Unity 6000.6에서 동작하고 비 Development Player에서 해당 코드를 제외하지만, Unity 6.8 업그레이드 전에는 새 managed code variant 방식으로 이전해야 합니다. [Unity 6.6 공식 변경 안내](https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66). 상세 [빌드 결과](Artifacts/ReleasePreparation/BuildSummary.json), [경고 집계](Artifacts/ReleasePreparation/WarningSummary.json).

Samsung SM-S938N / Android 16에 `adb install -r` 업데이트 설치가 성공했습니다. 앱 삭제·pm clear는 사용하지 않았고 원본 저장 파일 해시가 모두 유지됐습니다. JSON 일괄 스냅샷과 원본 백업 사이에 백그라운드 광고 시간 체크포인트 0.0121초만 갱신된 것을 구분해 기록했습니다. 게임 진행·프로필·볼륨·배치 파일은 변하지 않았습니다. [업데이트 보존 결과](Artifacts/ReleasePreparation/UpdatePreservation.json).

### 실제 Google 샘플 UMP 양식 검증

운영 App ID가 없어도 공식 sample App ID가 제공한 **Publisher Test Ads** 양식으로 다음을 실제 검증했습니다. 이것은 사용자의 AdMob 계정이나 출시용 개인정보 메시지를 구성·검증했다는 뜻이 아닙니다. 모든 선택은 SDK가 제공한 실제 양식으로 처리했고 저장소에 가짜 동의 값을 쓰지 않았습니다.

| 검증 | 실제 결과 |
|---|---|
| EEA 개발 기기·지역 강제 | UMP Update 성공, Required / Privacy Required / CanRequestAds=false |
| 양식 전 광고 요청 | SDK 초기화·광고 로드 없음 확인 |
| Consent 선택 | ADB로 버튼 선택, SDK PurposeConsents 11개 허용, Obtained, 메뉴 복귀·테스트 광고 로드 |
| Reset 후 Do not consent | ADB로 버튼 선택, SDK PurposeConsents 허용 0개, Obtained, 메뉴 복귀 |
| 거부 후 SDK 판단 | CanRequestAds=true 반환. 거부를 모두 광고 차단으로 임의 해석하지 않고 SDK 결정을 따름. 실제 광고 개인화 방식까지 검사한 결과는 아님 |
| Privacy Options 재진입 | 기존 Settings 버튼 표시 → 실제 양식 다시 표시 → 수락으로 변경 → Settings/DONE 복귀 |
| 중복 초기화 방지 | Privacy Options 재진입 전후 같은 프로세스에서 SDK 초기화 1회. 준비 광고는 폐기 후 1회 재로드 |
| 비규제 Other 지역 | NotRequired / Privacy NotRequired / CanRequestAds=true, 양식 없이 정상 메뉴·초기화·테스트 로드 |
| 강제 설정 없는 일반 실행 | 새 PID 7080, 45초 이상 관측, NotRequired / Privacy NotRequired / CanRequestAds=true, Reset·강제 지역 로그 없음 |
| 일반 실행의 Settings | Privacy Options 버튼 숨김 확인, DONE으로 메뉴 정상 복귀 |
| 게임 저장 보존 | 각 검증에서 진행·프로필·배치·설정 JSON 불변 확인 |

수락·거부는 ADB 입력 결과이며 사람 손가락의 조작감 검증이 아닙니다. WebView가 접근성 텍스트를 제공한 경우 정확한 버튼 텍스트를 선택했고, 제공하지 않은 경우 바로 확인한 샘플 화면의 버튼 좌표를 사용했습니다. 양식이 나타나기 전/완료 후 다른 화면을 잘못 누르지 않도록 화면과 앱 포커스를 확인했습니다.

근거: [EEA 수락 전 양식](Artifacts/ReleasePreparation/Device/ump-EEA-Accept.png), [수락 결과](Artifacts/ReleasePreparation/Device/EEA-Accept-Choice.json), [거부 결과](Artifacts/ReleasePreparation/Device/EEA-Reject-Choice.json), [Privacy Options 화면](Artifacts/ReleasePreparation/Device/privacy-options-reentry.png), [재선택 결과](Artifacts/ReleasePreparation/Device/Privacy-Reaccept-Choice.json), [Other 결과](Artifacts/ReleasePreparation/Device/Ump-Other.json).

최종 [FinalVerification.json](Artifacts/ReleasePreparation/Device/FinalVerification.json)은 초기 진단·수락·거부/재선택·Other·일반 실행의 5개 PID에서 초기화가 각 1회이고, 메뉴/설정에서 전면 광고 표시가 0회임을 확인합니다. 현재는 강제 지역 없이 일반 메인 메뉴로 복구됐고 Continue는 기존 3×3 2번입니다. 게임 저장 JSON 및 원본 바이트 해시, 누적 클리어와 광고 쿨다운 카운터도 검증 전 값과 일치합니다. 광고 노출 간격을 낮추거나 기록을 초기화하지 않았습니다.

5개 PID 모두 InputUpdateType.None 0, FATAL EXCEPTION/fatal signal/ANR 0, AssetPack 선택적 조회 1회, Dex finalizer 메시지 2줄(기존과 같은 중복 스택)이었습니다. 일반 실행에서 선택적 `GoogleMobileAdsNative.Common` 조회의 SDK 진단도 재관측됐으며, 이전 9차 분석대로 SDK가 catch하여 보고한 선택적 Native 플러그인 버전 조회입니다. BACK callback 비활성 경고는 남아 있습니다. **이번 기기 검증에서는 ADB BACK 무응답을 다시 시험하지 않았고**, 이전의 간헐적 문제를 해결했다고 판단하지 않습니다. 원인이 확인되지 않은 엔진·Gradle·서드파티 내부 코드는 변경하지 않았습니다.

원본 UMP 로그에는 동의 문자열·기기 정보가 있을 수 있어 Git 제외된 `Artifacts/ReleasePreparation/Private/`에 보관하고, 공유용 결과에는 테스트 해시를 넣지 않았습니다. 사용자 실제 App ID·개인정보 메시지·문구·언어·거부/철회·대상 연령 검증은 ID와 콘솔 구성이 준비된 뒤 다시 해야 합니다. US states 메시지와 사용자 운영 광고 단위 검증은 미수행입니다.

## 출시 전 남은 작업

1. 실제 AdMob 앱/전면 광고 단위, 개인정보 메시지 게시, 테스트 기기 등록을 소유자가 수행하고 ID를 전달.
2. 대상 연령·배포 국가·광고 관련 아동 설정·Unity 엔진 진단 정책 확정. UMP만으로 모든 SDK 데이터 처리가 제어된다고 가정하지 않음.
3. 개인정보 처리방침 URL·개발자 웹사이트·app-ads.txt·앱 내부 정책 접근 수단 준비.
4. 실제 양식 수락·거부·재선택 및 운영 ID를 사용하는 등록 테스트 기기의 테스트 광고 검증.
5. 사용자 소유 업로드 키와 Play App Signing 준비, Version Code 이력 확인, 아이콘·스토어 자료·Data Safety·광고 ID·Contains ads·연령·등급 신고 확정.
6. 서명된 Release AAB 생성 후 내부 테스트와 출시 승인. 현재 이 작업에서는 업로드하지 않음.
7. 기존 Dex finalizer 로그와 간헐적 Android BACK 무응답 추적 지속. 이들 문제를 이번 준비 작업에서 해결했다고 보고하지 않음.
