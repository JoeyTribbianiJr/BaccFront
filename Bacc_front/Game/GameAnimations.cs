using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Bacc_front
{
    public class GameAnimations
    {
        public BitmapImage BeautyBmp { get; set; }
        List<BitmapSource> images { get; set; }
        MainWindow _window { get; set; }

        private List<double[]> xy = new List<double[]>() {
            new double[4]{316,640,436,54},
            new double[4]{330,640,500,888},
            new double[4]{316,640,436,78},
            new double[4]{330,640,500,864},
            new double[4]{316,640,436,102},
            new double[4]{330,640,500,840}
        };
        private double[] start_time = new double[6] { 0, 2.5, 5, 7.5, 11, 14 };
        private double visibleDuration = 23;
        private List<Storyboard> moveLst;
        private List<Storyboard> reversalLst;
        private List<Button> btnLst;
        private List<Button> sm_waybill_btns;
        private bool _hasCard5 = true;
        
        public GameAnimations()
        {
            _window = MainWindow.Instance;
            InitImages();
            MainWindow.Instance.RegisterName( _window.Audio.Name, _window.Audio);
        }
        private void InitImages()
        {
            _window.RegisterName(_window. Casino.Name, _window.Casino);
            images = new List<BitmapSource>();
            images.Add(GetImage("Img/desk.bmp"));
            images.Add(GetImage("Img/deal1.bmp"));
            images.Add(GetImage("Img/throw1.bmp"));
            images.Add(GetImage("Img/throw2.bmp"));
            images.Add(GetImage("Img/deal2.bmp"));
            images.Add(GetImage("Img/downhead.bmp"));
        }
        private BitmapSource GetPartImage(string ImgUri, int XCoordinate, int YCoordinate, int Width, int Height)
        {
            return new CroppedBitmap(BitmapFrame.Create(new Uri(ImgUri, UriKind.Relative)), new Int32Rect(XCoordinate, YCoordinate, Width, Height));
        }
        private BitmapSource GetImage(string ImgUri)
        {
            return BitmapFrame.Create(new Uri(ImgUri, UriKind.Relative));
        }
        private void PinCardAnimation()
        {
            var wavsb = (Storyboard)MainWindow.Instance.Resources["sbPinCard"];
            wavsb.Begin(MainWindow.Instance.Audio);
            var sb = CreatePinCard(0);
            sb.Begin(MainWindow.Instance.Casino);
        }
public void StartAnimation()
        {
            int b = 0;
            _hasCard5 = true;
            moveLst = new List<Storyboard>();
            reversalLst = new List<Storyboard>();
            btnLst = new List<Button>();
            if (Setting.Instance.GetStrSetting("single_double") == "单张牌")
            {
                for (int i = 0; i < 1; i++)
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
                var lastReversal = reversalLst[reversalLst.Count - 1];
                lastReversal.Completed += lastReversal_Completed;
                for (int i = 0; i < moveLst.Count; i++)
                {
                    moveLst[i].Begin(btnLst[i]);
                    reversalLst[i].Begin(btnLst[i]);
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
                var lastReversal = reversalLst[reversalLst.Count - 1];
                lastReversal.Completed += lastReversal_Completed;
                for (int i = 0; i < moveLst.Count; i++)
                {
                    moveLst[i].Begin(btnLst[i]);
                    reversalLst[i].Begin(btnLst[i]);
                }
            }
        }

        public void lastReversal_Completed(object sender, EventArgs e)
        {
            Game.Instance._animating = false;
            Game.Instance._aniTimer = 0;
        }
        public void CreateAnimation(int idx, Card card)
        {
            var name = "deal" + (idx + 1);
            var find = _window. FindName(name);

            Button btn;
            if (find == null)
            {
                btn = new Button() { Name = name };
                _window. RegisterName(btn.Name, btn);
                _window. Casino.Children.Add(btn);
            }
            else
            {
                btn = (Button)find;
            }
            btn.Style = (Style)_window. Resources["CardButton1"];
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
        public Storyboard CreatePinCard(double startTime = 0)
        {
            var chgBg = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dk = new DiscreteObjectKeyFrame();
            dk.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime));
            dk.Value = images[1];
            DiscreteObjectKeyFrame dk0 = new DiscreteObjectKeyFrame();
            dk0.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime +0.4));
            dk0.Value = images[2];
            DiscreteObjectKeyFrame dk1 = new DiscreteObjectKeyFrame();
            dk1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.8));
            dk1.Value = images[3];
            DiscreteObjectKeyFrame dk2 = new DiscreteObjectKeyFrame();
            dk2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 1));
            dk2.Value = images[0];
            chgBg.KeyFrames.Add(dk);
            chgBg.KeyFrames.Add(dk0);
            chgBg.KeyFrames.Add(dk1);
            chgBg.KeyFrames.Add(dk2);
            Storyboard.SetTargetName(chgBg, _window. Casino.Name);
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
        Storyboard CreateMove(double startTime, Button btn, double[] xy)
        {
            var image = new ImageBrush();
            image.ImageSource = new BitmapImage(new Uri("Img/card/back.bmp", UriKind.Relative));
            btn.Background = image;

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
            var chgBg = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dk = new DiscreteObjectKeyFrame(background);
            dk.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime + 0.5));
            dk.Value = background;
            chgBg.KeyFrames.Add(dk);
            Storyboard.SetTargetName(chgBg, btn.Name);
            DependencyProperty[] propertyChain2 = new DependencyProperty[]
            {
                Control. BackgroundProperty,
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
                 Control.RenderTransformOriginProperty
            };
            //Storyboard.SetTargetProperty(transformOrigin, new PropertyPath(propertyChain1));
            Storyboard.SetTargetProperty(transformOrigin, new PropertyPath(Control.RenderTransformOriginProperty));

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
                Control.RenderTransformProperty,
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
    }
}
