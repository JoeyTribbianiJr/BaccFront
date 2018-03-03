using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Forms;

namespace Bacc_front
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //base.OnStartup(e);

            //MainWindow w1 = new MainWindow();
            ControlBoard w2 = new ControlBoard();

            Screen s1 = Screen.AllScreens[0];
            Screen s2;

            s2 = Screen.AllScreens.Length >= 2 ? Screen.AllScreens[1] : Screen.AllScreens[0];

            Rectangle r1 = s1.Bounds;
            Rectangle r2 = s2.Bounds;

            w2.Top = r2.Top;
            w2.Left = r2.Left;

            //w1.Show();
            w2.Show();
            w2.WindowState = WindowState.Maximized;

            //w2.Owner = w1;

            Setting.Instance.InitSetting();
            using (SoundPlayer player = new SoundPlayer())
            {
                string location = System.Environment.CurrentDirectory + "\\Sounds\\liangzhu.wav";
                player.SoundLocation = location;
                player.Play();
            }

        }
    }
}
