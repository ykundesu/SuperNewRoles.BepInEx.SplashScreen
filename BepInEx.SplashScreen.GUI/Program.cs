using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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
    }
}
