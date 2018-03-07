using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Bacc_front
{
    public class SuperServer :AppServer
    {
        private AppServer appServer;
        private const int ImageFrame = 40;
        public SuperServer()
        {
            appServer = new AppServer();
        }
        private const int port = 54322;

        public void StartSever()
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
            //session.Send("01");
            TransmitImage(session);
        }

        void appServer_NewRequestReceived(AppSession session, SuperSocket.SocketBase.Protocol.StringRequestInfo requestInfo)
        {
            var type = Enum.Parse(typeof(RemoteCommand), requestInfo.Key);
            switch (type)
            {
                case RemoteCommand.ImportFront:
                    SendData(RemoteCommand.ImportFront, Game.Instance.LocalSessions,session);
                    break;
                case "2":
                    session.Send("You input 2");
                    break;
                default:
                    session.Send("Unknow ");
                    break;
            }
        }
        void appServer_SessionClosed(AppSession session, SuperSocket.SocketBase.CloseReason value)
        {
            session.Send("服务已关闭");
        }
        void SendData(RemoteCommand command,object obj, AppSession session)
        {
            var type = ((int)command).ToString().PadLeft(2,'0');
            byte[] typeByte = Encoding.Default.GetBytes(type);

            int len = 0;
            byte[] length = BitConverter.GetBytes(len);

            var str = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            byte[] byteBuffer = Encoding.Default.GetBytes(str);

            var data = typeByte.Concat(length).Concat(byteBuffer).ToArray();

            if (type.Length != 2)
            {
                throw new Exception();
            }
            session.Send(data, 0, data.Length);
        }
        #region 发送图片
        void TransmitImage(AppSession session)
        {
            Thread threadAccept = new Thread(new ParameterizedThreadStart(SendImage));
            threadAccept.IsBackground = true;
            threadAccept.Start(session);
        }
        private void SendImage(Object obj)
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                    AppSession session = (AppSession)obj;
                    MemoryStream ms = new MemoryStream();
                    Bitmap bitmap = GetScreen();
                    //bitmap.Save(ms, ImageFormat.Bmp);
                    CompressImage(bitmap, ms);
                    //CompressImage(bitmap, ms);
                    //var byteBuffer = Compress(ms.ToArray());
                    var byteBuffer = ms.ToArray();

                    if (session.Connected)
                    {
                        byte[] type = Encoding.Default.GetBytes("01");
                        int len = byteBuffer.Length;
                        byte[] length = BitConverter.GetBytes(len);
                        var data = type.Concat(length).Concat(byteBuffer).ToArray();
                        int legth = BitConverter.ToInt32(data, 2);

                        session.Send(data, 0, data.Length);
                        //Thread.Sleep(ImageFrame);
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    Debug.Write("发送出错");
                }
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                GZipStream Compress = new GZipStream(ms, CompressionMode.Compress);
                Compress.Write(bytes, 0, bytes.Length);
                Compress.Close();
                return ms.ToArray();
            }
        }
        public byte[] Decompress(Byte[] bytes, int len)//因为本例需求，我加了一个参数Len表示实际长度
        {
            try
            {
                using (MemoryStream tempMs = new MemoryStream())
                {
                    using (MemoryStream ms = new MemoryStream(bytes, 0, len))
                    {
                        GZipStream Decompress = new GZipStream(ms, CompressionMode.Decompress);
                        Decompress.CopyTo(tempMs);
                        Decompress.Close();
                        return tempMs.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 压缩图片，将压缩后的图片存入MemoryStream
        /// </summary>
        /// <param name="bitmap">原图片</param>
        /// <param name="ms">内存流</param>
        public void CompressImage(Bitmap bitmap, MemoryStream ms)
        {
            ImageCodecInfo ici = null;
            System.Drawing.Imaging.Encoder ecd = null;
            EncoderParameter ept = null;
            EncoderParameters eptS = null;
            try
            {
                ici = this.GetImageCoderInfo("image/jpeg");
                ecd = System.Drawing.Imaging.Encoder.Quality;
                eptS = new EncoderParameters(1);
                ept = new EncoderParameter(ecd, 50L);
                eptS.Param[0] = ept;
                bitmap.Save(ms, ici, eptS);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                ept.Dispose();
                eptS.Dispose();
            }
        }
        /// <summary>  
        /// 获取图片编码信息  
        /// </summary>  
        /// <param name="coderType">编码类型</param>  
        /// <returns>ImageCodecInfo</returns>  
        private ImageCodecInfo GetImageCoderInfo(string coderType)
        {
            ImageCodecInfo[] iciS = ImageCodecInfo.GetImageEncoders();

            ImageCodecInfo retIci = null;

            foreach (ImageCodecInfo ici in iciS)
            {
                if (ici.MimeType.Equals(coderType))
                    retIci = ici;
            }
            return retIci;
        }
        private Bitmap GetScreen()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            return bitmap;
        }
        #endregion
    }
}
