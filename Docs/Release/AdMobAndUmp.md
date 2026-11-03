# AdMob 운영 설정과 UMP 검증 절차

작성일 2026-10-28. 현재 운영 ID는 사용자가 아직 발급하지 않았습니다. 운영 광고를 활성화하지 않았습니다.

## 사용자가 AdMob에서 할 일

1. AdMob 계정을 만들고 Android 앱을 등록합니다. Play에 아직 공개되지 않았다면 미게시 앱으로 등록할 수 있으며, 이후 실제 스토어 등록과 연결합니다. 패키지는 현재 `com.colormovepuzzle.game`입니다. 패키지의 소유·사용 가능 여부는 계정에서 확인해야 합니다.
2. 앱 설정의 **App ID**(`ca-app-pub-…~…`)를 복사합니다. 앱 아래 **전면 광고 단위**를 만들고 **광고 단위 ID**(`ca-app-pub-…/…`)를 복사합니다. 이번에 배너나 보상형 광고 단위를 만들 필요는 없습니다.
3. Privacy & messaging에서 실제 앱을 선택하고 배포 지역에 맞는 European regulations 및 필요한 US states 메시지를 작성·게시합니다. 실제 개인정보 처리방침 URL, 언어, 수락·거부 및 개인정보 선택 옵션을 검토합니다. SDK 설치만으로 이 콘솔 작업이 완료되지 않습니다.
4. 실제 App ID와 전면 광고 단위 ID, 메시지 게시 상태, 대상 연령 결정을 전달합니다. ID 형식 검사는 계정 소유·단위 종류·서버 활성 상태를 증명하지 않습니다.

