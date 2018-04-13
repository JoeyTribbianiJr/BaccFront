using Bacc_front.Properties;
using Newtonsoft.Json;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                MaxRequestLength = 2048 * 2048,
                ReceiveBufferSize = 2048 * 2048,
                SendBufferSize = 2048 * 2048
            };
            if (!Setup(config))
            {
                Trace.WriteLine("端口设置失败");
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
                return;
            }
            Trace.WriteLine("服务启动成功");

            //while (true) ;
        }

        private void appServer_NewSessionConnected(AppSession session)
        {
            appSession = session;
        }

        void appServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            RemoteCommand type = (RemoteCommand)Enum.Parse(typeof(RemoteCommand), requestInfo.Key);
            switch (type)
            {
                case RemoteCommand.Login:
                    CheckLogin(requestInfo);
                    break;
                case RemoteCommand.SendFrontCurSession:
                    SendCurSessionToBack();
                    break;
                case RemoteCommand.ImportFrontLocalSessions:
                    SendData(RemoteCommand.ImportFrontLocalSessions, Game.Instance.LocalSessions, session);
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
                    SendBetRecordToBack(session,requestInfo);
                    break;
                default:
                    //session.Send("Unknow ");
                    break;
            }
        }


        private void OnImportBackNextSession(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                Game.Instance.Manager.ImportBackNextSession(requestInfo, session);
                SendData(RemoteCommand.ImportBackNextSessionOK, Game.Instance.CurrentSession, session);
            }
            catch (Exception ex)
            {
                SendData(RemoteCommand.ImportBackNextSessionFail, ex.Message, session);
            }
        }
        public void OnImportBack(AppSession session, StringRequestInfo requestInfo)
        {
            try
            {
                Game.Instance.Manager.ImportBack(requestInfo, session);
                SendData(RemoteCommand.ImportBackOK, Game.Instance.CurrentSession, session);
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
            if (Setting.connect_pass != pass)
            {
                SendData(RemoteCommand.Login, "False", appSession);
            }
            else
            {
                Login = true;
                SendData(RemoteCommand.Login, "OK", appSession);
                SendFrontPasswordToBack();
                SendFrontSettingToBack();
                SendCurSessionToBack();
                SendLiveDataToBack();
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
                Game.Instance.Manager.SetSummationBackBetRecordData();
                SendData(RemoteCommand.SendFrontSummationBetRecord, Game.Instance.Manager.BetRecordSummationDataToBack, appSession);
            }
        }

        private void SendBetRecordIdsToBack()
        {
            if (appSession.Connected && Login)
            {
                var list = Game.Instance.Manager.GetBetRecordIds();
                SendData(RemoteCommand.SendFrontBetRecordIdList, list,appSession);
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
        //public void SendWaybillToBack()
        //{
        //    if (appSession.Connected && Login)
        //    {
        //        SendData(RemoteCommand.SendFrontWaybill, Game.Instance.Waybill, appSession);
        //    }
        //}
        public void SendCurSessionToBack()
        {
            if (appSession.Connected && Login)
            {
                SendData(RemoteCommand.SendFrontCurSession, Game.Instance.CurrentSession, appSession);
            }
        }

        //public void OnReplaceWaybill(AppSession session, StringRequestInfo requestInfo)
        //{
        //    try
        //    {
        //        var local = Game.Instance.Manager.ReplaceWaybill(requestInfo, session);
        //        SendData(RemoteCommand.ReplaceWaybillOK, local.RoundsOfSession[Game.Instance.RoundIndex], session);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendData(RemoteCommand.ReplaceWaybillFail, ex.Message, session);
        //    }
        //}
        void appServer_SessionClosed(AppSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Login = false;
            session.Send("服务已关闭");
        }
        void SendData(RemoteCommand command, object obj, AppSession session)
        {
            var type = ((int)command).ToString().PadLeft(2, '0');
            byte[] typeByte = Encoding.UTF8.GetBytes(type);

            var str = JsonConvert.SerializeObject(obj);
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
        void SendData(RemoteCommand command, byte[] byteBuffer, AppSession session)
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

                    var ip = Setting.Instance.ServerUrl;
                    var url = "http://" + ip + ":98/getmsg";

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
                Countdown = Setting.Instance._betTime,
                State = (int)GameState.Betting,
                Winner = -1
            };

            var ip = Setting.Instance.ServerUrl;
            var url = "http://" + ip + ":98/getmsg";

            string Cards = "";
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
            var url = "http://" + ip + ":98/getmsg";
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
            var url = "http://" + ip + ":98/getmsg";
            var str = JsonConvert.SerializeObject(msg);

            var waybill = JsonConvert.SerializeObject(Game.Instance.HistoryWaybill);
            url += ("?" + "msg=" + str + "&waybill=" + waybill + "&room=" + Settings.Default.Room);

            try
            {
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
            }
            catch (Exception)
            {
            }
        }
    }
}
