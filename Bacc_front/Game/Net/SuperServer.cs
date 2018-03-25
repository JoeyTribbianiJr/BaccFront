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
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using WsUtils;

namespace Bacc_front
{
    public class SuperServer : AppServer
    {
        public static AppSession appSession;
        private AppServer appServer;
        private ImageUtils imageSender;
        private HttpClient httpClient;
        private const int port = 54322;
        public SuperServer()
        {
            imageSender = new ImageUtils();
            httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            httpClient.Timeout = TimeSpan.FromSeconds(0.5);
            appServer = new AppServer();
            appSession = new AppSession();
        }
        public void StartServer()
        {
            if (!appServer.Setup(port))
            {
                Trace.WriteLine("端口设置失败");
                return;
            }
            //连接时
            appServer.NewSessionConnected += appServer_NewSessionConnected;
            //接收信息时
            appServer.NewRequestReceived += appServer_NewRequestReceived;
            //关闭服务时
            appServer.SessionClosed += appServer_SessionClosed;
            Accept();

        }
        private void Accept()
        {
            if (!appServer.Start())
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
            SendFrontSettingToBack();
        }

        void appServer_NewRequestReceived(AppSession session, StringRequestInfo requestInfo)
        {
            RemoteCommand type = (RemoteCommand)Enum.Parse(typeof(RemoteCommand), requestInfo.Key);
            switch (type)
            {
                case RemoteCommand.Login:
                    CheckLogin(requestInfo);
                    break;
                case RemoteCommand.ImportFrontLocalSessions:
                    SendData(RemoteCommand.ImportFrontLocalSessions, Game.Instance.LocalSessions, session);
                    break;
                //case RemoteCommand.ReplaceWaybill:
                //    OnReplaceWaybill(session, requestInfo);
                //    break;
                case RemoteCommand.ImportBack:
                    OnImportBack(session, requestInfo);
                    break;
                case RemoteCommand.ImportBackNextSession:
                    OnImportBackNextSession(session, requestInfo);
                    break;
                case RemoteCommand.SendFrontBetRecord:
                    SendBetRecordToBack();
                    break;
                default:
                    session.Send("Unknow ");
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
            if(Setting.connect_pass != pass)
            {
                SendData(RemoteCommand.Login, "False", appSession);
            }
            else
            {
                SendData(RemoteCommand.Login, "OK", appSession);
            }
        }
        public void SendFrontPasswordToBack()
        {
            if (appSession.Connected)
            {
                SendData(RemoteCommand.SendFrontPassword, Setting.Instance.PasswordMap, appSession);
            }
        }
        public void SendFrontSettingToBack()
        {
            if (appSession.Connected)
            {
                SendData(RemoteCommand.SendFrontSetting, Setting.Instance.game_setting, appSession);
            }
        }
        public void SendLiveDataToBack()
        {
            if (appSession.Connected)
            {
                Game.Instance.Manager.SetBackLiveData();
                SendData(RemoteCommand.SendFrontLiveData, Game.Instance.BetBackLiveData, appSession);
            }
        }
        public void SendSummationBetRecordToBack()
        {
            if (appSession.Connected)
            {
                Game.Instance.Manager.SetSummationBackBetRecordData();
                SendData(RemoteCommand.SendFrontSummationBetRecord, Game.Instance.BetRecordSummationJsonDataToBack, appSession);
            }
        }
        public void SendBetRecordToBack()
        {
            if (appSession.Connected)
            {
                Game.Instance.Manager.SetBackBetRecordData();
                SendData(RemoteCommand.SendFrontBetRecord, Game.Instance.BetRecordJsonDataToBack, appSession);
            }
        }
        public void SendWaybillToBack()
        {
            if (appSession.Connected)
            {
                SendData(RemoteCommand.SendFrontWaybill, Game.Instance.Waybill, appSession);
            }
        }
        public void SendCurSessionToBack()
        {
            if (appSession.Connected)
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
                        Winner = (int)Game.Instance.CurrentRound.Winner.Item1,
                    };

                    var ip = Setting.Instance.ServerUrl;
                    var url = "http://" + ip + ":98/getmsg";
                    var str = JsonConvert.SerializeObject(msg);

                    string Cards = "";
                    if (Game.Instance.CountDown <= 5)
                    {
                        Cards = JsonConvert.SerializeObject(Game.Instance.ConvertHandCardForServerSB());
                    }
                    url += ("?" + "msg=" + str + "&cards=" + Cards);

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
        public void SendDealCommandToHttpServer()
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

            var waybill = JsonConvert.SerializeObject(Game.Instance.ConvertWaybillForServerSB());
            url += ("?" + "msg=" + str + "&waybill=" + waybill);

            try
            {
                //httpClient.SendAsync()
                httpClient.GetAsync(url);
            }
            catch (Exception)
            {
            }
        }
    }
}
