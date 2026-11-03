"""Approved ADB-only test-ad verification; never writes or resets app save data.
Actual frequency limits stay unchanged. Waits for genuine foreground gameplay time.
"""
import argparse, hashlib, json, pathlib, re, time, xml.etree.ElementTree as ET
import VerifyAutosaveDevice as base
import VerifyStabilizedDevice as ui

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "Artifacts/AdMob/Device"
BASELINE = OUT
base.OUT = ui.OUT = OUT
PACKAGE = ui.PACKAGE
MODELS = json.loads((ROOT / "Artifacts/AdMob/DeviceModels.json").read_text(encoding="utf-8-sig"))

def save(name, data):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(json.dumps(data, indent=2), encoding="utf-8")

def log(pid, name):
    raw = ui.adb("logcat", "-d", "-v", "threadtime", "--pid=" + pid)
    (OUT / name).write_bytes(raw)
    return raw.decode(errors="replace")

def smoke():
    before = json.loads((BASELINE / "BeforeUpdate.json").read_text())
    old_hashes = json.loads((BASELINE / "HashesBeforeUpdate.json").read_text())
    unchanged = base.read_saves() == before and all(hashlib.sha256(ui.adb("exec-out", "run-as", PACKAGE,
        "cat", base.SAVE_DIR + "/" + name)).hexdigest() == digest for name, digest in old_hashes.items())
    assert unchanged, "Pre-launch save files differ from the update backup"
    pid = base.launch("startup")
    time.sleep(15)
    image = ui.capture("startup")
    try: ui.menu(image); main = True
    except AssertionError: main = False
    native = log(pid, "LogcatStartup.txt")
    report = dict(pid=pid, updateSaveHashesUnchanged=unchanged, mainMenuVisible=main,
        umpUpdateSucceeded="[Ads] UMP consent information updated." in native,
        umpUpdateFailed="[Ads] UMP update failed" in native,
        initialized="[Ads] Google Mobile Ads initialization completed." in native,
        interstitialLoaded="[Ads] Test interstitial loaded." in native,
        adOpened="[Ads] Test interstitial opened." in native)
    save("Smoke.json", report); print(json.dumps(report), flush=True)

def model(size, number): return next(s for s in MODELS["stages"] if s["size"] == size and s["number"] == number)
def session_name(size): return ("progress-v1.json" if size == 3 else f"progress-{size}x{size}-v1.json") + ".session-v1.json"
def frequency(): return base.read_saves().get("ads-frequency-v1.json", {})
def ready_frequency(state):
    return state.get("totalClears", 0) >= 5 and state.get("gameplaySeconds", 0) >= 180 and (
        not state.get("hasShown") or (state["clearsSinceAd"] >= 4 and state["gameplaySeconds"] - state["lastAdGameplaySeconds"] >= 180))

def prepare_play():
    boxes, scale = ui.menu(ui.capture("timing-menu")); offset = boxes[0]["y"] - 1315 * scale
    profile = base.read_saves()["menu-profile-v1.json"]
    assert profile["hasContinue"]
    ui.tap(540 * scale, 948 * scale + offset)
    size = profile["lastGridSize"]
    cells, centers = ui.board(ui.capture("timing-board"), size, MODELS)
    saved = base.read_saves()[session_name(size)]
    assert cells == saved["cells"] and cells != model(size, profile["lastStage"])["target"]
    empty = cells.index(0); start = time.monotonic()
    print("Real foreground gameplay timing started; empty-cell taps do not change the board", flush=True)
    while time.monotonic() - start < 200:
        ui.tap(*centers[empty]); time.sleep(20)
        print(f"Actual active-play ledger: {frequency().get('gameplaySeconds', 0):.1f}s", flush=True)
    current = base.read_saves()[session_name(size)]
    assert current["cells"] == saved["cells"] and current["moveCount"] == saved["moveCount"]
    save("RealPlayTiming.json", dict(size=size, stage=profile["lastStage"], boardUnchanged=True,
        moveCount=current["moveCount"], realElapsedSeconds=time.monotonic() - start, frequency=frequency()))

