using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;

namespace BepInEx.SplashScreen
{
    internal sealed class LoadingLogListener : ILogListener
    {
        private LoadingLogListener() { }

        public static LoadingLogListener StartListening()
        {
            var l = new LoadingLogListener();
            Logger.Listeners.Add(l);
            return l;
        }

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            // This is not thread safe and can cause collection modifiex exceptions in random places. If called from inside a LogEvent it's 100% going to happen.
            // Logger.Listeners.Remove(this);
        }

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (_disposed) return;

            if (eventArgs.Source is ManualLogSource mls && mls.SourceName == "BepInEx" && eventArgs.Data is string message)
            {
                try
                {
                    switch (message)
                    {
                        case "Preloader started":
                            BepInExSplashScreenPatcher.SendStatus(LoadEvent.PreloaderStart);
                            break;
                        case "Preloader finished":
                            BepInExSplashScreenPatcher.SendStatus(LoadEvent.PreloaderFinish);
                            break;

                        case "Chainloader started":
                            BepInExSplashScreenPatcher.SendStatus(LoadEvent.ChainloaderStart);
                            break;
                        case "Chainloader startup complete":
                            BepInExSplashScreenPatcher.SendStatus(LoadEvent.ChainloaderFinish);

                            Dispose();

                            // Have to do this indirectly to avoid referencing the MonoBehaviour class
                            var threadingHelper = Traverse.CreateWithType("BepInEx.ThreadingHelper").Property("Instance");
                            threadingHelper.Method("StartSyncInvoke", new Type[] { typeof(Action) }).GetValue(new Action(() =>
                            {
                                threadingHelper.Method("StartCoroutine", new Type[] { typeof(IEnumerator) }).GetValue(DelayedCo());
                                // BepInExSplashScreenPatcher.SendStatus(LoadEvent.LoadFinished);
                            }));
                            break;

                        default:
                            const string patching = "Patching ";
                            const string skipping = "Skipping ";
                            const string loading = "Loading ";
                            if (message.StartsWith(patching) || message.StartsWith(loading))
                            {
                                BepInExSplashScreenPatcher.SendMessage(message);
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    BepInExSplashScreenPatcher.Logger.LogError($"Crash in {nameof(LogEvent)}, aborting. Exception: {e}");
                    BepInExSplashScreenPatcher.Dispose();
                }
            }
        }

        private static IEnumerator DelayedCo()
        {
            for (int i = 0; i < 10; i++)
                yield return null;
            BepInExSplashScreenPatcher.SendStatus(LoadEvent.LoadFinished);
        }
    }
}
