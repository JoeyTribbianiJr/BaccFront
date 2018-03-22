
using System.Collections;
using System.ComponentModel;

namespace Bacc_front
{
    public enum BetSide
    {
        [Description("庄")]
        banker = 0,
        [Description("和")]
        tie = 1,
        [Description("闲")]
        player =2
    }
    /// <summary>
    /// 每次押注面额
    /// </summary>
    public enum BetDenomination
    {
        big = 0,
        mini = 1
    }
    [PropertyChanged.ImplementPropertyChanged]
    public class WhoWin
    {
        public int Winner { get; set; }
        public WhoWin()
        {
            Winner = -1;
        }
    }
    public enum WinnerEnum
    {
        none = -1,
        banker = 0,
        tie = 1,
        player =2
    }
    public enum Suits
	{
		Spade,  //黑桃
		Heart,  //红心
		Club,   //梅花
		Diamond,    //方块
	}
	public enum Weight
	{
		One = 1,
		Two,
		Three,
		Four,
		Five,
		Six,
		Seven,
		Eight,
		Nine,
		Ten ,
		Jack ,
		Queen,
		King ,
	}
    public enum RemoteCommand
    {
        Image = 1,

        ImportFront,

        ReplaceWaybill,
        ReplaceWaybillOK,
        ReplaceWaybillFail,

        ImportBack,
        ImportBackOK,
        ImportBackFail,

        SendFrontPassword,
        SendFrontSetting,
        SendFrontSummationBetRecord,
        SendFrontBetRecord,
        SendFrontLiveData,
        SendFrontWaybill,
        SendFrontCurSession,
    }

    public enum GameState
    {
        Preparing =-1,
        Shuffling =0,
        Betting = 1,
        Dealing = 2,
        DealOver = 3
    }
    public class ParamToServer
    {
        public int SessionIndex { get; set; }
        public int RoundIndex { get; set; }
        public int Countdown { get; set; }
        //public int[] Cards { get; set; }
        public int Winner { get; set; }
        public int State { get; set; }
        //public ArrayList Waybill { get; set; }
    }
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
        integer,
        strings,
    }
    /// <summary>
    /// 后台累积押分记录
    /// </summary>
    public class BackBetRecord
    {
        public int PlayerId { get; set; }
        public int BetScore { get; set; }
        public int Profit { get; set; }
        public int DingFen { get; set; }
        public int ZhongFen { get; set; }
    }
    /// 后台实时盈利
    /// </summary>
    public class BackLiveData
    {
        public int SessionIndex { get; set; }
        public int RoundIndex { get; set; }
        public int DeskBanker { get; set; }
        public int DeskPlayer { get; set; }
        public int DeskTie { get; set; }
        public int Countdown { get; set; }
        public int State { get; set; }
        public int Winner { get; set; }
        public string JsonPlayerScores { get; set; }

        public int Differ { get; set; }
        public int Profit { get; set; }
        public int MostPlayer { get; set; }
        public int DeskCurScore { get; set; }
    }

    public class AddSubScoreRecord
    {
        public string PlayerId { get; set; }
        public int TotalAddScore { get; set; }
        public int TotalSubScore { get; set; }
        public int TotalAccount { get; set; }
    }
}