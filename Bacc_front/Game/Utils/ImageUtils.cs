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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WsUtils
{
    public class ImageUtils
    {
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
                ept = new EncoderParameter(ecd, 90L);
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
        public ImageCodecInfo GetImageCoderInfo(string coderType)
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
        public Bitmap GetScreen()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            return bitmap;
        }
        void TransmitImage(AppSession session)
        {
            //Thread threadAccept = new Thread(new ParameterizedThreadStart(SendImage));
            //threadAccept.IsBackground = true;
            //threadAccept.Start(session);
        }
        //public void SendImage(Object obj)
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            Thread.Sleep(100);
        //            AppSession session = (AppSession)obj;
        //            MemoryStream ms = new MemoryStream();
        //            Bitmap bitmap = GetScreen();
        //            //bitmap.Save(ms, ImageFormat.Bmp);
        //            CompressImage(bitmap, ms);
        //            //CompressImage(bitmap, ms);
        //            //var byteBuffer = Compress(ms.ToArray());
        //            var byteBuffer = ms.ToArray();

        //            if (session.Connected)
        //            {
        //                byte[] type = Encoding.UTF8.GetBytes("01");
        //                int len = byteBuffer.Length;
        //                byte[] length = BitConverter.GetBytes(len);
        //                var data = type.Concat(length).Concat(byteBuffer).ToArray();
        //                int legth = BitConverter.ToInt32(data, 2);

        //                session.Send(data, 0, data.Length);
        //                //Thread.Sleep(ImageFrame);
        //            }
        //            else
        //            {
        //                break;
        //            }
        //        }
        //        catch
        //        {
        //            Debug.Write("发送出错");
        //        }
        //    }
        //}
        public void SendImage(AppSession session)
        {
            if (session.Connected)
            {
                MemoryStream ms = new MemoryStream();
                Bitmap bitmap = GetScreen();
                CompressImage(bitmap, ms);
                var byteBuffer = ms.ToArray();
                byte[] type = Encoding.UTF8.GetBytes("01");
                int len = byteBuffer.Length;
                byte[] length = BitConverter.GetBytes(len);
                var data = type.Concat(length).Concat(byteBuffer).ToArray();
                int legth = BitConverter.ToInt32(data, 2);

                try
                {
                    session.Send(data, 0, data.Length);
                }
                catch (Exception)
                {
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

    }
}
