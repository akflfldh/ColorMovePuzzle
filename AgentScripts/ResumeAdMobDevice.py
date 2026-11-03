"""Resume the already displayed/closed test ad; no reinstall or second ad request.
Only normal ADB UI actions and read-only save inspection. Requires stage 2 on screen.
"""
import json, time
import VerifyAdMobDevice as a


def run():
    before = json.loads((a.OUT / "BeforeResume20261028/BeforeUpdate.json").read_text())
    previous_path = a.OUT / "Verification.json"
    report = json.loads(previous_path.read_text())
    failed = a.OUT / "ExerciseInitialCloseAttempt.json"
    if not failed.exists(): failed.write_bytes(previous_path.read_bytes())
    report["initialCloseAttempt"] = report.pop("error", None)
    report["closeMethod"] = "Visible native X at (1012,166), verified from ad-close-current.png"
    report["running"] = True
    try:
        assert report["adShown"] and report["nativeAdActivityObserved"]
        pid = a.ui.adb("shell", "pidof", a.PACKAGE).decode().strip()
        cells, centers = a.ui.board(a.ui.capture("resume-next-stage"), 3, a.MODELS)
        assert cells == a.model(3, 2)["cells"]
        current = a.base.read_saves()[a.session_name(3)]
        assert current["stageNumber"] == 2 and current["moveCount"] == 0
        native = a.log(pid, "LogcatAfterExplicitClose.txt")
        assert native.count("[Ads] Test interstitial opened.") == 1
        assert native.count("[Ads] Interstitial ended; resuming the stage transition.") == 1
        fence = a.frequency()
        assert fence["hasShown"] and not fence["pendingShow"] and fence["clearsSinceAd"] == 0
        report.update(adClosed=True, nextStageAfterCloseOrSkip=2, openedCallbackCount=1,
                      closedCallbackCount=1, frequencyAfterClose=fence)
        report["modes"].append(dict(size=3, nativeMove=True, legitimateClears=True))
        print("Native test ad opened/closed once; stage 2 and persisted cooldown verified", flush=True)

        move = a.model(3, 2)["solution"][0]
        a.ui.swipe(centers[move["from"]], centers[move["to"]])
        committed = a.base.read_saves()[a.session_name(3)]
        before_cells = a.ui.board(a.ui.capture("before-process-stop"), 3, a.MODELS)[0]
        assert before_cells == committed["cells"] and committed["moveCount"] == 1
        a.save("BeforeProcessStop.json", a.base.read_saves())
        a.log(pid, "LogcatFirst.txt")
        a.ui.adb("shell", "am", "force-stop", a.PACKAGE)
        time.sleep(1)
        second = a.base.launch("relaunch")
        assert second != pid
        report.update(firstPid=pid, relaunchPid=second)
        boxes, scale = a.ui.menu(a.ui.capture("relaunch-main"))
        offset = boxes[0]["y"] - 1315 * scale
        a.ui.tap(540 * scale, 948 * scale + offset)
        restored = a.ui.board(a.ui.capture("relaunch-continue"), 3, a.MODELS)[0]
        assert restored == before_cells
        assert a.base.read_saves()[a.session_name(3)] == committed
        report.update(continueAfterRestart=True, restoredStage=2, restoredSize=3,
                      restoredMoveCount=committed["moveCount"], restoredCells=restored)
        print("New process Continue restored the exact stage 2 cells and move count 1", flush=True)

        a.ui.back()
        try:
            a.ui.cards(a.ui.capture("android-back-selection"))
            report["androidBackToSelection"] = True
        except AssertionError:
            report["androidBackToSelection"] = False
            a.ui.tap(190 * scale, 100 * scale + offset)
            a.ui.cards(a.ui.capture("back-selection-fallback"))
        a.ui.back()
        try:
            a.ui.menu(a.ui.capture("android-back-menu"))
            report["androidBackToMenu"] = True
        except AssertionError:
            report["androidBackToMenu"] = False
            a.ui.tap(160 * scale, 94 * scale + offset)
            a.ui.menu(a.ui.capture("back-menu-fallback"))
        a.ui.tap(540 * scale, 948 * scale + offset)
        assert a.ui.board(a.ui.capture("final-continue"), 3, a.MODELS)[0] == before_cells
        current = a.base.read_saves()
        for name, data in before.items():
            if name.endswith(".bak") or "clearedStages" not in data: continue
            assert set(data["clearedStages"]).issubset(current[name]["clearedStages"])
        old_profile = before["menu-profile-v1.json"]
        new_profile = current["menu-profile-v1.json"]
        assert all(old_profile[k] == new_profile[k] for k in ("bgmVolume", "sfxVolume"))
        old_five = before[a.session_name(5)]
        new_five = current[a.session_name(5)]
        assert old_five["cells"] == new_five["cells"]
        assert new_five["moveCount"] == old_five["moveCount"] + 2
        assert new_five["stageNumber"] == old_five["stageNumber"]
        assert current["ads-frequency-v1.json"]["hasShown"]
        assert not current["ads-frequency-v1.json"]["pendingShow"]
        report.update(legacyClearsAndSettingsPreserved=True, fiveOriginalBoardPreserved=True,
                      frequencyRestored=True)
        a.save("AfterVerification.json", current)
        print("Existing cleared stages, volume settings and original 5x5 board preserved", flush=True)
        time.sleep(40)
        a.log(second, "LogcatRelaunch.txt")
        assert a.ui.adb("shell", "pidof", a.PACKAGE).decode().strip() == second
        report["passedGameFlow"] = True
    except Exception as error:
        report["error"] = repr(error)
        raise
    finally:
        report["running"] = False
        a.save("Verification.json", report)
    print(json.dumps({k:v for k,v in report.items() if not k.startswith("frequency")}), flush=True)


if __name__ == "__main__": run()
