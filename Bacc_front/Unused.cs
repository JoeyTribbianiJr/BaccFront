using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bacc_front
{
    class Unused
    {
        //private void ScanAndHandleKeyMap()
        //{
        //    while (true)
        //    {
        //        if (_isBetting)
        //        {
        //            foreach (var key in _all_userkeys_lst)
        //            {
        //                var keymodel = KeyMap[key];
        //                if (keymodel.Pressed)
        //                {
        //                    if (keymodel.Timer < _bet_rate)
        //                    {
        //                        keymodel.Timer += _frame_rate;
        //                    }
        //                    else
        //                    {
        //                        keymodel.Handler();
        //                        keymodel.Timer = 0;
        //                    }
        //                }
        //            }
        //        }
        //        Thread.Sleep(TimeSpan.FromMilliseconds(_frame_rate));
        //    }
        //}
        //public void HandleKeyMap()
        //{
        //    while (true)
        //    {
        //        if (_isBetting)
        //        {
        //            foreach (var key in _all_userkeys_lst)
        //            {
        //                var keymodel = KeyMap[key];
        //                if (keymodel.Pressed)
        //                {
        //                    if (keymodel.Timer < _bet_rate)
        //                    {
        //                        keymodel.Timer += _frame_rate;
        //                    }
        //                    else
        //                    {
        //                        keymodel.Handler();
        //                        keymodel.Timer = 0;
        //                    }
        //                }
        //            }
        //        }
        //        Thread.Sleep(TimeSpan.FromMilliseconds(_frame_rate));
        //    }
        //}

        //#region 键盘回调
        ///// <summary>
        ///// 松开按键回调
        ///// </summary>
        ///// <param name="key"></param>
        //internal void HandleKeyUp(int key)
        //{
        //    //if (_continuously_bet)
        //    //{
        //    //    _betKeyDownList.RemoveAll(k => k.keycode == key);
        //    //}
        //}
        ///// <summary>
        ///// 按下按键回调
        ///// </summary>
        ///// <param name="key"></param>
        //internal void HandleKeyDown(int key)
        //{
        //    //HandleBetKey(key);

        //    //if (_all_userkeys_lst.Contains(key))
        //    //{
        //    //    if (_bet_userkeys_lst.Contains(key))
        //    //    {
        //    //        if (_continuously_bet) //如果不是连续押分，直接执行
        //    //        {
        //    //            _betKeyDownList.Add(new KeyDownModel(key));
        //    //        }
        //    //        //连续押分要缓存按键
        //    //        HandleBetKey(key);
        //    //    }
        //    //    else
        //    //    {
        //    //        HandleOptionKey(key);
        //    //    }
        //    //}
        //}
        ///// <summary>
        ///// 连续押注按键处理，在每一帧执行
        ///// </summary>
        //private void HandleBetKeyList()
        //{
        //    foreach (var keydown in _betKeyDownList)
        //    {
        //        //第一次按键 或 在_bet_rate时间内未松开
        //        if (keydown.Timer == 0 || keydown.Timer >= _bet_rate)
        //        {
        //            HandleBetKey(keydown.keycode);
        //            keydown.Timer = 0;
        //        }
        //        keydown.Timer += _frame_rate;
        //    }
        //}
        ///// <summary>
        ///// 玩家押注
        ///// </summary>
        ///// <param name="key"></param>
        //private void HandleBetKey(int key)
        //{
        //    var p_idx = GetBetUser(key, out KeyFunc func);
        //    var player = Desk.Instance.Players[p_idx];

        //    player.Bet((BetSide)func);
        //}
        ///// <summary>
        ///// 玩家配置
        ///// </summary>
        ///// <param name="key"></param>
        //private void HandleOptionKey(int key)
        //{
        //    var p_idx = GetOptUser(key, out KeyFunc func);
        //    var player = Desk.Instance.Players[p_idx];
        //    switch (func)
        //    {
        //        //设置押注面额
        //        case KeyFunc.toggle_denomination:
        //            {
        //                player.SetDenomination();
        //                break;
        //            }
        //        //取消所有押注
        //        case KeyFunc.cancle_bet:
        //            {
        //                player.CancleBet();
        //                break;
        //            }
        //        //是否隐藏压了哪家
        //        case KeyFunc.hide_bet:
        //            {
        //                player.SetHide();
        //                break;
        //            }
        //    }
        //}
        ///// <summary>
        ///// 获取按下押注键的玩家
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns>玩家索引</returns>
        //int GetBetUser(int key, out KeyFunc func)
        //{
        //    func = KeyFunc.bet_bank;
        //    int p_idx = 0, f_idx = 0;
        //    for (int i = 0; i < _bet_userkeys_map.Count; i++)
        //    {
        //        var map = _bet_userkeys_map[i];
        //        f_idx = map.IndexOf(key);
        //        if (f_idx > -1)
        //        {
        //            func = (KeyFunc)f_idx;
        //            return p_idx;
        //        }
        //        p_idx++;
        //    }
        //    return -1;
        //}
        ///// <summary>
        ///// 获取按下配置键的玩家索引
        ///// </summary>
        ///// <param name="key"></param>
        ///// <returns></returns>
        //int GetOptUser(int key, out KeyFunc func)
        //{
        //    func = KeyFunc.bet_bank;
        //    var f_idx = 0;
        //    int p_idx = 0;
        //    for (int i = 0; i < _opt_userkeys_map.Count; i++)
        //    {
        //        var map = _opt_userkeys_map[i];
        //        f_idx = map.IndexOf(key);
        //        if (f_idx > -1)
        //        {
        //            func = (KeyFunc)(f_idx + 3);
        //            return p_idx;
        //        }
        //        p_idx++;
        //    }
        //    return -1;
        //}
        //#endregion


        //bool InitGame()
        //{
        //    if (_isGameInited)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        if (_isGameIniting)
        //        {
        //            return false;
        //        }
        //        else
        //        {
        //            _isGameIniting = true;
        //            Desk.Instance.InitDeskForGame();
        //            NoticeGameStart();

        //            AllSessions = new ObservableCollection<Session>();
        //            _betKeyDownList = new List<KeyDownModel>();

        //            //押注时间有几秒
        //            _betTime = _setting.GetIntSetting("bet_tm");
        //            _betTime = 3;
        //            _is3SecOn = _setting.GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;

        //            //提前几秒通知服务器押注结束
        //            _send_to_svr = 5;
        //            InitKeymap();

        //            SessionIndex = 0;
        //            NewSession();
        //            _isGameInited = true;
        //            _isGameIniting = false;
        //            return true;
        //        }
        //    }
        //}
    }

}