def close_test_ad(pid):
    # Only click an explicit native close affordance, never an advertisement CTA.
    try:
        ui.adb("shell", "uiautomator", "dump", "/data/local/tmp/colormove-admob-ui.xml")
        raw = ui.adb("exec-out", "cat", "/data/local/tmp/colormove-admob-ui.xml")
        (OUT / "AdUi.xml").write_bytes(raw)
        root = ET.fromstring(raw)
        for node in root.iter("node"):
            label = " ".join(node.get(k, "") for k in ("content-desc", "text", "resource-id")).lower()
            if "close" not in label and "닫기" not in label: continue
            bounds = list(map(int, re.findall(r"\d+", node.get("bounds", ""))))
            if len(bounds) == 4 and bounds[2] > bounds[0] and bounds[3] > bounds[1]:
                ui.tap((bounds[0] + bounds[2]) / 2, (bounds[1] + bounds[3]) / 2)
                return "native close affordance"
    except Exception as error:
        (OUT / "AdUiDumpError.txt").write_text(repr(error), encoding="utf-8")
    focus = ui.adb("shell", "dumpsys", "window").decode(errors="replace")
    if any("AdActivity" in line and PACKAGE in line for line in focus.splitlines() if "mCurrentFocus" in line):
        ui.back(); return "ADB Back in the native AdActivity"
    return None

