﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Threading;

namespace Bacc_front
{
    public class GameAnimationGenerator
    {
        public BitmapImage BeautyBmp { get; set; }
        List<BitmapSource> images { get; set; }
        MainWindow _window { get; set; }

        private List<double[]> xy = new List<double[]>() {
            new double[4]{330,636,500,846},
            new double[4]{316,636,436,2},
            new double[4]{330,636,500,886},
            new double[4]{316,636,436,42},
            new double[4]{330,636,500,924},
            new double[4]{316,636,436,82},
        };
        private double[] start_time = new double[6] { 0, 1.4, 2.8, 4.2, 10, 12 };
        private double[] reverse_start_time = new double[6] { 6, 7, 8, 9, 11, 13 };
        private double move_time = 0.8;
        private double reverse_time = 0.8;

        public double visibleDuration;

        private Storyboard moveSB;
        private Storyboard reversalSB;
        private Storyboard armSB;
        private Storyboard wavSB;
        //private ObjectAnimationUsingKeyFrames armChgBg;
        //private DoubleAnimationUsingKeyFrames chgWidth;
        //private DoubleAnimationUsingKeyFrames chgHeight;
        //private DoubleAnimationUsingKeyFrames chgY;
        //private DoubleAnimationUsingKeyFrames chgX;
        public List<Button> btnLst;
        private bool _hasCard5 = true;

        public GameAnimationGenerator()
        {
            _window = MainWindow.Instance;
            btnLst = new List<Button>();
            InitImages();
        }
        private void InitImages()
        {
            _window.RegisterName(_window.Beauty.Name, _window.Beauty);
            _window.RegisterName(_window.Audio0.Name, _window.Audio0);
            _window.RegisterName(_window.Audio1.Name, _window.Audio1);
            _window.RegisterName(_window.Audio2.Name, _window.Audio2);
            _window.RegisterName(_window.Audio3.Name, _window.Audio3);
            _window.RegisterName(_window.Audio4.Name, _window.Audio4);
            _window.RegisterName(_window.Audio5.Name, _window.Audio5);
            images = new List<BitmapSource>();
            images.Add(GetImage("Img/desk.png"));
            //images.Add(GetImage("Img/desk2.png"));
            images.Add(GetImage("Img/deal1.png"));
            images.Add(GetImage("Img/throw1.png"));
            images.Add(GetImage("Img/throw2.png"));
            images.Add(GetImage("Img/deal2.png"));
            images.Add(GetImage("Img/downhead.png"));
        }
        private BitmapSource GetPartImage(string ImgUri, int XCoordinate, int YCoordinate, int Width, int Height)
        {
            return new CroppedBitmap(BitmapFrame.Create(new Uri(ImgUri, UriKind.Relative)), new Int32Rect(XCoordinate, YCoordinate, Width, Height));
        }
        private BitmapSource GetImage(string ImgUri)
        {
            return BitmapFrame.Create(new Uri(ImgUri, UriKind.Relative));
        }

