using Bacc_front.Properties;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WsUtils;
using WsUtils.SqliteEFUtils;

namespace Bacc_front
{
    public class GameDataManager
    {
        public ObservableCollection<Session> LocalSessions = new ObservableCollection<Session>();
        private object myobj = new object();
        public ObservableCollection<BackBetRecord> BetRecordSummationDataToBack { get; set; }
        internal void ImportBackNextSession(StringRequestInfo requestInfo, AppSession session)
        {
            var data = requestInfo.Parameters[0];
            var back_session = JsonConvert.DeserializeObject<Session>(data) ?? throw new NullReferenceException();
            back_session.SessionId = Game.Instance.SessionIndex + 1;

            var next = Game.Instance.LocalSessions.FirstOrDefault(s => s.SessionId == Game.Instance.SessionIndex + 1);
            if (next == null)
            {
                Game.Instance.LocalSessions.Add(back_session);
            }
            else
            {
                next = back_session;
            }
            using (var db = new SQLiteDB())
            {
                db.LocalSessions.RemoveRange(db.LocalSessions);
                foreach (var s in Game.Instance.LocalSessions)
                {
                    db.LocalSessions.Add(new LocalSession
                    {
                        JsonSession = JsonConvert.SerializeObject(s)
                    });
                }
                db.SaveChanges();
            }
        }
        public void ImportBack(StringRequestInfo requestInfo, AppSession app_session)
        {
            var data = requestInfo.Parameters[0];
            var back_sessions = JsonConvert.DeserializeObject<ObservableCollection<Session>>(data);
            Game.Instance.LocalSessions = back_sessions ?? throw new NullReferenceException();
            try
            {
                using (var db = new SQLiteDB())
                {
                    db.LocalSessions.RemoveRange(db.LocalSessions);
                    foreach (var s in back_sessions)
                    {
                        db.LocalSessions.Add(new LocalSession
                        {
                            JsonSession = JsonConvert.SerializeObject(s)
                        });
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
            }
        }
        public Session ReplaceWaybill(StringRequestInfo requestInfo, AppSession app_session)
        {
            var data = requestInfo.Parameters[0];
            var session = JsonConvert.DeserializeObject<Session>(data);
            var local = Game.Instance.CurrentSession;
            for (int i = Game.Instance.RoundIndex; i < local.RoundNumber; i++)
            {
                local.RoundsOfSession[i] = session.RoundsOfSession[i];
            }
            return local;
        }
        public void SaveAddScoreAccount(int Add_score, int id)
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    WaitingBox.Show(ControlBoard.Instance, () =>
                    {
                        var acc = db.FrontAccounts.FirstOrDefault(a => a.IsClear == false);
                        ObservableCollection<AddSubScoreRecord> record = new ObservableCollection<AddSubScoreRecord>();
                        if (acc == null)
                        {
                            acc = new FrontAccount()
                            {
                                CreateTime = DateTime.Now,
                                IsClear = false,
                                JsonScoreRecord = ""
                            };
                            record = Desk.Instance.CreateNewScoreRecord();
                            acc.JsonScoreRecord = JsonConvert.SerializeObject(record);
                            db.FrontAccounts.Add(acc);
                        }
                        record = JsonConvert.DeserializeObject<ObservableCollection<AddSubScoreRecord>>(acc.JsonScoreRecord);
                        var p_record = record.First(a => Convert.ToInt32(a.PlayerId) == id);
                        p_record.TotalAddScore += Add_score;
                        p_record.TotalAccount += Add_score;

                        acc.JsonScoreRecord = JsonConvert.SerializeObject(record);

                        db.SaveChanges();

                    }, "数据存储中...");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
                MessageBox.Show("数据库出错");
            }
        }
        public void SaveSubScoreAccount(int Sub_score, int id)
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    WaitingBox.Show(ControlBoard.Instance, () =>
                    {
                        var acc = db.FrontAccounts.First(a => a.IsClear == false);
                        ObservableCollection<AddSubScoreRecord> record = new ObservableCollection<AddSubScoreRecord>();
                        if (acc == null)
                        {
                            acc = new FrontAccount()
                            {
                                CreateTime = DateTime.Now,
                                IsClear = false,
                                JsonScoreRecord = ""
                            };
                            record = Desk.Instance.CreateNewScoreRecord();
                            acc.JsonScoreRecord = JsonConvert.SerializeObject(record);
                            db.FrontAccounts.Add(acc);
                        }
                        record = JsonConvert.DeserializeObject<ObservableCollection<AddSubScoreRecord>>(acc.JsonScoreRecord);
                        var p_record = record.First(a => Convert.ToInt32(a.PlayerId) == id);
                        p_record.TotalSubScore += Sub_score;
                        p_record.TotalAccount -= Sub_score;

                        acc.JsonScoreRecord = JsonConvert.SerializeObject(record);

                        db.SaveChanges();
                    }, "数据存储中...");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
                MessageBox.Show("数据库出错");
            }
        }
        public void SetBackLiveData()
        {
            Game.Instance.BetBackLiveData = new BackLiveData()
            {
                SessionIndex = Game.Instance.SessionIndex,
                RoundIndex = Game.Instance.RoundIndex,
                DeskBanker = Desk.Instance.desk_amount[BetSide.banker],
                DeskPlayer = Desk.Instance.desk_amount[BetSide.player],
                DeskTie = Desk.Instance.desk_amount[BetSide.tie],
                Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
                Countdown = Game.Instance.CountDown,
                State = (int)Game.Instance.CurrentState,
                JsonPlayerScores = JsonConvert.SerializeObject(Desk.Instance.Players)
            };
        }
        public void InitBetRecordDataToBack()
        {
            BetRecordSummationDataToBack = new ObservableCollection<BackBetRecord>();
            foreach (var p in Desk.Instance.Players)
            {
                BetRecordSummationDataToBack.Add(new BackBetRecord()
                {
                    PlayerId = p.Id,
                    BetScore = 0,
                    DingFen = 0,
                    Profit = 0,
                    ZhongFen = 0
                });
            }
        }

        public void SetSummationBackBetRecordData()
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    DateTime time;
                    var last_acc = db.FrontAccounts.FirstOrDefault(a => a.IsClear == false);
                    time = last_acc != null ? last_acc.CreateTime : DateTime.MinValue;
                    var records = db.BetScoreRecords.Where(r => r.CreateTime >= time).ToList();

                    InitBetRecordDataToBack();

                    if (records != null)
                    {
                        var players = records.Where(rc => !string.IsNullOrEmpty( rc.JsonPlayerScores )).Select(r => JsonConvert.DeserializeObject<ObservableCollection<Player>>(r.JsonPlayerScores));
                        foreach (var bet_data in BetRecordSummationDataToBack)
                        {
                            foreach (var record in records)
                            {
                                if (string.IsNullOrEmpty(record.JsonPlayerScores))
                                {
                                    continue;
                                }
                                var ps = JsonConvert.DeserializeObject<ObservableCollection<Player>>(record.JsonPlayerScores);
                                var player = ps.FirstOrDefault(p => p.Id == bet_data.PlayerId);

                                var profit = Desk.GetProfit(record.Winner, player.BetScoreOnBank, player.BetScoreOnPlayer, player.BetScoreOnTie);
                                bet_data.Profit += profit;
                            }
                            bet_data.BetScore = players.Sum(lst => lst.Where(ps => ps.Id == bet_data.PlayerId).Sum(p => p.BetScore.Sum(s => s.Value)));
                            //顶分中分 todo
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message + ex.StackTrace);
#endif
            }
        }
        public List<int> GetBetRecordIds()
        {
            using (var db = new SQLiteDB())
            {
                var list = db.BetScoreRecords.Select(b => b.SessionIndex).Distinct().ToList();
                return list ?? new List<int>();
            }
        }
        public void SetBackBetRecordData(int id)
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    var records = db.BetScoreRecords.Where(
                        bs => bs.SessionIndex == id).ToList();
                    if (records != null)
                    {
                        Game.Instance.BetRecordJsonDataToBack = new ObservableCollection<BetScoreRecord>(records);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message + ex.StackTrace);
