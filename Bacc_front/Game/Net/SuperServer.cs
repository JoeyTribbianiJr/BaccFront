using Bacc_front.Properties;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows;
using WsUtils;

namespace Bacc_front
{
    public class SuperServer : AppServer
    {
        public static AppSession appSession;
        private ImageUtils imageSender;
        private HttpClient httpClient;
        private const int port = 54322;
        public bool Login = false;
        public SuperServer()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 512;
            imageSender = new ImageUtils();
            httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            httpClient.Timeout = TimeSpan.FromSeconds(0.5);
            appSession = new AppSession();
        }
        public void StartServer()
        {
            var config = new ServerConfig()
            {
                Port = port,
                TextEncoding = "UTF-8",
                MaxRequestLength = 1024 *1024,
                //ReceiveBufferSize = 2048 * 2048 * 4,
                ReceiveBufferSize = 1024 * 1024,
                //SendBufferSize = 2048 * 2048 * 4,
                SendBufferSize = 1024 *  1024,
            };
            if (!Setup(config))
            {
                Trace.WriteLine("端口设置失败");
                MessageBox.Show("端口设置失败");
                return;
            }
            //连接时
            NewSessionConnected += appServer_NewSessionConnected;
            //接收信息时
            NewRequestReceived += appServer_NewRequestReceived;
            //关闭服务时
            SessionClosed += appServer_SessionClosed;
            Accept();

        }
        private void Accept()
        {
            if (!Start())
            {
                Trace.WriteLine("启动服务失败");
                MessageBox.Show("启动服务失败");
                return;
            }
            Trace.WriteLine("服务启动成功");

            //while (true) ;
        }

