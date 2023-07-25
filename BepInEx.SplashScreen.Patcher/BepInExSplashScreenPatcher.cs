using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace BepInEx.SplashScreen
{
    public static class BepInExSplashScreenPatcher
    {
        public const string PipeName = "BepInEx.SplashScreen_Pipe";
        public const int ConnectionTimeoutMs = 5000;

        internal static readonly ManualLogSource Logger = Logging.Logger.CreateLogSource("BepInEx.SplashScreen");

        private static readonly Queue _StatusQueue = Queue.Synchronized(new Queue(4));
        private static string _lastMessage;
        private static LoadingLogListener _logListener;

        private static int _initialized;
        private static NamedPipeServerStream _pipeServer;
        private static Process _guiProcess;

        private static LoadEvent _highestSent = LoadEvent.None;

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
            if (Interlocked.Exchange(ref _initialized, 1) == 1) return;

            try
            {
                var config = (ConfigFile)AccessTools.Property(typeof(ConfigFile), "CoreConfig").GetValue(null, null);
                if (!config.Bind("Splash screen", "Show start-up progress splash screen", true, "Display a splash screen with information about game load progress on game start-up.").Value)
                    return;

                var guiExecutablePath = Path.Combine(Path.GetDirectoryName(typeof(BepInExSplashScreenPatcher).Assembly.Location) ?? Paths.PatcherPluginPath, "BepInEx.SplashScreen.GUI.exe");

                if (!File.Exists(guiExecutablePath))
                    throw new FileNotFoundException("Executable not found or inaccessible at " + guiExecutablePath);

                Logger.Log(LogLevel.Debug, "Starting GUI process: " + guiExecutablePath);

                var statusServer = new Thread(ServerThread);
                statusServer.IsBackground = true;
                statusServer.Start();

                _guiProcess = Process.Start(guiExecutablePath, Process.GetCurrentProcess().Id.ToString());

                SendStatus(LoadEvent.PreloaderStart);

                _logListener = LoadingLogListener.StartListening();
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to start GUI: " + e);
                Dispose();
            }
        }

        internal static void SendStatus(LoadEvent e)
        {
#if DEBUG
            Console.WriteLine($"send {e}  current: {_StatusQueue.Count} {string.Join(", ", _StatusQueue.Cast<LoadEvent>().Select(x => x.ToString()).ToArray())}");
#endif
            // hack: For some reason PreloaderFinish can be sent before PreloaderStart, and then again after, so do this to get rid of the first one.
            if (e <= _highestSent)
                return;
            _highestSent = e;

            _StatusQueue.Enqueue(e);
        }

        internal static void SendMessage(string message)
        {
            // Throttle messages
            _lastMessage = message;
        }

        private static void ServerThread()
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None);

                var waiting = true;

                _pipeServer.BeginWaitForConnection(_ => waiting = false, null);

                var sw = Stopwatch.StartNew();
                while (waiting)
                {
                    if (sw.Elapsed > TimeSpan.FromMilliseconds(ConnectionTimeoutMs))
                    {
                        _guiProcess?.Kill();
                        throw new TimeoutException("GUI did not connect");
                    }

                    Thread.Sleep(30);
                }

                Logger.LogDebug("Connected to the GUI");

                var formatter = new BinaryFormatter();
                while (_pipeServer.IsConnected)
                {
                    var status = Interlocked.Exchange(ref _lastMessage, null);
                    if (status != null)
                        formatter.Serialize(_pipeServer, status);

                    while (_StatusQueue.Count > 0)
                    {
                        var message = _StatusQueue.Dequeue();
                        Console.WriteLine("actually send " + message);
                        formatter.Serialize(_pipeServer, message);
                        if (message is LoadEvent e && e == LoadEvent.LoadFinished)
                        {
                            Logger.LogDebug("Game has started, ending connection");
                            break;
                        }
                    }

                    Thread.Sleep(150);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Crash in {nameof(ServerThread)}, aborting. Exception: {e}");
            }
            finally
            {
                Dispose();
            }
        }

        internal static void Dispose()
        {
            try
            {
                _pipeServer?.Dispose();

                Logger.Dispose();
                Logging.Logger.Sources.Remove(Logger);

                _logListener?.Dispose();

                _StatusQueue.Clear();
            }
            catch (Exception e)
            {
                // Welp, no logger to use. This shouldn't ever happen annyways.
                Console.WriteLine(e);
            }
        }
    }
}
