#region Usings

using System;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class EmptyOrNullToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return parameter;
                }

                if (value is string && string.IsNullOrEmpty((string)value))
                {
                    return parameter;
                }
            }
            catch (Exception)
            {
                return value;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return parameter;
                }

                if (value is string && string.IsNullOrEmpty((string)value))
                {
                    return parameter;
                }
            }
            catch (Exception)
            {
                return value;
            }

            return value;
        }
    }
}