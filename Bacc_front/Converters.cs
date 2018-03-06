using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
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
        public Object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var amount = System.Convert.ToInt32(value) / 1000;
            if (amount == 0)
            {
                amount = 1;
            }
            if (amount > 5)
            {
                amount = 5;
            }
            var img_path = "Img/c_" + amount + ".bmp";

            return img_path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
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
            if (amount > 5)
            {
                amount = 5;
            }
            switch (amount)
            {
                case 1:
                    top = 32;
                    break;
                case 2:
                    top = 27;
                    break;
                case 3:
                    top = 22;
                    break;
                case 4:
                    top = 15;
                    break;
                case 5:
                    top = 10;
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
    public class WayBillBackgroundConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var side = (WinnerEnum)value;
            if (side ==  WinnerEnum.banker)
            {
                return new SolidColorBrush(Colors.Red);
            }
            if (side ==  WinnerEnum.player)
            {
                return new SolidColorBrush(Colors.Blue);
            }
            if (side ==  WinnerEnum.tie)
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
}
