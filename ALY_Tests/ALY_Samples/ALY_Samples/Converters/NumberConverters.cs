#region Usings

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class RealConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if ((float)value == 0)
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            float? f = null;

            if (value == null)
            {
                return f;
            }

            string strValue = value.ToString();
            try
            {
                f = float.Parse(strValue);
            }
            catch
            {
            }

            return f;
        }
    }

    public class RealConverterZero : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if ((float)value == 0)
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            float? f = null;

            if (value == null)
            {
                return f;
            }

            string strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue))
            {
                strValue = "0";
            }

            try
            {
                f = float.Parse(strValue);
            }
            catch
            {
            }

            return f;
        }
    }

    public class DistanceRealConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            float? f = null;

            if (value == null)
            {
                return f;
            }

            string strValue = value.ToString();
            try
            {
                f = float.Parse(strValue);
            }
            catch
            {
            }

            return f;
        }
    }

    public class RealConverterTwoDecimalPlaces : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if ((float)value == 0)
            {
                return null;
            }

            return (string.Format("{0:0.##}", (float)value));
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value;
        }
    }

    public class IntegerConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string strValue = value.ToString().Trim();
            if (string.IsNullOrEmpty(strValue))
            {
                return null;
            }

            if (strValue == "0")
            {
                return parameter;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int? i = null;

            if (value == null)
            {
                return i;
            }

            string strValue = value.ToString().Trim();
            try
            {
                i = Int32.Parse(strValue);
            }
            catch
            {
            }

            return i;
        }
    }

    public class IntegerConverterZero : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string strValue = value.ToString().Trim();
            if (string.IsNullOrEmpty(strValue))
            {
                return null;
            }

            if (strValue == "0")
            {
                return parameter;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int? i = null;

            if (value == null)
            {
                return i;
            }

            string strValue = value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(strValue))
            {
                strValue = "0";
            }

            try
            {
                i = Int32.Parse(strValue);
            }
            catch
            {
            }

            return i;
        }
    }

    public class DecimalConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string strValue = value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(strValue))
            {
                return null;
            }

            decimal result = 0;
            if (decimal.TryParse(strValue, out result) == false)
            {
                return null;
            }

            if (result == 0)
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            decimal? d = null;

            if (value == null)
            {
                return d;
            }

            string strValue = value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(strValue))
            {
                return null;
            }

            decimal result = 0;
            if (decimal.TryParse(strValue, out result) == false)
            {
                return null;
            }

            if (result == 0)
            {
                return null;
            }

            return value;
        }
    }

    public class ItemHeightConverter : IValueConverter
    {
        private double itemFudge = 15;

        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            double height = System.Convert.ToDouble(value);
            int count = (parameter == null) ? 10 : System.Convert.ToInt32(parameter);
            return (height / count) - itemFudge;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return (double)0;
        }
    }

    public class AdjustWidth : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int i = 0;
            string strValue = value.ToString().Trim();
            try
            {
                i = Int32.Parse(strValue);
            }
            catch
            {
            }

            int adjustment = 0;
            if (parameter != null)
            {
                string strParameter = parameter.ToString().Trim();
                try
                {
                    adjustment = Int32.Parse(strParameter);
                }
                catch
                {
                }
            }

            int r = i + adjustment;
            return (r <= 50) ? ((i <= 50) ? i : 50) : r;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return (double)0;
        }
    }

    public class AlertPageSize : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int i = 0;
            string strValue = value.ToString().Trim();
            try
            {
                i = Int32.Parse(strValue);
            }
            catch
            {
            }

            int rowheight = 40;
            if (parameter != null)
            {
                string strParameter = parameter.ToString().Trim();
                try
                {
                    rowheight = Int32.Parse(strParameter);
                }
                catch
                {
                }
            }

            int pagesize = (i - 50) / rowheight; // 50 = static column header
            return (pagesize <= 1) ? 1 : pagesize;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return (double)0;
        }
    }


    public class LengthToWidthConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            long len, maxwidth, width;
            try
            {
                len = Int64.Parse(value.ToString());
            }
            catch
            {
                len = 1;
            }

            try
            {
                maxwidth = Int64.Parse(parameter.ToString());
            }
            catch
            {
                maxwidth = 300;
            }

            if (len < 2)
            {
                width = len * 24;
            }

            if (len < 3)
            {
                width = len * 23;
            }

            if (len < 4)
            {
                width = len * 22;
            }
            else if (len < 8)
            {
                width = len * 21;
            }
            else if (len < 13)
            {
                width = len * 20;
            }
            else if (len < 21)
            {
                width = len * 15;
            }
            else
            {
                width = len * 7;
            }

            if (width > maxwidth)
            {
                width = maxwidth;
            }

            return width;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return parameter;
        }
    }

    public class ColumnWidthConverter : IValueConverter
    {
        // Parameter comes in the formath x.y where x is the number of total columns and y is the number of columns the data should span evenly
        // if only x is passed in, assume 1 column.
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            float retValue = 1;
            float beginValue;
            float columnSpan;
            float totalColumns;
            String splitChar = ".";
            totalColumns = 1;
            columnSpan = 1;

            string strValue = value.ToString().Trim();
            try
            {
                beginValue = float.Parse(strValue);
            }
            catch
            {
                beginValue = 1;
            }

            strValue = parameter.ToString().Trim();
            string[] strParms = strValue.Split(splitChar.ToCharArray());

            // parse out the number of columns
            if (strParms.Length > 0)
            {
                try
                {
                    totalColumns = float.Parse(strParms[0]);
                }
                catch
                {
                    totalColumns = 1;
                }
            }

            // parse out the number of columns to span.
            if (strParms.Length > 1)
            {
                try
                {
                    columnSpan = float.Parse(strParms[1]);
                }
                catch
                {
                    columnSpan = 1;
                }
            }

            // do the math
            try
            {
                if (totalColumns > 0)
                {
                    retValue = beginValue / totalColumns * columnSpan;
                }
            }
            catch
            {
                retValue = 1;
            }

            return retValue;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return parameter;
        }
    }

    public class EmptyStringToNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null || string.IsNullOrEmpty(value.ToString()) ? null : value;
        }
    }

    public class HyphenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string inputStr = (string)value;
            if (!string.IsNullOrEmpty(inputStr))
            {
                int pos = 2;
                if (parameter != null)
                {
                    try
                    {
                        pos = System.Convert.ToInt16(parameter);
                    }
                    catch
                    {
                        // no paramter passed in or invalid passed in so we'll just use the default
                    }
                }

                int remain = inputStr.Length - pos;
                Regex re = new Regex(@".*?(\w{" + pos + @"}).*?(\w{" + remain + @"})");
                if (inputStr.Length > 2)
                {
                    return re.Replace(inputStr, "$1-$2");
                }

                return inputStr;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string phone = (string)value;
            phone = phone.Replace(".", "");
            return phone;
        }
    }

    public class GridHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool isSelected = (bool)value;
            string rowHeight = "0";

            if (isSelected)
            {
                rowHeight = "*";
            }

            return rowHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return (null);
        }
    }
}