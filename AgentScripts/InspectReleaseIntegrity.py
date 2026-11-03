import hashlib, json, pathlib, zipfile
ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "Artifacts/ReleasePreparation"
changed = []
with zipfile.ZipFile(OUT / "BeforeChanges.zip") as archive:
    for entry in archive.infolist():
        relative = entry.filename.replace('\\', '/')
        path = ROOT / relative
        if not path.is_file() or hashlib.sha256(path.read_bytes()).digest() != hashlib.sha256(archive.read(entry)).digest():
            changed.append(relative)
data = json.loads((ROOT / "Artifacts/AdMob/DataBefore.json").read_text(encoding="utf-8-sig"))
data_changed = [item["path"] for item in data if hashlib.sha256((ROOT / item["path"]).read_bytes()).hexdigest().upper() != item["sha256"]]
scene = 'Assets/ColorMovePuzzle/Scenes/ColorMovePuzzle.unity'
protected = [p for p in changed if p.startswith('Assets/ColorMovePuzzle/Core/') or p == scene
             or p.startswith('Assets/ColorMovePuzzle/Data/') or p in ('Packages/manifest.json', 'Packages/packages-lock.json')]
report = dict(existingDataFiles=len(data), stageDataUnchanged=not data_changed,
              sceneAndCoreAndPackagesUnchanged=not protected,
              changedProtectedFiles=protected, changedExistingFiles=changed)
(OUT / 'Integrity.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
