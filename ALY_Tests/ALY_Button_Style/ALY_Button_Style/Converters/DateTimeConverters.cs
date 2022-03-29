#region Usings

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class DateTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DateTime validDate;
                if (DateTime.TryParse(value.ToString(), out validDate))
                {
                    return validDate;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }

    public class DateTimeOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DateTimeOffset? dt = value as DateTimeOffset?;
                if (dt == null)
                {
                    return null;
                }

                return new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute,
                    dt.Value.Second);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = value as DateTime?;
            if (dt == null)
            {
                return null;
            }

            var ret = new DateTimeOffset(dt.Value, DateTimeOffset.Now.Offset);
            return ret;
        }
    }

    public class DateTimeOffsetToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                DateTimeOffset? dt = value as DateTimeOffset?;
                if (dt == null)
                {
                    return null;
                }

                var t = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute,
                    dt.Value.Second);

                return t.TimeOfDay;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = value as DateTime?;
            if (dt == null)
            {
                return null;
            }

            var ret = new DateTimeOffset(dt.Value, DateTimeOffset.Now.Offset);
            return ret;
        }
    }

    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTime date;

                try
                {
                    date = (DateTime)value;
                }
                catch
                {
                    date = ((DateTimeOffset)value).DateTime;
                }

                //if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                    //return date.ToString("HHmm");
                }

                return date.ToShortTimeString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTime date;

                try
                {
                    date = (DateTime)value;
                }
                catch
                {
                    date = ((DateTimeOffset)value).DateTime;
                }

                //if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
                {
                   // return date.ToShortDateString() + " " + date.ToString("HHmm");
                }

                return date.ToShortDateString() + " " + date.ToShortTimeString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FormatTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime)
            {
               // return new CustomTimeFormat("HHmm");
            }

            return new ShortTimeFormat();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    
    public class DateTimeMinValueToNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = value as DateTime?;
            if (dt == null)
            {
                return null;
            }

            if (dt == DateTime.MinValue)
            {
                return null;
            }

            return dt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return DateTime.MinValue;
            }

            return value;
        }
    }

    public class DateTimeSQLMinValueToNullConverter : IValueConverter
    {
        private static readonly DateTime minSQLDateTime = new DateTime(1753, 1, 1);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? dt = value as DateTime?;
            if (dt == null)
            {
                return null;
            }

            if (dt == DateTime.MinValue)
            {
                return null;
            }

            if (dt == minSQLDateTime)
            {
                return null;
            }

            return dt;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return minSQLDateTime;
            }

            return value;
        }
    }

    public class SOCDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? date = (DateTime?)value;
            return (date == null) ? "SOC: Undefined" : "SOC: " + ((DateTime)date).ToShortDateString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CertPeriodForDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // The date to lookup is the value and the list of CertificationPeriods is the parameter
            String ReturnString = "";
            if (value == null)
            {
                return ReturnString;
            }

            ////AdmissionCertification acReturn = (AdmissionCertification)value;

            //if (acReturn.PeriodStartDate != null)
            //{
            //    ReturnString = ((DateTime)acReturn.PeriodStartDate).ToShortDateString();
            //}

            //ReturnString = ReturnString + " Thru ";
            //if (acReturn.PeriodEndDate != null)
            //{
            //    ReturnString = ReturnString + ((DateTime)acReturn.PeriodEndDate).ToShortDateString();
            //}

            return ReturnString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ShortDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTime date;

                try
                {
                    date = (DateTime)value;
                }
                catch
                {
                    date = ((DateTimeOffset)value).DateTime;
                }

                if (date == DateTime.MinValue)
                {
                    return "";
                }

                return date.ToShortDateString().Trim();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}