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
        private object myobj = new object();
        public void ImportBack(StringRequestInfo requestInfo, AppSession app_session)
        {
            var data = requestInfo.Parameters[0];
            var back_sessions = JsonConvert.DeserializeObject<ObservableCollection<Session>>(data);
            Game.Instance.LocalSessions = back_sessions;
            Game.Instance.LocalSessionIndex = Game.Instance.SessionIndex;
            //if (back_sessions.Count >= Game.Instance.SessionIndex + 1)
            //{
            //    //for (int j = round_idx; j < cur_session.RoundNumber; j++)
            //    //{
            //    //    cur_session.RoundsOfSession[j] = back_sessions[sess_idx].RoundsOfSession[round_idx];
            //    //}
            //    for (int i = sess_idx + 1; i < local_sessions.Count(); i++)
            //    {
            //        local_sessions[i] = back_sessions[i];
            //    }
            //    for (int k = local_sessions.Count(); k < back_sessions.Count; k++)
            //    {
            //        local_sessions.Add(back_sessions[k]);
            //    }
            //}
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
        public void ResetWaybill()
        {
            var rounds = Game.Instance.CurrentSession.RoundsOfSession;
            if (Game.Instance.Waybill == null)
            {
                Game.Instance.Waybill = new ObservableCollection<WhoWin>();
                for (int i = 0; i < rounds.Count; i++)
                {
                    Game.Instance.Waybill.Add(new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    });
                }
            }
            else
            {
                for (int i = 0; i < rounds.Count; i++)
                {
                    Game.Instance.Waybill[i] = new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    };
                }
            }
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

                    }, "第一次嘛，要有耐心哦~");
                }

            }
            catch (Exception e)
            {
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
                    }, "第一次嘛，要有耐心哦~");
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("数据库出错");
            }
        }
        public void SetBackLiveData()
        {
            //lock (myobj)
            //{
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
            //}
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
                    if (records != null)
                    {
                        Game.Instance.BetRecordSummationJsonDataToBack = JsonConvert.SerializeObject(records);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
        public void SetBackBetRecordData()
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    var records = db.BetScoreRecords.Where(bs => bs.SessionIndex >= db.BetScoreRecords.Max(s => s.SessionIndex) - 30).ToList();
                    if (records != null)
                    {
                        Game.Instance.BetRecordJsonDataToBack = JsonConvert.SerializeObject(records);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
        public void SavePlayerScoresAndBetRecords()
        {
            try
            {
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
                    db.BetScoreRecords.Add(record);
                    Settings.Default.JsonPlayerScores = JsonConvert.SerializeObject(Desk.Instance.Players);
                    Settings.Default.Save();
                    db.SaveChanges();


                }
            }
            catch (Exception)
            {
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }
        public void TestDatabase()
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    WaitingBox.Show(MainWindow.Instance, () =>
                    {
                        var test = db.FrontAccounts.FirstOrDefault();
                        if (test == null)
                        {
                        }
                    }, "初始化数据库...");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("程序数据库故障，重启游戏或联系工程师");
            }
        }

    }
}