공식 근거: [시작하기](https://developers.google.com/admob/unity/quick-start), [UMP](https://developers.google.com/admob/unity/privacy), [US 메시지](https://developers.google.com/admob/unity/privacy/us-iab-support).

## 프로젝트의 정확한 설정 위치

| 위치 | 필드 | 현재 값 / 변경 시점 |
|---|---|---|
| `Assets/ColorMovePuzzle/Data/AdConfig.asset` Inspector | Test Ads | true 유지. 운영 활성화 승인 전 false로 바꾸지 않음 |
| 같은 에셋 | Development Consent App Id | 현재 비어 있음. 향후 소유자가 제공한 실제 App ID로 UMP 메시지를 테스트할 때만 입력. 전면/보상형 광고 단위는 계속 Google 공식 테스트 ID |
| 같은 에셋 | Production App Id | 비어 있음. 소유자가 제공한 App ID만 입력 |
| 같은 에셋 | Production Interstitial Id | 비어 있음. 실제 전면 광고 단위 ID만 입력 |
| 같은 에셋 | Production Rewarded Id | 비워 둠. 현재 보상형 광고 노출 없음 |
| 같은 에셋 | Release Privacy And Audience Reviewed | false. 아래 정책·연령·동의 검증 후 소유자가 검토한 경우에만 true |
| `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` | AdMob Android App Id | 현재 공식 테스트 App ID. 운영 전환 승인 후 실제 App ID와 일치시켜야 함 |

App ID의 `~`와 광고 단위의 `/`는 서로 바꿀 수 없습니다. 공백·줄바꿈·비 ASCII 숫자·공식 테스트 publisher가 들어간 운영 ID를 차단합니다. 선택적으로 입력한 보상형 ID도 검사합니다. 비어 있는 운영 값에 테스트 ID로 fallback하지 않습니다.

현재 개발용 동의 App ID가 비어 있어 Google sample App ID를 사용합니다. 콘솔 메시지를 준비한 뒤에는 Test Ads=true를 유지한 채 Development Consent App Id와 Google SDK 설정의 App ID를 같은 실제 값으로 맞춰 테스트할 수 있습니다. 이 경우에도 운영 광고 단위는 사용하지 않습니다. 실제 값을 제공받기 전에는 해당 필드를 채우지 않습니다.

Development는 Test Ads=true인 구성이어야 빌드할 수 있습니다. 운영 구성을 선택한 채 Development를 빌드하면 실패하므로 테스트 구성을 복구해야 합니다. Release는 Test Ads=true이면 실패하고, 실제 ID·검토·SDK App ID 일치·AAB·업로드 키 설정을 모두 요구합니다. 즉 현재는 안전하게 Release가 차단됩니다. 운영 설정 변경과 운영 광고 요청은 별도 사용자 확인 이후 작업입니다.

## 테스트 기기 등록

일반 개발 APK는 공식 테스트 App/광고 단위만 사용합니다. 실제 단위 검증이 필요해지면 AdMob의 Settings → Test devices에서 기기를 등록하고 반영을 확인한 뒤 테스트합니다. 또는 공식 `RequestConfiguration.TestDeviceIds`로 등록할 수 있지만 현재 게임에 운영 단위를 요청하는 코드는 추가하지 않았습니다. 반드시 화면의 **Test Ad** 표시를 확인하며, 표시가 없으면 운영 광고를 클릭하거나 반복 요청하지 않습니다. [Google 테스트 광고 지침](https://developers.google.com/admob/unity/test-ads).

AdMob 광고 테스트 기기 ID와 UMP `TestDeviceHashedIds`는 서로 다른 용도로 취급합니다. ADB serial이나 Android Advertising ID를 UMP 해시 대신 넣지 않습니다. UMP 로그가 안내한 해시만 해당 기기에 사용하고 공유 문서·소스에는 기록하지 않습니다.

## UMP 코드 검토 결과

`GoogleAdBackend` → `AdConsentController` → `AdManager` 순서입니다. 실행마다 Update를 1회 요청하고, 필요한 양식은 안전한 메뉴/설정 화면에서 표시합니다. SDK의 CanRequestAds만 사용하며, 앱이 임의로 동의 문자열을 저장해 허용 상태를 대체하지 않습니다. PrivacyOptionsRequirementStatus가 Required일 때 기존 Settings의 Privacy Options를 표시합니다. 요청 가능 상태가 바뀌면 준비된 광고를 폐기·재판단합니다. 초기화는 프로세스당 1회, 양식·로드·광고 종료는 중복 처리를 방지합니다. 거부 선택이 곧 모든 광고의 금지라는 뜻은 아니므로, 실제 SDK가 반환한 CanRequestAds를 따릅니다. [공식 UMP 흐름](https://developers.google.com/admob/unity/privacy).

## 개발 빌드에서 지역 검증

`UmpDevelopmentDiagnostics`와 호출 지점은 모두 `#if DEVELOPMENT_BUILD || UNITY_EDITOR` 안에 있습니다. Release에는 강제 지역·테스트 기기 처리·Reset 호출 코드가 컴파일되지 않습니다. 에셋에 지역이나 테스트 해시를 저장하지 않습니다.

개발 APK에만 ADB 실행 Intent로 `cmp.ump.region=EEA` 또는 `Other`, `cmp.ump.testDevice=<해당 UMP 해시>`, `cmp.ump.reset=true`를 전달할 수 있습니다. 유효한 테스트 기기가 없으면 무시합니다. Reset은 UMP SDK 동의 상태만 초기화하고 퍼즐 저장 파일은 건드리지 않습니다. 일반 실행에는 이 extras를 전달하지 않습니다. 개발 빌드라도 광고 단위는 공식 테스트용 그대로입니다.

검증 도구는 `AgentScripts/ProbeUmpDevelopmentDevice.py EEA`, `Other`, `Normal`입니다. 실제 기기 로그에서 해시를 읽고, 게임 저장 파일 불변을 확인하며, 요약에는 해시를 제거합니다. 원본은 Git 제외된 `Artifacts/ReleasePreparation/Private/`에 저장합니다. 실제 양식이 나타나면 수락 → 재실행, Reset 후 거부 → 재실행, Settings → Privacy Options 재진입 순서로 확인합니다. 양식이 나오지 않으면 수락·거부가 검증됐다고 기록하지 않습니다. 콘솔 설정 오류를 가짜 동의로 우회하지 않습니다.

최종 실행 결과와 막힌 항목은 [출시 준비 보고서](../../README-Release.md)에 기록합니다.
