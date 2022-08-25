#region Usings

using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class FederalTaxIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string inputStr = (string)value;
            if (!string.IsNullOrEmpty(inputStr))
            {
                if (inputStr.Length == 9)
                {
                    return Regex.Replace(inputStr, @".*?(\d{2}).*?(\d{7}).*", "$1-$2");
                }

                return inputStr;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string id = (string)value;
            id = id.Replace("-", "");
            if (String.IsNullOrEmpty(id))
            {
                id = null;
            }

            return id;
        }
    }
}