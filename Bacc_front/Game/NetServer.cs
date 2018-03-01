using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Bacc_front
{
    public class NetServer 
    {
        private const int port = 54322;
        private const int Time = 1000;
        private Socket serverSocket;
        private IPEndPoint serverIEP;

        private Bitmap GetScreen()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            return bitmap;
        }

        public void StartServer()
        {
            Debug.Write("开始监听");
            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverIEP = new IPEndPoint(IPAddress.Any, port);
                serverSocket.Bind(serverIEP);
                serverSocket.Listen(1);

                Thread threadAccept = new Thread(new ThreadStart(Accept));
                threadAccept.IsBackground = true;
                threadAccept.Start();
            }
            catch
            {
                Debug.Write("start出错");
            }
        }
        
       private void Accept()
        {
            while (true)
            {
                Socket client = serverSocket.Accept();

                Debug.Write("一名用户连接");

                Thread threadSend = new Thread(new ParameterizedThreadStart(Send));
                threadSend.IsBackground = true;
                threadSend.Start(client);
            }
        }

        private void Send(Object Obj)
        {
            try
            {
                Socket client = (Socket)Obj;
                //Bitmap bitmap = GetBitmapFromScreen();
                Bitmap bitmap = GetScreen();
                Byte[] byteBuffer = new Byte[1048567];
                byteBuffer = BitmapToByte(bitmap);

                if (client.Connected)
                {
                    client.BeginSend(byteBuffer, 0, byteBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), client);
                }
            }
            catch
            {
                Debug.Write("发送出错");
            }

        }

        private void SendCallBack(IAsyncResult AR)
        {
            try
            {
                Thread.Sleep(Time);

                Socket client = (Socket)AR.AsyncState;
                //Bitmap bitmap = GetBitmapFromScreen();
                Bitmap bitmap = GetScreen();
                Byte[] byteBuffer = new Byte[1048567];
                byteBuffer = BitmapToByte(bitmap);

                if (client.Connected)
                {
                    client.BeginSend(byteBuffer, 0, byteBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), client);
                }
            }
            catch
            {
            }

        }

        private Bitmap GetBitmapFromScreen()
        {
            Rectangle rc = SystemInformation.VirtualScreen;
            var bitmap = new Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }

            return bitmap;

        }

        private Byte[] BitmapToByte(Bitmap bitmap)
        {

            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            Byte[] buffer = new Byte[1048567];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
            return buffer;

        }


        private void SetImage(BitmapImage bitmapImage)
        {

            //imageScreen.Source = bitmapImage;

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
                ici = this.getImageCoderInfo("image/jpeg");
                ecd = System.Drawing.Imaging.Encoder.Quality;
                eptS = new EncoderParameters(1);
                ept = new EncoderParameter(ecd, 10L);
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
        private ImageCodecInfo getImageCoderInfo(string coderType)
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
    }
}
