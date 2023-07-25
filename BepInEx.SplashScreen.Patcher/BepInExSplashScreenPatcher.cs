using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace BepInEx.SplashScreen
{
    public static class BepInExSplashScreenPatcher
    {
        public const string Version = "1.0";

        internal static readonly ManualLogSource Logger = Logging.Logger.CreateLogSource("Splash");
        private static readonly Queue _StatusQueue = Queue.Synchronized(new Queue(10, 2));
        private static LoadingLogListener _logListener;
        private static Process _guiProcess;
        private static int _initialized;

        public static IEnumerable<string> TargetDLLs
        {
            get
            {
                // Use whatever gets us to run faster, or at all
                Initialize();
                return Enumerable.Empty<string>();
            }
        }

        public static void Patch(AssemblyDefinition _)
        {
            // Use whatever gets us to run faster, or at all
            Initialize();
        }

        public static void Initialize()
        {
            // Only allow to run once
            if (Interlocked.Exchange(ref _initialized, 1) == 1) return;

            try
            {
                var config = (ConfigFile)AccessTools.Property(typeof(ConfigFile), "CoreConfig").GetValue(null, null);
                if (!config.Bind("SplashScreen", "Enabled", true, "Display a splash screen with information about game load progress on game start-up.").Value)
                    return;

                var guiExecutablePath = Path.Combine(Path.GetDirectoryName(typeof(BepInExSplashScreenPatcher).Assembly.Location) ?? Paths.PatcherPluginPath, "BepInEx.SplashScreen.GUI.exe");

                if (!File.Exists(guiExecutablePath))
                    throw new FileNotFoundException("Executable not found or inaccessible at " + guiExecutablePath);

                Logger.Log(LogLevel.Debug, "Starting GUI process: " + guiExecutablePath);

                var psi = new ProcessStartInfo(guiExecutablePath, Process.GetCurrentProcess().Id.ToString())
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                _guiProcess = Process.Start(psi);

                new Thread(CommunicationThread) { IsBackground = true }.Start(_guiProcess);

                _logListener = LoadingLogListener.StartListening();
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to start GUI: " + e);
                Kill();
            }
        }

        internal static void SendMessage(string message)
        {
            _StatusQueue.Enqueue(message);
        }

        private static void CommunicationThread(object processArg)
        {
            try
            {
                var guiProcess = (Process)processArg;

                guiProcess.Exited += (sender, args) => Kill();

                guiProcess.OutputDataReceived += (sender, args) => Logger.Log(LogLevel.Debug, "[GUI] " + args.Data.Replace('\t', '\n').TrimEnd('\n'));
                guiProcess.BeginOutputReadLine();

                guiProcess.ErrorDataReceived += (sender, args) => Logger.Log(LogLevel.Error, "[GUI] " + args.Data.Replace('\t', '\n').TrimEnd('\n'));
                guiProcess.BeginErrorReadLine();

                guiProcess.StandardInput.AutoFlush = false;

                Logger.LogDebug("Connected to the GUI process");

                var any = false;
                while (!guiProcess.HasExited)
                {
                    while (_StatusQueue.Count > 0 && guiProcess.StandardInput.BaseStream.CanWrite)
                    {
                        guiProcess.StandardInput.WriteLine(_StatusQueue.Dequeue());
                        any = true;
                    }

                    if (any)
                    {
                        any = false;
                        guiProcess.StandardInput.Flush();
                    }

                    Thread.Sleep(150);
                }
            }
            catch (ThreadAbortException)
            {
                // I am die, thank you forever
            }
            catch (Exception e)
            {
                Logger.LogError($"Crash in {nameof(CommunicationThread)}, aborting. Exception: {e}");
            }
            finally
            {
                Kill();
            }
        }

        internal static void Kill()
        {
            try
            {
                _logListener?.Dispose();

                _StatusQueue.Clear();
                _StatusQueue.TrimToSize();

                try
                {
                    if (_guiProcess != null && !_guiProcess.HasExited)
                    {
                        Logger.LogDebug("Closing GUI process");
                        _guiProcess.Kill();
                    }
                }
                catch (Exception)
                {
                    // _guiProcess already quit so Kill threw
                }

                Logger.Dispose();
                // todo not thread safe
                // Logging.Logger.Sources.Remove(Logger);
            }
            catch (Exception e)
            {
                // Welp, no Logger left to use. This shouldn't ever happen annyways.
                Console.WriteLine(e);
            }
        }
    }
}
