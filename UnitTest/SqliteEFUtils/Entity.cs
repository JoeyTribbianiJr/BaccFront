using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace WsUtils.SqliteEFUtils
{
    /// <summary>
    /// 押分记录,也是路单记录
    /// </summary>
    [Table("BetScoreRecords")]
    public class BetScoreRecord
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateTime { get; set; }
        public int SessionIndex { get; set; }
        //public int RoundIndex { get; set; }
        //public int Winner { get; set; }
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
        public string JsonScoreRecord { get; set; }
    }
}