#endif
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
        public void SaveNewSession(Session session)
        {
            if (session == null)
            {
                return;
            }
            using (var db = new SQLiteDB())
            {
                foreach (var round in session.RoundsOfSession)
                {
                    var record = new BetScoreRecord()
                    {
                        CreateTime = DateTime.Now,
                        SessionIndex = session.SessionId,
                        RoundIndex = session.RoundsOfSession.IndexOf(round),
                        DeskBanker = 0,
                        DeskPlayer = 0,
                        DeskTie = 0,
                        Winner = (int)round.Winner.Item1,
                        JsonPlayerScores = ""
                    };
                    var history = db.BetScoreRecords.FirstOrDefault(b => b.SessionIndex == record.SessionIndex && b.RoundIndex == record.RoundIndex);
                    if (history != null)
                    {
                        db.BetScoreRecords.Remove(history);
                    }
                    db.BetScoreRecords.Add(record);
                }
                db.SaveChanges();
            }
        }
        public void SaveBetRecords()
        {
            try
            {
                SetBackLiveData();
                using (var db = new SQLiteDB())
                {
                    var live_data = Game.Instance.BetBackLiveData;

                    var record = new BetScoreRecord()
                    {
                        CreateTime = DateTime.Now,
                        SessionIndex = live_data.SessionIndex,
                        RoundIndex = live_data.RoundIndex,
                        DeskBanker = live_data.DeskBanker,
                        DeskPlayer = live_data.DeskPlayer,
                        DeskTie = live_data.DeskTie,
                        Winner = live_data.Winner,
                        JsonPlayerScores = live_data.JsonPlayerScores
                    };
                    var history = db.BetScoreRecords.FirstOrDefault(b => b.SessionIndex == record.SessionIndex && b.RoundIndex == record.RoundIndex);
                    if (history != null)
                    {
                        db.BetScoreRecords.Remove(history);
                    }
                    db.BetScoreRecords.Add(record);
                    //db.BetScoreRecords.Add(record);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message + ex.StackTrace);
#endif
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
        public void InitLocalSessions()
        {
            try
            {
                WaitingBox.Show(() =>
               {
                   using (var db = new SQLiteDB())
                   {
                       var sessions = db.LocalSessions;
                       if (sessions != null && sessions.Count() != 0)
                       {
                           LocalSessions.Clear();
                           foreach (var sess in sessions)
                           {
                               var mod = JsonConvert.DeserializeObject<Session>(sess.JsonSession);
                               LocalSessions.Add(mod);
                           }
                       }
                   }
               }, "初始化数据库...");
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message + ex.StackTrace);
#endif
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
    }
}
