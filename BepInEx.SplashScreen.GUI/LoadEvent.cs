namespace BepInEx.SplashScreen
{
    public enum LoadEvent
    {
        None = 0,
        PreloaderStart,
        PreloaderFinish,
        ChainloaderStart,
        ChainloaderFinish,
        LoadFinished,
    }
    public enum SNRLoadEvent
    {
        None = 0,
        RoleloaderStart,
        RoleloaderFinish,
        AssetBundleLoadStart,
        AssetBundleLoadFinish,
        RpcLoadStart,
        RpcLoadFinish,
        CustomOptionLoadStart,
        CustomOptionLoadFinish,
        LoadEventListenerStart,
        LoadEventListenerFinish,
        LoadTrophyStart,
        LoadTrophyFinish,
        LoadFinished,
    }
}
