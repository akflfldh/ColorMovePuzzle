"""Compare protected project content against the phase-11 pre-change backup."""
import hashlib, json, pathlib, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Artifacts/ClosedTest'

def digest(data):
    return hashlib.sha256(data).hexdigest().upper()

def run():
    changed = []
    with zipfile.ZipFile(OUT / 'BeforeChanges.zip') as archive:
        for entry in archive.infolist():
            if entry.is_dir():
                continue
            relative = entry.filename.replace('\\', '/')
            path = ROOT / relative
            if not path.is_file() or digest(path.read_bytes()) != digest(archive.read(entry)):
                changed.append(relative)
    baseline = json.loads((ROOT / 'Artifacts/AdMob/DataBefore.json').read_text(encoding='utf-8-sig'))
    data_changed = [item['path'] for item in baseline
                    if digest((ROOT / item['path']).read_bytes()) != item['sha256']]
    scene = 'Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity'
    protected = [path for path in changed if path.startswith(('Assets/ColorMovePuzzle/Core/',
        'Assets/ColorMovePuzzle/Data/')) or path == scene or path in (
        'Packages/manifest.json', 'Packages/packages-lock.json',
        'Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset')]
    report = dict(existingDataFiles=len(baseline), stageDataUnchanged=not data_changed,
        changedDataFiles=data_changed, sceneAndCoreAndPackagesAndSdkSettingsUnchanged=not protected,
        changedProtectedFiles=protected, changedExistingFiles=changed)
    (OUT / 'Integrity.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))
    assert not protected and not data_changed, 'Protected project content changed'

if __name__ == '__main__':
    run()
