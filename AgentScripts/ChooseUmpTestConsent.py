"""ADB click of an explicitly labeled Google sample UMP choice, not live publisher consent."""
import argparse, json, re, time, xml.etree.ElementTree as ET
import ProbeUmpDevelopmentDevice as p
a = p.a
a.base.OUT = a.ui.OUT = p.OUT


def run(choice, label, verified_visible=False, return_settings=False):
    assert re.fullmatch(r'[A-Za-z0-9_-]+', label)
    before = a.base.read_saves()
    pid = a.ui.adb('shell', 'pidof', a.PACKAGE).decode().strip()
    initial = a.ui.adb('logcat', '-d', '-v', 'threadtime', '--pid=' + pid).decode(errors='replace')
    assert '[Ads QA] Developer test geography=EEA;' in initial
    a.ui.adb('shell','uiautomator','dump','/data/local/tmp/cmp-ump-choice.xml')
    raw = a.ui.adb('exec-out','cat','/data/local/tmp/cmp-ump-choice.xml')
    root = ET.fromstring(raw)
    expected = 'Consent' if choice == 'accept' else 'Do not consent'
    nodes = [n for n in root.iter('node') if n.get('text','').strip() == expected or n.get('content-desc','').strip() == expected]
    if nodes:
        assert b'Publisher Test Ads' in raw
        bounds = list(map(int,re.findall(r'\d+',nodes[0].get('bounds',''))))
        assert len(bounds) == 4
        point = ((bounds[0]+bounds[2])/2,(bounds[1]+bounds[3])/2)
        origin = 'ADB click of exact native UMP button text'
    else:
        assert verified_visible and b'android.webkit.WebView' in raw
        manifest = (a.ROOT / 'Artifacts/ReleasePreparation/ApkManifest.txt').read_text(encoding='utf-16')
        assert 'ca-app-pub-3940256099942544~3347511713' in manifest
        image = a.ui.capture('ump-' + label + '-before-tap')
        try: a.ui.menu(image); menu_visible = True
        except AssertionError: menu_visible = False
        assert not menu_visible and image[0] == 1080 and image[1] == 2340
        # Exact button centers from the just-inspected sample form screenshot, not an ad CTA.
        point = (540, 1702 if choice == 'accept' else 1854)
        origin = 'ADB tap of visually verified sample UMP button; WebView provides no accessibility text'
    a.ui.tap(*point)
    time.sleep(8)
    image = a.ui.capture('ump-' + label + '-after')
    if return_settings:
        # DONE center verified from privacy-settings.png in this device's unchanged UI.
        a.ui.tap(540, 1767)
        a.ui.menu(a.ui.capture('ump-' + label + '-done'))
    else:
        a.ui.menu(image)
    native = a.ui.adb('logcat','-d','-v','threadtime','--pid=' + pid).decode(errors='replace')
    (p.PRIVATE / (label + '-choice.txt')).write_text(native, encoding='utf-8')
    purpose = re.findall(r'Writing to storage: \[IABTCF_PurposeConsents\] ([01]+)', native)
    assert purpose, 'No SDK purpose consent result was observed'
    if choice == 'accept': assert '1' in purpose[-1]
    else: assert '1' not in purpose[-1]
    current = a.base.read_saves()
    assert all(before[n] == current[n] for n in before if not n.startswith('ads-'))
    logs = [line for line in native.splitlines() if '[Ads' in line]
    report = dict(choice=choice, input=origin, point=point, pid=pid,
        publisher='Google sample Publisher Test Ads', purposeConsentCount=purpose[-1].count('1'),
        returnedToMenu=True, privacyOptionsReentry=return_settings, gameSavesPreserved=True, sdkInitializations=native.count('[Ads] Google Mobile Ads initialization completed.'),
        testAdsLoaded=native.count('[Ads] Test interstitial loaded.'), logs=logs)
    (p.OUT / (label + '-Choice.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report),flush=True)


if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('choice',choices=['accept','reject']); parser.add_argument('--label',required=True)
    parser.add_argument('--verified-visible-button',action='store_true')
    parser.add_argument('--return-settings',action='store_true')
    args=parser.parse_args(); run(args.choice,args.label,args.verified_visible_button,args.return_settings)
