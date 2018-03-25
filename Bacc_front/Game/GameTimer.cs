﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Bacc_front
{
    public class GameTimer
    {
        private const double _web_frame = 200;
        private DispatcherTimer CountdownTimer { get; set; }
        private DispatcherTimer PrintTestTimer{ get; set; }
        private System.Timers.Timer WebTimer { get; set; }
        private Thread KeyListener { get; set; }
        CancellationTokenSource cts = new CancellationTokenSource();

        public void StartWebTimer()
        {
            WebTimer =  new System.Timers.Timer(_web_frame);
            WebTimer.Elapsed += WebTimer_Elapsed;
            WebTimer.Start();
        }

        private void WebTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Game.Instance.WebServer.SendLiveDataToBack();
        }

        public void StartCountdownTimer(TimeSpan interval, EventHandler handler)
        {
            CountdownTimer = new DispatcherTimer { Interval = interval };
            CountdownTimer.Tick += handler;
            CountdownTimer.Start();
        }
        public void StartPrintTestTimer()
        {
            PrintTestTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            PrintTestTimer.Tick += Game.Instance.GamePrinter.TestPrinter;
            PrintTestTimer.Start();
        }
        public void StopPrintTestTimer()
        {
            PrintTestTimer.IsEnabled = false;
        }
        public void StopTimer()
        {
            ControlBoard.Instance.btnStartGame.IsEnabled = true;
            Game.Instance._isGameStarting = false;

            CountdownTimer.Stop();
            cts.Cancel();
        }
        public void StartKeyListenTimer(TimeSpan interval,Action handler)
        {
            KeyListener =  new Thread(new ThreadStart(() => {
                while (!cts.Token.IsCancellationRequested)
                {
                    Thread.Sleep(interval);
                    handler();
                }
            }))
            {
                IsBackground = true
            };
            KeyListener.Start();
        }
        public bool IsCountdownTick(float timer, int countdown, int totaltime)
        {
            if (totaltime - timer <= countdown)
            {
                return true;
            }
            return false;
        }
        public bool IsStateTimeOver(float timer, int totaltime)
        {
            return IsCountdownTick(timer, 0, totaltime);
        }
        public void SetCountDownWithTimer(float cur_timer, int total_seconds)
        {
            var count_down = total_seconds - Math.Floor(cur_timer);
            count_down = count_down < 0 ? 0 : count_down;
            Game.Instance. CountDown = Convert.ToInt32(count_down);
            MainWindow.Instance.txtCountDown.Text =Game.Instance. CountDown.ToString();
        }
        public bool DoOneThingInTimespan(float timer, ref bool isActionDone, ref bool hasStartAction, float start, float end, Action action)
        {
            if (isActionDone)
            {
                return true;
            }
            if (start <= timer)
            {
                if (!hasStartAction)
                {
                    hasStartAction = true;
                    action();
                }
                return false;
            }
            if (timer >= end)
            {
                isActionDone = true;
            }
            return false;
        }
        public bool DoDealAnimationInTimespan(float timer, ref bool isActionDone, ref bool hasStartAction, float start, float end, Action action)
        {
            if (isActionDone)
            {
                return true;
            }
            if (timer<end && timer >= start + 12)
            {
                Game.Instance.Set4CardState();
            }
            if (start <= timer)
            {
                if (!hasStartAction)
                {
                    hasStartAction = true;
                    action();
                }
                return false;
            }
            if (timer >= end)
            {
                isActionDone = true;
            }
            return false;
        }
    }
}