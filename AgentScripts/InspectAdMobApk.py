import argparse, io, json, pathlib, struct, zipfile
root = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--apk', default='Builds/Android/ColorMovePuzzle-AdMob-Test-Development.apk')
parser.add_argument('--report', default='Artifacts/AdMob/ApkSdkClasses.json')
args = parser.parse_args()
apk = root / args.apk
definitions = set()
with zipfile.ZipFile(apk) as archive:
    for name in archive.namelist():
        if not name.startswith("classes") or not name.endswith(".dex"): continue
        data = archive.read(name)
        string_count, string_offset, type_count, type_offset = struct.unpack_from("<4I", data, 56)
        class_count, class_offset = struct.unpack_from("<2I", data, 96)
        strings = []
        for index in range(string_count):
            offset = struct.unpack_from("<I", data, string_offset + index * 4)[0]
            while data[offset] & 128: offset += 1
            offset += 1
            strings.append(data[offset:data.index(0, offset)].decode("utf-8", errors="replace"))
        for index in range(class_count):
            type_index = struct.unpack_from("<I", data, class_offset + index * 32)[0]
            string_index = struct.unpack_from("<I", data, type_offset + type_index * 4)[0]
            definitions.add(strings[string_index])
    metadata = archive.read("assets/bin/Data/Managed/Metadata/global-metadata.dat")
aar = next((root / "Library/PackageCache").glob("com.google.ads.mobile@*/Plugins/Android/googlemobileads-unity.aar"))
with zipfile.ZipFile(aar) as package:
    with zipfile.ZipFile(io.BytesIO(package.read("classes.jar"))) as jar:
        bridge_classes = ["L" + n[:-6] + ";" for n in jar.namelist() if n.endswith(".class") and "Interstitial" in n and "$" not in n]
report = dict(classesDefined=len(definitions),
    googleMobileAdsApiDefined="Lcom/google/android/gms/ads/MobileAds;" in definitions,
    umpApiDefined="Lcom/google/android/ump/ConsentInformation;" in definitions,
    officialInterstitialBridgeClasses=bridge_classes,
    unityInterstitialBridgeDefined=bool(bridge_classes) and all(c in definitions for c in bridge_classes),
    appUiMetadataReferences=metadata.count(b"Unity.AppUI"))
(root / args.report).write_text(json.dumps(report, indent=2), encoding="utf-8")
assert report["googleMobileAdsApiDefined"] and report["umpApiDefined"] and report["unityInterstitialBridgeDefined"]
assert report["appUiMetadataReferences"] == 0
print(json.dumps(report))
