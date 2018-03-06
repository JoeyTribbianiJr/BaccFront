using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bacc_front
{
    /// <summary>
    /// ControlBoard.xaml 的交互逻辑
    /// </summary>
    public partial class ControlBoard : Window
    {
        public ObservableCollection<Player> Players { get; set; }
        private const int player_num = 14;
        public static ControlBoard Instance { get; set; }
        public ControlBoard()
        {
            InitializeComponent();
            Instance = this;

            InitPlayersForBoard();
            dgScore.ItemsSource = Desk.Instance.Players;

            lstButton.ItemsSource = Setting.Instance.game_setting;
            WindowState = WindowState.Maximized;
            Activated += ControlBoard_Activated;
            WindowStyle = WindowStyle.None;
        }
        //Players = new ObservableCollection<Player>()
        //            {
        //                new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 }
        //            };
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        //            Players.Add(new Player(1, null) { Id = 1, Add_score = 2000, Last_add = 3009, Last_sub = 23324, Balance = 234, Sub_score = 533 });
        private void ControlBoard_Activated(object sender, EventArgs e)
        {
            Focus();
            Mouse.Click(MouseButton.Left);
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
            var desk = Desk.Instance;
            for (int i = 0; i < player_num + 1; i++)
            {
                if (i == 12)
                {
                    continue;
                }
                var p = new Player(i + 1, desk.CalcPlayerEarning);
                p.Balance = 8000;
                desk.Players.Add(p);
            }
        }
        #endregion
        private void OnAddScore(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            var idx = dgScore.SelectedIndex;
            Desk.Instance.Players[idx].AddScore(score);
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
            grdConfig.Visibility = grdConfig.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }
        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obj_str = JsonConvert.SerializeObject(Setting.Instance.game_setting);
                var util = new WsUtils.FileUtils();
                util.WriteFile("Config/Config.json", obj_str, true);
                MessageBox.Show("设置成功，将重启机器");
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
            else if (Game.Instance._isBetting)
            {
                var keymodel = Game.Instance.KeyMap[(int)e.Key];
                if (keymodel.IsKey)
                {
                    keymodel.Pressed = true;
                    if (e.IsRepeat)
                    {
                        if (keymodel.Timer >= Game._bet_rate)
                        {
                            keymodel.Handler();
                            keymodel.Timer = 0;
                        }
                    }
                    else
                    {
                        keymodel.Handler();
                    }
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Game.Instance._isBetting)
            {
                var key = (int)e.Key;
                Game.Instance.KeyMap[key].Pressed = false;
            }
        }

        private void OnBackupScore(object sender, RoutedEventArgs e)
        {

        }

        private void OnRestoreScore(object sender, RoutedEventArgs e)
        {

        }


    }
}
