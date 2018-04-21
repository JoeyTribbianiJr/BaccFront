using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WsUtils.SqliteEFUtils;
using Bacc_front.Properties;
using WsUtils;
using System.Windows.Media.Imaging;

namespace Bacc_front
{
    public class SumLine
    {
        public string Title { get; set; }
        public int Add_score { get; set; }
        public int Last_add { get; set; }
        public int Balance { get; set; }
        public int Sub_score { get; set; }
        public int Last_sub { get; set; }
    }
    public delegate void delegateStartGameClick();
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

        public int AccountPageIndex = 0;
        public ObservableCollection<SumLine> ScoreSum { get; set; }
        private Dictionary<string, SettingItem> TempSettings = new Dictionary<string, SettingItem>();

        public ControlBoard()
        {
            InitializeComponent();
            Instance = this;
            ScoreSum = new ObservableCollection<SumLine>();
            ServerCountdown = 0;
            player = new MediaPlayer();
            //StartClick += ControlBoard_StartClick;
            dgScore.DataContext = Desk.Instance;
            lstButton.ItemsSource = Setting.Instance.game_setting;

            dgSum.ItemsSource = ScoreSum;
            SumPlayerScore();

            KeyDown += Game.Instance.KeyListener.Window_KeyDown;
            KeyUp += Game.Instance.KeyListener.Window_KeyUp;

            //WindowState = WindowState.Minimized;
            //Activated += ControlBoard_Activated;
            //WindowStyle = WindowStyle.None;
        }

