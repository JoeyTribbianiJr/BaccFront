using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Bacc_front.Properties;
using WsUtils;

namespace Bacc_front
{
    [PropertyChanged.ImplementPropertyChanged]
    public class Setting
    {
        public const string secret_key = "0109010901090109";
        public const int max_session_num = 1000;
        /// <summary>
        /// 游戏参数
        /// </summary>
        public bool _is3SecOn = false;
        public string ServerUrl;
        public int CurSessionIndex;
        public int _check_waybill_tm;
        public int _mi_deal_time;
        public string _deal_style;
        public int _big_chip_facevalue;
        public int _mini_chip_facevalue;
        public int _min_limit_bet;
        public int _total_limit_red;
        public int _can_cancle_bet;
        public int _desk_limit_red;
        public int _tie_limit_red;
        public bool _limit_red_on_3sec;
        public string _single_double;
        public bool _bgm_on;
        public int _boom;
        public int _betTime;
        public int _round_num_per_session;
        public int _COM_PORT = 1;
        public string _print_font;
        public string _is_print_bill;
        public int _betSpeed;    //10,20,30,40,50,60,70,80,90,100。0是不连续押分

        public const string AdministratorPwd = "28750833";
        public Dictionary<string, string> PasswordMap;
        public string waiter_pwd;
        public string manager_pwd;
        public string boss_pwd;
        public string account_pwd;
        public string quit_front_pwd;
        public string shutdown_pwd;
        public string clear_account_pwd;
        public string[] manager_menu_items = new string[]
        {
            "deal_style","mi_deal_time", "check_waybill_tm","bet_tm","open_3_sec", "big_chip_facevalue",
            "mini_chip_facevalue", "min_limit_bet","bgm",
        };
        /// <summary>
        /// 牌局设置
        /// </summary>
        public Dictionary<string, SettingItem> game_setting = new Dictionary<string, SettingItem>();
        private static Setting instance;

        public PlayType play_type;

