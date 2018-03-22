using Newtonsoft.Json;
using WsUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
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
using WsUtils.SqliteEFUtils;
using Bacc_front.Properties;

namespace Bacc_front
{
    /// <summary>
    /// ControlBoard.xaml 的交互逻辑
    /// </summary>
    public partial class ControlBoard : Window
    {

        public MediaPlayer player;
        /// <summary>
        /// 定时器
        /// </summary>
        public Thread thread;
        /// <summary>
        /// 给服务器发送的倒计时
        /// </summary>
        public int ServerCountdown { get; set; }
        public ObservableCollection<Player> Players { get; set; }
        public static ControlBoard Instance { get; set; }
        private Dictionary<string, SettingItem> TempSettings = new Dictionary<string, SettingItem>();
        public ControlBoard()
        {
            InitializeComponent();
            Instance = this;
            ServerCountdown = 0;

            dgScore.ItemsSource = Desk.Instance.Players;
            lstButton.ItemsSource = Setting.Instance.game_setting;

            KeyDown += Game.Instance.KeyListener.Window_KeyDown;
            KeyUp += Game.Instance.KeyListener.Window_KeyUp;

            //WindowState = WindowState.Minimized;
            //Activated += ControlBoard_Activated;
            //WindowStyle = WindowStyle.None;
        }

        private void ControlBoard_Activated(object sender, EventArgs e)
        {
            //Focus();
            //Mouse.Click(MouseButton.Left);
            //Mouse.Click(MouseButton.Left);
        }

        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            Game.Instance._isGameStarting = true;
            if (Setting.Instance._bgm_on)
            {
                player = new MediaPlayer();
                string location = System.Environment.CurrentDirectory + "\\Wav\\BGM\\";
                var wavs = System.IO.Directory.GetFiles(location);
                var rdm = new Random().Next(wavs.Length);
                var bgm = wavs[rdm];
                try
                {
                    player.Open(new Uri(bgm, UriKind.Absolute));
                    player.MediaEnded += LoopPlay;
                    player.Play();
                }
                catch (Exception)
                {
                    MessageBox.Show("打开背景音乐失败");
                }
            }

            btnStartGame.IsEnabled = false;
            var game = Game.Instance;
            game.Start();
            //Keyboard.Press(Key.Tab);
        }
        private void LoopPlay(object sender, EventArgs e)
        {
            MediaPlayer player = (MediaPlayer)sender;
            string location = System.Environment.CurrentDirectory + "\\Wav\\BGM\\";
            var wavs = System.IO.Directory.GetFiles(location);
            var rdm = new Random().Next(wavs.Length);
            var bgm = wavs[rdm];
            player.Open(new Uri(bgm, UriKind.Absolute));
            player.Play();
        }

        private void OnAddScore(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Button;

            var score = Convert.ToInt32(btn.Tag);
            //EventArgs继承自MouseEventArgs,所以可以强转  

            if (e.RightButton == MouseButtonState.Pressed)
            {
                score = -score;
            }
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
            try
            {
                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].CancleAddOrSub();
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }
        private void OnConfirmAddScore(object sender, RoutedEventArgs e)
        {
            try
            {
                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].ConfirmAdd();
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }

