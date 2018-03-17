using Newtonsoft.Json;
using System;
using System.Collections;
using WsUtils.SqliteEFUtils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Bacc_front
{


    [PropertyChanged.ImplementPropertyChanged]
    public class Setting
    {

        /// <summary>
        /// 游戏参数
        /// </summary>
        public bool _is3SecOn = false;
        public string ServerIP;
        public int CurSessionIndex;
        public string JsonPlayers;
        public string JsonAccounts;
        public int _shufTime;
        public int _betTime;
        public int _roundNumPerSession;
        public int _COM_PORT = 1;
        public bool is_cut_bill;
        public bool is_print_bill;
        public int _betSpeed;    //10,20,30,40,50,60,70,80,90,100。0是不连续押分

        public string AdministratorPwd = "17692135195";
        public const string waiter_pwd = "54321";
        public const string manager_pwd = "55321";
        public const string boss_pwd = "56321";
        public const string account_pwd = "56789";
        public const string quit_front_pwd = "999999";
        public const string shutdown_pwd = "888888";
        public const string clear_account_pwd = "100000";
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
            var util = new WsUtils.FileUtils();
            try
            {
            var serverip = util.ReadFile("Config/ServerIP.json");
            ServerIP = JsonConvert.DeserializeObject<string>(serverip);
            }
            catch (Exception)
            {
                MessageBox.Show("Config/ServerIP.json 文件读取失败");
            }

            using (var db = new SQLiteDB())
            {
                try
                {
                    GameSetting setting = db.GameSettings.First<GameSetting>();
                    if (setting.IsGameInit == false)
                    {
                        foreach (var acc in db.FrontAccounts)
                        {
                            db.FrontAccounts.Remove(acc);
                        }
                        db.FrontAccounts.Add(new WsUtils.SqliteEFUtils.FrontAccount()
                        {
                            IsClear = false,
                            CreateTime = DateTime.Now,
                            JsonPlayerAccount = ""
                        });

                        foreach (var acc in db.BetScoreRecords)
                        {
                            db.BetScoreRecords.Remove(acc);
                        }

                        InitGameSetting();
                        db.SaveChanges();

                    }
                    else
                    {
                        game_setting = JsonConvert.DeserializeObject<Dictionary<string, SettingItem>>(setting.JsonGameSettings);
                        JsonPlayers = setting.JsonPlayerScores;
                        CurSessionIndex = setting.CurrentSessionIndex;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    MessageBox.Show(ex.Message);
#endif
                    MessageBox.Show("读取数据库出错");
                }
            }

            is_cut_bill = GetStrSetting("is_cut_bill") == "切单" ? true : false;
            is_print_bill = GetStrSetting("is_print_bill") == "打印路单" ? true : false;

            _betSpeed = GetIntSetting("bet_speed");
            _roundNumPerSession = GetIntSetting("round_num_per_session");
            _is3SecOn = GetStrSetting("open_3_sec") == "3秒功能开" ? true : false;
            _betTime = GetIntSetting("bet_tm");    //押注时间有几秒
            _betTime = 15;
            _shufTime = 2;
        }

        private void InitGameSetting()
        {
            //Game.Instance.SessionIndex = -1;
            CurSessionIndex = -1;
            JsonAccounts = null;
            game_setting.Add("printer", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[2] { "热敏打印机:1", "热敏打印机:2" }
            });
            game_setting.Add("is_cut_bill", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.strings,
                Values = new string[2] { "切单", "不切单" }
            });
            game_setting.Add("is_print_bill", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.strings,
                Values = new string[2] { "打印路单", "不打印路单" }
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
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "对单时间:0", "对单时间:3", "对单时间:30", "对单时间:60", "对单时间:90", "对单时间:120", "对单时间:150", "对单时间:180" }
            });
            game_setting.Add("bet_tm", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "押分时间:10", "押分时间:15", "押分时间:20", "押分时间:30", "押分时间:35", "押分时间:40", "押分时间:45", "押分时间:50", "押分时间:55", "押分时间:60", "押分时间:65", "押分时间:70", "押分时间:75", "押分时间:80", "押分时间:90", }
            });
            game_setting.Add("big_chip_facevalue", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "大筹码:10", "大筹码:20", "大筹码:30", "大筹码:50", "大筹码:100", "大筹码:150", "大筹码:200", "大筹码:500", "大筹码:1000" }
            });
            game_setting.Add("mini_chip_facevalue", new SettingItem()
            {
                SelectedIndex = 0,
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
                Values = new string[] { "总限红:1000", "总限红:2000", "总限红:3000", "总限红:5000", "总限红:10000", "总限红:20000" }
            });
            game_setting.Add("can_cancle_bet", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "根据限红自动调整:0", "押多少分不能取消:100", "押多少分不能取消:200", "押多少分不能取消:300", "押多少分不能取消:500", "押多少分不能取消:1000", "押多少分不能取消:1500", "押多少分不能取消:2000", "押多少分不能取消:3000", "押多少分不能取消:5000" }
            });
            game_setting.Add("desk_limit_red", new SettingItem()
            {
                SelectedIndex = 0,
                Type = SettingItemType.integer,
                Values = new string[] { "单台限红:100", "单台限红:200", "单台限红:300", "单台限红:500", "单台限红:1000", "单台限红:2000", "单台限红:3000", "单台限红:5000", "单台限红:10000", "单台限红:20000", "单台限红:30000", "单台限红:50000", "单台限红:100000" }
            });
            game_setting.Add("tie_limit_red", new SettingItem()
            {
                SelectedIndex = 0,
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
                SelectedIndex = 1,
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
    }
}
