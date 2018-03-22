using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Bacc_front
{
    public class PrintSth
    {
        private const int test_rate = 3;  //三秒钟检测一次
        private int test_count = 0;
        public bool TestPrinter()
        {
            if (Setting.Instance._is_print_bill)
            {
                if (test_count % test_rate == 0)
                {
                    test_count = 0;
                    try
                    {
                        if (!Printer.PrintTest(Setting.Instance._COM_PORT))
                        {
                            Game.Instance.CoreTimer.StopTimer();
                            HandlePrinterFault();
                            return false;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        HandlePrinterFault();
                        return false;
                    }
                    finally
                    {
                        test_count++;
                    }
                }
            }
            return true;
        }
        public void HandlePrinterFault()
        {
            if( MessageBox.Show("打印机故障，等待处理") == MessageBoxResult.OK)
            {
                if(TestPrinter())
                {
                    Game.Instance.Start();
                }
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
            Printer.PrintString(Setting.Instance._COM_PORT, str);
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