        private void OnSubScore(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Button;
            var score = Convert.ToInt32(btn.Tag);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                score = -score;
            }
            try
            {
                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].SubScore(score);
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }

        private void OnSubAllScore(object sender, RoutedEventArgs e)
        {
            try
            {
                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].SubAllScore();
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }

        private void OnConfirmSubScore(object sender, RoutedEventArgs e)
        {
            try
            {
                var idx = dgScore.SelectedIndex;
                Desk.Instance.Players[idx].ConfirmSub();
            }
            catch (Exception)
            {
                MessageBox.Show("未选中玩家");
            }
        }
        private void OnConfig(object sender, RoutedEventArgs e)
        {
            if (grdConfig.Visibility == Visibility.Hidden)
            {
                //打开配置页面
                btnStartGame.IsEnabled = false;
                gdAddBtns.IsEnabled = false;

                txtActiveSessionIndex.Text = (Game.Instance.SessionIndex + 2).ToString();
                grdConfig.Visibility = Visibility.Visible;
                btnConfig.Content = "退出";
                btnConfig.Background = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                //保存参数
                if (int.TryParse(txtActiveSessionIndex.Text, out int session_index) && 1 <= session_index && session_index <= Setting.max_session_num)
                {
                    SaveSetting(session_index - 2);

                    spConfigLst.Visibility = Visibility.Hidden;
                    lstButton.Visibility = Visibility.Hidden;
                    btnAnalyzeWaybill.Visibility = Visibility.Hidden;
                    btnClearAccount.Visibility = Visibility.Hidden;
                    spAccount.Visibility = Visibility.Hidden;
                    btnShudown.Visibility = Visibility.Visible;

                    grdConfig.Visibility = Visibility.Hidden;

                    btnConfig.Content = "配置";
                    btnConfig.Background = new SolidColorBrush(Colors.MediumSeaGreen);
                    gdAddBtns.IsEnabled = true;
                    btnStartGame.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("请输入1-" + Setting.max_session_num + "的数字");
                    return;
                }
            }
        }
        public void SaveSetting(int session_index)
        {
            Settings.Default.GameSetting = JsonConvert.SerializeObject(Setting.Instance.game_setting);
            Settings.Default.CurrentSessionIndex = session_index;
            Settings.Default.Save();

            Setting.Instance.ResetGameSetting();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (System.Windows.Forms.Screen.AllScreens.Length == 1)
                {
                    //Instance.Topmost = false;
                    //MainWindow.Instance.Topmost = true;
                    //MainWindow.Instance.Activate();
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
            //Game.Instance.SessionIndex = -1;
            //Game.Instance.NewSession();
            //Game.Instance.RoundIndex++;
            //Game.Instance.GamePrinter.PrintWaybill();
            //var p = new Printer();
            //p.Write("庄闲闲闲闲闲");
            //Printer.PrintString(1,"庄闲闲闲闲闲");
        }

        private void btnTestThread_Click(object sender, RoutedEventArgs e)
        {
            Game.Instance.KeyListener.StartListen();
        }

        private void btnPwdNum_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag;
            var num = Convert.ToInt32(tag);
            txtPwd.Password += num;
        }

        private void btnConfirmPwd(object sender, RoutedEventArgs e)
        {
            var pwd = txtPwd.Password;
            var key = Setting.Instance.PasswordMap.FirstOrDefault(p => p.Value == pwd).Key;
            if (string.IsNullOrEmpty(key))
            {
                txtPwd.Password = "";
                return;
            }
            if (Game.Instance._isGameStarting)
            {
                string[] _close_acc_keys = new string[] { "account_pwd", "clear_account_pwd", "quit_front_pwd", "shutdown_pwd" };
                if(!_close_acc_keys.Contains(key))
                {
                    txtPwd.Password = "";
                    return;
                }
            }
            switch (key)
            {
                case "waiter_pwd":
                    spConfigLst.Visibility = Visibility.Visible;

                    lstButton.Visibility = Visibility.Hidden;
                    btnOpenCashBox.Visibility = Visibility.Hidden;
                    btnAnalyzeWaybill.Visibility = Visibility.Hidden;
                    btnClearAccount.Visibility = Visibility.Hidden;
                    btnShudown.Visibility = Visibility.Visible;
                    break;
                case "manager_pwd":
                    spConfigLst.Visibility = Visibility.Visible;
                    lstButton.Visibility = Visibility.Visible;
                    btnOpenCashBox.Visibility = Visibility.Visible;
                    btnAnalyzeWaybill.Visibility = Visibility.Visible;
                    btnClearAccount.Visibility = Visibility.Visible;
                    btnShudown.Visibility = Visibility.Visible;
                    lstButton.ItemsSource = Setting.Instance.game_setting.Where(kv => Setting.Instance.manager_menu_items.Contains(kv.Key));
                    break;
                case "boss_pwd":
                    spConfigLst.Visibility = Visibility.Visible;
                    lstButton.Visibility = Visibility.Visible;
                    btnOpenCashBox.Visibility = Visibility.Visible;
                    btnAnalyzeWaybill.Visibility = Visibility.Visible;
                    btnClearAccount.Visibility = Visibility.Visible;
                    btnShudown.Visibility = Visibility.Visible;
                    lstButton.ItemsSource = Setting.Instance.game_setting;
                    break;
                case "audit_account_pwd":
                    spAccount.Visibility = Visibility.Visible;
                    try
                    {
                        using (var db = new SQLiteDB())
                        {
                            var account = db.FrontAccounts.First(acc => acc.IsClear == false);
                            if (account == null)
                            {
                                MessageBox.Show("目前尚无记录");
                                return;
                            }
                            var cur_acc = JsonConvert.DeserializeObject<ObservableCollection<AddSubScoreRecord>>(account.JsonScoreRecord);
                            DataTable table = WsUtils.DataTableExtensions.ToDataTable(cur_acc);
                            var dr = table.NewRow();
                            dr["PlayerId"] = "总 计";
                            dr["TotalAddScore"] = table.Compute("SUM(TotalAddScore)", null);
                            dr["TotalSubScore"] = table.Compute("SUM(TotalSubScore)", null);
                            dr["TotalAccount"] = table.Compute("SUM(TotalAccount)", null);
                            table.Rows.Add(dr);
                            dgAccount.ItemsSource = table.DefaultView;
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        MessageBox.Show(ex.Message);
#endif
                        MessageBox.Show("读取账本失败");
                    }
                    break;
                case "clear_account_pwd":
                    btnClearAccount.Visibility = Visibility.Visible;
                    break;
                case "audit_bet_record_pwd":
                    spAccount.Visibility = Visibility.Visible;
                    break;
                case "quit_front_pwd":
                    SaveSetting(Game.Instance.SessionIndex);
                    App.Current.Shutdown();
                    break;
                case "shutdown_pwd":
                    ShutdownComputer();
                    break;
                default:
                    txtPwd.Password = "";
                    break;
            }
            txtPwd.Password = "";
        }

        private void btnDeletePwd(object sender, RoutedEventArgs e)
        {
            var pwd = txtPwd.Password;
            if (pwd.Length > 0)
            {
                txtPwd.Password = pwd.Substring(0, pwd.Length - 1);
            }
        }

        private void btnClearAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Desk.Instance.Players.Sum(p => p.Balance) > 0)
            {
                MessageBox.Show("桌面有余分不可清零，请先下分");
                return;
            }
            if (MessageBox.Show("确认清零？", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var db = new SQLiteDB())
                {
                    var account = db.FrontAccounts.First(acc => acc.IsClear == false);
                    if (account == null)
                    {
                        MessageBox.Show("目前尚无记录可清零");
                        return;
                    }

                    var records = JsonConvert.DeserializeObject<ObservableCollection<AddSubScoreRecord>>(account.JsonScoreRecord);
                    foreach (var a in records)
                    {
                        a.TotalAccount = 0;
                        a.TotalAddScore = 0;
                        a.TotalSubScore = 0;
                    }

                    account.JsonScoreRecord = JsonConvert.SerializeObject(records);
                    account.IsClear = true;
                    account.ClearTime = DateTime.Now;
                    db.SaveChanges();

                    DataTable table = WsUtils.DataTableExtensions.ToDataTable(records);
                    var dr = table.NewRow();
                    dr["PlayerId"] = "总 计";
                    dr["TotalAddScore"] = table.Compute("SUM(TotalAddScore)", null);
                    dr["TotalSubScore"] = table.Compute("SUM(TotalSubScore)", null);
                    dr["TotalAccount"] = table.Compute("SUM(TotalAccount)", null);
                    table.Rows.Add(dr);
                    dgAccount.ItemsSource = table.DefaultView;
                }
            }
        }

        public void ShutdownComputer()
        {
            Process.Start("shutdown", " -r -t 0");
        }
        private void OnShutdown(object sender, RoutedEventArgs e)
        {
            //Save first
            ShutdownComputer();
        }

        private void btnAnalyzeWaybill_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPrintAccount_Click(object sender, RoutedEventArgs e)
        {
            Game.Instance.GamePrinter.PrintAccount();
        }

        private void btnPreActiveSession_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnNextActiveSession_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
