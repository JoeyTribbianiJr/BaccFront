using Bacc_front;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace WsUtils.SqliteEFUtils
{
    /// <summary>
    /// 后台实时盈利
    /// </summary>
    public class BackRecord
    {
        public int SessionIndex { get; set; }
        public int RoundIndex { get; set; }
        public int DeskBanker { get; set; }
        public int DeskPlayer { get; set; }
        public int DeskTie { get; set; }
        public int Differ { get; set; }
        public int Profit { get; set; }
        public int MostPlayer { get; set; }
        public int Countdown { get; set; }
        public string JsonSession { get; set; }
        public int State { get; set; }
        public int Winner { get; set; }
        public int DeskCurScore { get; set; }
        public string JsonPlayerScores { get; set; }
    }
    
    public class BetScoreRecord
    {
        [Key]
        public int Id { get; set; }
        public int SessionIndex { get; set; }
        public int RoundIndex { get; set; }
        public int DeskBanker { get; set; }
        public int DeskPlayer { get; set; }
        public int DeskTie { get; set; }
        public int Countdown { get; set; }
        public string JsonSession{ get; set; }
        public int State { get; set; }
        public int Winner { get; set; }
        public string JsonPlayerScores { get; set; }
    }
    /// <summary>
    /// 前台账目
    /// </summary>
    public class FrontAccount
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; }
        public bool IsClear { get; set; }
        public DateTime ClearTime { get; set; }
        public string JsonPlayerAccount { get; set; }
    }
    public class GameSetting
    {
        [Key]
        public int Id { get; set; }
        public string JsonGameSettings { get; set; }
        public int CurrentSessionIndex { get; set; }
        public string JsonPlayerScores { get; set; }
        public bool IsGameInit { get; set; }
    }
}
