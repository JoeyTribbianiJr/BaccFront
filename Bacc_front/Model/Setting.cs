using System;
using System.Collections;
using System.Collections.Generic;

namespace Bacc_front
{
	public class SettingItem
	{
		//显示在菜单上的描述
		public int SelectedIndex { get; set; }

		public SettingItemType Type { get; set; }
		//item的所有可选值
		public string[] Values { get; set; }
	}
	public enum PlayType
	{
		single,
		two
	}
	public enum SettingItemType
	{
		intager,
		strings
	}

	public class Setting
	{
		/// <summary>
		/// 游戏参数
		/// </summary>
		public Dictionary<string, SettingItem> app_setting = new Dictionary<string, SettingItem>();
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
			InitGameSetting();
		}

		private void InitAppSetting()
		{
			game_setting.Add("send_to_svr_second", new SettingItem()
			{
				SelectedIndex = 1,
				Type = SettingItemType.intager,
				Values = new string[2] { "通知服务器:3", "通知服务器:5" }
			});
		}
		private void InitGameSetting()
		{
			game_setting.Add("printer", new SettingItem()
			{
				SelectedIndex = 1,
				Type = SettingItemType.intager,
				Values = new string[2] { "热敏打印机:1", "热敏打印机:2" }
			});

			game_setting.Add("round_num_per_session", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "一场局数:30", "一场局数:45", "一场局数:66" }
			});

			game_setting.Add("check_waybill_tm", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "对单时间:30", "对单时间:60" }
			});

			game_setting.Add("bet_tm", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "押分时间:30", "押分时间:60" }
			});
			//game_setting.Add("shuffle_tm", new SettingItem() { desc = "洗牌时间", value = 120, values = new int[] { 30, 60 } });

			game_setting.Add("big_chip_facevalue", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "大筹码:100", "大筹码:500", "大筹码:1000" }
			});

			game_setting.Add("mini_chip_facevalue", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "小筹码:1", "小筹码:10", "小筹码:100" }
			});

			game_setting.Add("total_limit_red", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "总限红:3000", "总限红:5000" }
			});

			game_setting.Add("desk_limit_red", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "单台限红:3000", "单台限红:5000", "单台限红:30000" }
			});

			game_setting.Add("tie_limit_red", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.intager,
				Values = new string[] { "和限红:3000", "和限红:5000", "和限红:30000" }
			});

			//game_str_setting.Add("bgm_on", new SettingStrItem() { desc = "背景音乐开关", value = "背景音乐开", values = new string[] { "背景音乐开", "背景音乐关" } });
			//game_str_setting.Add("waybill_font", new SettingStrItem() { desc = "露单字体", value = "大字体露单", values = new string[] { "大字体露单", "小字体露单" } });
			//game_str_setting.Add("break_machine", new SettingStrItem() { desc = "是否爆机", value = "爆机无", values = new string[] { "爆机无", "爆机有" } });
			//game_str_setting.Add("open_3_sec", new SettingStrItem() { desc = "3秒功能开", value = "3秒功能开", values = new string[] { "3秒功能开", "3秒功能关" } });
			game_setting.Add("open_3_sec", new SettingItem()
			{
				SelectedIndex = 0,
				Type = SettingItemType.strings,
				Values = new string[] { "3秒功能开", "3秒功能关" }
			});
			//game_str_setting.Add("print_waybill", new SettingStrItem() { desc = "打印露单", value = "打印露单", values = new string[] { "打印露单" } });
			//game_str_setting.Add("single_double", new SettingStrItem() { desc = "单张牌", value = "单张牌", values = new string[] { "单张牌", "两张牌" } });
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
