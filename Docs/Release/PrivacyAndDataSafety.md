# 개인정보 및 Data Safety 작성용 근거표

작성일 2026-10-28. **제출된 신고나 완성된 개인정보 처리방침이 아닙니다.** 개발자 신원·연락처·배포 국가·대상 연령·실제 운영 SDK 설정을 확정한 뒤 소유자가 최종 답변해야 합니다.

## 실제 프로젝트에서 확인한 사항

- 퍼즐 진행, 해금, 클리어, 배치, 이동 횟수, Continue, 볼륨은 Unity persistentDataPath의 JSON 파일에 저장합니다. 자체 계정·로그인·클라우드 저장·서버 전송 코드와 자체 네트워크 API 호출은 게임 코드에서 발견되지 않았습니다. 이것을 SDK나 OS까지 포함한 '데이터 수집 없음'으로 해석하지 않습니다.
- Google Mobile Ads Unity 11.6.0 / Android GMA 25.5.0 / UMP 4.0.0이 포함됩니다. UMP 선택은 SDK가 관리합니다. 제3자 중개 광고 네트워크는 이번 프로젝트에 추가하지 않았습니다.
- Unity Analytics, Unity Ads, Unity Purchasing 서비스는 설정상 비활성입니다. 반면 `UnityConnectSettings.asset`의 `m_EngineDiagnosticsEnabled=1`을 확인했고 실제 Android 빌드에도 Diagnostics Data enabled 경고가 있습니다. Unity 엔진 진단을 비활성이라고 단정하지 않습니다. Release 전 실제 Player에서 전송 여부·동의·보존·옵트아웃 적용을 확인해야 합니다. 이번 작업에서는 이 설정을 임의로 바꾸지 않습니다. [Unity 진단 설정 문서](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/comp-manager-group/diagnostics-settings), [Unity 사용자 개인정보 문서](https://unity.com/legal/game-player-and-app-user-privacy-policy).
- 기존 APK의 병합 Manifest에는 INTERNET, ACCESS_NETWORK_STATE, AD_ID, ACCESS_ADSERVICES_AD_ID, ACCESS_ADSERVICES_ATTRIBUTION, ACCESS_ADSERVICES_TOPICS 등이 있습니다. 정밀 위치·카메라·마이크·연락처 읽기 권한은 이 Manifest에서 발견되지 않았습니다. 광고 SDK의 조회 intent(SMS/전화 등)를 관련 읽기 권한으로 오해하지 않아야 합니다.
- Android OS 백업/복원·앱 저장소 정책도 최종 Release Manifest와 백업 규칙으로 따로 확인해야 합니다. '기기 밖으로 절대 나가지 않는다'는 문구는 쓰지 않습니다.

## Google SDK 근거와 신고 후보

아래는 GMA 25.5.0에 대한 [Google의 데이터 공개 문서](https://developers.google.com/admob/unity/privacy/play-data-disclosure)를 현재 설정과 대조한 작성 후보입니다. Google은 IP, 제품 상호작용, 진단, 기기·계정 식별자의 자동 수집·공유와 광고·분석·사기 방지 목적, TLS 전송을 설명합니다. 실제 앱의 조건과 Play 양식 정의에 맞춰 답변해야 합니다.

실기기 EEA 검증에서 본 **Publisher Test Ads** 양식은 Google sample App ID의 테스트 메시지입니다. 그 양식의 파트너 수나 정밀 위치 문구를 실제 ColorMovePuzzle 운영 앱의 수집 범위로 확정하지 않습니다. 사용자 실제 앱의 메시지·벤더 선택·권한·SDK 설정으로 다시 확인해야 합니다.

| SDK 근거 | Play Data Safety에서 검토할 데이터 유형 | 소유자가 확정할 사항 |
|---|---|---|
| IP로 일반 위치 추정 가능 | 대략적 위치 | 수집/공유 및 목적. 위치 권한 없음과 위치 데이터 없음은 다름 |
| 실행·탭·영상 시청 등의 상호작용 | 앱 활동 / 앱 상호작용 | 광고·분석 목적, 필수/선택 및 처리 조건 |
| SDK·앱 성능과 장애 진단 | 앱 정보 및 성능 / 진단·충돌 로그 등 | 실제 엔진/SDK 진단 범위와 보존·삭제 |
| 광고 ID·앱 세트 ID 및 해당 경우 기타 식별자 | 기기 또는 기타 ID | AD_ID 사용, 동의·제한 광고·아동 설정별 처리 |

현재 AD_ID 제거 Manifest를 적용하지 않았습니다. '광고 ID 미사용'으로 신고하면 안 됩니다. 현재 보상형 기반은 있지만 노출·보상 소비자가 없고, 구매·로그인·사용자 메시지 기능도 없습니다. 아직 없는 기능을 신고 항목에 추가하지 않습니다. '수집', '공유'의 예외, 일시적 처리, 삭제 요청 지원 여부는 [Play Data Safety 공식 안내](https://support.google.com/googleplay/android-developer/answer/10787469)의 정의와 실제 계약/설정을 검토한 뒤 결정합니다. SDK 기본 설명만으로 모든 최종 체크박스를 임의 확정하지 않습니다.

## 개인정보 처리방침 초안에 채울 내용

아래 항목을 공개 웹페이지로 작성해야 합니다. `[미정]` 상태로 게시하지 마십시오.

1. 앱 표시 이름과 패키지, 운영자/법인명 `[미정]`, 문의 이메일 `[미정]`, 시행일 `[미정]`.
2. 기기에 저장하는 진행·설정·퍼즐 스냅샷의 목적과 삭제 방법, OS 백업 가능 여부. 앱 자체의 계정·클라우드 저장 부재를 SDK 데이터 수집과 구별해 설명.
3. Google AdMob/UMP 및 적용되는 Unity 엔진 진단의 데이터 종류·목적·수신자·보존·삭제 절차. 광고 제공·분석·부정 사용 방지, 국제 처리·제3자 정책 링크 등을 실제 계약과 운영 설정에 맞춰 기재.
4. 지역별 동의, 거부·철회·Privacy Options, Android 광고 ID 재설정/삭제, 권리 요청 연락처와 처리 절차. 제공하지 않는 SDK 삭제 기능을 있다고 쓰지 않음.
5. 대상 연령·아동 정책. 퍼즐 장르라는 이유로 아동 대상/비대상을 자동 결정하지 않음. 아동·혼합 대상이라면 Families 정책, UMP under-age 태그와 광고 요청의 아동 관련 설정을 별도로 검토. 현재 기본 ConsentRequestParameters만 사용하므로 아동 처리가 완성됐다고 볼 수 없음.
6. 안전한 전송과 로컬 저장의 범위를 구별. 로컬 JSON이 암호화돼 있다고 주장하지 않음. 문의·변경 고지 방식 명시.

Play는 스토어와 **앱 내부 양쪽에서** 정책 URL에 접근할 수 있어야 한다고 안내합니다. 현재 UMP Privacy Options는 개인정보 처리방침 링크를 대신하지 않습니다. URL과 게시 문구가 아직 없어 이번 작업에서 새 UI를 만들지 않았으며, **앱 내부 정책 링크 연결은 출시 전 남은 작업**입니다. [공식 앱 검토 준비 안내](https://support.google.com/googleplay/android-developer/answer/9859455).

## 개발자 웹사이트와 app-ads.txt

소유자가 관리하는 공개 HTTPS 웹사이트, 문의 수단, 정책 페이지를 마련하고 Play 스토어의 개발자 웹사이트에 등록합니다. AdMob이 제공하는 publisher별 app-ads.txt 줄을 그대로 복사해 해당 호스트의 `/app-ads.txt`에 게시하고 크롤러 검증을 확인합니다. App ID나 광고 단위 ID를 publisher ID 대신 넣지 않습니다. 아직 실제 publisher와 도메인이 없어 파일을 생성·게시하지 않았습니다. [AdMob app-ads.txt 공식 절차](https://support.google.com/admob/answer/9363762).