        private void OnConfig(object sender, RoutedEventArgs e)
        {
            if (grdConfig.Visibility == Visibility.Hidden)
            {
                //打开配置页面
                //btnStartGame.IsEnabled = false;
                gdAddBtns.IsEnabled = false;

                txtActiveSessionIndex.Text = Game.Instance._sessionStrIndex;
                grdConfig.Visibility = Visibility.Visible;
                btnConfig.Content = "退出";
                btnConfig.Background = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                //保存参数
                if (int.TryParse(txtActiveSessionIndex.Text, out int session_str_index) && 1 <= session_str_index && session_str_index <= Setting.max_session_num)
                {
                    SaveSetting(session_str_index - 1);
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
                    //btnStartGame.IsEnabled = true;
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
            if (Game.Instance.CurrentState != GameState.Preparing)
            {
                return;
            }
            Settings.Default.GameSetting = JsonConvert.SerializeObject(Setting.Instance.game_setting);
            Settings.Default.CurrentSessionIndex = session_index;// setting.default里始终保存上一局局数
            Settings.Default.Save();

            Setting.Instance.ResetGameSetting();
        }

        public void Players_CollectionChanged(object sender, EventArgs e)
        {
            SumPlayerScore();
        }
        public void SumPlayerScore()
        {

            var ps = Desk.Instance.Players;
            var sl = new SumLine
            {
                Title = "总计",
                Add_score = ps.Sum(p => p.Add_score),
                Last_add = ps.Sum(p => p.Last_add),
                Balance = ps.Sum(p => p.Balance),
                Last_sub = ps.Sum(p => p.Last_sub),
                Sub_score = ps.Sum(p => p.Sub_score)
            };
            ScoreSum = new ObservableCollection<SumLine>
                {
                    sl
                };
            dgSum.ItemsSource = ScoreSum;
        }

        private void ControlBoard_Activated(object sender, EventArgs e)
        {
            //Focus();
            //Mouse.Click(MouseButton.Left);
            //Mouse.Click(MouseButton.Left);
        }

        private void OnStartGame(object sender, RoutedEventArgs e)
        {
            if (Game.Instance._isShulffling)
            {
                Game.Instance.BreakShuffle();
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
               {
                   btnStartGame.IsEnabled = false;
               }), System.Windows.Threading.DispatcherPriority.Send, null);
                //StartClick();
                if (Setting.Instance._bgm_on)
                {
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
                Game.Instance.Start();
            }
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

        private void OnBackupScore(object sender, RoutedEventArgs e)
        {

        }

        private void OnRestoreScore(object sender, RoutedEventArgs e)
        {

        }

        private void btnOpenCashBox1_Click(object sender, RoutedEventArgs e)
        {
            if (Game.Instance._isGameStarting)
            {
                OnConfig(btnConfig, new RoutedEventArgs());
            }
            else
            {
                Printer.OpenEPSONCashBox(1);
            }
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
            var map = Setting.Instance.PasswordMap;
            var key = Setting.Instance.PasswordMap.FirstOrDefault(p => p.Value == pwd).Key;
            if (string.IsNullOrEmpty(key))
            {
                txtPwd.Password = "";
                return;
            }
            if (Game.Instance._isGameStarting)
            {
                string[] _close_acc_keys = new string[] { "middle_check_waybill_pwd", "audit_account_pwd", "clear_account_pwd", "audit_bet_record_pwd", "quit_front_pwd", "shutdown_pwd" };
                if (!_close_acc_keys.Contains(key))
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
                    var table = Game.Instance.Manager.SetAccountDataTable(AccountPageIndex);
                    if (table == null)
                    {
                        MessageBox.Show("目前尚无记录");
                        return;
                    }
                    dgAccount.ItemsSource = table.DefaultView;
                    spAccount.Visibility = Visibility.Visible;
                    break;
                case "clear_account_pwd":
                    btnClearAccount.Visibility = Visibility.Visible;
                    break;
                case "audit_bet_record_pwd":
                    var win_records = new BetRecord();
                    win_records.Show();
                    break;
                case "quit_front_pwd":
                    SaveSetting(Game.Instance.SessionIndex);
                    App.Current.Shutdown();
                    break;
                case "shutdown_pwd":
                    ShutdownComputer();
                    break;
                case "middle_check_waybill_pwd":
                    MiddleCheck();
                    break;
                default:
                    txtPwd.Password = "";
                    break;
            }
            txtPwd.Password = "";
        }
        private void MiddleCheck()
        {
            if (!Game.Instance._isGameStarting)
            {
                MessageBox.Show("尚未开局");
                return;
            }
            Game.Instance.CoreTimer.StopTimer();
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

                    account.IsClear = true;
                    account.ClearTime = DateTime.Now;
                    db.SaveChanges();

                    var table = Game.Instance.Manager.SetAccountDataTable(0);
                    dgAccount.ItemsSource = table.DefaultView;
                }
            }
        }
        private void btnPreAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountPageIndex >= 1)
            {
                return;
            }
            var table = Game.Instance.Manager.SetAccountDataTable(++AccountPageIndex);
            if (table == null)
            {
                AccountPageIndex--;
            }
            dgAccount.ItemsSource = table.DefaultView;
        }
        private void btnNextAccount_Click(object sender, RoutedEventArgs e)
        {
            if (--AccountPageIndex < 0)
            {
                AccountPageIndex = 0;
            }
            var table = Game.Instance.Manager.SetAccountDataTable(AccountPageIndex);
            if (table == null)
            {
                AccountPageIndex = 0;
            }
            dgAccount.ItemsSource = table.DefaultView;
        }
        public void BreakdownGame()
        {
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    ControlBoard.Instance.btnStartGame.IsEnabled = false;
                    player.Stop();
//                    var img = new ImageUtils().GetScreen();
//                    BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
//                        img.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
//BitmapSizeOptions.FromEmptyOptions());
//                    imgMask.Source = bitmapSource;
//                    imgMask.Visibility = Visibility.Visible;
                    

                    Game.Instance.Animator.StopAllAnimation();
                    Game.Instance.CoreTimer.StopTimer();
                    App.Current.Dispatcher.Thread.Abort();
                    //Thread.CurrentThread.Abort();
                }));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ControlBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
            if (int.TryParse(txtActiveSessionIndex.Text, out int session_index))
            {
                if (session_index <= 1)
                {
                    txtActiveSessionIndex.Text = 1000.ToString();
                }
                else
                {
                    txtActiveSessionIndex.Text = (session_index - 1).ToString();
                }
            }
        }

        private void btnNextActiveSession_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtActiveSessionIndex.Text, out int session_index))
            {
                if (session_index >= 1000)
                {
                    txtActiveSessionIndex.Text = 1.ToString();
                }
                else
                {
                    txtActiveSessionIndex.Text = (session_index + 1).ToString();
                }
            }

        }

        private void dgScore_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var p in Desk.Instance.Players)
            {
                p.Add_score = 0;
                p.Sub_score = 0;
            }
        }

    }
}
