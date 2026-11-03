"""Finish normal-region QA, summarize PID logs, and verify game bytes remain intact."""
import hashlib, json, re
import ProbeUmpDevelopmentDevice as p
a = p.a
a.base.OUT = a.ui.OUT = p.OUT


def run():
    accept = json.loads((p.OUT / 'EEA-Accept-Choice.json').read_text())
    reject = json.loads((p.OUT / 'EEA-Reject-Choice.json').read_text())
    reentry = json.loads((p.OUT / 'Privacy-Reaccept-Choice.json').read_text())
    assert accept['choice'] == 'accept' and accept['purposeConsentCount'] > 0 and accept['returnedToMenu']
    assert reject['choice'] == 'reject' and reject['purposeConsentCount'] == 0 and reject['returnedToMenu']
    assert reentry['privacyOptionsReentry'] and reentry['sdkInitializations'] == 1 and reentry['purposeConsentCount'] > 0
    normal = (p.PRIVATE / 'Ump-Normal.txt').read_text(errors='replace')
    assert '[Ads QA] Developer test geography=' not in normal
    assert '[Ads QA] Developer-requested UMP-only reset' not in normal
    assert 'status=NotRequired, privacy=NotRequired, canRequest=True' in normal
    boxes, scale = a.ui.menu(a.ui.capture('normal-menu'))
    offset = boxes[0]['y'] - 1315 * scale
    a.ui.tap(540 * scale, 1660 * scale + offset)
    settings = a.ui.capture('normal-settings')
    buttons = a.ui.components(settings, {a.ui.rgb('DFE7F4'):1})
    privacy = [b for b in buttons if abs(b['width']-600*scale)<12*scale and abs(b['height']-68*scale)<12*scale]
    assert not privacy, 'Privacy Options must be hidden when SDK says NotRequired'
    a.ui.tap(540 * scale, 1510 * scale + offset)
    a.ui.menu(a.ui.capture('normal-final-menu'))
    pid = a.ui.adb('shell','pidof',a.PACKAGE).decode().strip()
    (p.PRIVATE / 'Normal-Final.txt').write_bytes(a.ui.adb('logcat','-d','-v','threadtime','--pid='+pid))
    (p.PRIVATE / 'ExitInfo.txt').write_bytes(a.ui.adb('shell','dumpsys','activity','exit-info',a.PACKAGE))
    before = json.loads((a.ROOT / 'Artifacts/ReleasePreparation/DeviceBeforeUpdate/BeforeUpdate.json').read_text())
    hashes = json.loads((a.ROOT / 'Artifacts/ReleasePreparation/DeviceBeforeUpdate/HashesBeforeUpdate.json').read_text())
    current = a.base.read_saves()
    assert all(before[n] == current[n] for n in before if not n.startswith('ads-'))
    raw_unchanged = all(hashlib.sha256(a.ui.adb('exec-out','run-as',a.PACKAGE,'cat',a.base.SAVE_DIR+'/'+n)).hexdigest() == v
                        for n,v in hashes.items() if not n.startswith('ads-'))
    assert raw_unchanged
    assert all(before['ads-frequency-v1.json'][k] == current['ads-frequency-v1.json'][k]
               for k in ('totalClears','clearsSinceAd','hasShown','lastAdGameplaySeconds','pendingShow'))
    log_by_pid = {}
    for path in p.PRIVATE.glob('*.txt'):
        if path.name == 'ExitInfo.txt': continue
        text = path.read_text(errors='replace')
        match = re.search(r'^\d\d-\d\d\s+\S+\s+(\d+)\s+', text, re.MULTILINE)
        if match and len(text) > len(log_by_pid.get(match.group(1),'')):
            log_by_pid[match.group(1)] = text
    patterns = {'inputNone':'InputUpdateType.None','assetPackProbe':'ClassNotFoundException: Didn\'t find class "com.google.android.play.core.assetpacks.AssetPackManager"',
        'dexFinalizerLines':'java.lang.AssertionError: Failed to close dex file in finalizer.',
        'fatalException':'FATAL EXCEPTION','fatalSignal':'Fatal signal','anr':'ANR in ',
        'sdkInitializations':'[Ads] Google Mobile Ads initialization completed.',
        'interstitialLoads':'[Ads] Test interstitial loaded.', 'interstitialDisplays':'[Ads] Test interstitial opened.',
        'backCallbackWarning':'OnBackInvokedCallback is not enabled',
        'sdkOptionalAssemblyDiagnostic':"Could not load the file 'GoogleMobileAdsNative.Common'"}
    logs = {k:{name:v.count(pattern) for name,pattern in patterns.items()} for k,v in log_by_pid.items()}
    assert all(v['sdkInitializations'] <= 1 and not v['interstitialDisplays'] for v in logs.values())
    assert all(not (v['inputNone'] or v['fatalException'] or v['fatalSignal'] or v['anr']) for v in logs.values())
    report = dict(normalLaunchRestored=True, normalPrivacyButtonHidden=True, finalMenuVisible=True,
        gameSaveJsonPreserved=True, gameSaveRawHashesPreserved=raw_unchanged, adFrequencyCountsPreserved=True,
        consentAcceptanceVerified=True, consentRejectionVerified=True, privacyReentryVerified=True,
        noProductionIdsUsed=True, input='ADB; no human touch-feel test', finalPid=pid, logsByPid=logs,
        currentContinue={k:current['menu-profile-v1.json'][k] for k in ('lastGridSize','lastStage')})
    (p.OUT / 'FinalVerification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (p.OUT / 'AfterUmpProbes.json').write_text(json.dumps(current,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)


if __name__ == '__main__': run()
