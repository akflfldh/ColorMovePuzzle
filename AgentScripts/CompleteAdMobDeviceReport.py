"""Read-only completion after host interruption during final Logcat observation.
Validate saved UI evidence and live state; never repeat an ad or gameplay action.
"""
import json, re
import VerifyAdMobDevice as a


def run():
    report = json.loads((a.OUT / "ExerciseInitialCloseAttempt.json").read_text())
    before = json.loads((a.OUT / "BeforeResume20261028/BeforeUpdate.json").read_text())
    stopped = json.loads((a.OUT / "BeforeProcessStop.json").read_text())
    after = json.loads((a.OUT / "AfterVerification.json").read_text())
    session = a.session_name(3)
    committed = stopped[session]
    assert committed["stageNumber"] == 2 and committed["moveCount"] == 1
    assert after[session] == committed
    for name in ("before-process-stop", "relaunch-continue", "final-continue"):
        cells, _ = a.ui.board(a.ui.png(a.OUT / (name + ".png")), 3, a.MODELS)
        assert cells == committed["cells"]
    a.ui.menu(a.ui.png(a.OUT / "relaunch-main.png"))
    a.ui.cards(a.ui.png(a.OUT / "back-selection-fallback.png"))
    a.ui.menu(a.ui.png(a.OUT / "android-back-menu.png"))
    try:
        a.ui.cards(a.ui.png(a.OUT / "android-back-selection.png"))
        back_selection = True
    except AssertionError:
        back_selection = False
    live = a.base.read_saves()
    assert live[session] == committed
    assert a.ui.board(a.ui.capture("completion-live-continue"), 3, a.MODELS)[0] == committed["cells"]
    for name, data in before.items():
        if name.endswith(".bak") or "clearedStages" not in data: continue
        assert set(data["clearedStages"]).issubset(live[name]["clearedStages"])
    assert all(before["menu-profile-v1.json"][k] == live["menu-profile-v1.json"][k]
               for k in ("bgmVolume", "sfxVolume"))
    assert before[a.session_name(5)]["cells"] == live[a.session_name(5)]["cells"]
    assert live[a.session_name(5)]["moveCount"] == before[a.session_name(5)]["moveCount"] + 2
    assert live["ads-frequency-v1.json"]["hasShown"]
    assert not live["ads-frequency-v1.json"]["pendingShow"]
    first = (a.OUT / "LogcatFirst.txt").read_text(errors="replace")
    first_pid = re.search(r"^\d\d-\d\d\s+\S+\s+(\d+)\s+", first, re.MULTILINE).group(1)
    second_pid = a.ui.adb("shell", "pidof", a.PACKAGE).decode().strip()
    assert second_pid != first_pid
    second = a.log(second_pid, "LogcatRelaunch.txt")
    assert first.count("[Ads] Test interstitial opened.") == 1
    assert first.count("[Ads] Interstitial ended; resuming the stage transition.") == 1
    report["initialCloseAttempt"] = report.pop("error", None)
    report.update(running=False, adClosed=True,
        closeMethod="Visible native X at (1012,166), verified from ad-close-current.png",
        nextStageAfterCloseOrSkip=2, openedCallbackCount=1, closedCallbackCount=1,
        firstPid=first_pid, relaunchPid=second_pid, continueAfterRestart=True,
        restoredStage=2, restoredSize=3, restoredMoveCount=1, restoredCells=committed["cells"],
        androidBackToSelection=back_selection, androidBackToMenu=True,
        legacyClearsAndSettingsPreserved=True, fiveOriginalBoardPreserved=True,
        frequencyRestored=True, passedGameFlow=True,
        completion="Host interrupted after UI/save assertions; finalized from saved screenshots, durable snapshots and live read-only checks")
    report["modes"].append(dict(size=3, nativeMove=True, legitimateClears=True))
    a.save("Verification.json", report)
    a.save("CompletionLiveSaves.json", live)
    (a.OUT / "ExitInfo.txt").write_bytes(a.ui.adb("shell", "dumpsys", "activity", "exit-info", a.PACKAGE))
    markers = {
        "inputUpdateNone": "InputUpdateType.None", "assetPackProbe": "ClassNotFoundException: Didn't find class \"com.google.android.play.core.assetpacks.AssetPackManager\"",
        "dexFinalizerMessageLines": "java.lang.AssertionError: Failed to close dex file in finalizer.",
        "fatalException": "FATAL EXCEPTION", "fatalSignal": "Fatal signal", "anr": "ANR in ",
        "googleFlagsDiagnostic": "E FilePhenotypeFlags:",
        "sdkCaughtOptionalAssemblyPayload": "Could not load the file 'GoogleMobileAdsNative.Common'",
        "backCallbackWarning": "OnBackInvokedCallback is not enabled",
        "umpUpdated": "[Ads] UMP consent information updated.",
        "sdkInitialized": "[Ads] Google Mobile Ads initialization completed.",
        "testInterstitialLoaded": "[Ads] Test interstitial loaded.",
        "testInterstitialOpened": "[Ads] Test interstitial opened.",
        "testInterstitialEnded": "[Ads] Interstitial ended; resuming the stage transition."}
    summary = {"firstPid":first_pid, "relaunchPid":second_pid,
        "first":{k:first.count(v) for k,v in markers.items()},
        "relaunch":{k:second.count(v) for k,v in markers.items()}}
    a.save("LogSummary.json", summary)
    print(json.dumps(summary), flush=True)
    print("Read-only finalization passed: real test ad closed, next stage and exact Continue restoration confirmed", flush=True)


if __name__ == "__main__": run()
