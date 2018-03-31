using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Bacc_front
{
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
                if (amount > 9)
                {
                    amount = 9;
                }
                //string img_path;
                //var choose_denomination = player.Choose_denomination;
                //if (choose_denomination == BetDenomination.mini)
                //{
                  var  img_path = "Img/chips/" + (amount + 9) + ".GIF";
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
                return "Img/chips/10.GIF";
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
                    img_path= "Img/chips/10.GIF";
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
                    img_path = "Img/chips/" + (amount + 9) + ".GIF";
                    return img_path;
                }
                else
                {
                    img_path = "Img/chips/X" + (amount + 9) + ".GIF";
                    return img_path;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("转换成错" + ex.Message);
                return "Img/chips/10.GIF";
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
                    top = 22;
                    break;
                case 2:
                    top = 20;
                    break;
                case 3:
                    top = 18;
                    break;
                case 4:
                    top = 15;
                    break;
                case 5:
                    top = 13;
                    break;
                case 9:
                    top = 8;
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
            return new SolidColorBrush(Colors.AliceBlue);
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
}