def exercise():
    before = json.loads((BASELINE / "BeforeUpdate.json").read_text())
    pid = ui.adb("shell", "pidof", PACKAGE).decode().strip()
    report = dict(input="ADB tap/swipe/keyevent, not human touch feel", modes=[], adShown=False, running=True)
    try:
        boxes, scale = ui.menu(ui.capture("exercise-menu")); offset = boxes[0]["y"] - 1315 * scale
        def to_menu(name):
            ui.tap(190 * scale, 100 * scale + offset); ui.cards(ui.capture(name + "-selection"))
            ui.tap(160 * scale, 94 * scale + offset); ui.menu(ui.capture(name + "-menu"))
        def enter(size, name):
            boxes, _ = ui.menu(ui.capture(name + "-menu")); ui.tap(boxes[size - 3]["x"], boxes[size - 3]["y"])
            for _ in range(3): ui.swipe((540 * scale, 800 * scale), (540 * scale, 1600 * scale), 400)
            visible = ui.cards(ui.capture(name + "-selection"), True); ui.tap(visible[0]["x"], visible[0]["y"])
            cells, centers = ui.board(ui.capture(name + "-board"), size, MODELS)
            assert cells == model(size, 1)["cells"]
            return cells, centers
        profile = before["menu-profile-v1.json"]
        if profile.get("hasContinue"):
            size, number = profile["lastGridSize"], profile["lastStage"]
            original = before.get(session_name(size))
            expected = original["cells"] if original and original.get("active") and original["stageNumber"] == number else model(size, number)["cells"]
            count = original["moveCount"] if original and original.get("active") and original["stageNumber"] == number else 0
            ui.tap(540 * scale, 948 * scale + offset)
            cells, original_centers = ui.board(ui.capture("original-continue"), size, MODELS)
            assert cells == expected
            current = base.read_saves()[session_name(size)]; assert current["moveCount"] == count
            report["originalContinuePreserved"] = dict(size=size, stage=number, moves=count, cellsMatch=True)
            if size == 5:
                source, target = next((f, t) for f in range(25) for t in ui.neighbors(cells, 5, f)
                    if [cells[t] if i == f else cells[f] if i == t else c for i,c in enumerate(cells)] != model(size, number)["target"])
                ui.swipe(original_centers[source], original_centers[target])
                changed = cells.copy(); changed[source], changed[target] = changed[target], changed[source]
                assert ui.board(ui.capture("original-five-moved"), size, MODELS)[0] == changed
                ui.swipe(original_centers[target], original_centers[source])
                assert ui.board(ui.capture("original-five-reversed"), size, MODELS)[0] == cells
                assert base.read_saves()[session_name(size)]["moveCount"] == count + 2
                report["modes"].append(dict(size=5, nativeMove=True, previousBoardRetained=True, movesAfterTest=count + 2))
            to_menu("original")
        # Native input in 4x4, with an ordinary reverse move that also counts as a move.
        cells, centers = enter(4, "four")
        source, target = next((f, t) for f in range(16) for t in ui.neighbors(cells, 4, f))
        ui.swipe(centers[source], centers[target]); changed = cells.copy(); changed[source], changed[target] = changed[target], changed[source]
        assert ui.board(ui.capture("four-moved"), 4, MODELS)[0] == changed
        ui.swipe(centers[target], centers[source]); assert ui.board(ui.capture("four-reversed"), 4, MODELS)[0] == cells
        report["modes"].append(dict(size=4, nativeMove=True)); to_menu("four")
        if not any(m["size"] == 5 for m in report["modes"]):
            cells, centers = enter(5, "five")
            source, target = next((f, t) for f in range(25) for t in ui.neighbors(cells, 5, f))
            ui.swipe(centers[source], centers[target]); changed = cells.copy(); changed[source], changed[target] = changed[target], changed[source]
            assert ui.board(ui.capture("five-moved"), 5, MODELS)[0] == changed
            ui.swipe(centers[target], centers[source]); assert ui.board(ui.capture("five-reversed"), 5, MODELS)[0] == cells
            report["modes"].append(dict(size=5, nativeMove=True)); to_menu("five")
        cells, centers = enter(3, "three"); stage = model(3, 1)
        # Keep the puzzle in progress long enough to satisfy the unchanged 180s limit.
        first, rest = stage["solution"][0], stage["solution"][1:]
        ui.swipe(centers[first["from"]], centers[first["to"]])
        assert ui.board(ui.capture("three-first-move"), 3, MODELS)[0] != stage["target"]
        native_before_wait = log(pid, "LogcatBeforeWait.txt")
        qualify = "[Ads] Google Mobile Ads initialization completed." in native_before_wait
        report["realAdQualificationAttempted"] = qualify
        start = time.monotonic()
        while qualify:
            state = frequency(); needed = max(180, state.get("lastAdGameplaySeconds", 0) + 180 if state.get("hasShown") else 180)
            if state.get("gameplaySeconds", 0) >= needed: break
            if time.monotonic() - start > 300: raise RuntimeError("Foreground gameplay time did not reach the unchanged frequency limit")
            print(f"Waiting for real active gameplay time: {state.get('gameplaySeconds', 0):.0f}/{needed:.0f}s", flush=True)
            time.sleep(20)
        for move in rest: ui.swipe(centers[move["from"]], centers[move["to"]])
        ui.capture("clear-result")
        for replay in range(8 if qualify else 0):
            state = frequency()
            if ready_frequency(state): break
            ui.tap(540 * scale, 1280 * scale + offset)
            assert ui.board(ui.capture(f"replay-{replay}"), 3, MODELS)[0] == stage["cells"]
            for move in stage["solution"]: ui.swipe(centers[move["from"]], centers[move["to"]])
            ui.capture(f"replay-{replay}-clear")
        state = frequency()
        if qualify: assert ready_frequency(state)
        report["frequencyBeforeNext"] = state
        if not qualify: report["notQualifiedReason"] = "Native SDK initialization was not permitted/completed; no consent bypass or fake eligibility was used."
        pre = log(pid, "LogcatBeforeNext.txt")
        assert "[Ads] Test interstitial opened." not in pre, "Unexpected ad before NEXT"
        ui.tap(540 * scale, 1140 * scale + offset); time.sleep(3)
        native = log(pid, "LogcatAfterNext.txt")
        focus = ui.adb("shell", "dumpsys", "window").decode(errors="replace")
        native_ad = any("AdActivity" in line and PACKAGE in line for line in focus.splitlines() if "mCurrentFocus" in line)
        report["adShown"] = "[Ads] Test interstitial opened." in native or native_ad
        report["nativeAdActivityObserved"] = native_ad
        if report["adShown"]:
            ui.capture("test-interstitial")
            report["closeMethod"] = close_test_ad(pid); time.sleep(2)
        next_cells, centers = ui.board(ui.capture("next-stage"), 3, MODELS)
        assert next_cells == model(3, 2)["cells"]
        report["nextStageAfterCloseOrSkip"] = 2
        report["modes"].append(dict(size=3, nativeMove=True, legitimateClears=True))
        move = model(3, 2)["solution"][0]; ui.swipe(centers[move["from"]], centers[move["to"]])
        committed = base.read_saves()[session_name(3)]; before_cells = ui.board(ui.capture("before-process-stop"), 3, MODELS)[0]
        assert before_cells == committed["cells"]
        log(pid, "LogcatFirst.txt")
        ui.adb("shell", "am", "force-stop", PACKAGE); time.sleep(1)
        second = base.launch("relaunch"); report["firstPid"] = pid; report["relaunchPid"] = second; assert second != pid
        boxes, scale = ui.menu(ui.capture("relaunch-main")); offset = boxes[0]["y"] - 1315 * scale
        ui.tap(540 * scale, 948 * scale + offset)
        assert ui.board(ui.capture("relaunch-continue"), 3, MODELS)[0] == before_cells
        assert base.read_saves()[session_name(3)]["moveCount"] == committed["moveCount"]
        report["continueAfterRestart"] = True
        ui.back()
        try: ui.cards(ui.capture("android-back-selection")); report["androidBackToSelection"] = True
        except AssertionError:
            report["androidBackToSelection"] = False
            ui.tap(190 * scale, 100 * scale + offset); ui.cards(ui.capture("back-selection-fallback"))
        ui.back()
        try: ui.menu(ui.capture("android-back-menu")); report["androidBackToMenu"] = True
        except AssertionError:
            report["androidBackToMenu"] = False
            ui.tap(160 * scale, 94 * scale + offset); ui.menu(ui.capture("back-menu-fallback"))
        ui.tap(540 * scale, 948 * scale + offset)
        assert ui.board(ui.capture("final-continue"), 3, MODELS)[0] == before_cells
        current = base.read_saves()
        for name, data in before.items():
            if name.endswith(".bak") or "clearedStages" not in data: continue
            assert set(data["clearedStages"]).issubset(current[name]["clearedStages"])
        original_profile = before["menu-profile-v1.json"]; current_profile = current["menu-profile-v1.json"]
        assert original_profile["bgmVolume"] == current_profile["bgmVolume"] and original_profile["sfxVolume"] == current_profile["sfxVolume"]
        report["legacyClearsAndSettingsPreserved"] = True
        save("AfterVerification.json", current)
        time.sleep(40); log(second, "LogcatRelaunch.txt")
        report["passedGameFlow"] = True
        print(json.dumps({k:v for k,v in report.items() if k != "frequencyBeforeNext"}), flush=True)
    except Exception as error:
        report["error"] = repr(error); raise
    finally:
        report["running"] = False; save("Verification.json", report)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--backup", action="store_true"); parser.add_argument("--smoke", action="store_true"); parser.add_argument("--exercise", action="store_true"); parser.add_argument("--baseline-folder", default=""); parser.add_argument("--prepare-play", action="store_true")
    args = parser.parse_args()
    if args.baseline_folder:
        assert re.fullmatch(r"[A-Za-z0-9_-]+", args.baseline_folder)
        BASELINE = OUT / args.baseline_folder
    if args.backup: base.OUT = BASELINE
    if args.backup: base.backup()
    elif args.smoke: smoke()
    elif args.exercise: exercise()
    elif args.prepare_play: prepare_play()
