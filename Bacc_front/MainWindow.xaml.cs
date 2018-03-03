﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PropertyChanged;

namespace Bacc_front
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Casino.DataContext = Desk.Instance;
            Game.Instance.NoticeGameStart += BindWaybills;
            Game.Instance.NoticeRoundOver += ResetSmWaybill;
            Game.Instance.NoticeDealCard += StartAnimation;
            //WindowState = WindowState.Maximized;
        }

        public BitmapImage BeautyBmp { get; set; }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var keymodel = Game.Instance.KeyMap[(int)e.Key];
            if (keymodel.IsKey)
            {
                if (Game.Instance._isBetting)
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
            //if (e.IsRepeat) { return; }
            //else if (Game.Instance._isBetting)
            //{
            //    var key = (int)e.Key;
            //    Game.Instance.KeyMap[key].Pressed = true;
            //}
            //Game.Instance.HandleKeyDown(code);
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //if (Game.Instance._isBetting)
            //{
            var key = (int)e.Key;
            Game.Instance.KeyMap[key].Pressed = false;
            //}
            //Game.Instance.HandleKeyUp((int)e.Key);
        }
        private void BindWaybills()
        {
            var round_num = Setting.Instance.GetIntSetting("round_num_per_session");
            var grid = this.grdBigWaybill;
            //grid.DataContext = Desk.Instance.Waybill;
            sm_waybill_btns = new List<Button>();
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
                    Source = this,
                    Path = new PropertyPath("Waybill[" + i + "].Winner"),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(block, DataContextProperty, myBinding);

                //大路单按钮绑定
                var blockBig = new Button();
                blockBig.Style = (Style)Resources["WaybillBlock"];
                blockBig.Tag = i;
                grid.Children.Add(blockBig);

                var myBindingBig = new Binding
                {
                    Source = Desk.Instance,
                    Path = new PropertyPath("Waybill[" + i + "].Winner"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(blockBig, DataContextProperty, myBindingBig);

                var col = i / 6;
                var row = i % 6;
                Grid.SetRow(blockBig, row);
                Grid.SetColumn(blockBig, col);
            }

            ResetSmWaybill();
        }
        void StartAnimation()
        {
            int b = 0;
            moveLst = new List<Storyboard>();
            reversalLst = new List<Storyboard>();
            btnLst = new List<Button>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    try
                    {
                        var card = Desk.Instance._curHandCards[j][i];
                        CreateAnimation(b, card);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        _hasCard5 = false;
                    }
                    b++;
                }
            }
            var lastReversal = reversalLst[reversalLst.Count - 1];
            lastReversal.Completed += lastReversal_Completed;
            //lastReversal.Duration = TimeSpan.FromSeconds(5);
            for (int i = 0; i < moveLst.Count; i++)
            {
                moveLst[i].Begin(btnLst[i]);
                reversalLst[i].Begin(btnLst[i]);
            }
        }

        private void lastReversal_Completed(object sender, EventArgs e)
        {
            Game.Instance._animating = false;
            Game.Instance._aniTimer = 0;
        }

        void CreateAnimation(int idx, Card card)
        {
            var name = "deal" + (idx + 1);
            var find = FindName(name);

            Button btn;
            if (find == null)
            {
                btn = new Button() { Name = name };
                RegisterName(btn.Name, btn);
                Casino.Children.Add(btn);
            }
            else
            {
                btn = (Button)find;
            }
            btn.Style = (Style)Resources["CardButton1"];
            var bmpIdx = card.GetPngName;
            var cardBmp = new BitmapImage(new Uri("Img/card/" + bmpIdx + ".png", UriKind.Relative));
            double startTime = start_time[idx];

            if (!_hasCard5 && idx == 5)
            {
                startTime -= 3;
            }

            var move = CreateMove(startTime, btn, xy[idx]);
            moveLst.Add(move);
            var reversal = CreateReversal(startTime + 1, btn, cardBmp);
            reversalLst.Add(reversal);
            btnLst.Add(btn);

        }
        Storyboard CreateMove(double startTime, Button btn, double[] xy)
        {
            var image = new ImageBrush();
            image.ImageSource = new BitmapImage(new Uri("Img/card/back.bmp", UriKind.Relative));
            btn.Background = image;

            var chgVisible = new ObjectAnimationUsingKeyFrames();
            //chgVisible.Duration = new Duration(TimeSpan.FromSeconds(visibleDuration));
            DiscreteObjectKeyFrame dk1 = new DiscreteObjectKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = Visibility.Hidden
            };
            DiscreteObjectKeyFrame dk2 = new DiscreteObjectKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)),
                Value = Visibility.Visible
            };
            DiscreteObjectKeyFrame dk3 = new DiscreteObjectKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(visibleDuration)),
                Value = Visibility.Hidden
            };
            chgVisible.KeyFrames.Add(dk1);
            chgVisible.KeyFrames.Add(dk2);
            chgVisible.KeyFrames.Add(dk3);
            Storyboard.SetTargetName(chgVisible, btn.Name);
            Storyboard.SetTargetProperty(chgVisible, new PropertyPath(VisibilityProperty));

            var chgY = new DoubleAnimationUsingKeyFrames();
            var y0 = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = xy[0]
            };
            var yStart = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)),
                Value = xy[0]
            };
            var yEnd = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.5)),
                Value = xy[1]
            };
            chgY.KeyFrames.Add(y0);
            chgY.KeyFrames.Add(yStart);
            chgY.KeyFrames.Add(yEnd);
            Storyboard.SetTargetName(chgY, btn.Name);
            Storyboard.SetTargetProperty(chgY, new PropertyPath(Canvas.TopProperty));

            var chgX = new DoubleAnimationUsingKeyFrames();
            var x0 = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = xy[2]
            };
            var xStart = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)),
                Value = xy[2]
            };
            var xEnd = new EasingDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.5)),
                Value = xy[3]
            };
            chgX.KeyFrames.Add(x0);
            chgX.KeyFrames.Add(xStart);
            chgX.KeyFrames.Add(xEnd);
            Storyboard.SetTargetName(chgX, btn.Name);
            Storyboard.SetTargetProperty(chgX, new PropertyPath(Canvas.LeftProperty));

            var move = new Storyboard();
            move.Children.Add(chgVisible);
            move.Children.Add(chgY);
            move.Children.Add(chgX);

            return move;
        }
        Storyboard CreateReversal(double startTime, Button btn, BitmapImage background)
        {
            //CardBmp = new BitmapImage(new Uri("Img/card/22.png", UriKind.Relative));
            var chgBg = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dk = new DiscreteObjectKeyFrame(background);
            dk.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.5));
            //var bmp = (Bitmap)Properties.Resources.ResourceManager.GetObject("_" + 11);
            //BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            //dk.Value = bitmapSource;
            dk.Value = background;
            chgBg.KeyFrames.Add(dk);
            Storyboard.SetTargetName(chgBg, btn.Name);
            DependencyProperty[] propertyChain2 = new DependencyProperty[]
            {
                BackgroundProperty,
                ImageBrush.ImageSourceProperty
            };
            Storyboard.SetTargetProperty(chgBg, new PropertyPath("(0).(1)", propertyChain2));

            var transformOrigin = new PointAnimationUsingKeyFrames();
            EasingPointKeyFrame kf1 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)));
            EasingPointKeyFrame kf2 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)));
            EasingPointKeyFrame kf3 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0.5), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 1)));
            transformOrigin.KeyFrames.Add(kf1);
            transformOrigin.KeyFrames.Add(kf2);
            transformOrigin.KeyFrames.Add(kf3);
            Storyboard.SetTargetName(transformOrigin, btn.Name);
            DependencyProperty[] propertyChain1 = new DependencyProperty[]
            {
                RenderTransformOriginProperty
            };
            //Storyboard.SetTargetProperty(transformOrigin, new PropertyPath(propertyChain1));
            Storyboard.SetTargetProperty(transformOrigin, new PropertyPath(RenderTransformOriginProperty));

            var d_transform = new DoubleAnimationUsingKeyFrames();
            EasingDoubleKeyFrame kf4 = new EasingDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)));
            EasingDoubleKeyFrame kf5 = new EasingDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)));
            EasingDoubleKeyFrame kf6 = new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 1)));
            d_transform.KeyFrames.Add(kf4);
            d_transform.KeyFrames.Add(kf5);
            d_transform.KeyFrames.Add(kf6);
            Storyboard.SetTargetName(d_transform, btn.Name);
            DependencyProperty[] propertyChain3 = new DependencyProperty[]
            {
                RenderTransformProperty,
                TransformGroup.ChildrenProperty,
                ScaleTransform.ScaleXProperty
            };
            Storyboard.SetTargetProperty(d_transform, new PropertyPath("(0).(1)[0].(2)", propertyChain3));

            var reversal = new Storyboard();
            reversal.Children.Add(transformOrigin);
            reversal.Children.Add(d_transform);
            reversal.Children.Add(chgBg);

            return reversal;
        }
        private void ResetSmWaybill()
        {
            var Waybill = Desk.Instance.Waybill;
            int pre_row = 0, pre_col = 0, pre = 0;
            int cur_side = Waybill[0].Winner;

            sm_waybill_btns[0].SetValue(Grid.ColumnProperty, pre_row);
            sm_waybill_btns[0].SetValue(Grid.RowProperty, pre_col);

            for (int i = 1; i < Waybill.Count; i++)
            {
                var w_i = Waybill[i].Winner;
                var w_pre = Waybill[pre].Winner;


                if (w_pre == (int)Winner.tie)
                {
                    if (cur_side == (int)Winner.tie)
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
                    if (w_i == (int)Winner.tie || w_i == w_pre)
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
        private List<double[]> xy = new List<double[]>() {
            new double[4]{316,640,436,54},
            new double[4]{330,640,500,888},
            new double[4]{316,640,436,78},
            new double[4]{330,640,500,864},
            new double[4]{316,640,436,102},
            new double[4]{330,640,500,840}
        };
        private double[] start_time = new double[6] { 0, 2.5, 5, 7.5, 11, 14 };
        private double visibleDuration = 15;
        private List<Storyboard> moveLst;
        private List<Storyboard> reversalLst;
        private List<Button> btnLst;
        private double onceAnimation = 0;
        private List<Button> sm_waybill_btns;
        private bool _hasCard5;

    }

}
