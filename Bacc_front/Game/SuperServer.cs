using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bacc_front
{
    class SuperServer
    {
        public void StartSever()
        {
            var appServer = new AppServer();
            int port = 8888;
            if (!appServer.Setup(port))
            {
                Console.WriteLine("端口设置失败");
                Console.ReadKey();
                return;
            }
            //连接时
            appServer.NewSessionConnected += appServer_NewSessionConnected;
            //接收信息时
            appServer.NewRequestReceived += appServer_NewRequestReceived;
            //关闭服务时
            appServer.SessionClosed += appServer_SessionClosed;
            if (!appServer.Start())
            {
                Console.WriteLine("启动服务失败");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("服务启动成功，输入q退出");

            while (true)
            {
                var str = Console.ReadLine();
                if (str.ToLower().Equals("q"))
                {
                    break;
                }
            }
            Console.WriteLine();
            appServer.Stop();
            Console.WriteLine("服务已停止，按任意键退出");
            Console.ReadKey();
        }

        private static void appServer_NewSessionConnected(AppSession session)
        {
            session.Send("Hello World!");
        }

        static void appServer_NewRequestReceived(AppSession session, SuperSocket.SocketBase.Protocol.StringRequestInfo requestInfo)
        {
            switch (requestInfo.Key.ToLower())
            {
                case "1":
                    session.Send("You input 1");
                    break;
                case "2":
                    session.Send("You input 2");
                    break;
                default:
                    session.Send("Unknow ");
                    break;
            }
        }

        static void appServer_SessionClosed(AppSession session, CloseReason value)
        {
            session.Send("服务已关闭");
        }
    }
}
