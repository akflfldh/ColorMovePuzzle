# 비공개 테스트 업로드 키와 재빌드

이번 전용 채널은 광고 초기화·광고 요청·광고 노출이 없는 Release AAB입니다. UMP 갱신/양식/Settings Privacy Options는 사용자 제공 App ID로 유지합니다. Development와 광고를 사용하는 운영 Release 설정은 그대로 남습니다.

## 승인 후 생성한 업로드 키

기존 Unity Publishing Settings와 프로젝트에서 사용자 keystore가 발견되지 않아, 사용자의 명시적 승인 후 2026-10-28에 아래 업로드 키를 생성했습니다. `.android/debug.keystore`는 기존 개발용 키이므로 재사용하지 않았습니다.

- 생성 위치: `C:\Users\dongd\UnitySigning\ColorMovePuzzle\colormove-upload.keystore` (프로젝트 밖).
- 형식/알고리즘: PKCS12, RSA 3072-bit, SHA256withRSA, 유효기간 10,000일.
- alias: `colormove-upload`.
- 인증서 이름: `CN=ColorMovePuzzle Upload`. 개인 이름·법인·주소·국적을 추정해 넣지 않습니다.
- 비밀번호: 암호학적 난수 32바이트에서 생성. 콘솔/채팅에 표시하지 않음.
- 비밀번호 보관: 같은 외부 폴더의 `upload-password.dpapi`에 Windows **CurrentUser DPAPI 암호화**로 보관. 평문 파일을 생성하지 않음.
- 디렉터리 권한: 현재 Windows 사용자·SYSTEM·Administrators로 제한.
- 기존 키/암호 파일이 있으면 덮어쓰지 않고 중단.

검토한 도구는 [CreateUploadKey.ps1](../../AgentScripts/CreateUploadKey.ps1)입니다. 외부 통신/다운로드/업로드 명령이 없고 로컬 Unity JDK keytool의 genkeypair/exportcert만 실행합니다. 비밀번호는 로그와 명령줄 인자에 출력하지 않습니다. 키·암호·메타데이터·공개 인증서 중 하나라도 이미 있으면 중단합니다. 자식 프로세스의 임시 비밀번호 환경변수 참조도 정리합니다. 이 스크립트를 업데이트 빌드마다 다시 실행하지 마십시오.

생성 도구 SHA-256: `9850849B0D023DCC5E0B5DAAD20E9C15E0931206AB710E5D60855C9D01054056`.

업로드 인증서 SHA-256: `3C:E1:68:06:B2:C0:3C:99:0E:19:F1:B7:81:31:3A:48:60:A4:BB:77:B8:35:A9:DF:E4:6E:58:35:4B:5C:C3:E5`.

인증서 유효기간: 2026-10-28 ~ 2054-03-15. 공개 인증서는 `Artifacts/ClosedTest/UploadCertificate.pem`에도 있습니다. 개인 키를 포함하는 keystore와 DPAPI 비밀번호는 프로젝트 밖에만 존재합니다.

## AAB 서명 구조

Unity는 업로드 비밀번호를 모르는 상태에서 서명 전 AAB를 빌드합니다. 해당 빌드의 임시 Gradle release signingConfig만 제거하며 원본 Development/운영 Gradle 템플릿은 바꾸지 않습니다. Bundletool과 실제 IL2CPP 출력으로 패키지·API·Release·광고 차단·UMP 보존을 검사한 뒤 외부 JDK jarsigner로 서명합니다.

서명 프로세스의 비밀번호는 DPAPI에서 메모리로만 복원하고 자식 프로세스의 `-storepass:env` / `-keypass:env`로 전달합니다. 명령줄·Unity PlayerSettings·Gradle·스크립트·Git에 평문 업로드 비밀번호를 기록하지 않습니다. 완료 후 메모리 바이트 배열과 자식 프로세스 환경변수 참조를 정리합니다.

## 반드시 별도로 해 둘 백업

DPAPI 파일은 현재 Windows 사용자/머신에 종속됩니다. **keystore와 DPAPI 파일만 다른 PC에 복사해 둔 것으로 복구가 보장되지는 않습니다.** 현재 PC를 잃기 전에 본인이 비밀번호 관리자에 생성된 비밀번호를 별도로 기록해야 합니다. [Microsoft CurrentUser DPAPI 설명](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.dataprotectionscope).

본인이 원할 때만 프로젝트 폴더에서 `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/ShowUploadKeyPassword.ps1`를 직접 실행하면 로컬 대화상자에 마스킹된 비밀번호가 나옵니다. Reveal을 직접 선택해 비밀번호 관리자에 기록하십시오. Bypass는 이 프로세스에서만 적용되며 시스템 실행 정책을 변경하지 않습니다. 이 도구는 자동 실행하거나 캡처하지 않으며 콘솔·클립보드로 자동 출력하지 않습니다. 비밀번호를 채팅에 보내지 마십시오.

