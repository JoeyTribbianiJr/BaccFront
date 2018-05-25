using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using HslCommunication.BasicFramework;

namespace Bacc_front
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// 状态栏文字
        /// </summary>
        public string StateText { get; set; }
        public static MainWindow Instance { get; set; }
        private List<Button> sm_waybill_btns;
        public MainWindow()
        {
            InitializeComponent();
            //Register();
            Instance = this;
            cvsHidebar.Visibility = Visibility.Visible;

            spPlayers.DataContext = Desk.Instance;
            cvsHidebar.DataContext = Desk.Instance;
            bdSign.DataContext = Desk.Instance;

            txtCountDown.DataContext = Game.Instance;
            txtState.DataContext = Game.Instance;
            txtSessionIndex.DataContext = Game.Instance;
            txtBetState.DataContext = Game.Instance;
            txtBanker.DataContext = Game.Instance;
            txtPlayer.DataContext = Game.Instance;
            bdBankerState.DataContext = Game.Instance;
            bdPlayerState.DataContext = Game.Instance;

            PreviewKeyDown += Game.Instance.KeyListener.Window_KeyDown;
            PreviewKeyUp += Game.Instance.KeyListener.Window_KeyUp;

            Game.Instance.NoticeWindowBind += BindWaybills;
            Game.Instance.NoticeRoundOver += ResetSmWaybill;
            WindowStyle = WindowStyle.None;
        }
        SoftAuthorize softAuthorize;
        private void Register()
        {
            InitializeComponent();
            softAuthorize = new HslCommunication.BasicFramework.SoftAuthorize();
            softAuthorize.FileSavePath = Environment.CurrentDirectory + @"\Authorize.txt"; // 设置存储激活码的文件，该存储是加密的
            softAuthorize.LoadByFile();

            // 检测激活码是否正确，没有文件，或激活码错误都算作激活失败
            if (!softAuthorize.IsAuthorizeSuccess(AuthorizeEncrypted))
            {
                // 显示注册窗口
                using (HslCommunication.BasicFramework.FormAuthorize form =
                    new HslCommunication.BasicFramework.FormAuthorize(
                        softAuthorize,
                        "请联系管理员获取激活码",
                        AuthorizeEncrypted))
                {
                    if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        // 授权失败，退出
                        Close();
                        App.Current.Shutdown();
                    }
                }
            }
        }

        /// <summary>
        /// 一个自定义的加密方法，传入一个原始数据，返回一个加密结果
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        private string AuthorizeEncrypted(string origin)
        {
            // 此处使用了组件支持的DES对称加密技术
            return HslCommunication.BasicFramework.SoftSecurity.MD5Encrypt(origin, "12345678");
        }
        private void MainWindow_Activated(object sender, EventArgs e)
        {
        }
        private void BindWaybills()
        {
            var round_num = Game.Instance.Waybill.Count;
            sm_waybill_btns = new List<Button>();
            try
            {
                for (int i = 0; i < round_num; i++)
                {
                    //小路单按钮绑定
                    var block = new Button();
                    block.Style = (Style)Resources["SmallWaybillButton"];
                    block.Tag = i;
                    grdSmallWaybill.Children.Add(block);
                    sm_waybill_btns.Add(block);

                    Binding myBinding = new Binding
                    {
                        Source = Game.Instance,
                        Path = new PropertyPath("Waybill[" + i + "].Winner"),
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    BindingOperations.SetBinding(block, DataContextProperty, myBinding);

                    //大路单按钮绑定
                    var blockBig = new Button();
                    blockBig.Style = (Style)Resources["WaybillBlock"];
                    blockBig.Tag = i;
                    grdBigWaybill.Children.Add(blockBig);

                    var myBindingBig = new Binding
                    {
                        Source = Game.Instance,
                        Path = new PropertyPath("Waybill[" + i + "].Winner"),
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    BindingOperations.SetBinding(blockBig, DataContextProperty, myBindingBig);

                    var col = i / 6;
                    var row = i % 6;
                    Grid.SetRow(blockBig, row);
                    Grid.SetColumn(blockBig, col);
                }

                grdBigWaybill.DataContext = Game.Instance;
                grdSmallWaybill.DataContext = Game.Instance;
            }
            catch (Exception)
            {

                throw;
            }

            //ResetSmWaybill();
        }
        private void ResetSmWaybill()
        {
            try
            {

            var Waybill = Game.Instance.Waybill;
            int pre_row = 0, pre_col = 0, pre = 0;
            int cur_side = Waybill[0].Winner;

            sm_waybill_btns[0].SetValue(Grid.ColumnProperty, pre_row);
            sm_waybill_btns[0].SetValue(Grid.RowProperty, pre_col);

            for (int i = 1; i < Waybill.Count; i++)
            {
                var w_i = Waybill[i].Winner;
                var w_pre = Waybill[pre].Winner;

                if (w_pre == (int)WinnerEnum.tie)
                {
                    if (cur_side == (int)WinnerEnum.tie || w_i == (int)WinnerEnum.tie)
                    {
                        if (++pre_row >= 9)
                        {
                            pre_row = 0;
                            pre_col++;
                        }
                        if (w_i != (int)WinnerEnum.tie)
                        {

                            cur_side = w_i;
                        }
                    }
                    else
                    {
                        if (w_i == cur_side)
                        {
                            if (++pre_row >= 9)
                            {
                                pre_row = 0;
                                pre_col++;
                            }
                        }
                        else
                        {
                            pre_row = 0;
                            pre_col++;
                            cur_side = w_i;
                        }
                    }
                }
                else
                {
                    if (w_i == (int)WinnerEnum.tie || w_i == w_pre)
                    {
                        if (++pre_row >= 9)
                        {
                            pre_row = 0;
                            pre_col++;
                        }
                    }
                    else
                    {
                        pre_row = 0;
                        pre_col++;
                        cur_side = w_i;
                    }
                }

                sm_waybill_btns[i].SetValue(Grid.ColumnProperty, pre_col);
                sm_waybill_btns[i].SetValue(Grid.RowProperty, pre_row);
                pre++;
            }
            }
            catch (Exception ex)
            {
#if DEBUG
                //MessageBox.Show(ex.Message + ex.StackTrace);
#endif
            }
        }

        private void gif_MediaEnded(object sender, RoutedEventArgs e)
        {
            var gif = (MediaElement)sender;
            gif.Position = new TimeSpan(0, 0, 1);
            gif.Play();
        }
    }

}
