#region Usings

using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class NDCConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string inputStr = (string)value;
            if (!string.IsNullOrEmpty(inputStr))
            {
                //return Behaviors.TextBoxFilters.FormatNDCText(inputStr.Replace("-", ""));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string phone = (string)value;
            return phone;
        }
    }

    public class PhoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string inputStr = (string)value;
            if (!string.IsNullOrEmpty(inputStr))
            {
                if (inputStr.Length == 10)
                {
                    return Regex.Replace(inputStr, @".*?(\d{3}).*?(\d{3}).*?(\d{4}).*", "$1.$2.$3");
                }

                if (inputStr.Length == 7)
                {
                    return Regex.Replace(inputStr, @".*?(\d{3}).*?(\d{4}).*", "$1.$2");
                }

                return inputStr;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string phone = (string)value;
            phone = phone.Replace(".", "");
            return phone;
        }
    }
}