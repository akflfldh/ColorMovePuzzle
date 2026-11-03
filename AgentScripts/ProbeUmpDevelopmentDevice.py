"""Development-only official UMP geography probe; never edits game saves.
The only reset is ConsentInformation.Reset inside the SDK's Development code.
Does not accept/deny a form or install an APK automatically.
"""
import argparse, hashlib, json, pathlib, re, time
import VerifyAdMobDevice as a

OUT = a.ROOT / "Artifacts/ReleasePreparation/Device"
PRIVATE = a.ROOT / "Artifacts/ReleasePreparation/Private"
a.base.OUT = a.ui.OUT = OUT


def run(region, label=None, wait=20):
    label = label or region
    assert re.fullmatch(r'[A-Za-z0-9_-]+', label)
    OUT.mkdir(parents=True, exist_ok=True); PRIVATE.mkdir(parents=True, exist_ok=True)
    before = a.base.read_saves()
    original = OUT / "BeforeUmpProbes.json"
    if not original.exists(): original.write_text(json.dumps(before, indent=2), encoding="utf-8")
    args = []
    if region != "Normal":
        source = (a.ROOT / "Artifacts/AdMob/Device/LogcatRelaunch.txt").read_text(errors="replace")
        device = re.search(r'addTestDeviceHashedId\("([0-9A-Fa-f]{32})"\)', source).group(1)
        args = ["--es", "cmp.ump.region", region, "--es", "cmp.ump.testDevice", device, "--ez", "cmp.ump.reset", "true"]
    a.ui.adb("shell", "am", "force-stop", a.PACKAGE)
    a.ui.adb("shell", "am", "start", "-W", "-n", a.base.ACTIVITY, *args)
    time.sleep(wait)
    pid = a.ui.adb("shell", "pidof", a.PACKAGE).decode().strip()
    raw = a.ui.adb("logcat", "-d", "-v", "threadtime", "--pid=" + pid)
    (PRIVATE / ("Ump-" + label + ".txt")).write_bytes(raw)
    text = raw.decode(errors="replace")
    a.ui.capture("ump-" + label)
    current = a.base.read_saves()
    # Ad elapsed-time checkpoints may advance, but progress/profile/session bytes must not change.
    game_names = [name for name in before if not name.startswith("ads-")]
    preserved = all(before[name] == current[name] for name in game_names)
    assert preserved, "UMP diagnostics must not alter game saves"
    logs = [line for line in text.splitlines() if "[Ads" in line or ("UserMessagingPlatform:" in line and any(s in line.lower() for s in ("error", "misconfig", "message", "failed")))]
    logs = [re.sub(r'ca-app-pub-[0-9]+[~/][0-9]+|[0-9A-Fa-f]{32}', '<redacted>', line) for line in logs]
    report = dict(region=region, pid=pid, gameSavesPreserved=preserved,
        developerOverrideObserved=("[Ads QA] Developer test geography=" in text),
        umpUpdateSucceeded="[Ads] UMP consent information updated." in text,
        sdkInitialized="[Ads] Google Mobile Ads initialization completed." in text,
        interstitialLoaded="[Ads] Test interstitial loaded." in text,
        formReturned="[Ads QA] UMP form returned;" in text,
        fatalExceptionCount=text.count("FATAL EXCEPTION"), inputNoneCount=text.count("InputUpdateType.None"),
        logs=logs)
    (OUT / ("Ump-" + label + ".json")).write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("region", choices=["EEA", "Other", "Normal"])
    parser.add_argument('--label'); parser.add_argument('--wait', type=float, default=20)
    args = parser.parse_args(); run(args.region, args.label, args.wait)
