#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ColorMovePuzzle.Ads
{
    // Entire type and every call site are excluded from non-Development Players.
    // No serialized test device, forced geography or reset flag is shipped in assets.
    public static class UmpDevelopmentDiagnostics
    {
        public static ConsentRequestParameters CreateRequest()
        {
            var request = new ConsentRequestParameters();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                string region = intent.Call<string>("getStringExtra", "cmp.ump.region");
                string device = intent.Call<string>("getStringExtra", "cmp.ump.testDevice");
                if (region != "EEA" && region != "Other") return request;
                if (string.IsNullOrEmpty(device) || !System.Text.RegularExpressions.Regex.IsMatch(device, @"\A[0-9A-Fa-f]{32}\z"))
                { Debug.Log("[Ads QA] Ignoring geography request without a valid developer test device."); return request; }
                if (!Enum.TryParse(region, out DebugGeography geography)) return request;
                request.ConsentDebugSettings = new ConsentDebugSettings {
                    DebugGeography = geography, TestDeviceHashedIds = new List<string> { device }
                };
                if (intent.Call<bool>("getBooleanExtra", "cmp.ump.reset", false))
                { ConsentInformation.Reset(); Debug.Log("[Ads QA] Developer-requested UMP-only reset completed."); }
                Debug.Log("[Ads QA] Developer test geography=" + region + "; official test ad units remain required.");
            }
            catch (Exception) { Debug.Log("[Ads QA] Developer launch options unavailable; using normal UMP request."); }
#endif
            return request;
        }
    }
}
#endif
