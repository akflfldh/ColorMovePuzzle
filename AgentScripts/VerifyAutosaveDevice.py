"""ADB UI save/resume verification. Run --verify only after update-install approval.
Uses actual taps/swipes and screenshots; reads app JSON but never writes app storage.
"""
import argparse, datetime, hashlib, json, pathlib, time
import VerifyStabilizedDevice as ui

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "Artifacts/Autosave/Device"
ui.OUT = OUT
PACKAGE = ui.PACKAGE
ACTIVITY = PACKAGE + "/com.unity3d.player.UnityPlayerGameActivity"
SAVE_DIR = "/sdcard/Android/data/" + PACKAGE + "/files/ColorMovePuzzle"

def read_saves():
    result = {}
    for name in ui.adb("shell", "run-as", PACKAGE, "ls", SAVE_DIR).decode().split():
        if name.endswith(".json") or name.endswith(".json.bak"):
            assert "/" not in name and ".." not in name
            text = ui.adb("exec-out", "run-as", PACKAGE, "cat", SAVE_DIR + "/" + name).decode("utf-8")
            result[name] = json.loads(text)
    return result

def backup():
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / "BeforeUpdate.json"
    if path.exists(): raise RuntimeError("Preserve the existing pre-update backup")
    values = read_saves()
    path.write_text(json.dumps(values, ensure_ascii=False, indent=2), encoding="utf-8")
    originals = OUT / "SavesBeforeUpdate"; originals.mkdir(exist_ok=True)
    hashes = {}
    for name in values:
        raw = ui.adb("exec-out", "run-as", PACKAGE, "cat", SAVE_DIR + "/" + name)
        (originals / name).write_bytes(raw); hashes[name] = hashlib.sha256(raw).hexdigest()
    (OUT / "HashesBeforeUpdate.json").write_text(json.dumps(hashes, indent=2), encoding="utf-8")
    print("Read-only pre-update save backup captured", flush=True)

def launch(name):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / (name + "-launch.txt")).write_bytes(ui.adb("shell", "am", "start", "-W", "-n", ACTIVITY))
    time.sleep(5)
    return ui.adb("shell", "pidof", PACKAGE).decode().strip()

