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
        public static bool HasPaper = true;
        public static bool IsDoorClosed = true;
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
                serialPort = new SerialPort();
                serialPort.PortName = "COM1";
                serialPort.BaudRate = 9600;//波特率
                serialPort.Parity = System.IO.Ports.Parity.Odd;//奇校验
                serialPort.StopBits = System.IO.Ports.StopBits.One;//停止位
                serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                //serialPort.PinChanged += SerialPort_PinChanged;
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
                    MessageBox.Show("没有端口1或者被占用，请检查");
                    //App.Current.Shutdown();
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
                data.AddRange(new byte[] { 0x1C, 0x26, 0x1B, 0x61, 0x01, });
                var beilv = new byte[] { 0x1C, 0X21, 0x0C, };
                if (Setting.Instance._print_font == "大字体路单")
                {
                    var big_font = new byte[] { 0x1D, 0X21, 0x11, };
                    data.AddRange(big_font);
                }
                if (Setting.Instance._print_font == "小字体路单")
                {
                    var mini_font = new byte[] { 0x1D, 0X21, 0x00, };
                    data.AddRange(mini_font);
                }
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

        //测试打印机是否缺纸
        public static void PaperTest()
        {
            byte[] testbt = { 0x10, 0x04, 0x04 };
            serialPort.Write(testbt, 0, testbt.Length);
            System.Threading.Thread.Sleep(100);
        }
        //测试打印机是否开门
        public static void DoorTest()
        {
            byte[] testbt = { 0x10, 0x04, 0x02 };
            serialPort.Write(testbt, 0, testbt.Length);
            System.Threading.Thread.Sleep(100);
        }
        private void SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            // 如果上面说的三个输入信号中，任意一个输入发生变化
            // 都会引发PinChanged事件。因此，需要判断究竟是哪个
            // 引脚信号变化引发此事件
            bool b;
            try
            {
                switch (e.EventType)
                {
                    case System.IO.Ports.SerialPinChange.CDChanged:
                        b = serialPort.CDHolding;
                        break;
                    case System.IO.Ports.SerialPinChange.CtsChanged:
                        b = serialPort.CtsHolding;
                        break;
                    case System.IO.Ports.SerialPinChange.DsrChanged:
                        b = serialPort.DsrHolding;
                        if (!b && Setting.Instance._is_print_bill == "打印路单" 
                            && Game.Instance.CurrentState != GameState.Shuffling 
                            && Game.Instance.CurrentState != GameState.Printing)
                        {
                            Game.Instance.CoreTimer.StopTimer();
                            //ControlBoard.Instance.btnStartGame.IsEnabled = true;
                            MessageBox.Show("打印机故障，本局作废");
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }
        //主要是在从COM端口中接收数据时触发
        public void sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte[] door = new byte[1] { 0x00 };
            try
            {
                serialPort.Read(door, 0, 1);
                if (door[0] == 0x00)
                {
                    return;
                }
                if (door[0] == 0x1E)
                {
                    HasPaper = true;
                }
                if (door[0] == 0x7E)
                {
                    HasPaper = false;
                }
                if (door[0] == 0x44)
                {
                    IsDoorClosed = true;
                }
                if (door[0] == 0x88)
                {
                    IsDoorClosed = false;
                }
                if (Setting.Instance._is_print_bill == "打印路单" 
                    && Game.Instance._isGameStarting
                    && Game.Instance.CurrentState != GameState.Shuffling
                    && Game.Instance.CurrentState != GameState.Printing)
                {
                    if (door[0] == 0x88)
                    {
                        IsDoorClosed = false;
                        MessageBox.Show("打印机门开，本局作废");
                        Game.Instance.CoreTimer.StopTimer();
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(door[0].ToString() + ex.Message + ex.StackTrace);
#endif

            }
        }
        public static void OpenEPSONCashBox(int port)//, int a_intBaudRate, int a_intDataBits)
        {
            try
            {
                byte[] byteA = { 0x1B, 0x70, 0x00, 0x05, 0x03 };
                serialPort.Write(byteA, 0, byteA.Length);
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show("开锁失败");
            }
            finally
            {//如果端口被占用需要重新设置
            }
        }

        public void PrintWaybill()
        {
            var title = "百乐2号\n";
            var cur_date = DateTime.Now.ToString("yyyy年M月d日\n");
            var cur_time = DateTime.Now.ToString("HH:mm:ss\n");
            var s_idx = "第" + Game.Instance._sessionStrIndex + "局\n";
            var waybill = Game.Instance.CurrentSession.RoundsOfSession;
            var str = title + cur_date + cur_time + s_idx;
            for (int j = 0; j < 6; j++)
            {
                for (int i = 0; i < waybill.Count; i += 6)
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
