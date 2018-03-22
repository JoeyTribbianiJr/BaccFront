using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;

namespace Bacc_front
{
    public class Printer
    {
        private static SerialPort serialPort;   //串口

        public Printer(string portName)
        {
            try
            {
                //
                //串口初始化
                //
                serialPort = new SerialPort();
                serialPort.PortName = portName;
                serialPort.BaudRate = 9600;//波特率
                serialPort.Parity = System.IO.Ports.Parity.Odd;//奇校验
                serialPort.StopBits = System.IO.Ports.StopBits.One;//停止位
                OpenPort();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        public Printer()
        {
            try
            {
                //
                //串口初始化
                //
                serialPort = new SerialPort();
                serialPort.PortName = "COM1";
                serialPort.BaudRate = 9600;//波特率
                serialPort.Parity = System.IO.Ports.Parity.Odd;//奇校验
                serialPort.StopBits = System.IO.Ports.StopBits.One;//停止位
                OpenPort();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void OpenPort()
        {
            if (null != serialPort)
            {
                try
                {
                    if (!serialPort.IsOpen)
                        serialPort.Open();

                }
            catch(Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }

        private void ClosePort()
        {
            try
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.Dispose();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void Write(byte[] data, int len)
        {
            try
            {
                serialPort.Write(data, 0, len);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// 向打印机发送命令
        /// </summary>
        /// <param name="data">命令数据</param>
        public void Write(params byte[] data)
        {
            try
            {
                serialPort.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="strBuf">要发送的字符串</param>
        public void Write(string strBuf)
        {
            try
            {
                byte[] data = ToHex(strBuf, "GB2312");
                serialPort.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// 根据编码方式转换字符串为byte[]
        /// </summary>
        /// <param name="str">目标字符串</param>
        /// <param name="charset">编码方式</param>
        /// <returns>转换后的byte[]</returns>
        private static byte[] ToHex(string str, string charset)//
        {
            if (str.Length % 2 != 0)
                str += "";
            Encoding enc = Encoding.GetEncoding(charset);
            return enc.GetBytes(str);
        }

        /// <summary>
        /// 图片取模
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private byte[,] GetBytesByBMP(Bitmap bmp)
        {
            //bitArray存储像素点
            bool[,] bitArray = new bool[bmp.Height % 8 == 0 ?
                bmp.Height : (bmp.Height / 8 + 1) * 8, bmp.Width];

            for (int i = 0; i < bmp.Width; ++i)//获取图片点阵
            {
                for (int j = 0; j < bmp.Height; ++j)
                {
                    //获取点的ARGB
                    Color pixel = bmp.GetPixel(i, j);

                    double gray = pixel.R * 0.299 +
                        pixel.G * 0.587 + pixel.B * 0.114;
                    if (gray < 192)//是深色
                        bitArray[j, i] = true;
                    else //非深色(浅色)
                        bitArray[j, i] = false;
                }
            }

            byte[,] res =//存储字模
                new byte[bitArray.GetLength(0) / 8, bitArray.GetLength(1)];
            int resRow = 0, resCol = 0;
            for (int i = 0; i < bmp.Height; i = i + 8)
            {
                resCol = 0;
                for (int j = 0; j < bmp.Width; ++j)
                {
                    byte b = 0;
                    //二进制转十进制
                    for (int k = (i + 7), p = 0; k > i - 1; --k)
                    {
                        b += (byte)((bitArray[k, j] ? 1 : 0) *
                            Math.Pow(2, p++));
                    }
                    res[resRow, resCol++] = b;

                }
                ++resRow;
            }

            return res;
        }
        /// <summary>
        /// 打印图片
        /// </summary>
        /// <param name="bmp">图片</param>
        /// <param name="enlarge">放大倍数</param>
        public void printImage(Bitmap bmp)
        {
            byte[,] gImage = GetBytesByBMP(bmp);

            for (int i = gImage.GetLength(0) - 1; i >= 0; --i)
            {
                byte lowLen = (byte)((gImage.GetLength(1) << 8) >> 8);
                byte highLen = (byte)(gImage.GetLength(1) >> 8);

                Write(0x1B, 0x4B, lowLen, highLen);
                for (int j = 0; j < gImage.GetLength(1); j++)
                    Write(gImage[i, j]);
                Write(0x0D);
            }
        }

        public static string PrintString(int Port, string val)
        {
            System.IO.Ports.SerialPort sp = new System.IO.Ports.SerialPort();
            sp.PortName = "COM" + Port.ToString();
            try
            {
                sp.Open();
            }
            catch
            {
                return "端口被占用";
            }
            List<byte> data = new List<byte>();
            data.AddRange(new byte[] { 0x1C, 0x26 });
            string[] lines = val.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                byte[] content = ToHex(lines[i].Replace("\r", ""), "GB2312");
                //byte[] content = System.Text.Encoding.GetEncoding("GB2312").GetBytes(lines[i].Replace("\r", ""));
                byte[] wapbt = { 0x0a };
                data.AddRange(content);
                data.AddRange(wapbt);
            }
            byte[] cutbt = { 0x1d, 0x56, 0x42, 0x11 };
            data.AddRange(cutbt);
            byte[] databt = data.ToArray();
            sp.Write(databt, 0, databt.Length);
            sp.Close();
            return null;
        }
        //测试打印机是否接在这个端口
        public static bool PrintTest(int Port)
        {
            SerialPort sp = new SerialPort
            {
                PortName = "COM" + Port.ToString()
            };
            sp.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(sp_DataReceived);
            try
            {
                sp.Open();
            }
            catch
            {
                return false;
            }
            Recived = false;
            byte[] testbt = { 0x1D, 0x49, 0x01 };
            sp.Write(testbt, 0, testbt.Length);
            System.Threading.Thread.Sleep(100);
            sp.Close();
            return Recived;
        }
        public static void OpenEPSONCashBox(int port)//, int a_intBaudRate, int a_intDataBits)
        {
            System.IO.Ports.SerialPort sp = new System.IO.Ports.SerialPort();
            sp.PortName = "COM" + port.ToString();
            try
            {
                sp.Open();
                byte[] byteA = { 0x1B, 0x70, 0x00, 0x45, 0x45 };
                sp.Write(byteA, 0, byteA.Length);
                System.Threading.Thread.Sleep(100);
            }
            catch(Exception ex)
            {
                MessageBox.Show("开锁失败");
            }
            finally
            {//如果端口被占用需要重新设置
                sp.Close();
            }
        }

        static bool Recived = false;
        //主要是在从COM端口中接收数据时触发
        public static void sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            Recived = true;
        }

    }
}
