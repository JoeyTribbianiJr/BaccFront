using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace Bacc_front
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
    public MediaPlayer player;
        /// <summary>
        /// 网络服务器
        /// </summary>
        public NetServer scr_transfer;
        protected override void OnStartup(StartupEventArgs e)
        {
            Setup();
            //scr_transfer = new NetServer();
            //scr_transfer.StartServer();
            //SuperServer ss = new SuperServer();
            //ss.StartSever();
        }

        private void Setup()
        {
            MainWindow w1 = new Bacc_front.MainWindow();
            ControlBoard w2 = new ControlBoard();
            Screen s1 = Screen.AllScreens[0];
            Screen s2;

            s2 = Screen.AllScreens.Length >= 2 ? Screen.AllScreens[1] : Screen.AllScreens[0];

            Rectangle r1 = s1.Bounds;
            Rectangle r2 = s2.Bounds;

            w1.Top = r1.Top;
            w1.Left= r1.Left;
            w2.Top = r2.Top;
            w2.Left = r2.Left;
            w1.Show();
            w2.Show();
            //w1.Owner = w2;

            player = new MediaPlayer();
            string location = System.Environment.CurrentDirectory + "\\Wav\\scene.wav";
            player.Open(new Uri(location, UriKind.Absolute));
            player.MediaEnded += LoopPlay;
            player.Play();

            SuperServer ss = new SuperServer();
            ss.StartSever();
        }

        private void LoopPlay(object sender, EventArgs e)
        {
            MediaPlayer player = (MediaPlayer)sender;
            string location = System.Environment.CurrentDirectory + "\\Wav\\scene.wav";
            player.Open(new Uri(location, UriKind.Absolute));
            player.Play();
        }
    }
}
