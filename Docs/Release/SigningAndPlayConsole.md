# 서명 키와 Google Play Console 준비

이번 작업에서는 콘솔에 업로드하거나 공개 출시하지 않습니다. 키와 비밀번호도 생성·수집하지 않습니다.

## 현재 Android 설정

패키지 `com.colormovepuzzle.game`, Version Name `1.0`, Version Code `1`, Minimum API `26`, IL2CPP/ARM64, Portrait를 유지합니다. Target API는 자동 선택에서 명시적 **36**으로 고정합니다. 2026-10-28 확인 기준 새 앱/업데이트의 API 36 요구와 맞습니다. 제출 직전에 최신 요구를 다시 확인하십시오. [Target API 정책](https://support.google.com/googleplay/android-developer/answer/11926878).

Version Code는 Play에 이미 올라간 값보다 커야 합니다. 콘솔 이력을 알 수 없어 임의 증가하지 않았습니다. 현재 패키지 이름의 Play 등록 가능 여부와 최종 브랜드도 소유자가 확정해야 합니다. 출시 후 패키지 변경은 별도 앱이 되므로 신중하게 결정합니다.

## 키가 없는 경우의 안전한 절차

1. Unity Publishing Settings → Keystore Manager에서 **프로젝트와 Git 폴더 밖**의 전용 디렉터리를 선택해 업로드용 keystore와 alias를 만듭니다. 또는 JDK `keytool -genkeypair -v -keystore <저장소 밖 경로> -alias <본인이 정한 alias> -keyalg RSA -keysize 2048 -validity 10000`을 직접 실행하고 대화식 프롬프트로 비밀번호를 입력합니다. 비밀번호를 명령 인자나 채팅에 넣지 않습니다.
2. 비밀번호는 비밀번호 관리자에, keystore는 암호화된 별도 백업에 보관합니다. 접근 권한을 제한하고 복구 방법을 확인합니다. `.jks`, `.keystore`, `.p12`, `.pfx`, `.key`, `.env*`, `Signing/`은 Git 제외 규칙을 추가했습니다. 이미 추적된 파일을 자동 제거한 것은 아니므로 `git ls-files` 검토도 필요합니다.
3. Unity Android Publishing Settings에 파일/alias를 연결하고 비밀번호는 해당 Editor 세션에서만 입력합니다. CI는 비밀 저장소에서 주입하고 로그로 출력하지 않습니다. AgentScripts나 ProjectSettings에 비밀번호를 기록하지 않습니다.
4. Play App Signing에서 앱 서명 키와 업로드 키의 역할을 구분합니다. 일반적으로 업로드 키로 AAB에 서명하고, Play가 배포 APK를 앱 서명 키로 서명합니다. 콘솔에서 공개 인증서 지문을 확인해 보관합니다. 기존 디버그 키를 출시 키로 사용하지 않습니다. [Android 공식 서명 설명](https://developer.android.com/studio/publish/app-signing).

기존 USB 테스트 앱은 디버그 서명입니다. Play 서명 APK와 서명이 다르면 같은 패키지의 직접 업데이트 설치가 거절될 수 있습니다. 이를 해결하려고 기존 기기의 앱을 제거하거나 데이터를 초기화하지 않습니다. 실기기 보존과 Play 설치 테스트 방법은 출시 서명을 확정한 뒤 별도 계획합니다.

## AAB 빌드 순서

1. 소유자가 실제 ID와 UMP 메시지를 준비하고 정책·대상 연령을 검토합니다. 운영 활성화는 별도 확인 후 [AdMob 설정 문서](AdMobAndUmp.md)의 위치에 적용합니다.
2. Build Settings에서 Android, **Build App Bundle**, Development Build 해제, Script Debugging 해제, Autoconnect Profiler 해제, 첫 씬 `Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity`를 확인합니다.
3. 업로드 키를 연결하고 `Color Move Puzzle → Release → Check Readiness`를 실행합니다. 이 도구는 ID·서명 파일·플랫폼 설정을 검사하지만 정책 승인·계정 소유·콘솔 메시지·버전 이력까지 보장하지 않습니다.
4. `Builds/Android/ColorMovePuzzle-Release.aab`로 빌드합니다. Runtime Pipeline은 Release에서 비활성 상태를 유지합니다. Test Ads가 남으면 빌드가 차단됩니다. 서명 비밀번호가 없거나 AAB가 아닌 Release도 차단합니다.
5. 실제 AAB의 manifest, signing certificate, 권한, 네이티브 라이브러리 16KB 호환성, UMP debug 코드 부재와 테스트 App/선택 광고 ID 부재를 검사하고 내부 테스트 계획을 세웁니다. 아직 생성되지 않은 AAB가 이 검사를 통과했다고 간주하지 않습니다.

## 사용자가 Play Console에서 할 일

- 개발자 신원·연락처·결제/세금 관련 계정 요구를 본인 계정 상태에 맞춰 완료합니다. 앱을 만들고 패키지·표시 이름·기본 언어·게임/퍼즐 분류·가격·배포 국가를 결정합니다.
- Play App Signing 및 업로드 키를 등록하고 실제 AAB와 증가한 Version Code를 확인합니다.
- App content에서 **Contains ads: Yes**, 광고 ID 사용, Data Safety, 개인정보 URL, 대상 연령, IARC 콘텐츠 등급, 앱 접근 안내를 실제 기능·SDK 설정에 맞춰 작성합니다. 광고가 있다는 사실과 인앱 구매 유무는 별도입니다.
- [스토어 초안](StoreListing.ko.md)의 글과 최종 아이콘/이미지·개발자 웹사이트를 준비합니다.
- 계정 유형과 개설 시점에 따른 테스트·본인 확인 요건을 확인합니다. 현재 공식 안내상 2023-11-13 이후 신규 개인 계정은 연속 14일 동안 12명 이상 opt-in한 비공개 테스트 등의 요건이 적용됩니다. 이 작업에서는 계정 상태를 알 수 없으며 충족 여부도 확인하지 않았습니다. [공식 테스트 요건](https://support.google.com/googleplay/android-developer/answer/14151465).
- 추후 내부 테스트 트랙에서 동의 수락·거부·재선택, 광고 실패, 저장·복원, BACK과 Dex 로그를 확인한 뒤 공개 출시 여부를 직접 결정합니다. 이번 작업에서는 내부 테스트 업로드도 수행하지 않습니다.
