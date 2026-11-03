"""Inspect the unsigned Release bundle and generated IL2CPP before upload-key creation/signing."""
import hashlib, json, pathlib, re, struct, subprocess, xml.etree.ElementTree as ET, zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Artifacts/ClosedTest'
BUNDLE=ROOT/'Builds/Android/Intermediate/ColorMovePuzzle-ClosedTest-unsigned.aab'
JAVA=pathlib.Path('C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe')
TOOL=JAVA.parents[2]/'Tools/bundletool-all-1.17.2.jar'

def tool(*args):
    result=subprocess.run([str(JAVA),'-jar',str(TOOL),*args],capture_output=True,text=True,encoding='utf-8',errors='replace')
    assert result.returncode==0,result.stderr
    return result.stdout

def native_elf(data):
    assert data[:6]==b'\x7fELF\x02\x01'
    assert struct.unpack_from('<H',data,18)[0]==183
    offset=struct.unpack_from('<Q',data,32)[0]
    size,count=struct.unpack_from('<HH',data,54)
    loads=[struct.unpack_from('<Q',data,offset+i*size+48)[0] for i in range(count) if struct.unpack_from('<I',data,offset+i*size)[0]==1]
    assert loads and all(v>=16384 for v in loads)
    return loads

def method_body(source,name):
    match=re.search(r'\b'+name+r'_m[0-9A-F]+\s*\([^;\n]*\)\s*\{',source)
    assert match,'Missing compiled method '+name
    start=source.index('{',match.start()); depth=1; end=start+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}'); end+=1
    return source[match.start():end]

def run():
    settings=(ROOT/'Assets/ColorMovePuzzle/Editor/ClosedTestReleaseSettings.asset').read_text(encoding='utf-8-sig')
    expected_name=re.search(r'^  versionName:\s*(.+)$',settings,re.MULTILINE).group(1).strip().strip('"')
    expected_code=int(re.search(r'^  versionCode:\s*(\d+)$',settings,re.MULTILINE).group(1))
    validation=tool('validate','--bundle='+str(BUNDLE))
    (OUT/'BundletoolValidateUnsigned.txt').write_text(validation,encoding='utf-8')
    xml=tool('dump','manifest','--bundle='+str(BUNDLE),'--module=base')
    (OUT/'UnsignedManifest.xml').write_text(xml,encoding='utf-8')
    manifest=ET.fromstring(xml); android='{http://schemas.android.com/apk/res/android}'
    assert manifest.get('package')=='com.colormovepuzzle.game'
    assert manifest.get(android+'versionCode')==str(expected_code) and manifest.get(android+'versionName')==expected_name
    sdk=manifest.find('uses-sdk'); assert sdk.get(android+'minSdkVersion')=='26' and sdk.get(android+'targetSdkVersion')=='36'
    app=manifest.find('application'); assert app.get(android+'debuggable','false')=='false'
    assert all(p.get(android+'shell','false')=='false' for p in app.findall('profileable'))
    metas={m.get(android+'name'):m.get(android+'value') for m in app.findall('meta-data')}
    assert metas['com.google.android.gms.ads.APPLICATION_ID']=='ca-app-pub-9556737105305150~6563007441'
    unity=[n for n in list(app) if n.tag=='activity' and 'UnityPlayerGameActivity' in n.get(android+'name','')]
    # Bundletool emits compiled Android enum values; ActivityInfo.SCREEN_ORIENTATION_PORTRAIT = 1.
    assert unity and unity[0].get(android+'screenOrientation') in ('portrait','1')
    config=tool('dump','config','--bundle='+str(BUNDLE))
    (OUT/'BundleConfig.json').write_text(config,encoding='utf-8')
    with zipfile.ZipFile(BUNDLE) as z:
        signatures=[n for n in z.namelist() if n.upper().startswith('META-INF/') and n.upper().endswith(('.RSA','.DSA','.EC','.SF'))]
        assert not signatures,'Intermediate bundle must be unsigned for separate upload-key signing'
        libs=[n for n in z.namelist() if n.startswith('base/lib/') and n.endswith('.so')]
        assert libs and all(n.startswith('base/lib/arm64-v8a/') for n in libs)
        alignments={n:native_elf(z.read(n)) for n in libs}
        metadata=z.read('base/assets/bin/Data/Managed/Metadata/global-metadata.dat')
        assert b'UmpDevelopmentDiagnostics' not in metadata and b'cmp.ump.region' not in metadata and b'cmp.ump.reset' not in metadata
        assert b'Unity.AppUI' not in metadata
    source='\n'.join(p.read_text(errors='replace') for p in (ROOT/'Library/Bee/artifacts/Android/il2cppOutput/cpp').glob('ColorMovePuzzle.Runtime*.cpp'))
    methods={name:method_body(source,name) for name in ['AdBuildPolicy_get_AllowAdRequests','GoogleAdBackend_Initialize',
        'GoogleAdBackend_LoadInterstitial','GoogleAdBackend_LoadRewarded','GoogleAdBackend_UpdateConsent','GoogleAdBackend_ShowPrivacyOptions']}
    assert re.search(r'return\s+\(bool\)0\s*;',methods['AdBuildPolicy_get_AllowAdRequests'])
    for name in ('GoogleAdBackend_Initialize','GoogleAdBackend_LoadInterstitial','GoogleAdBackend_LoadRewarded'):
        assert all(api not in methods[name] for api in ('MobileAds_Initialize','InterstitialAd_Load','RewardedAd_Load'))
    assert 'ConsentInformation_Update' in methods['GoogleAdBackend_UpdateConsent']
    assert 'ConsentForm_ShowPrivacyOptionsForm' in methods['GoogleAdBackend_ShowPrivacyOptions']
    (OUT/'CompiledAdPolicy.txt').write_text('\n\n'.join(methods.values()),encoding='utf-8')
    permissions=[n.get(android+'name') for n in manifest.findall('uses-permission')]
    providers=[n.get(android+'name') for n in app.findall('provider')]
    report=dict(passed=True,sha256=hashlib.sha256(BUNDLE.read_bytes()).hexdigest().upper(),bytes=BUNDLE.stat().st_size,
        package='com.colormovepuzzle.game',versionName=expected_name,versionCode=expected_code,minApi=26,targetApi=36,
        debuggable=False,managedVariant='Release',channel='ClosedTestNoAds',unsigned=True,
        arm64Only=True,elf16KCompatible=True,nativeLibraries=alignments,
        compiledAdRequestsDisabled=True,compiledSdkAdCallsRemoved=True,umpAndPrivacyCompiled=True,
        developmentUmpControlsAbsent=True,appUiMetadataAbsent=True,permissions=permissions,providers=providers,
        includesAdvertisingIdPermission='com.google.android.gms.permission.AD_ID' in permissions)
    (OUT/'BundlePreflight.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='nativeLibraries'}))

if __name__=='__main__':run()
