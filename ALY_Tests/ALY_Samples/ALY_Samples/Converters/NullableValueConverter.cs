#region Usings

using System;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class NullableValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && string.IsNullOrWhiteSpace(value.ToString()))
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                return null;
            }

            return value;
        }

        #endregion
    }

    public class DecimalNullableValueConverter : IValueConverter
    {
        //Called when binding from an object property to a control property
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal defaultValue = 0.00m;
            decimal result = defaultValue;
            var strResult = String.Format("{0:0.00}", result);
            if (value == null)
            {
                return strResult;
            }

            if (string.IsNullOrEmpty(value.ToString()))
            {
                return strResult;
            }

            if (decimal.TryParse(value.ToString(), out result))
            {
                strResult = String.Format("{0:0.00}", result);
            }

            return strResult;
        }

        //Called with two-way data binding as value is pulled out of control and put back into the property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal defaultValue = 0.00m;
            if (value == null)
            {
                return defaultValue;
            }

            if (string.IsNullOrEmpty(value.ToString()))
            {
                return defaultValue;
            }

            return value;
        }
    }

    public class IntegerNullableValueConverter : IValueConverter
    {
        //Called when binding from an object property to a control property
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int defaultValue = 0;
            int result = defaultValue;
            if (int.TryParse(value.ToString(), out result) == false)
            {
                return defaultValue;
            }

            return result;
        }

        //Called with two-way data binding as value is pulled out of control and put back into the property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int defaultValue = 0;
            if (value == null)
            {
                return defaultValue;
            }

            return value;
        }
    }

    public class GuidValueConverter : IValueConverter
    {
        //Called when binding from an object property to a control property
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        //Called with two-way data binding as value is pulled out of control and put back into the property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Guid
                defaultValue =
                    Guid.Empty; //If property is required, you will need to implement a customer validation annotation to check for Guid.Empty
            if (value == null)
            {
                return defaultValue;
            }

            Guid result;
            if (Guid.TryParse(value.ToString(), out result))
            {
                return result;
            }

            return defaultValue;
        }
    }

    public class GuidNullableValueConverter : IValueConverter
    {
        //Called when binding from an object property to a control property
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        //Called with two-way data binding as value is pulled out of control and put back into the property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            Guid result;
            if (Guid.TryParse(value.ToString(), out result))
            {
                return result;
            }

            return null;
        }
    }
}