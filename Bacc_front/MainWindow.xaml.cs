using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

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

        public MainWindow()
        {
            InitializeComponent();
            
            Instance = this;
            spPlayers.DataContext = Desk.Instance;
            cvsHidebar.DataContext = Desk.Instance;
            bdSign.DataContext = Desk.Instance;

            txtCountDown.DataContext = Game.Instance;
            txtState.DataContext = Game.Instance;
            txtRoundIndex.DataContext = Game.Instance;
            txtSessionIndex.DataContext = Game.Instance;
            txtBanker.DataContext = Game.Instance;
            txtPlayer.DataContext = Game.Instance;
            bdBankerState.DataContext = Game.Instance;
            bdPlayerState.DataContext = Game.Instance;

            Game.Instance.NoticeWindowBind += BindWaybills;
            //Game.Instance.NoticeDealCard += StartAnimation;
            Game.Instance.NoticeRoundOver += ResetSmWaybill;
            //WindowState = WindowState.Maximized;
            Activated += MainWindow_Activated;
            WindowStyle = WindowStyle.None;
        }
        
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Focus();
            Mouse.Click(MouseButton.Left);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (System.Windows.Forms.Screen.AllScreens.Length == 1)
                {
                    ControlBoard.Instance.Topmost = true;
                    Instance.Topmost = false;
                    ControlBoard.Instance.Activate();
                }
            }
            else if (Game.Instance._isInBetting)
            {
                var keycode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
                var keymodel = Game.Instance.KeyMap[(int)keycode];
                if (keymodel.IsKey)
                {
                    keymodel.Pressed = true;
                    //if (e.IsRepeat)
                    //{
                    //    if (keymodel.Timer >= Game._bet_rate)
                    //    {
                    //        keymodel.Handler();
                    //        keymodel.Timer = 0;
                    //    }
                    //}
                    //else
                    //{
                    //    keymodel.Handler();
                    //}
                }

            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var keycode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            //if (Game.Instance._isBetting)
            //{
            var key = (int)keycode;
            Game.Instance.KeyMap[key].Pressed = false;
            Game.Instance.KeyMap[key].Timer = 0;
            //}
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
                    if (cur_side == (int)WinnerEnum.tie)
                    {
                        if (++pre_row > 10)
                        {
                            pre_row = 0;
                            pre_col++;
                        }
                        cur_side = w_i;
                    }
                    else
                    {
                        if (w_i == cur_side)
                        {
                            if (++pre_row > 10)
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
                        if (++pre_row > 10)
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
       

    }

}
