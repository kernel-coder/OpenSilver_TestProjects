#region Usings

using System;
using System.Globalization;
using System.Windows.Data;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Core.Converters
{
    public class ComboBoxItemNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        return null;
                    }

                    return 0;
                }
                catch
                {
                    return 0;
                }
            }

            if (value is Int32)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        if ((int)value <= 0)
                        {
                            return null;
                        }
                    }
                }
                catch
                {
                }
            }
            else if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        return null;
                    }

                    return 0;
                }
                catch
                {
                    return 0;
                }
            }

            return value;
        }
    }

    public class ComboBoxItemNullAllowMinusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        return null;
                    }

                    return 0;
                }
                catch
                {
                    return 0;
                }
            }

            if (value is Int32)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        if ((int)value == 0)
                        {
                            return null;
                        }

                        if ((int)value <= -2)
                        {
                            return null;
                        }
                        // fell thru on positives and -1 (both are allowable)
                    }
                }
                catch
                {
                }
            }
            else if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        return null;
                    }

                    return 0;
                }
                catch
                {
                    return 0;
                }
            }

            return value;
        }
    }

    public class ComboBoxItemNullConverterToZero : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null) ? 0 : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        return null;
                    }
                }
                catch
                {
                    return 0;
                }
            }
            else if (value is Int32)
            {
                try
                {
                    if (targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        if ((int)value <= 0)
                        {
                            return null;
                        }
                    }
                }
                catch
                {
                }
            }

            return value;
        }
    }

    public class ServiceTypeMultiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String retString = "";
            String CertTasks = value as String;
            String splitChar = "|";

            String[] tasks = CertTasks.Split(splitChar[0]);
            int stKey;
            foreach (String s in tasks)
                try
                {
                    stKey = System.Convert.ToInt32(s);
                    retString = ServiceTypeCache.GetDescriptionFromKey(stKey) + " - ";
                }
                catch
                {
                }

            return retString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InsCoorWorklistStatusKeyToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int status = 0;
            if (value != null)
            {
                status = (int)value;
            }

            string ret = " ";
            switch (status)
            {
                case 0:
                    ret = " ";
                    break;
                case 1:
                    ret = "In Process";
                    break;
                case 2:
                    ret = "Return to Work List";
                    break;
                case 3:
                    ret = "Processed";
                    break;
            }

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = "";
            if (value != null)
            {
                status = (string)value;
            }

            int ret = 0;
            switch (status)
            {
                case " ":
                    ret = 0;
                    break;
                case "In Process":
                    ret = 1;
                    break;
                case "Return to Work List":
                    ret = 2;
                    break;
                case "Processed":
                    ret = 3;
                    break;
            }

            return ret;
        }
    }
}