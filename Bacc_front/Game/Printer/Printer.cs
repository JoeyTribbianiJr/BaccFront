using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
namespace Bacc_front
{
    public class Printer
    {
        public bool IsInPrinting;
        private static SerialPort serialPort;   //串口
        public int Fault = 0;
        public Printer(string portName)
        {
            try
            {
                serialPort = new SerialPort();
                serialPort.PortName = portName;
                serialPort.BaudRate = 9600;//波特率
                serialPort.Parity = Parity.Odd;//奇校验
                serialPort.StopBits = StopBits.One;//停止位
                OpenPort();
            }
            catch (Exception e)
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
                serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                OpenPort();
            }
            catch (Exception e)
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
                catch (Exception e)
                {
                    MessageBox.Show("无法打开端口1，请检查");
                    App.Current.Shutdown();
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
            catch (Exception e)
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
        public bool PrintString(int Port, string val)
        {
            try
            {

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
                serialPort.Write(databt, 0, databt.Length);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //测试打印机门是否关闭
        public void PrintTest()
        {
            byte[] testbt = { 0x10, 0x04, 0x02, 0x10, 0x04, 0x04 };
            serialPort.Write(testbt, 0, testbt.Length);
            System.Threading.Thread.Sleep(100);
        }
        //主要是在从COM端口中接收数据时触发
        public void sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte[] receive = new byte[2];
            serialPort.Read(receive, 0, 8);
            if (receive[0] != 80)
            {
                Fault = 1;
            }
            else if (receive[1] != 13)
            {
                Fault = 2;
            }
            else
            {
                Fault = 0;
            }
        }
        public static void OpenEPSONCashBox(int port)//, int a_intBaudRate, int a_intDataBits)
        {
            System.IO.Ports.SerialPort sp = new System.IO.Ports.SerialPort();
            sp.PortName = "COM" + port.ToString();
            try
            {
                sp.Open();
                byte[] byteA = { 0x1B, 0x70, 0x00, 0x05, 0x03 };
                sp.Write(byteA, 0, byteA.Length);
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show("开锁失败");
            }
            finally
            {//如果端口被占用需要重新设置
                sp.Close();
            }
        }

        public void TestPrinter(object sender, EventArgs e)
        {
            //if (Setting.Instance._is_print_bill == "打印路单" || Setting.Instance._is_print_bill == "打印不监控")
            //{
            //    {
            //    }
            //}
            if (Setting.Instance._is_print_bill == "打印路单")
            {
                try
                {
                    PrintTest();
                    if (Game.Instance.GamePrinter.Fault != 0)
                    {
                        HandlePrinterFault();
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                }
            }
        }
        public void HandlePrinterFault()
        {
            var str = Game.Instance.GamePrinter.Fault == 1 ? "钱箱被打开" : "打印机缺纸";
            Game.Instance.CoreTimer.StopTimer();
            var result = MessageBox.Show(str, "警告", MessageBoxButton.OK);
            Game.Instance.CoreTimer.StopPrintTestTimer();
            if (result == MessageBoxResult.OK)
            {
                PrintTest();
                Game.Instance.Start();
                Game.Instance.CoreTimer.StartPrintTestTimer();
            }
        }
        public void PrintWaybill()
        {

            var waybill = Game.Instance.CurrentSession.RoundsOfSession;
            var str = "";
            for (int i = 0; i < waybill.Count; i += 6)
            {
                for (int j = 0; j < 6; j++)
                {
                    var winner = waybill[i + j].Winner.Item1;
                    Type t = winner.GetType();
                    FieldInfo info = t.GetField(Enum.GetName(t, winner));
                    DescriptionAttribute description = (DescriptionAttribute)Attribute.GetCustomAttribute(info, typeof(DescriptionAttribute));
                    str = str + description.Description;
                }
                str += "\n";
            }
            str = str.TrimEnd('\n');
            PrintString(Setting.Instance._COM_PORT, str);
        }
        public void PrintAccount()
        {
            //var account = Desk.Instance.Accounts;
            //var str = "";
            //str += "座位   总上分   总下分   总账\n";
            //for (int i = 0; i < account.Count; i ++)
            //{
            //    var a = account[i];
            //    str += " " + a.PlayerId + "   " + a.TotalAddScore + " " + a.TotalSubScore + " " + a.TotalAccount;
            //    str += "\n";
            //}
            //str = str.TrimEnd('\n');
            //Printer.PrintString(Setting.Instance._COM_PORT, str);
        }

    }
}
