using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Bacc_front
{
    public class ChipFontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var blc = (int)value;
            if(blc < 1000)
            {
                return 28;
            }
            if(blc >= 1000 && blc < 10000)
            {
                return 24;
            }
            if(blc >= 10000)
            {
                return 20;
            }
            return 26;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChipVisibleConverter : IValueConverter
    {
        public Object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value) == 0 ? Visibility.Hidden : Visibility.Visible;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class ChipBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int amount = 0;
                amount = System.Convert.ToInt32(value) / 1000;
                //amount = (int)values[0] / 1000;
                if (amount == 0)
                {
                    amount = 1;
                }
                if (amount > 5)
                {
                    amount = 5;
                }
                //string img_path;
                //var choose_denomination = player.Choose_denomination;
                //if (choose_denomination == BetDenomination.mini)
                //{
                //var  img_path = "Img/chips/" + (amount + 9) + ".GIF";
                var img_path = "Img/chips/" + (amount + 9).ToString() + ".png";
                //var  img_path = "Img/chips/11.png";
                //}
                //else
                //{
                //    img_path = "Img/chips/X" + (amount + 9) + ".GIF";
                //}

                return img_path;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("转换成错" + ex.StackTrace);
                return "Img/chips/10.png";
                //return "Img/chips/10.GIF";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //public class ChipBackgroundConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        try
    //        {
    //            int amount = 0;
    //            if (value.GetType() == DependencyProperty.UnsetValue.GetType())
    //            {
    //                var mg_path = "Img/chips/10.GIF";
    //                return mg_path;
    //            }
    //            var player = (Player)value;
    //            if (player == null)
    //            {
    //                return "Img/chips/10.GIF";
    //            }
    //            amount = player.Balance / 1000;
    //            //amount = (int)values[0] / 1000;
    //            if (amount == 0)
    //            {
    //                amount = 1;
    //            }
    //            if (amount > 9)
    //            {
    //                amount = 9;
    //            }
    //            string img_path;
    //            var choose_denomination = player.Choose_denomination;
    //            if (choose_denomination == BetDenomination.mini)
    //            {
    //                img_path = "Img/chips/" + (amount + 9) + ".GIF";
    //            }
    //            else
    //            {
    //                img_path = "Img/chips/X" + (amount + 9) + ".GIF";
    //            }

    //            return img_path;
    //        }
    //        catch (Exception ex)
    //        {
    //            //MessageBox.Show("转换成错" + ex.StackTrace);
    //            return "Img/chips/10.GIF";
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class ChipBackgroundMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                //return "Img/chips/10.GIF";
                //MessageBox.Show("一次");
                int amount = 0;
                string img_path;
                if (values[0].GetType() == DependencyProperty.UnsetValue.GetType())
                {
                    //img_path= "Img/chips/10.GIF";
                    img_path= "Img/chips/10.png";
                    return img_path;
                }
                amount = (int)values[0] / 1000;
                //amount = (int)values[0] / 1000;
                if (amount == 0)
                {
                    amount = 1;
                }
                if (amount > 9)
                {
                    amount = 9;
                }
                var choose = (int)values[1];
                if (choose == 1)
                {
                    img_path = "Img/chips/" + (amount + 9) + ".png";
                    //img_path = "Img/chips/" + (amount + 9) + ".GIF";
                    return img_path;
                }
                else
                {
                    img_path = "Img/chips/X" + (amount + 9) + ".png";
                    //img_path = "Img/chips/X" + (amount + 9) + ".GIF";
                    return img_path;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("转换成错" + ex.Message);
                return "Img/chips/10.png";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChipTextMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int top = 1;
            var amount = System.Convert.ToInt32(value) / 1000;
            if (amount == 0)
            {
                amount = 1;
            }
            if (amount > 9)
            {
                amount = 9;
            }
            switch (amount)
            {
                case 1:
                    top = 16;
                    break;
                case 2:
                    top = 13;
                    break;
                case 3:
                    top = 12;
                    break;
                case 4:
                    top = 10;
                    break;
                case 5:
                    top = 9;
                    break;
                case 9:
                    top = 9;
                    break;
                default:
                    top = 10;
                    break;
            }
            var margin = new Thickness(0, top, 0, 0);
            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ChipForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var choose = (BetDenomination)value;
            if (choose == BetDenomination.big)
            {
                return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class WayBillBackgroundConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var side = (WinnerEnum)value;
            if (side == WinnerEnum.banker)
            {
                return new SolidColorBrush(Colors.Red);
            }
            if (side == WinnerEnum.player)
            {
                return new SolidColorBrush(Colors.Blue);
            }
            if (side == WinnerEnum.tie)
            {
                return new SolidColorBrush(Colors.Green);
            }
            //return new SolidColorBrush(Colors.AliceBlue);
            return new SolidColorBrush(Colors.Red);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class WayBillContentConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var side = (WinnerEnum)value;
            switch (side)
            {
                case WinnerEnum.banker:
                    return "庄";
                case WinnerEnum.player:
                    return "闲";
                case WinnerEnum.tie:
                    return "和";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class WayBillVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var side = (WinnerEnum)value;
            switch (side)
            {
                case WinnerEnum.tie:
                case WinnerEnum.player:
                case WinnerEnum.banker:
                    return Visibility.Visible;
                case WinnerEnum.none:
                    return Visibility.Hidden;
                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ConfigButtonBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var content = (string)value;
            if (content == "退出")
            {
                return Colors.OrangeRed;
            }
            else
            {
                return Colors.MediumSeaGreen;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PlayerScoreSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ps = (ObservableCollection<Player>)value;
            //DataTable table = WsUtils.DataTableExtensions.ToDataTable(ps);
            DataTable table = new DataTable();
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("Add_score", typeof(int));
            table.Columns.Add("Last_add", typeof(int));
            table.Columns.Add("Balance", typeof(int));
            table.Columns.Add("Last_sub", typeof(int));
            table.Columns.Add("Sub_score", typeof(int));
            var dr = table.NewRow();
            dr["Title"] = "总 计";
            dr["Add_score"] = ps.Sum(p => p.Add_score);
            dr["Last_add"] = ps.Sum(p => p.Last_add);
            dr["Balance"] = ps.Sum(p => p.Balance);
            dr["Last_sub"] = ps.Sum(p => p.Last_sub);
            dr["Sub_score"] = ps.Sum(p => p.Sub_score);
            table.Rows.Add(dr);
            return table;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BetRecordSmallContentConverter: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var winner = (int)values[3];
            if(winner == (int)WinnerEnum.none)
            {
                return "";
            }
            var b = (int)values[0];
            var t = (int)values[1];
            var p = (int)values[2];
            var profit = Desk.GetProfit(winner, b, p, t);
            return profit > 0 ? "+" :(profit == 0)?"": "-";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TotalSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var users = value as IEnumerable<Player>;
            if (users == null)
                return 0;

            return users.Sum(u => u.Balance);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
