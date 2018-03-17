using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;

namespace WsUtils.SqliteEFUtils
{
    public class SQLiteDB : DbContext
    {
        //public DbSet<Person> Persons { get; set; }
        public DbSet<FrontAccount> FrontAccounts { get; set; }
        public DbSet<BetScoreRecord> BetScoreRecords { get; set; }
        public DbSet<GameSetting> GameSettings { get; set; }
        public SQLiteDB()
            : base("SqliteBacc")
        {

        }
    }
}
