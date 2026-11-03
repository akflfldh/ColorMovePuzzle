using System;

namespace ColorMovePuzzle.Ads
{
    public sealed class AdConsentController
    {
        readonly IAdBackend backend;
        bool started, updateFinished, formPending, formInFlight;
        public bool Busy { get; private set; }
        public bool CanRequestAds => started && updateFinished && !Busy && !formPending && !formInFlight && backend.CanRequestAds;
        public bool PrivacyOptionsRequired => updateFinished && backend.PrivacyOptionsRequired;
        public AdConsentController(IAdBackend source) { backend = source; }
        public void Start()
        {
            if (started) return; started = true;
            backend.UpdateConsent(success => {
                if (updateFinished) return;
                updateFinished = true; formPending = success;
                UnityEngine.Debug.Log(success ? "[Ads] UMP consent information updated." : "[Ads] UMP update failed; using only the SDK's request-permission decision.");
            });
        }
        public void Tick(bool safeScreen)
        {
            if (!formPending || Busy || formInFlight || (!safeScreen && backend.ConsentRequired)) return;
            formPending = false; formInFlight = true; Busy = backend.ConsentRequired;
            // The SDK guarantees no form is shown when consent is not required.
            // A late non-required response must not force the player back to a menu.
            backend.ShowRequiredConsent(() => { formInFlight = false; Busy = false; UnityEngine.Debug.Log("[Ads] UMP form check finished."); });
        }
        public bool ShowPrivacyOptions()
        {
            if (!PrivacyOptionsRequired || Busy) return false;
            Busy = true;
            backend.ShowPrivacyOptions(() => Busy = false);
            return true;
        }
    }
}
