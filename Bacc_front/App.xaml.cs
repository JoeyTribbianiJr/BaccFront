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
        protected override void OnStartup(StartupEventArgs e)
        {
            Setup();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            ControlBoard.Instance.SaveGame();
        }

        private void Setup()
        {
            MainWindow w1 = new MainWindow();
            ControlBoard w2 = new ControlBoard();
            Screen s1 = Screen.AllScreens[0];
            Screen s2;

            s2 = Screen.AllScreens.Length >= 2 ? Screen.AllScreens[1] : Screen.AllScreens[0];

            Rectangle r1 = s1.Bounds;
            Rectangle r2 = s2.Bounds;

            w1.Top = r1.Top;
            w1.Left = r1.Left;
            w2.Top = r2.Top;
            w2.Left= r2.Left + r2.Width - w2.Width;
            w1.Show();
            w2.Show();
            w1.Owner = w2;

            

            SuperServer ss = new SuperServer();
            ss.StartSever();
        }

        
    }
}
