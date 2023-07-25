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
            // todo This is not thread safe and can cause collection modifiex exceptions in random places. If called from inside a LogEvent it's 100% going to happen.
            // Logger.Listeners.Remove(this);
        }

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (_disposed) return;

            if (eventArgs.Source.SourceName == "BepInEx" && eventArgs.Data != null)
            {
                try
                {
                    var message = eventArgs.Data as string ?? eventArgs.Data.ToString();
                    if (message == "Chainloader startup complete")
                    {
                        // Nothing to log after this point
                        Dispose();

                        // Wait until the first frame finishes to close the splash screen
                        // Have to do this indirectly to avoid referencing the MonoBehaviour class
                        var threadingHelper = Traverse.CreateWithType("BepInEx.ThreadingHelper").Property("Instance");
                        threadingHelper.Method("StartSyncInvoke", new[] { typeof(Action) })
                                       .GetValue(new Action(() => threadingHelper.Method("StartCoroutine", new[] { typeof(IEnumerator) })
                                                                                 .GetValue(DelayedCo())));
                    }

                    BepInExSplashScreenPatcher.SendMessage(message);
                }
                catch (Exception e)
                {
                    BepInExSplashScreenPatcher.Logger.LogError($"Crash in {nameof(LogEvent)}, aborting. Exception: {e}");
                    BepInExSplashScreenPatcher.Kill();
                }
            }
        }

        private static IEnumerator DelayedCo()
        {
            const int framesUntilFinished = 1;
            for (int i = 0; i < framesUntilFinished; i++)
                yield return null;

            BepInExSplashScreenPatcher.Kill();
        }
    }
}
