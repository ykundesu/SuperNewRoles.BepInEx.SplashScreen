using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BepInEx.SplashScreen
{
    public partial class SplashScreen : Form
    {
        private const string WorkingStr = "...";
        private const string DoneStr = "...完了";
        private string _gameLocation;
        private int _pluginPercentDone;

        public SplashScreen()
        {
            InitializeComponent();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100 + checkedListBox1.Items.Count * 15;
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
                    SetStatusMain("BepInExパッチャーを適用中...");
                    break;

                case LoadEvent.PreloaderFinish:
                    checkedListBox1.SetItemChecked(1, true);
                    AppendToItem(1, DoneStr);
                    SetStatusMain("パッチャーの適用が完了しました。");
                    SetStatusDetail("まもなくプラグインの読み込みが開始されます。\n読み込みが停止している場合は、エントリーポイントを確認してください。");
                    break;

                case LoadEvent.ChainloaderStart:
                    AppendToItem(2, WorkingStr);
                    SetStatusMain("BepInExプラグインを読み込み中...");
                    break;

                case LoadEvent.ChainloaderFinish:
                    _pluginPercentDone = 100;
                    checkedListBox1.SetItemChecked(2, true);
                    AppendToItem(2, DoneStr);
                    AppendToItem(3, WorkingStr);
                    SetStatusMain("プラグインの読み込みが完了しました。");
                    SetStatusDetail("ゲーム開始を待機中...\n一部のプラグインは読み込み完了までさらに時間が必要かもしれません。");
                    break;

                    // 残りのコードは変更なし
            }
        }

        private void AppendToItem(int index, string str)
        {
            var current = checkedListBox1.Items[index].ToString();
            checkedListBox1.Items[index] = current + str;
        }

        private void UpdateProgress()
        {
            progressBar1.Value = checkedListBox1.CheckedItems.Count * 15 + _pluginPercentDone;
        }

        public void SetStatusMain(string msg)
        {
            labelTop.Text = msg;
        }

        public void SetStatusDetail(string msg)
        {
            labelBot.Text = msg;
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
                pictureBox1.Visible = false;
            }
        }

        public void SetGameLocation(string location)
        {
            if (location != null)
            {
                _gameLocation = location;
            }
            else
            {
                button1.Visible = false;
            }
        }

        public void SetPluginProgress(int percentDone)
        {
            _pluginPercentDone = Math.Min(100, Math.Max(Math.Max(0, percentDone), _pluginPercentDone));
            UpdateProgress();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Process.Start(_gameLocation);
        }
    }
}
