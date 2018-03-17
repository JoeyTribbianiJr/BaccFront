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
using System.Windows.Threading;
using WsUtils;
using WsUtils.SqliteEFUtils;

namespace Bacc_front
{
    public class GameDataManager
    {
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
            if (Game.Instance. Waybill == null)
            {
                Game.Instance.Waybill = new ObservableCollection<WhoWin>();
                for (int i = 0; i < rounds.Count; i++)
                {
                    Game.Instance. Waybill.Add(new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    });
                }
            }
            else
            {
                for (int i = 0; i < rounds.Count; i++)
                {
                    Game.Instance. Waybill[i] = new WhoWin()
                    {
                        Winner = (int)WinnerEnum.none
                    };
                }
            }
        }

        public void SetBetRecord()
        {
            Game.Instance.BetRecord = new BetScoreRecord()
            {
                DeskBanker = Desk.Instance.desk_amount[BetSide.banker],
                DeskPlayer = Desk.Instance.desk_amount[BetSide.player],
                DeskTie = Desk.Instance.desk_amount[BetSide.tie],
                Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
                Countdown = Game.Instance.CountDown,
                State = (int)Game.Instance.CurrentState,
                //JsonSession = JsonConvert.SerializeObject(Game.Instance.CurrentSession),
                JsonSession = "",
               
                RoundIndex = Game.Instance.RoundIndex,
                SessionIndex = Game.Instance.SessionIndex,
                JsonPlayerScores = JsonConvert.SerializeObject(Desk.Instance.Players)
            };
        }
        
        public void SavePlayerScoresAndBetRecords()
        {
            try
            {
                using (var db = new SQLiteDB())
                {
                    db.BetScoreRecords.Add(Game.Instance.BetRecord);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        

    }
}
