namespace ColorMovePuzzle.Ads
{
    public static class AdBuildPolicy
    {
        public const string ClosedTestDefine = "COLORMOVE_CLOSED_TEST_NO_ADS";
#if COLORMOVE_CLOSED_TEST_NO_ADS
        public static bool AllowAdRequests => false;
        public static string Channel => "ClosedTestNoAds";
#else
        public static bool AllowAdRequests => true;
        public static string Channel => "Standard";
#endif
    }
}
