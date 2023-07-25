using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BepInEx.SplashScreen
{
    public partial class SplashScreen : Form
    {
        private const string WorkingStr = "...";
        private const string DoneStr = "...Done";
        private string _gameLocation;

        public SplashScreen()
        {
            InitializeComponent();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = checkedListBox1.Items.Count;
            progressBar1.Value = 0;

            labelTop.Font = new Font(labelTop.Font, FontStyle.Bold);

            AppendToItem(0, WorkingStr);
        }

        public void ProcessEvent(LoadEvent e)
        {
            switch (e)
            {
                case LoadEvent.PreloaderStart:
                    checkedListBox1.SetItemChecked(0, true);
                    AppendToItem(0, DoneStr);
                    AppendToItem(1, WorkingStr);
                    SetStatusMain("BepInEx patchers are being applied...");
                    break;
                case LoadEvent.PreloaderFinish:
                    checkedListBox1.SetItemChecked(1, true);
                    AppendToItem(1, DoneStr);
                    SetStatusMain("Finished applying patchers.");
                    break;
                case LoadEvent.ChainloaderStart:
                    AppendToItem(2, WorkingStr);
                    SetStatusMain("BepInEx plugins are being loaded...");
                    break;
                case LoadEvent.ChainloaderFinish:
                    checkedListBox1.SetItemChecked(2, true);
                    AppendToItem(2, DoneStr);
                    AppendToItem(3, WorkingStr);
                    SetStatusMain("Finished loading plugins.");
                    SetStatusDetail("Waiting for the game to start...\nSome plugins might need more time to finish loading.");
                    break;
                case LoadEvent.LoadFinished:
                    //AppendToItem(3, "Done");
                    //checkedListBox1.SetItemCheckState(3, CheckState.Checked);
                    Environment.Exit(0);
                    return;
            }

            progressBar1.Value = checkedListBox1.CheckedItems.Count;
            checkedListBox1.Invalidate();
        }

        public void SetStatusMain(string msg)
        {
            labelTop.Text = msg;
        }

        public void SetStatusDetail(string msg)
        {
            labelBot.Text = msg;
        }

        private void AppendToItem(int index, string str)
        {
            var current = checkedListBox1.Items[index].ToString();
            checkedListBox1.Items[index] = current + str;
        }

        public void SetIcon(Image icon)
        {
            if (icon != null)
            {
                pictureBox1.SizeMode = icon.Height < pictureBox1.Height ? PictureBoxSizeMode.CenterImage : PictureBoxSizeMode.Zoom;
                pictureBox1.Image = icon;
            }
            else
            {
                HideRow(tableLayoutPanel1.GetRow(pictureBox1));
            }
        }

        private void HideRow(int index)
        {
            var iconRowStyle = tableLayoutPanel1.RowStyles[index];
            iconRowStyle.SizeType = SizeType.Absolute;
            iconRowStyle.Height = 0;
        }

        public void SetGameLocation(string location)
        {
            if (location != null)
            {
                _gameLocation = location;
            }
            else
            {
                HideRow(tableLayoutPanel1.GetRow(button1));
                button1.Enabled = false;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Process.Start(_gameLocation);
        }
    }
}