        private void appServer_NewSessionConnected(AppSession session)
        {
            if (appSession.Connected)
            {
                appSession.Close();
            }
            appSession = session;
        }
        void appServer_SessionClosed(AppSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Login = false;
            Game.Instance.CoreTimer.StopWebTimer();
            session.Send("服务已关闭");
        }
        void appServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                RemoteCommand type = (RemoteCommand)Enum.Parse(typeof(RemoteCommand), requestInfo.Key);
                LogHelper.WriteLog(typeof(RemoteCommand), "收到消息:" + type.ToString() + requestInfo.Key);
                switch (type)
                {
                    case RemoteCommand.Login:
                        CheckLogin(requestInfo);
                        break;
                    case RemoteCommand.SendFrontCurSession:
                        SendCurSessionToBack();
                        break;
                    case RemoteCommand.ImportFrontLocalSessions:
                        SendCompressedCommand(RemoteCommand.ImportFrontLocalSessions, Game.Instance.LocalSessions, session);
                        break;
                    case RemoteCommand.ImportBack:
                        OnImportBack(session, requestInfo);
                        break;
                    case RemoteCommand.ImportBackNextSession:
                        OnImportBackNextSession(session, requestInfo);
                        break;
                    case RemoteCommand.SendFrontBetRecordIdList:
                        SendBetRecordIdsToBack();
                        break;
                    case RemoteCommand.SendFrontBetRecord:
                        SendBetRecordToBack(session, requestInfo);
                        break;
                    case RemoteCommand.SendFrontAccount:
                        SendAccountToBack(session, requestInfo);
                        break;
                    case RemoteCommand.ClearFrontAccount:
                        Game.Instance.Manager.ClearFrontAccountByBack();
                        break;
                    case RemoteCommand.ClearFrontLocalSessions:
                        Game.Instance.Manager.ClearLocalSessions();
                        break;
                    case RemoteCommand.SetWinner:
                        SetWinner(session, requestInfo);
                        break;
                    case RemoteCommand.KillBig:
                        KillBig(session, requestInfo);
                        break;
                    case RemoteCommand.ExtraWaybill:
                        ExtraWaybill();
                        break;
                    case RemoteCommand.ShutdownFront:
                        ControlBoard.Instance.ShutdownComputer();
                        break;
                    case RemoteCommand.BreakdownFront:
                        ControlBoard.Instance.BreakdownGame();
                        break;
                    case RemoteCommand.LockFront:
                        Game.Instance.SetGameLock();
                        if (appSession.Connected && Login)
                        {
                            SendData(RemoteCommand.LockFrontOK, "", appSession);
                        }
                        break;
                    case RemoteCommand.UnlockFront:
                        Game.Instance.SetGameUnlock();
                        if (appSession.Connected && Login)
                        {
                            SendData(RemoteCommand.UnlockFrontOK, "", appSession);
                        }
                        break;
                    case RemoteCommand.ModifyFrontPassword:
                        ModifyFrontPassword(session, requestInfo);
                        break;
                    case RemoteCommand.ModifyFrontSetting:
                        ModifyFrontSetting(session, requestInfo);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(Object), "UnobservedTaskException:" + ex.Message + ex.StackTrace);
                //throw;
            }
        }


        private void ModifyFrontSetting(AppSession session, StringRequestInfo requestInfo)
        {
            var data = requestInfo.Parameters[0];
            var setting = JsonConvert.DeserializeObject<Dictionary<string, SettingItem>>(data);
            Setting.Instance.game_setting = setting;
            ControlBoard.Instance.SaveSetting(Game.Instance.SessionIndex);
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.ModifyFrontSettingOK, "", appSession);
                Thread.Sleep(100);
                SendFrontSettingToBack();
            }
        }
        private void ModifyFrontPassword(AppSession session, StringRequestInfo requestInfo)
        {
            var data = requestInfo.Parameters[0];
            if (string.IsNullOrEmpty(data))
            {
                return;
            }
            var dicpwd = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
            if (dicpwd == null)
            {
                return;
            }
            Setting.Instance.SavePassword(dicpwd);
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.ModifyFrontPasswordOK, "", appSession);
                Thread.Sleep(100);
                SendFrontPasswordToBack();
            }
        }

        private void OnImportBackNextSession(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                Game.Instance.Manager.ImportBackNextSession(requestInfo, session);
                if (Game.Instance.CurrentState == GameState.Preparing || Game.Instance.CurrentState == GameState.Shuffling)
                {
                    SendData(RemoteCommand.ImportBackNextSessionOnCurrentSession, Game.Instance.CurrentSession, session);
                }
                else
                {
                    SendData(RemoteCommand.ImportBackNextSessionOnNextSession, Game.Instance.CurrentSession, session);
                }
                Thread.Sleep(100);
                SendCurSessionToBack();
            }
            catch (Exception ex)
            {
                SendData(RemoteCommand.ImportBackFail, ex.Message, session);
            }
        }
        public void OnImportBack(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                Game.Instance.Manager.ImportBack(requestInfo, session);
            }
            catch (Exception ex)
            {
                SendData(RemoteCommand.ImportBackFail, ex.Message, session);
            }
        }

        private void CheckLogin(StringRequestInfo requestInfo)
        {
            var data = requestInfo.Parameters[0];
            var pass = JsonConvert.DeserializeObject<string>(data);
            if (Setting.AdministratorPwd != pass && Setting.Instance.PasswordMap["conn_front_pwd"] != pass )
            {
                SendData(RemoteCommand.Login, "False", appSession);
            }
            else
            {
                Login = true;
                SendData(RemoteCommand.Login, "OK", appSession);
                Thread.Sleep(100);
                SendFrontPasswordToBack();
                Thread.Sleep(300);
                SendFrontSettingToBack();
                Thread.Sleep(300);
                SendLiveDataToBack();
                Thread.Sleep(300);
                var s1 = DateTime.Now;
                SendCurSessionToBack();
                var span1 = (DateTime.Now - s1).TotalMilliseconds;
                Thread.Sleep(500);
                SendIsGameLockToBack();
                Thread.Sleep(100);
                SendSummationBetRecordToBack();
                Thread.Sleep(800);
                Game.Instance.CoreTimer.StartWebTimer();
            }
        }
        private void SendIsGameLockToBack()
        {
            if (Game.Instance.IsGameLocked)
            {
                SendData(RemoteCommand.LockFrontOK, "", appSession);
            }
            else
            {
                SendData(RemoteCommand.UnlockFrontOK, "", appSession);
            }
        }
        public void SendFrontPasswordToBack()
        {
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.SendFrontPassword, Setting.Instance.PasswordMap, appSession);
            }
        }
        public void SendFrontSettingToBack()
        {
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.SendFrontSetting, Setting.Instance.game_setting, appSession);
            }
        }
        public void SendLiveDataToBack()
        {
            if (appSession.Connected && Login)
            {
                Game.Instance.Manager.SetBackLiveData();
                SendData(RemoteCommand.SendFrontLiveData, Game.Instance.BetBackLiveData, appSession);
            }
        }
        public void SendSummationBetRecordToBack()
        {
            if (appSession.Connected && Login)
            {
                //var s = DateTime.Now;
                Game.Instance.Manager.SetSummationBackBetRecordData();
                SendData(RemoteCommand.SendFrontSummationBetRecord, Game.Instance.Manager.BetRecordList, appSession);
                //var span = (DateTime.Now - s).TotalMilliseconds;
                //LogHelper.WriteLog(typeof(Object), "结束SendSummationBetRecord:" + span.ToString());
            }
        }

        private void SendBetRecordIdsToBack()
        {
            if (appSession.Connected && Login)
            {
                var list = Game.Instance.Manager.GetBetRecordIds();
                SendData(RemoteCommand.SendFrontBetRecordIdList, list, appSession);
            }
        }
        public void SendBetRecordToBack(AppSession session, StringRequestInfo requestInfo)
        {
            var data = requestInfo.Parameters[0];
            var id = JsonConvert.DeserializeObject<int>(data);
            if (appSession.Connected && Login)
            {
                Game.Instance.Manager.SetBackBetRecordData(id);
                SendData(RemoteCommand.SendFrontBetRecord, Game.Instance.BetRecordJsonDataToBack, appSession);
            }
        }
        public void SendAccountToBack(AppSession session, StringRequestInfo requestInfo)
        {
            var data = requestInfo.Parameters[0];
            var skip_count = JsonConvert.DeserializeObject<int>(data);
            if (appSession.Connected && Login)
            {
                var table = Game.Instance.Manager.SelectAccount(skip_count);
                SendData(RemoteCommand.SendFrontAccount, table, appSession);
            }
        }
        public void SendCurSessionToBack()
        {
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.SendFrontCurSession, Game.Instance.CurrentSession, appSession);
            }
        }

        public void SetWinner(AppSession session, StringRequestInfo requestInfo)
        {
            if (Game.Instance.CurrentState == GameState.Dealing)
            {
                return;
            }
            try
            {
                var data = requestInfo.Parameters[0];
                var winner = JsonConvert.DeserializeObject<BetSide>(data);

                var round = Session.CreateRoundByWinner(winner);
                var roundidx = Game.Instance.RoundIndex == -1 ? 0 : Game.Instance.RoundIndex;
                Game.Instance.CurrentSession.RoundsOfSession[roundidx] = round;
            }
            catch (Exception ex)
            {
            }
        }
        public void KillBig(AppSession session, StringRequestInfo requestInfo)
        {
            if (Game.Instance.CurrentState == GameState.Betting && !Game.Instance._isIn3)
            {
                Game.Instance._isKillBig = !Game.Instance._isKillBig;
            }
            else
            {
                return;
            }
        }
        private void ExtraWaybill()
        {
            Game.Instance.GamePrinter.PrintWaybill();
        }
        public void SendData(RemoteCommand command, object obj, AppSession session)
        {
            LogHelper.WriteLog(typeof(RemoteCommand), "发送消息:" + command.ToString());
            var type = ((int)command).ToString().PadLeft(2, '0');
            byte[] typeByte = Encoding.UTF8.GetBytes(type);

            var str = JsonConvert.SerializeObject(obj) + "\r\n";
            byte[] byteBuffer = Encoding.UTF8.GetBytes(str);

            int len = byteBuffer.Length;
            byte[] length = BitConverter.GetBytes(len);

            var data = typeByte.Concat(length).Concat(byteBuffer).ToArray();

            if (type.Length != 2)
            {
                return;
            }
            try
            {
                session.Send(data, 0, data.Length);
            }
            catch (Exception)
            {

            }
        }
        public void SendCompressedCommand(RemoteCommand command, object obj, AppSession session)
        {
            LogHelper.WriteLog(typeof(RemoteCommand), "发送消息:" + command.ToString());
            var type = ((int)command).ToString().PadLeft(2, '0');
            byte[] typeByte = Encoding.UTF8.GetBytes(type);

            var str = JsonConvert.SerializeObject(obj);
            var comp_str= CompressHelper. Compress(str);
            byte[] byteBuffer = Encoding.UTF8.GetBytes(comp_str);

            int len = byteBuffer.Length;
            byte[] length = BitConverter.GetBytes(len);

            var data = typeByte.Concat(length).Concat(byteBuffer).ToArray();

            if (type.Length != 2)
            {
                return;
            }
            try
            {
                session.Send(data, 0, data.Length);
            }
            catch (Exception)
            {

            }
        }
        void SendByteData(RemoteCommand command, byte[] byteBuffer, AppSession session)
        {
            var type = ((int)command).ToString().PadLeft(2, '0');
            byte[] typeByte = Encoding.UTF8.GetBytes(type);

            int len = byteBuffer.Length;
            byte[] length = BitConverter.GetBytes(len);

            //var str = JsonConvert.SerializeObject(obj);
            //byte[] byteBuffer = Encoding.UTF8.GetBytes(str);

            var data = typeByte.Concat(length).Concat(byteBuffer).ToArray();

            if (type.Length != 2)
            {
                throw new Exception();
            }
            session.Send(data, 0, data.Length);
        }
        public void SendToHttpServer()
        {
            if (Game.Instance._isSendingToServer)
            {
                if (Game.Instance.PriorCountDown != Game.Instance.CountDown)
                {
                    Game.Instance.PriorCountDown = Game.Instance.CountDown;

                    if (Game.Instance.CurrentState != Game.Instance.PriorState)
                    {
                        Game.Instance.PriorState = Game.Instance.CurrentState;
                    }

                    var msg = new ParamToServer()
                    {
                        RoundIndex = Game.Instance.RoundIndex,
                        SessionIndex = Game.Instance.SessionIndex,
                        Countdown = Game.Instance.CountDown,
                        State = (int)Game.Instance.CurrentState,
                        Winner = -1
                    };
                    //LogHelper.WriteLog(typeof(Object), "发送普通倒计时：" + Game.Instance.CountDown + "  时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));

                    var ip = Setting.Instance.ServerUrl;
                    //var url = "http://" + ip + ":98/getmsg";
                    var url = "http://" + ip + ":" + Settings.Default.port + "/" +Settings.Default.url;

                    string Cards = "";
                    if (Game.Instance.CurrentState == GameState.Dealing)
                    {
                        msg.Winner = (int)Game.Instance.CurrentRound.Winner.Item1;
                        Cards = JsonConvert.SerializeObject(Game.Instance.CardsForDeal);
                    }
                    var str = JsonConvert.SerializeObject(msg);
                    var waybill = JsonConvert.SerializeObject(Game.Instance.HistoryWaybill);
                    url += ("?" + "msg=" + str + "&waybill=" + waybill + "&cards=" + Cards + "&room=" + Settings.Default.Room);

                    try
                    {
                        httpClient.GetAsync(url);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        /// <summary>
        /// 在押分的前一秒发送
        /// </summary>
        public void SendBetStartToHttpServer()
        {
            //如果本场是最后一场
            if (Game.Instance.RoundIndex + 1 == Setting.Instance._round_num_per_session)
            {
                return;
            }
            var msg = new ParamToServer()
            {
                RoundIndex = Game.Instance.RoundIndex + 1, //这里是下一场的场数
                SessionIndex = Game.Instance.SessionIndex,
                Countdown = Setting.Instance._betTime + 1, //线上接收到后要减一
                State = (int)GameState.Betting,
                Winner = -1
            };

            var ip = Setting.Instance.ServerUrl;
            //var url = "http://" + ip + ":98/getmsg";
                    var url = "http://" + ip + ":" + Settings.Default.port + "/" +Settings.Default.url;

            string Cards = "";
            var str = JsonConvert.SerializeObject(msg);
            var waybill = JsonConvert.SerializeObject(Game.Instance.HistoryWaybill);
            url += ("?" + "msg=" + str + "&waybill=" + waybill + "&cards=" + Cards + "&room=" + Settings.Default.Room);

            try
            {
                httpClient.GetAsync(url);
                //LogHelper.WriteLog(typeof(Object), "发送提前倒计时：" + (Setting.Instance._betTime + 1) + "  时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
            }
            catch (Exception)
            {
            }
        }
        public void SendDealStartToHttpServer()
        {
            var msg = new ParamToServer()
            {
                RoundIndex = Game.Instance.RoundIndex,
                SessionIndex = Game.Instance.SessionIndex,
                Countdown = 0,
                State = (int)GameState.Dealing,
                Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
            };

            var ip = Setting.Instance.ServerUrl;
            //var url = "http://" + ip + ":98/getmsg";
            var url = "http://" + ip + ":" + Settings.Default.port + "/" +Settings.Default.url;
            var str = JsonConvert.SerializeObject(msg);

            string Cards = "";
            var waybill = JsonConvert.SerializeObject(Game.Instance.HistoryWaybill);
            Cards = JsonConvert.SerializeObject(Game.Instance.CardsForDeal);
            url += ("?" + "msg=" + str + "&waybill=" + waybill + "&cards=" + Cards + "&room=" + Settings.Default.Room);

            try
            {
                httpClient.GetAsync(url);
            }
            catch (Exception)
            {
            }
        }
        public void SendDealEndToHttpServer()
        {
            var msg = new ParamToServer()
            {
                RoundIndex = Game.Instance.RoundIndex,
                SessionIndex = Game.Instance.SessionIndex,
                Countdown = Game.Instance.CountDown,
                State = (int)GameState.DealOver,
                Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
            };

            var ip = Setting.Instance.ServerUrl;
            //var url = "http://" + ip + ":98/getmsg";
                    var url = "http://" + ip + ":" + Settings.Default.port + "/" +Settings.Default.url;
            var str = JsonConvert.SerializeObject(msg);

            var waybill = JsonConvert.SerializeObject(Game.Instance.HistoryWaybill);
            url += ("?" + "msg=" + str + "&waybill=" + waybill + "&room=" + Settings.Default.Room);

            try
            {
                //httpClient.Timeout = TimeSpan.FromSeconds(5);
                //httpClient.SendAsync()
                httpClient.GetAsync(url).ContinueWith((task) =>
                {
                    HttpResponseMessage rslt = task.Result;
                    if (rslt.IsSuccessStatusCode)
                    {
                        rslt.Content.ReadAsStringAsync().ContinueWith((rTask) =>
                        {
                            if (rTask.Result == "baoji")
                            {
                                Game.Instance._isServerBoom = true;
                            }
                        });
                    }
                });
                //httpClient.Timeout = TimeSpan.FromSeconds(0.5);
            }
            catch (Exception)
            {
            }
        }

        public void SendBoomToHttpServer()
        {
            var msg = new ParamToServer()
            {
                RoundIndex = Game.Instance.RoundIndex,
                SessionIndex = Game.Instance.SessionIndex,
                Countdown = Game.Instance.CountDown,
                State = (int)GameState.Booming,
                Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
            };

            var ip = Setting.Instance.ServerUrl;
            var url = "http://" + ip + ":98/getmsg";
            var str = JsonConvert.SerializeObject(msg);

            ArrayList arr = new ArrayList();
            for (int i = 0; i < Game.Instance.CurrentSession.RoundNumber; i++)
            {
                arr.Add(Game.Instance.CurrentSession.RoundsOfSession[i].Winner.Item1);
            }
            var waybill = JsonConvert.SerializeObject(arr);
            url += ("?" + "msg=" + str + "&waybill=" + waybill + "&room=" + Settings.Default.Room);

            try
            {
                httpClient.GetAsync(url).ContinueWith((task) =>
                {
                    HttpResponseMessage rslt = task.Result;
                    if (rslt.IsSuccessStatusCode)
                    {
                        rslt.Content.ReadAsStringAsync().ContinueWith((rTask) =>
                        {
                            if (rTask.Result == "baoji")
                            {
                                Game.Instance._isServerBoom = true;
                            }
                        });
                    }
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
