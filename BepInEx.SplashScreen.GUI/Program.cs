using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BepInEx.SplashScreen
{
    public static class Program
    {
        private static SplashScreen _mainForm;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(params string[] args)
        {
            try
            {
                Application.SetCompatibleTextRenderingDefault(false);

                _mainForm = new SplashScreen();
                _mainForm.Show();

                var pid = int.Parse(args.Last());
                var gameProcess = Process.GetProcessById(pid);

                BeginReadingInput(gameProcess);

                try
                {
                    // Get game name
                    _mainForm.Text = $@"{gameProcess.ProcessName} is loading...";

                    // Get game location and icon
                    var gameExecutable = gameProcess.MainModule.FileName;
                    _mainForm.SetGameLocation(Path.GetDirectoryName(gameExecutable));
                    _mainForm.SetIcon(IconManager.GetLargeIcon(gameExecutable, true, true).ToBitmap());

                    BeginSnapPositionToGameWindow(gameProcess);
                }
                catch (Exception e)
                {
                    _mainForm.SetGameLocation(null);
                    _mainForm.SetIcon(null);
                    Log("Failed to get some info about the game process: " + e, true);
                    Debug.Fail(e.ToString());
                }

                Log("Splash screen window started successfully", false);

                Application.Run(_mainForm);
            }
            catch (Exception e)
            {
                Log("Failed to create window: " + e, true);
                Debug.Fail(e.ToString());
            }
        }

        public static void ProcessInputMessage(string message)
        {
            try
            {
                switch (message)
                {
                    case "Preloader started":
                        _mainForm.ProcessEvent(LoadEvent.PreloaderStart);
                        break;
                    case "Preloader finished":
                        _mainForm.ProcessEvent(LoadEvent.PreloaderFinish);
                        break;
                    case "Chainloader started":
                        _mainForm.ProcessEvent(LoadEvent.ChainloaderStart);
                        break;
                    case "Chainloader startup complete":
                        _mainForm.ProcessEvent(LoadEvent.ChainloaderFinish);
                        break;

                    default:
                        const string patching = "Patching ";
                        const string skipping = "Skipping ";
                        const string loading = "Loading ";
                        if (message.StartsWith(patching) || message.StartsWith(loading))
                        {
                            //todo throttle?
                            _mainForm.SetStatusDetail(message);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Log($"Crash in {nameof(ProcessInputMessage)}: {e}", true);
            }
        }

        public static void Log(string message, bool error)
        {
            // Patcher reads standard output so Console can be used to pass info back to it
            // Replace newlines with tabs to send multiline messages as a single line
            (error ? Console.Error : Console.Out).WriteLine(message.Replace("\t", "    ").Replace('\n', '\t').Replace("\r", ""));
        }

        private static void BeginSnapPositionToGameWindow(Process gameProcess)
        {
            new Thread(SnapPositionToGameWindowThread) { IsBackground = true }.Start(gameProcess);
        }
        private static void SnapPositionToGameWindowThread(object processArg)
        {
            try
            {
                var gameProcess = (Process)processArg;
                while (true)
                {
                    if (_mainForm.Visible)
                    {
                        // Ignore console window
                        if (gameProcess.MainWindowHandle == IntPtr.Zero || gameProcess.MainWindowTitle.StartsWith("BepInEx") || gameProcess.MainWindowTitle.StartsWith("Select BepInEx"))
                        {
                            gameProcess.Refresh();
                        }
                        else
                        {
                            if (!GetWindowRect(new HandleRef(_mainForm, gameProcess.MainWindowHandle), out var rct)) throw new InvalidOperationException("GetWindowRect failed :(");

                            if (!default(RECT).Equals(rct))
                            {
                                var x = rct.Left + (rct.Right - rct.Left) / 2 - _mainForm.Width / 2;
                                var y = rct.Top + (rct.Bottom - rct.Top) / 2 - _mainForm.Height / 2;
                                var newLocation = new Point(x, y);

                                if (_mainForm.Location != newLocation) _mainForm.Location = newLocation;

                                if (_mainForm.FormBorderStyle != FormBorderStyle.None)
                                {
                                    // At this point the form is snapped to the main game window so prevent user from trying to drag it
                                    _mainForm.FormBorderStyle = FormBorderStyle.None;
                                    //_mainForm.BackColor = Color.White;
                                    _mainForm.PerformLayout();
                                }
                            }
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception)
            {
                // Not much we can do here, it's not critical either way
                Environment.Exit(1);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void BeginReadingInput(Process gameProcess)
        {
            new Thread(ClientThread) { IsBackground = true }.Start(gameProcess);
        }
        private static void ClientThread(object processArg)
        {
            try
            {
                var gameProcess = (Process)processArg;

                //Console.InputEncoding = Encoding.UTF8;
                using (var inStream = Console.OpenStandardInput())
                using (var inReader = new StreamReader(inStream))
                {
                    while (inStream.CanRead && !gameProcess.HasExited)
                    {
                        ProcessInputMessage(inReader.ReadLine());
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString(), true);
                Debug.Fail(e.ToString());
            }

            Environment.Exit(0);
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }
    }
}