def verify():
    models = json.loads((ROOT / "Artifacts/AndroidStabilization/DeviceModels.json").read_text(encoding="utf-8-sig"))
    before = json.loads((OUT / "BeforeUpdate.json").read_text(encoding="utf-8"))
    report = {"passed": False, "input": "ADB tap/swipe/keyevent; not human touch feel", "moves": []}
    try:
        installed = read_saves()
        report["preLaunchDataPreserved"] = installed == before
        assert report["preLaunchDataPreserved"], "Update changed save data before first launch"
        old_hashes = json.loads((OUT / "HashesBeforeUpdate.json").read_text())
        report["updateFileHashesUnchanged"] = all(hashlib.sha256(ui.adb("exec-out", "run-as", PACKAGE,
            "cat", SAVE_DIR + "/" + name)).hexdigest() == digest for name, digest in old_hashes.items())
        assert report["updateFileHashesUnchanged"]
        pid = launch("first")
        report["firstPid"] = pid
        boxes, scale = ui.menu(ui.capture("main-menu"))
        offset = boxes[0]["y"] - 1315 * scale
        ui.tap(boxes[2]["x"], boxes[2]["y"])
        for _ in range(3): ui.swipe((540 * scale, 800 * scale), (540 * scale, 1600 * scale), 400)
        visible = ui.cards(ui.capture("five-selection"), True)
        ui.tap(visible[0]["x"], visible[0]["y"])
        cells, centers = ui.board(ui.capture("five-start"), 5, models)
        initial = next(s["cells"] for s in models["stages"] if s["size"] == 5 and s["number"] == 1)
        assert cells == initial
        previous = None
        session_name = "progress-5x5-v1.json.session-v1.json"
        for count in range(1, 4):
            legal = [(f, t) for f in range(25) for t in ui.neighbors(cells, 5, f) if previous != (t, f)]
            source, target = legal[0]
            if len(ui.neighbors(cells, 5, source)) == 1: ui.tap(*centers[source])
            else: ui.swipe(centers[source], centers[target])
            cells[source], cells[target] = cells[target], cells[source]
            assert ui.board(ui.capture(f"move-{count}"), 5, models)[0] == cells
            saved = read_saves()[session_name]
            assert saved["cells"] == cells and saved["moveCount"] == count and saved["active"]
            report["moves"].append({"count": count, "from": source, "to": target, "cells": cells.copy(), "savedImmediately": True})
            print(f"5x5 move {count}: UI and durable JSON match", flush=True)
            previous = source, target
            if count == 2:
                # Home/focus loss, then foreground the existing process without terminating it.
                ui.adb("shell", "input", "keyevent", 3); time.sleep(1)
                same_pid = launch("foreground")
                assert same_pid == pid
                assert ui.board(ui.capture("home-return"), 5, models)[0] == cells
                report["homeForegroundPreserved"] = True
        committed = read_saves()[session_name]
        (OUT / "BeforeTermination.json").write_text(json.dumps(read_saves(), indent=2), encoding="utf-8")
        (OUT / "LogcatFirst.txt").write_bytes(ui.adb("logcat", "-d", "-v", "threadtime", "--pid=" + pid))
        ui.adb("shell", "am", "force-stop", PACKAGE); time.sleep(1)
        second = launch("relaunch"); report["relaunchPid"] = second
        assert second != pid
        boxes, scale = ui.menu(ui.capture("relaunch-menu")); offset = boxes[0]["y"] - 1315 * scale
        ui.tap(540 * scale, 948 * scale + offset)
        restored, _ = ui.board(ui.capture("continue-restored"), 5, models)
        after = read_saves()
        assert restored == cells and after[session_name] == committed
        report["restoredCells"] = restored
        report["restoredMoveCount"] = after[session_name]["moveCount"]
        report["continueRestored"] = True
        # Verify next real move is accepted after restore, then undo via a legal reverse move.
        source, target = next((f, t) for f in range(25) for t in ui.neighbors(cells, 5, f))
        ui.swipe(centers[source], centers[target])
        moved = cells.copy(); moved[source], moved[target] = moved[target], moved[source]
        assert ui.board(ui.capture("after-resume-move"), 5, models)[0] == moved
        assert read_saves()[session_name]["moveCount"] == 4
        ui.swipe(centers[target], centers[source])
        assert ui.board(ui.capture("after-resume-reverse"), 5, models)[0] == cells
        assert read_saves()[session_name]["moveCount"] == 5
        report["inputAfterRestore"] = True
        ui.back()
        try:
            ui.cards(ui.capture("back-selection")); report["androidBackToSelection"] = True
        except AssertionError:
            report["androidBackToSelection"] = False
            ui.tap(190 * scale, 100 * scale + offset)
            ui.cards(ui.capture("back-selection-ui"))
        ui.back()
        try:
            ui.menu(ui.capture("back-main-menu")); report["androidBackToMenu"] = True
        except AssertionError:
            report["androidBackToMenu"] = False
            ui.tap(160 * scale, 94 * scale + offset)
            ui.menu(ui.capture("back-main-menu-ui"))
        ui.tap(540 * scale, 948 * scale + offset)
        assert ui.board(ui.capture("final-resumed-board"), 5, models)[0] == cells
        current = read_saves()
        for name, data in before.items():
            if name.endswith(".bak"): continue
            if "clearedStages" in data: assert current[name]["clearedStages"] == data["clearedStages"]
        profile = current["menu-profile-v1.json"]
        old_profile = before["menu-profile-v1.json"]
        assert profile["bgmVolume"] == old_profile["bgmVolume"] and profile["sfxVolume"] == old_profile["sfxVolume"]
        assert profile["lastGridSize"] == 5 and profile["lastStage"] == 1
        report["legacyClearsAndVolumesPreserved"] = True
        report["finalMoveCount"] = current[session_name]["moveCount"]
        (OUT / "AfterVerification.json").write_text(json.dumps(current, indent=2), encoding="utf-8")
        print("Cold-start Continue restored the exact board and 3 moves; subsequent moves and BACK checked", flush=True)
        # Dex finalizer was delayed in prior runs. Observe without filtering it away.
        time.sleep(40)
        (OUT / "LogcatRelaunch.txt").write_bytes(ui.adb("logcat", "-d", "-v", "threadtime", "--pid=" + second))
        assert ui.adb("shell", "pidof", PACKAGE).decode().strip() == second
        report["passed"] = True
    except Exception as error:
        report["error"] = repr(error)
        raise
    finally:
        report["finishedUtc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        (OUT / "Verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--backup", action="store_true"); parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    if args.backup: backup()
    elif args.verify: verify()
