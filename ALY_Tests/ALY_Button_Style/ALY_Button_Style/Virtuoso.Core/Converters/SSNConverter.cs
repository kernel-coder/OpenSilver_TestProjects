#region Usings

using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class SSNConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string inputStr = (string)value;
            if (!string.IsNullOrEmpty(inputStr))
            {
                if (inputStr.Length == 9)
                {
                    return Regex.Replace(inputStr, @".*?(\d{3}).*?(\d{2}).*?(\d{4}).*", "$1-$2-$3");
                }

                return inputStr;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string phone = (string)value;
            phone = phone.Replace("-", "");
            if (String.IsNullOrEmpty(phone))
            {
                phone = null;
            }

            return phone;
        }
    }
}