keystore와 공개 인증서·alias 정보를 암호화한 별도 저장소/오프라인 매체에 백업하고, 비밀번호는 별도 접근 경로의 비밀번호 관리자에 보관합니다. 접근권한·복원 가능 여부도 확인합니다. `.keystore`, `.jks`, `.p12`, `.pfx`, `.key`, `.dpapi`는 Git 제외 대상입니다. 생성 비밀번호 복구를 실제 완료했다고 소유자 대신 보고하지 않습니다.

### 다른 컴퓨터에서 복구하는 순서

1. 암호화한 백업에서 `colormove-upload.keystore`와 `upload-certificate.pem`을 새 PC의 프로젝트 밖 개인 폴더로 복원합니다. 형식 `PKCS12`, alias `colormove-upload`, 위 인증서 지문을 함께 보관합니다. DPAPI 파일만으로는 이동하지 않습니다.
2. 비밀번호 관리자의 별도 항목에서 keystore 비밀번호를 확인합니다. 현재 생성한 PKCS12는 키 비밀번호와 저장소 비밀번호가 같습니다. 비밀번호를 명령줄·스크립트·Git·채팅에 붙여넣지 않습니다.
3. 새 PC의 JDK로 `keytool -list -v -storetype PKCS12 -keystore "D:\SecureSigning\colormove-upload.keystore" -alias colormove-upload`를 실행하고, 로컬 비밀번호 입력 프롬프트에 입력합니다. 경로는 실제 복원 위치로 바꿉니다. 열기에 성공하고 SHA-256 지문이 위 값과 같아야 합니다.
4. 빌드 후 서명 전 검증을 통과한 AAB에 `jarsigner -keystore "D:\SecureSigning\colormove-upload.keystore" -storetype PKCS12 -sigalg SHA256withRSA -digestalg SHA-256 -signedjar signed.aab unsigned.aab colormove-upload`를 실행합니다. 비밀번호는 프롬프트에 입력합니다. 이 방식은 기존 PC의 DPAPI에 의존하지 않습니다. 자동화가 필요하면 새 계정의 보안 비밀 저장소를 별도로 연결하고, 평문 설정 파일은 만들지 않습니다.
5. `jarsigner -verify -verbose -certs signed.aab`와 `keytool -printcert -jarfile signed.aab`로 서명과 지문을 다시 확인합니다. 새 업로드 키를 생성해 대체하지 않습니다.

이 백업/새 PC 복구 절차는 안내만 했으며 사용자의 암호 관리자 저장과 오프라인 백업을 대신 수행하지 않았습니다.

## 향후 같은 키를 재사용하는 순서

1. `Assets/ColorMovePuzzle/Editor/ClosedTestReleaseSettings.asset`에서 Version Code를 Play Console의 모든 기존 업로드보다 크게 올리고 Version Name을 검토합니다. 이번 첫 업로드는 소유자 확인에 따라 `1.0` / `1`입니다.
2. `Color Move Puzzle → Closed Test → Build Release AAB (No Ads)` 실행. 빌드별 define만 사용하고 완료 시 기존 SDK App ID·빌드 토글·버전을 복원합니다.
3. `python AgentScripts/PreflightClosedTestBundle.py` 실행.
4. 같은 Windows 계정에서 `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/SignClosedTestBundle.ps1` 실행. 기존 외부 keystore와 DPAPI를 재사용합니다. 키를 새로 만들거나 이전 서명된 AAB를 덮어쓰지 않습니다.
5. `python AgentScripts/VerifySignedClosedTestBundle.py`를 실행합니다. 서명·Manifest·버전과 광고 정책 검사를 완료한 **최종 서명 파일**을 선택합니다. `Intermediate/…unsigned.aab`는 업로드하지 않습니다.

## Play App Signing과 업로드 키

업로드 키는 본인이 AAB 제출 권한을 증명하는 서명키입니다. Play App Signing의 앱 서명 키는 Google이 기기에 배포하는 APK에 서명하는 키입니다. 보통 Google 관리 앱 서명 키와 별도의 업로드 키를 사용합니다. 업로드 키 분실/교체와 앱 서명 키 분실은 다른 문제이므로, 키 재설정은 콘솔 절차에 따라 처리해야 합니다. [Android 공식 서명 문서](https://developer.android.com/studio/publish/app-signing).

현재 USB 개발 APK는 디버그 서명입니다. Play 배포 서명이 다르면 기존 APK에 직접 업데이트되지 않을 수 있습니다. 기존 폰의 앱을 삭제하거나 데이터를 지우지 말고 별도 테스트 기기 또는 데이터 보존 계획을 준비하십시오. 이번 AAB 작업에서는 스마트폰의 설치 상태나 저장 데이터를 변경하지 않습니다.
