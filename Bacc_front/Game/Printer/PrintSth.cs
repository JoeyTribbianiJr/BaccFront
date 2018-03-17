using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bacc_front
{
    public class PrintSth
    {
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
            var account= Desk.Instance.Accounts;
            var str = "";
            str += "座位   总上分   总下分   总账\n";
            for (int i = 0; i < account.Count; i ++)
            {
                var a = account[i];
                str += " " + a.PlayerId + "   " + a.TotalAddScore + " " + a.TotalSubScore + " " + a.TotalAccount;
                str += "\n";
            }
            str = str.TrimEnd('\n');
            Printer.PrintString(Setting.Instance._COM_PORT, str);
        }
    }
}
