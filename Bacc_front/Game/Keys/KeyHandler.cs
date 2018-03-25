using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using WsUtils;

namespace Bacc_front
{
    public class KeyHandler
    {
        private const int _listenFrameRate = 50;    //毫秒
        private const int _maxKeyNum = 300;
        private List<int> _all_userkeys_lst;
        private KeyDownModel[] _keyMap;
        private Object _myLock = new object();
        public KeyHandler()
        {
            InitKeyMap();
        }
        public void StartListen()
        {
            Game.Instance.CoreTimer.StartKeyListenTimer(TimeSpan.FromMilliseconds(_listenFrameRate), Handle);
        }
        private void InitKeyMap()
        {
            _keyMap = new KeyDownModel[_maxKeyNum];
            var content = FileUtils.getInstance().readStreamingFile("keymap.txt");
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("初始化出错：找不到Config/keymap.txt文件或文件为空！");
                //App.Current.Shutdown();
                return;
            }
            var lines = content.Split('\n');
            for (int i = 0; i < _keyMap.Length; i++)
            {
                _keyMap[i] = new KeyDownModel(i);
            }
            _all_userkeys_lst = new List<int>();
            int p = 0;
            for (int i = 0; i < lines.Length - 1; i += 8)
            {
                List<int> keys = new List<int>();
                int func = 0;
                for (int j = 0; j < 8; j++)
                {
                    var arr = lines[i + j].Split('=');
                    if (0 < j && j < 7 && arr.Length == 2)
                    {
                        var keycode = Convert.ToInt32(arr[1]);
                        var na = new KeyDownModel(keycode)
                        {
                            IsKey = true,
                            Pressed = false,
                            Handler = SetKeyAction(func, p)
                        };
                        _keyMap[keycode] = na;
                        _all_userkeys_lst.Add(keycode);
                        func++;
                    }
                }
                p++;
            }
        }
        private Action SetKeyAction(int func_id, int p_id)
        {
            Action action;
            switch (func_id)
            {
                #region 弃用
                //case 0:
                //    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.banker));
                //    break;
                //case 1:
                //    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.tie));
                //    break;
                //case 2:
                //    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.player));
                //    break;
                //case 3:
                //    action = new Action(() => Desk.Instance.Players[p_id].SetDenomination());
                //    break;
                //case 4:
                //    action = new Action(() => Desk.Instance.Players[p_id].CancleBet());
                //    break;
                //case 5:
                //    action = new Action(() => Desk.Instance.Players[p_id].SetHide());
                //    break;
                //default:
                //    action = new Action(() => { });
                //    break;
                #endregion
                case 1:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.banker));
                    break;
                case 2:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.tie));
                    break;
                case 0:
                    action = new Action(() => Desk.Instance.Players[p_id].Bet(BetSide.player));
                    break;
                case 4:
                    action = new Action(() => Desk.Instance.Players[p_id].ChangeDenomination());
                    break;
                case 3:
                    action = new Action(() => Desk.Instance.Players[p_id].CancleBet());
                    break;
                case 5:
                    action = new Action(() => Desk.Instance.Players[p_id].SetHide());
                    break;
                default:
                    action = new Action(() => { });
                    break;
            }
            return action;
        }
        public void CanclePressed()
        {
            foreach (var key in _all_userkeys_lst)
            {
                var keymodel = _keyMap[key];
                keymodel.Pressed = false;
                keymodel.Timer = 0;
                keymodel.FirstHandled = false;
            }
        }
        private void Handle()
        {
            if (Game.Instance._isInBetting)
            {
                var speed = Setting.Instance._betSpeed;
                foreach (var key in _all_userkeys_lst)
                {
                    var keymodel = _keyMap[key];
                    if (keymodel.Pressed)
                    {
                        keymodel.Timer += 5;
                        if (!keymodel.FirstHandled)
                        {
                            keymodel.Handler();
                            keymodel.FirstHandled = true;
                            Game.Instance.SetCancleSpace();
                            keymodel.Timer = -60;
                            continue;
                        }
                        if (keymodel.Timer >= (110 - speed) && 0 != Setting.Instance._betSpeed)
                        {
                            keymodel.Handler();
                            keymodel.Timer = 0;
                            Game.Instance.SetCancleSpace();
                        }
                    }
                }
            }

        }
        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                //MessageBox.Show("tab");
                //if (System.Windows.Forms.Screen.AllScreens.Length == 1)
                //{
                //    ControlBoard.Instance.Topmost = true;
                //    Topmost = false;
                //    ControlBoard.Instance.Activate();
                //}
            }
            else if (Game.Instance._isInBetting)
            {
                System.Windows.Forms.Keys keycode;
                keycode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
                if(e.SystemKey == Key.F10)
                {
                    keycode = System.Windows.Forms.Keys.F10;
                    e.Handled = true;
                }
                if(e.SystemKey == Key.F9)
                {
                    keycode = System.Windows.Forms.Keys.F9;
                    e.Handled = true;
                }
                if(e.SystemKey == Key.PageUp)
                {
                    keycode = System.Windows.Forms.Keys.PageUp;
                    e.Handled = true;
                }
                if(e.SystemKey == Key.PageDown)
                {
                    keycode = System.Windows.Forms.Keys.PageDown;
                    e.Handled = true;
                }
                //if (e.Key == Key.System)
                //{
                //}
                var keymodel = _keyMap[(int)keycode];
                if (keymodel.IsKey)
                {
                    keymodel.Pressed = true;
                }
            }

        }
        public void Window_KeyUp(object sender, KeyEventArgs e)
        {
            System.Windows.Forms.Keys keycode;
            keycode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            if (e.Key == Key.System)
            {
                keycode = System.Windows.Forms.Keys.F10;
            }
            try
            {
                var key = (int)keycode;
                //lock (_myLock)
                //{
                _keyMap[key].Pressed = false;
                _keyMap[key].Timer = 0;
                _keyMap[key].FirstHandled = false;
                //Game.Instance.StateText += _keyMap[key].keycode + " ";
                //}
            }
            catch (Exception ex)
            {
            }
        }
    }
}
