using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Bacc_front
{
    /// <summary>
    /// ControlBoard.xaml 的交互逻辑
    /// </summary>
    public partial class ControlBoard : Window
    {
        /// <summary>
        /// 定时器
        /// </summary>
        public Thread thread;
        /// <summary>
        /// 给服务器发送的倒计时
        /// </summary>
        public int ServerCountdown { get; set; }
        public ObservableCollection<Player> Players { get; set; }
        private const int player_num = 14;
        public static ControlBoard Instance { get; set; }
        private Dictionary<string, SettingItem> TempSettings = new Dictionary<string, SettingItem>();
        public ControlBoard()
        {
            InitializeComponent();
            Instance = this;
            ServerCountdown = 0;
            InitPlayersForBoard();
            dgScore.ItemsSource = Desk.Instance.Players;

            txtServerIP.DataContext = Setting.Instance;

            lstButton.ItemsSource = Setting.Instance.game_setting;
            //WindowState = WindowState.Maximized;
            Activated += ControlBoard_Activated;
            WindowStyle = WindowStyle.None;
        }

        private void ControlBoard_Activated(object sender, EventArgs e)
        {
            //Focus();
            Mouse.Click(MouseButton.Left);
            //Mouse.Click(MouseButton.Left);
        }

        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            var game = Game.Instance;
            game.Start();
            Keyboard.Press(Key.Tab);
        }
        #region 初始化
        private void InitPlayersForBoard()
        {
            try
            {
                var desk = Desk.Instance;
                var util = new WsUtils.FileUtils();
                var str = util.ReadFile("Config/Players.json");
                if (string.IsNullOrEmpty(str))
                {
                    for (int i = 0; i < player_num + 1; i++)
                    {
                        if (i == 12)
                        {
                            continue;
                        }
                        var p = new Player(i + 1, desk.CalcPlayerEarning);
                        p.Balance = 500;
                        desk.Players.Add(p);
                    }
                }
                else
                {
                    var players = JsonConvert.DeserializeObject<ObservableCollection<Player>>(str);
                    desk.Players = players;
                }
            }
            catch (Exception)
            {

            }
        }
        #endregion
        private void OnAddScore(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            try
            {

                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].AddScore(score);
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }
        private void OnCancleAddScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].CancleAddOrSub();
        }
        private void OnConfirmAddScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].ConfirmAdd();
        }

        private void OnSubScore(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].SubScore(score);
        }

        private void OnSubAllScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].SubAllScore();
        }

        private void OnConfirmSubScore(object sender, RoutedEventArgs e)
        {
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].ConfirmSub();
        }

        private void OnConfig(object sender, RoutedEventArgs e)
        {
            this.TempSettings = Setting.Instance.game_setting;
            grdConfig.Visibility = grdConfig.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }
        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obj_str = JsonConvert.SerializeObject(Setting.Instance.game_setting);
                var util = new WsUtils.FileUtils();
                util.WriteFile("Config/Config.json", obj_str, true);
                obj_str = JsonConvert.SerializeObject(Setting.Instance.ServerIP);
                util.WriteFile("Config/ServerIP.json", obj_str, true);
                MessageBox.Show("设置成功，请重启游戏");
                //Process.Start("shutdown"," -r -t 0");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存设置出错！");
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (System.Windows.Forms.Screen.AllScreens.Length == 1)
                {
                    Instance.Topmost = false;
                    MainWindow.Instance.Topmost = true;
                    MainWindow.Instance.Activate();
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //if (Game.Instance._isInBetting)
            //{
            //    var key = (int)e.Key;
            //    Game.Instance.KeyMap[key].Pressed = false;
            //}
        }

        private void OnBackupScore(object sender, RoutedEventArgs e)
        {

        }

        private void OnRestoreScore(object sender, RoutedEventArgs e)
        {

        }

        private void OnConnetServer(object sender, RoutedEventArgs e)
        {
            Game.Instance._isSendingToServer = true;
        }

        private void btnOpenCashBox1_Click(object sender, RoutedEventArgs e)
        {
            Printer.OpenEPSONCashBox(1);
        }

        private void btnOpenCashBox2_Click(object sender, RoutedEventArgs e)
        {
            Printer.OpenEPSONCashBox(2);
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            Game.Instance.SessionIndex = -1;
            Game.Instance.NewSession();
            Game.Instance.RoundIndex++;
            Game.Instance.PrintWaybill();
            //var p = new Printer();
            //p.Write("庄闲闲闲闲闲");
            //Printer.PrintString(1,"庄闲闲闲闲闲");
        }
    }
}
