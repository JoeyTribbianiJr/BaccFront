using Bacc_front;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace WsUtils.SqliteEFUtils
{
    /// <summary>
    /// 只保存后台发送过来的
    /// </summary>
    public class LocalSession
    {
        [Key]
        public int Id { get; set; }
        public string JsonSession { get; set; }
    }
    /// <summary>
    /// 押分记录,也是路单记录
    /// </summary>
    public class BetScoreRecord
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; }
        public int SessionIndex { get; set; }
        public int RoundIndex { get; set; }
        public int DeskBanker { get; set; }
        public int DeskPlayer { get; set; }
        public int DeskTie { get; set; }
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
        public string JsonScoreRecord{ get; set; }
    }
}