        public void PinCardAnimation()
        {
            var wavsb = (Storyboard)MainWindow.Instance.Resources["sbPinCard"];
            wavsb.Begin(MainWindow.Instance.Audio0);
            var sb = CreatePinCard(0);
            sb.Begin(MainWindow.Instance.Beauty);
        }
        public Storyboard CreatePinCard(double startTime = 0)
        {
            var chgBg = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dk = new DiscreteObjectKeyFrame();
            dk.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime));
            dk.Value = images[1];
            DiscreteObjectKeyFrame dk0 = new DiscreteObjectKeyFrame();
            dk0.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.3));
            dk0.Value = images[2];
            DiscreteObjectKeyFrame dk1 = new DiscreteObjectKeyFrame();
            dk1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.6));
            dk1.Value = images[3];
            DiscreteObjectKeyFrame dk2 = new DiscreteObjectKeyFrame();
            dk2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 1.1));
            dk2.Value = images[0];
            chgBg.KeyFrames.Add(dk);
            chgBg.KeyFrames.Add(dk0);
            chgBg.KeyFrames.Add(dk1);
            chgBg.KeyFrames.Add(dk2);
            Storyboard.SetTargetName(chgBg, _window.Beauty.Name);
            DependencyProperty[] propertyChain2 = new DependencyProperty[]
            {
                Control.BackgroundProperty,
                ImageBrush.ImageSourceProperty
            };
            Storyboard.SetTargetProperty(chgBg, new PropertyPath("(0).(1)", propertyChain2));

            Storyboard pin = new Storyboard();
            pin.Children.Add(chgBg);
            return pin;
        }

        public void StopAllAnimation()
        {
            moveSB?.Stop();
            reversalSB?.Stop();
            armSB?.Stop();
            wavSB?.Stop();
        }
        public void StartDealAnimation()
        {
            //ThreadPool.QueueUserWorkItem(DoDealAnimation);
            DoDealAnimation("j");
        }

        private void DoDealAnimation(object state)
        {
            try
            {
                int b = 0;
                _hasCard5 = true;
                moveSB = new Storyboard();
                reversalSB = new Storyboard();
                armSB = new Storyboard();
                wavSB = new Storyboard();

                if (Setting.Instance.GetStrSetting("single_double") == "单张牌")
                {
                    for (int i = 0; i < 1; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            var card = Game.Instance.CurrentRound.HandCard[j][i];
                            CreateAnimation(b, card);
                            b++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            try
                            {
                                var card = Game.Instance.CurrentRound.HandCard[j][i];
                                CreateAnimation(b, card);
                            }
                            catch (ArgumentOutOfRangeException e)
                            {
                                _hasCard5 = false;
                            }
                            b++;
                        }
                    }
                    armSB.Begin(_window.Beauty);
                    wavSB.Begin(_window);
                    moveSB.Begin(_window);
                    reversalSB.Begin(_window);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //public void StartDealAnimation()
        //{
        //    try
        //    {

        //        int b = 0;
        //        _hasCard5 = true;
        //        moveSB = new Storyboard();
        //        reversalSB = new Storyboard();
        //        armSB = new Storyboard();
        //        wavSB = new Storyboard();

        //        if (Setting.Instance.GetStrSetting("single_double") == "单张牌")
        //        {
        //            for (int i = 0; i < 1; i++)
        //            {
        //                for (int j = 0; j < 2; j++)
        //                {
        //                    var card = Game.Instance.CurrentRound.HandCard[j][i];
        //                    CreateAnimation(b, card);
        //                    b++;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            for (int i = 0; i < 3; i++)
        //            {
        //                for (int j = 0; j < 2; j++)
        //                {
        //                    try
        //                    {
        //                        var card = Game.Instance.CurrentRound.HandCard[j][i];
        //                        CreateAnimation(b, card);
        //                    }
        //                    catch (ArgumentOutOfRangeException e)
        //                    {
        //                        _hasCard5 = false;
        //                    }
        //                    b++;
        //                }
        //            }
        //            armSB.Begin(_window.Casino);
        //            wavSB.Begin(_window);
        //            moveSB.Begin(_window);
        //            reversalSB.Begin(_window);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}

        public void CreateAnimation(int idx, Card card)
        {
            #region 拿到牌的容器button
            var name = "deal" + (idx + 1);
            var find = _window.FindName(name);
            Button btn;
            if (find == null)
            {
                btn = new Button() { Name = name };
                _window.RegisterName(btn.Name, btn);
                _window.Casino.Children.Add(btn);
                btn.Style = (Style)_window.Resources["CardButton"];
                btnLst.Add(btn);
            }
            else
            {
                btn = (Button)find;
            }
            #endregion

            #region 初始化变量
            var bmpIdx = card.PngName;
            var cardBmp = new BitmapImage(new Uri("Img/card/" + bmpIdx + ".png", UriKind.Relative));
            double startTime = start_time[idx];
            if (!_hasCard5 && idx == 5)
            {
                startTime = start_time[4];
            }
            #endregion

            CreateWav(startTime, btn, idx);
            CreateArm(startTime, btn, idx);
            CreateMove(startTime + 0.4, btn, idx);
            var rev_start = reverse_start_time[idx];
            CreateReversal(rev_start, btn, cardBmp);
        }
        public ObjectAnimationUsingKeyFrames CreateArmAnimation(double startTime, Button btn, int idx)
        {
            var deal_style = Setting.Instance.GetStrSetting("deal_style");
            ObjectAnimationUsingKeyFrames armChgBg = new ObjectAnimationUsingKeyFrames();

            DiscreteObjectKeyFrame deal1 = new DiscreteObjectKeyFrame();
            deal1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime));
            deal1.Value = images[1];

            DiscreteObjectKeyFrame deal2 = new DiscreteObjectKeyFrame();
            deal2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.4));
            deal2.Value = images[4];

            //DiscreteObjectKeyFrame dk1 = new DiscreteObjectKeyFrame();
            //dk1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.8));
            //dk1.Value = images[4];


            armChgBg.KeyFrames.Add(deal1);
            armChgBg.KeyFrames.Add(deal2);

            if (deal_style == "直接开牌1")  //手不回到桌下
            {
                var count = Game.Instance.CurrentRound.HandCard[0].Count + Game.Instance.CurrentRound.HandCard[1].Count;
                if (idx < 3)
                {
                    DiscreteObjectKeyFrame hold = new DiscreteObjectKeyFrame();
                    hold.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.9));
                    hold.Value = images[1];
                    armChgBg.KeyFrames.Add(hold);
                }
                else
                {
                    DiscreteObjectKeyFrame back = new DiscreteObjectKeyFrame();
                    back.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.9));
                    back.Value = images[0];
                    armChgBg.KeyFrames.Add(back);
                }
                //if ( idx == count - 1)    //如果是最后一张
                //{
                //    //DiscreteObjectKeyFrame hold = new DiscreteObjectKeyFrame();
                //    //hold.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.9));
                //    //hold.Value = images[1];
                //    //armChgBg.KeyFrames.Add(hold);

                //    if (idx == 5)
                //    {
                //        DiscreteObjectKeyFrame back = new DiscreteObjectKeyFrame();
                //        back.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 1));
                //        back.Value = images[0];
                //        armChgBg.KeyFrames.Add(back);
                //    }
                //    else
                //    {
                //        startTime = reverse_start_time[idx] ;
                //        DiscreteObjectKeyFrame back = new DiscreteObjectKeyFrame();
                //        back.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime));
                //        back.Value = images[0];
                //        armChgBg.KeyFrames.Add(back);
                //    }
                //}
            }
            else if (deal_style == "直接开牌2")  //手回到桌下
            {
                DiscreteObjectKeyFrame back = new DiscreteObjectKeyFrame();
                back.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.9));
                back.Value = images[0];
                armChgBg.KeyFrames.Add(back);

            }
            Storyboard.SetTargetName(armChgBg, _window.Beauty.Name);
            DependencyProperty[] propertyChain2 = new DependencyProperty[]
            {
                Canvas.BackgroundProperty,
                ImageBrush.ImageSourceProperty
            };
            Storyboard.SetTargetProperty(armChgBg, new PropertyPath("(0).(1)", propertyChain2));
            return armChgBg;
        }
        public MediaTimeline CreateWavAnimation(double startTime, Button btn, int idx)
        {
            Uri uri;
            if (idx  == 0 || idx == 2 || idx ==4)
            {
                uri = new Uri("Wav/sendP.wav", UriKind.Relative);
            }
            else
            {
                uri = new Uri("Wav/sendB.wav", UriKind.Relative);
            }
            startTime += 0.3f;
            MediaElement audio = (MediaElement)_window.FindName(("Audio" + idx.ToString()));
            MediaTimeline timeline = new MediaTimeline(TimeSpan.FromSeconds(startTime), new Duration(TimeSpan.FromSeconds(0.8)));
            timeline.Source = uri;
            Storyboard.SetTargetName(timeline, audio.Name);
            return timeline;
        }


        public void CreateWav(double startTime, Button btn, int idx)
        {
            var timeline = CreateWavAnimation(startTime, btn, idx);
            wavSB.Children.Add(timeline);
        }
        public void CreateArm(double startTime, Button btn, int idx)
        {
            var hehe = CreateArmAnimation(startTime, btn, idx);
            armSB.Children.Add(hehe);
        }
        public void CreateMove(double startTime, Button btn, int idx)
        {
            var xy = this.xy[idx];
            var image = new ImageBrush();
            image.ImageSource = new BitmapImage(new Uri("Img/card/back.bmp", UriKind.Relative));
            btn.Background = image;

            #region 控制牌显示
            var chgVisible = new ObjectAnimationUsingKeyFrames();
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
            Storyboard.SetTargetProperty(chgVisible, new PropertyPath(Control.VisibilityProperty));
            #endregion

            #region 控制大小
            var chgWidth = new DoubleAnimationUsingKeyFrames();
            LinearDoubleKeyFrame widthk1 = new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = 0
            };
            LinearDoubleKeyFrame widthk2 = new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.2)),
                Value = 99
            };
            //LinearDoubleKeyFrame widthk3 = new LinearDoubleKeyFrame()
            //{
            //    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(visibleDuration)),
            //    Value = 0
            //};
            chgWidth.KeyFrames.Add(widthk1);
            chgWidth.KeyFrames.Add(widthk2);
            //chgWidth.KeyFrames.Add(widthk3);
            Storyboard.SetTargetName(chgWidth, btn.Name);
            Storyboard.SetTargetProperty(chgWidth, new PropertyPath(Button.WidthProperty));

            var chgHeight = new DoubleAnimationUsingKeyFrames();
            LinearDoubleKeyFrame heightk1 = new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)),
                Value = 0
            };
            LinearDoubleKeyFrame heightk2 = new LinearDoubleKeyFrame()
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.2)),
                Value = 132
            };
            //LinearDoubleKeyFrame heightk3 = new LinearDoubleKeyFrame()
            //{
            //    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(visibleDuration)),
            //    Value = 0
            //};
            chgHeight.KeyFrames.Add(heightk1);
            chgHeight.KeyFrames.Add(heightk2);
            //chgHeight.KeyFrames.Add(widthk3);
            Storyboard.SetTargetName(chgHeight, btn.Name);
            Storyboard.SetTargetProperty(chgHeight, new PropertyPath(Button.HeightProperty));
            #endregion

            #region 控制移动
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
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + move_time)),
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
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + move_time)),
                Value = xy[3]
            };
            chgX.KeyFrames.Add(x0);
            chgX.KeyFrames.Add(xStart);
            chgX.KeyFrames.Add(xEnd);
            Storyboard.SetTargetName(chgX, btn.Name);
            Storyboard.SetTargetProperty(chgX, new PropertyPath(Canvas.LeftProperty));
            #endregion

            moveSB.Children.Add(chgWidth);
            moveSB.Children.Add(chgHeight);
            moveSB.Children.Add(chgVisible);
            moveSB.Children.Add(chgY);
            moveSB.Children.Add(chgX);
        }
        public void CreateReversal(double startTime, Button btn, BitmapImage background)
        {
            var chgBg = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dk = new DiscreteObjectKeyFrame(background);
            dk.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + reverse_time / 2));
            dk.Value = background;
            chgBg.KeyFrames.Add(dk);
            Storyboard.SetTargetName(chgBg, btn.Name);
            DependencyProperty[] propertyChain2 = new DependencyProperty[]
            {
                Button.BackgroundProperty,
                ImageBrush.ImageSourceProperty
            };
            Storyboard.SetTargetProperty(chgBg, new PropertyPath("(0).(1)", propertyChain2));

            var transformOrigin = new PointAnimationUsingKeyFrames();
            EasingPointKeyFrame kf1 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)));
            EasingPointKeyFrame kf2 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)));
            EasingPointKeyFrame kf3 = new EasingPointKeyFrame(new System.Windows.Point(0.5, 0.5), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + reverse_time)));
            transformOrigin.KeyFrames.Add(kf1);
            transformOrigin.KeyFrames.Add(kf2);
            transformOrigin.KeyFrames.Add(kf3);
            Storyboard.SetTargetName(transformOrigin, btn.Name);
            DependencyProperty[] propertyChain1 = new DependencyProperty[]
            {
                 Button.RenderTransformOriginProperty
            };
            Storyboard.SetTargetProperty(transformOrigin, new PropertyPath(Control.RenderTransformOriginProperty));

            var d_transform = new DoubleAnimationUsingKeyFrames();
            EasingDoubleKeyFrame kf4 = new EasingDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)));
            EasingDoubleKeyFrame kf5 = new EasingDoubleKeyFrame(-1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime)));
            EasingDoubleKeyFrame kf6 = new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + reverse_time)));
            d_transform.KeyFrames.Add(kf4);
            d_transform.KeyFrames.Add(kf5);
            d_transform.KeyFrames.Add(kf6);
            Storyboard.SetTargetName(d_transform, btn.Name);
            DependencyProperty[] propertyChain3 = new DependencyProperty[]
            {
                Control.RenderTransformProperty,
                TransformGroup.ChildrenProperty,
                ScaleTransform.ScaleXProperty
            };
            Storyboard.SetTargetProperty(d_transform, new PropertyPath("(0).(1)[0].(2)", propertyChain3));

            reversalSB.Children.Add(transformOrigin);
            reversalSB.Children.Add(d_transform);
            reversalSB.Children.Add(chgBg);
        }
    }
}
