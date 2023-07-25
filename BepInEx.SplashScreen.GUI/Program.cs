using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

                var thread = new Thread(ClientThread);
                thread.IsBackground = true;
                thread.Start();

                try
                {
                    var pid = int.Parse(args.Last());
                    var gameProcess = Process.GetProcessById(pid);

                    // Get game name
                    _mainForm.Text = $@"{gameProcess.ProcessName} is loading...";

                    // Get game location and icon
                    var gameExecutable = gameProcess.MainModule.FileName;
                    _mainForm.SetGameLocation(Path.GetDirectoryName(gameExecutable));
                    _mainForm.SetIcon(IconManager.GetLargeIcon(gameExecutable, true, true).ToBitmap());

                    new Thread(() =>
                        {
                            try
                            {
                                while (true)
                                {
                                    if (_mainForm.Visible)
                                    {
                                        // Ignore console window
                                        if (gameProcess.MainWindowHandle == IntPtr.Zero || gameProcess.MainWindowTitle.StartsWith("BepInEx"))
                                        {
                                            gameProcess.Refresh();
                                        }
                                        else
                                        {
                                            if (!GetWindowRect(new HandleRef(_mainForm, gameProcess.MainWindowHandle), out var rct))
                                                throw new InvalidOperationException("GetWindowRect failed :(");

                                            if (!default(RECT).Equals(rct))
                                            {
                                                var x = rct.Left + (rct.Right - rct.Left) / 2 - _mainForm.Width / 2;
                                                var y = rct.Top + (rct.Bottom - rct.Top) / 2 - _mainForm.Height / 2;
                                                var newLocation = new Point(x, y);

                                                if (_mainForm.Location != newLocation)
                                                    _mainForm.Location = newLocation;

                                                if (_mainForm.FormBorderStyle != FormBorderStyle.None)
                                                {
                                                    // At this point the form is snapped to the main game window so prevent user from trying to drag it
                                                    _mainForm.FormBorderStyle = FormBorderStyle.None;
                                                    _mainForm.BackColor = Color.White;
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
                            }
                        })
                    { IsBackground = true }.Start();
                }
                catch (Exception e)
                {
                    _mainForm.SetGameLocation(null);
                    _mainForm.SetIcon(null);
                    Debug.Fail(e.ToString());
                }

                Application.Run(_mainForm);
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
        }

        private static void ClientThread()
        {
            NamedPipeClientStream pipeClient;
            var formatter = new BinaryFormatter();
            try
            {
                pipeClient = new NamedPipeClientStream(".", BepInExSplashScreenPatcher.PipeName, PipeDirection.In);
                pipeClient.Connect(BepInExSplashScreenPatcher.ConnectionTimeoutMs);
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
                Environment.Exit(0);
                return;
            }
            while (pipeClient.IsConnected)
            {
                try
                {
                    var incoming = formatter.Deserialize(pipeClient);
                    if (incoming is LoadEvent e)
                        _mainForm.ProcessEvent(e);
                    else if (incoming is string msg)
                        _mainForm.SetStatusDetail(msg);

                    // Handle rapidfire events
                    Application.DoEvents();
                }
                catch (SerializationException)
                { }
                catch (Exception e)
                {
                    Debug.Fail(e.ToString());
                }
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
