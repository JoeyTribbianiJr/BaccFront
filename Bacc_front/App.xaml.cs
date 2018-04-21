﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using WsUtils;

namespace Bacc_front
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += App_OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                //System.Windows.MessageBox.Show(args.Exception.Message);
                LogHelper.WriteLog(typeof(Object), "UnobservedTaskException:" + sender.ToString() + args.Exception.Message + args.Exception.StackTrace);
                args.SetObserved();
            };
        }
        /// <summary>  
        /// UI线程抛出全局异常事件处理  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("UI线程全局异常");
            }
        }

        /// <summary>  
        /// 非UI线程抛出全局异常事件处理  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("非UI线程全局异常");
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Setup();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + ex.StackTrace);
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        private void Setup()
        {
            

            MainWindow w1 = new MainWindow();
            ControlBoard w2 = new ControlBoard();

            //var win_records = new BetRecord();
            //win_records.Show();
            //return;

            Screen s2 = Screen.AllScreens[0];
            Screen s1;

            s1 = Screen.AllScreens.Length >= 2 ? Screen.AllScreens[1] : Screen.AllScreens[0];

            Rectangle r1 = s1.Bounds;
            Rectangle r2 = s2.Bounds;

            w1.Top = r1.Top;
            w1.Left = r1.Left;
            w2.Top = r2.Top;
            w2.Left= r2.Left + r2.Width - w2.Width;
            w1.Show();
            w2.Show();
            w1.Owner = w2;
        }
    }
}