        public static Setting Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Setting();
                }
                return instance;
            }
        }

        private Setting()
        {
            try
            {
                if (!Settings.Default._isGameInited)
                {
                    InitGameSetting();
                    Settings.Default.GameSetting = JsonConvert.SerializeObject(game_setting);
                    Settings.Default.CurrentSessionIndex = -1;
                    Settings.Default._isGameInited = true;
                    Settings.Default.Save();
                }
                InitPassward();
                SetGameSetting();
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message);
#endif
                MessageBox.Show("读取用户配置文件出错");
            }
        }
        public void SetGameSetting()
        {
            game_setting = JsonConvert.DeserializeObject<Dictionary<string, SettingItem>>(Settings.Default.GameSetting);
            //ServerUrl = AESEncrypt.Decrypt(Settings.Default.ServerUrl, secret_key);
            if (string.IsNullOrEmpty(Settings.Default.ServerUrl))
            {
                ServerUrl = "127.0.0.1";
            }
            else
            {
                ServerUrl = AESEncrypt.Decrypt(Settings.Default.ServerUrl, secret_key);
            }
            //保存的是当前运行的局数
            CurSessionIndex = Settings.Default.CurrentSessionIndex;
            _is_print_bill = GetStrSetting("is_print_bill");
            _betSpeed = GetIntSetting("bet_speed");
            _round_num_per_session = GetIntSetting("round_num_per_session");
            _is3SecOn = GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;
            _betTime = GetIntSetting("bet_tm");    //押注时间有几秒
            _check_waybill_tm = GetIntSetting("check_waybill_tm");
            _mi_deal_time = GetIntSetting("mi_deal_time");
            _deal_style = GetStrSetting("deal_style");
            _big_chip_facevalue = GetIntSetting("big_chip_facevalue");
            _mini_chip_facevalue = GetIntSetting("mini_chip_facevalue");
            _min_limit_bet = GetIntSetting("min_limit_bet");
            _total_limit_red = GetIntSetting("total_limit_red");
            _can_cancle_bet = GetIntSetting("can_cancle_bet");
            _desk_limit_red = GetIntSetting("desk_limit_red");
            _tie_limit_red = GetIntSetting("tie_limit_red");
            _limit_red_on_3sec = GetStrSetting("limit_red_on_3sec") == "超限红可试分" ? true : false;
            _print_font = GetStrSetting("print_font");
            _single_double = GetStrSetting("single_double");
            _bgm_on = GetStrSetting("bgm") == "背景音乐开" ? true : false;
            _boom = GetIntSetting("boom");
        }
        public void ResetGameSetting()
        {
            SetGameSetting();
            foreach (var player in Desk.Instance.Players)
            {
                player.Denominations[0] = _big_chip_facevalue;
                player.Denominations[1] = _mini_chip_facevalue;
            }
            if (Game.Instance.CurrentState == GameState.Preparing)
            {
                Game.Instance.SessionIndex = CurSessionIndex;   //读取的之前运行时保存的局数
                Game.Instance.ResetSession();
                //Game.Instance.CurrentSession.SessionId = CurSessionIndex;
            }
        }
        private void InitGameSetting()
        {
            CurSessionIndex = -1;
            game_setting.Add("printer", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[2] { "热敏打印机:1", "热敏打印机:2" }
            });
            game_setting.Add("print_font", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.strings,
                Values = new string[2] { "小字体路单", "大字体路单" }
            });
            game_setting.Add("is_print_bill", new SettingItem()
            {
                SelectedIndex = 1,
                Type = SettingItemType.strings,
                Values = new string[3] { "打印路单", "不打印路单","打印不监控" }
            });
            game_setting.Add("deal_style", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.strings,
                Values = new string[4] { "直接开牌1", "眯牌开牌1", "直接开牌2", "眯牌开牌2" }
            });
            game_setting.Add("mi_deal_time", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "开牌时间:3", "开牌时间:5", "开牌时间:8" }
            });
            game_setting.Add("bet_speed", new SettingItem()
            {
                SelectedIndex = 9,
                Type = SettingItemType.integer,
                Values = new string[] { "连续押分速度:0", "连续押分速度:10", "连续押分速度:20", "连续押分速度:30", "连续押分速度:40", "连续押分速度:50", "连续押分速度:60", "连续押分速度:70", "连续押分速度:80", "连续押分速度:90", "连续押分速度:100" }
            });
            game_setting.Add("round_num_per_session", new SettingItem()
            {
                SelectedIndex = 5,
                Type = SettingItemType.integer,
                Values = new string[] { "一场局数:40", "一场局数:45", "一场局数:50", "一场局数:55", "一场局数:60", "一场局数:66" }
            });
            game_setting.Add("check_waybill_tm", new SettingItem()
            {
                SelectedIndex = 2,
                Type = SettingItemType.integer,
                Values = new string[] { "对单时间:0", "对单时间:3", "对单时间:30", "对单时间:60", "对单时间:90", "对单时间:120", "对单时间:150", "对单时间:180" }
            });
            game_setting.Add("bet_tm", new SettingItem()
            {
                SelectedIndex = 4,
                Type = SettingItemType.integer,
                Values = new string[] { "押分时间:10", "押分时间:15", "押分时间:20", "押分时间:25", "押分时间:30", "押分时间:35", "押分时间:40", "押分时间:45", "押分时间:50", "押分时间:55", "押分时间:60", "押分时间:65", "押分时间:70", "押分时间:75", "押分时间:80", "押分时间:90", }
            });
            game_setting.Add("big_chip_facevalue", new SettingItem()
            {
                SelectedIndex = 4,
                Type = SettingItemType.integer,
                Values = new string[] { "大筹码:10", "大筹码:20", "大筹码:30", "大筹码:50", "大筹码:100", "大筹码:150", "大筹码:200", "大筹码:500", "大筹码:1000" }
            });
            game_setting.Add("mini_chip_facevalue", new SettingItem()
            {
                SelectedIndex = 3,
                Type = SettingItemType.integer,
                Values = new string[] { "小筹码:1", "小筹码:2", "小筹码:5", "小筹码:10", "小筹码:20", "小筹码:30", "小筹码:40", "小筹码:50", "小筹码:100" }
            });
            game_setting.Add("min_limit_bet", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "最小限注:10", "最小限注:20", "最小限注:30", "最小限注:50", "最小限注:100" }
            });
            game_setting.Add("total_limit_red", new SettingItem()
            {
                SelectedIndex = 2,
                Type = SettingItemType.integer,
                Values = new string[] { "全台限红:1000", "全台限红:2000", "全台限红:3000", "全台限红:5000", "全台限红:10000", "全台限红:20000" }
            });
            game_setting.Add("can_cancle_bet", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "根据限红自动调整:0", "押多少分不能取消:100", "押多少分不能取消:200", "押多少分不能取消:300", "押多少分不能取消:500", "押多少分不能取消:1000", "押多少分不能取消:1500", "押多少分不能取消:2000", "押多少分不能取消:3000", "押多少分不能取消:5000" }
            });
            game_setting.Add("desk_limit_red", new SettingItem()
            {
                SelectedIndex = 9,
                Type = SettingItemType.integer,
                Values = new string[] { "单台限红:100", "单台限红:200", "单台限红:300", "单台限红:500", "单台限红:1000", "单台限红:2000", "单台限红:3000", "单台限红:5000", "单台限红:10000", "单台限红:20000", "单台限红:30000", "单台限红:50000", "单台限红:100000" }
            });
            game_setting.Add("tie_limit_red", new SettingItem()
            {
                SelectedIndex = 2,
                Type = SettingItemType.integer,
                Values = new string[] { "和限红:100", "和限红:200", "和限红:300", "和限红:500", "和限红:1000" }
            });
            game_setting.Add("open_3_sec", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.strings,
                Values = new string[] { "3秒功能开", "3秒功能关" }
            });
            game_setting.Add("limit_red_on_3sec", new SettingItem()
            {
                SelectedIndex = 1,
                Type = SettingItemType.strings,
                Values = new string[] { "超限红可试分", "超限红不可试分" }
            });
            game_setting.Add("single_double", new SettingItem()
            {
                SelectedIndex = 1,
                Type = SettingItemType.strings,
                Values = new string[] { "单张牌", "两张牌" }
            });
            game_setting.Add("bgm", new SettingItem()
            {
                SelectedIndex = 1,
                Type = SettingItemType.strings,
                Values = new string[] { "背景音乐关", "背景音乐开" }
            });
            game_setting.Add("boom", new SettingItem()
            {
                SelectedIndex = 3,
                Type = SettingItemType.strings,
                Values = new string[] { "爆机:0", "爆机:5000", "爆机:10000", "爆机:20000", "爆机:30000", "爆机:50000", "爆机:100000", "爆机:200000", "爆机:300000", "爆机:500000", }
            });
        }
        public int GetIntSetting(string key)
        {
            var item = game_setting[key];
            var str = item.Values[item.SelectedIndex];
            var v = str.Split(':')[1];
            return Convert.ToInt32(v);
        }
        public string GetStrSetting(string key)
        {
            var item = game_setting[key];
            var str = item.Values[item.SelectedIndex];
            return str;
        }

        public ObservableCollection<Player> GetJsonPlayersScore()
        {
            var json = AESEncrypt.Decrypt(Settings.Default.JsonPlayerScores, secret_key);
            return JsonConvert.DeserializeObject<ObservableCollection<Player>>(json);
        }
        public void SaveJsonPlayersScoreToDefault(ObservableCollection<Player> players)
        {
            var json = JsonConvert.SerializeObject(players);
            var text = AESEncrypt.Encrypt(json, secret_key);
            Settings.Default.JsonPlayerScores = text;
            Settings.Default.Save();
        }
        private void InitPassward()
        {
            PasswordMap = new Dictionary<string, string>();
            var set = Settings.Default;
            PasswordMap.Add("waiter_pwd", AESEncrypt.Decrypt(set.waiter_pwd, secret_key));
            PasswordMap.Add("manager_pwd", AESEncrypt.Decrypt(set.manager_pwd, secret_key));
            PasswordMap.Add("boss_pwd", AESEncrypt.Decrypt(set.boss_pwd, secret_key));
            PasswordMap.Add("audit_account_pwd", AESEncrypt.Decrypt(set.audit_account_pwd, secret_key));
            PasswordMap.Add("audit_bet_record_pwd", AESEncrypt.Decrypt(set.audit_bet_record_pwd, secret_key));
            PasswordMap.Add("quit_front_pwd", AESEncrypt.Decrypt(set.quit_front_pwd, secret_key));
            PasswordMap.Add("conn_front_pwd", AESEncrypt.Decrypt(set.conn_front_pwd, secret_key));
            PasswordMap.Add("shutdown_pwd", AESEncrypt.Decrypt(set.shutdown_pwd, secret_key));
            PasswordMap.Add("clear_account_pwd", AESEncrypt.Decrypt(set.clear_account_pwd, secret_key));
            PasswordMap.Add("middle_check_waybill_pwd", AESEncrypt.Decrypt(set.middle_check_waybill_pwd, secret_key));
        }
        public void SavePassword(Dictionary<string, string> dic)
        {
            var set = Settings.Default;
            set.waiter_pwd = AESEncrypt.Encrypt(dic["waiter_pwd"], secret_key);
            set.manager_pwd = AESEncrypt.Encrypt(dic["manager_pwd"], secret_key);
            set.boss_pwd = AESEncrypt.Encrypt(dic["boss_pwd"], secret_key);
            set.audit_account_pwd = AESEncrypt.Encrypt(dic["audit_account_pwd"], secret_key);
            set.audit_bet_record_pwd = AESEncrypt.Encrypt(dic["audit_bet_record_pwd"], secret_key);
            set.quit_front_pwd = AESEncrypt.Encrypt(dic["quit_front_pwd"], secret_key);
            set.conn_front_pwd = AESEncrypt.Encrypt(dic["conn_front_pwd"], secret_key);
            set.shutdown_pwd = AESEncrypt.Encrypt(dic["shutdown_pwd"], secret_key);
            set.clear_account_pwd = AESEncrypt.Encrypt(dic["clear_account_pwd"], secret_key);
            set.middle_check_waybill_pwd = AESEncrypt.Encrypt(dic["middle_check_waybill_pwd"], secret_key);
            set.Save();
            InitPassward();
        }
    }
}
