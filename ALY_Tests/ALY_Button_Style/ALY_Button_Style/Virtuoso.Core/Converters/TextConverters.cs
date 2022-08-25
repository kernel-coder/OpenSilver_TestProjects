#region Usings

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class ZeroToNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string v = value as string;
            if (v == "0")
            {
                return null;
            }

            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string v = value as string;
            return (v == null) ? "0" : value;
        }
    }

    public class ViewEditLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "Edit";
            }

            bool isProtected = true;
            try
            {
                isProtected = System.Convert.ToBoolean(value);
            }
            catch
            {
            }

            return isProtected ? "View" : "Edit";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value.ToString().ToLower().Trim() == "View") ? true : false;
        }
    }

    public class HighlightBrushErrorColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush b = (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            if (value != null)
            {
                b = (Brush)System.Windows.Application.Current.Resources["ValidationSummaryBrush1"];
            }

            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ServiceLineGroupHeaderLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "Grouping Level <Unknown> ";
            }

            int? LevelNum = 0;
            try
            {
                LevelNum = value as int?;
            }
            catch
            {
            }

            return "Grouping Label " + LevelNum + " ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value.ToString().ToLower().Trim() == "View") ? true : false;
        }
    }

    public class MARDocumentedStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1 = administered     = HighlightBrush
            // 2 = not administered = RedBrush
            // 3 = untouched        = GreenBrush
            try
            {
                if (value.GetType() == typeof(MARDocumentState))
                {
                    var state = (MARDocumentState)value;
                    switch (state)
                    {
                        case MARDocumentState.Administered:
                            return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
                        case MARDocumentState.NotAdministered:
                            return (Brush)System.Windows.Application.Current.Resources["RedBrush"];
                        case MARDocumentState.UnTouched:
                            return (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
                        default:
                            return (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
                    }
                }

                return (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
            }
            catch
            {
                return (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FutureDateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? date = value as DateTime?;
            if (date == null)
            {
                return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            if (((DateTime)date).Date > DateTime.Today.Date)
            {
                return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            return (Brush)System.Windows.Application.Current.Resources["RedBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsInFiveDayWindowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isInFiveDayWindow = value as bool?;
            if ((isInFiveDayWindow == null) || (isInFiveDayWindow == false))
            {
                return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            return (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class HasICDChangedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? hasICDChanged = value as bool?;
            if ((hasICDChanged == null) || (hasICDChanged == false))
            {
                return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            return (Brush)System.Windows.Application.Current.Resources["ValidationBrush5"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OrdersTrackingPatientColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush b;
            Color c;
            int? status = value as int?;

            if (status.HasValue
                && ((CodeLookupCache.GetCodeFromKey(status.Value) == "D")
                    || (CodeLookupCache.GetCodeFromKey(status.Value) == "T")
                )
               )
            {
                c = Color.FromArgb(255, 255, 0, 0);
                b = new SolidColorBrush(c);
            }
            else
            {
                b = (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OrdersTrackingPhysicianColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c;
            int? status = value as int?;

            Brush b;
            if (status.HasValue && (CodeLookupCache.GetCodeFromKey(status.Value) == "Portal"))
            {
                c = Color.FromArgb(255, 200, 50, 200);
                b = new SolidColorBrush(c);
            }
            else
            {
                b = (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
            }

            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OrdersTrackingStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string statusString = "Unknown";
            int? status = value as int?;
            if (status.HasValue
                && OrdersTrackingHelpers.IsStatusValid(status.Value)
               )
            {
                statusString = OrdersTrackingHelpers.StatusDescription(status.Value);
            }

            return statusString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? statusInt = null;
            string statusString = value as string;

            foreach (int i in Enum.GetValues(typeof(OrdersTrackingStatus)))
                if (statusString == OrdersTrackingHelpers.StatusDescription(i))
                {
                    statusInt = i;
                    break;
                }

            return statusInt;
        }
    }

    public class OrdersTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string orderTypeString = "Unknown";
            int? orderType = value as int?;
            if (orderType.HasValue
                && OrdersTrackingHelpers.IsOrderTypeValid(orderType.Value)
               )
            {
                orderTypeString = OrdersTrackingHelpers.OrderTypeDescription(orderType.Value);
            }

            return orderTypeString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? orderTypeInt = null;
            string statusString = value as string;

            foreach (int i in Enum.GetValues(typeof(OrderTypesEnum)))
                if (statusString == OrdersTrackingHelpers.OrderTypeDescription(i))
                {
                    orderTypeInt = i;
                    break;
                }

            return orderTypeInt;
        }
    }

    public class NullToAllConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            if (strValue == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(strValue.Trim()))
            {
                return "All";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class DroppedLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? dropped = value as bool?;

            string ret = "Included";

            if (dropped.HasValue && dropped.Value)
            {
                ret = "Dropped";
            }

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InsVerStateLineRowCovnerter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            int retValue = 3;
            string val = value as string;

            if (string.IsNullOrEmpty(val))
            {
                retValue = 2;
            }

            return retValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return (null);
        }
    }

    public class UsingDiagnosisCodersLabel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool diagnosisCodersPostSignature = false;
            try
            {
                diagnosisCodersPostSignature = System.Convert.ToBoolean(value);
            }
            catch
            {
            }

            return "Using Diagnosis Coders" + ((diagnosisCodersPostSignature) ? " (post signature)" : "");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return (null);
        }
    }

    public class UsingHISCoordinatorLabel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool hisCoordinatorCanEdit = false;
            try
            {
                hisCoordinatorCanEdit = System.Convert.ToBoolean(value);
            }
            catch
            {
            }

            return "Using HIS Coordinators" + ((hisCoordinatorCanEdit) ? " (with edit)" : "");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return (null);
        }
    }

    public class UsingOASISCoordinatorLabel : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool oasisCoordinatorCanEdit = false;
            try
            {
                oasisCoordinatorCanEdit = System.Convert.ToBoolean(value);
            }
            catch
            {
            }

            return "Using OASIS Coordinators" + ((oasisCoordinatorCanEdit) ? " (with edit)" : "");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return (null);
        }
    }

    public class HISRFAToRFADescriptionLongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rfa = value as string;
            if (rfa == null)
            {
                return null;
            }

            return OasisCache.GetHISOasisSurveyRFADescriptionLongByRFA(rfa);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (null);
        }
    }

    public class OASISRFAToRFADescriptionLongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rfa = value as string;
            if (rfa == null)
            {
                return null;
            }

            return OasisCache.GetOASISOasisSurveyRFADescriptionLongByRFA(rfa);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (null);
        }
    }

    public class TunnelingDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string tunnelings = value as string;

            if (tunnelings == null)
            {
                return "";
            }

            string tunnelingDescription = null;
            if (string.IsNullOrWhiteSpace(tunnelings) == false)
            {
                string[] tunnelingsDelimiter = { "|" };
                string[] tunnelingPiecesDelimiter = { "@" };
                string[] tunnelingArray = tunnelings.Split(tunnelingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] tunnelingPiecesArray = null;
                if (tunnelingArray.Length != 0)
                {
                    foreach (string tunneling in tunnelingArray)
                        if (!string.IsNullOrEmpty(tunneling))
                        {
                            tunnelingPiecesArray = tunneling.Split(tunnelingPiecesDelimiter,
                                StringSplitOptions.RemoveEmptyEntries);
                            if (tunnelingPiecesArray.Length == 2)
                            {
                                if (tunnelingDescription != null)
                                {
                                    tunnelingDescription = tunnelingDescription + char.ToString('\r');
                                }

                                return tunnelingDescription + string.Format("At {0} o'clock with depth of {1} cm",
                                    tunnelingPiecesArray[0], tunnelingPiecesArray[1]);
                            }
                        }
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class UnderminingDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] underminingsDelimiter = { "|" };
            string[] underminingPiecesDelimiter = { "@" };

            string underminings = value as string;
            string underminingDescription = null;
            if (string.IsNullOrWhiteSpace(underminings) == false)
            {
                string[] underminingArray =
                    underminings.Split(underminingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
                if (underminingArray.Length != 0)
                {
                    foreach (string undermining in underminingArray)
                        if (!string.IsNullOrEmpty(undermining))
                        {
                            var underminingPiecesArray = undermining.Split(underminingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            if (underminingPiecesArray.Length == 4)
                            {
                                if (underminingDescription != null)
                                {
                                    underminingDescription = underminingDescription + char.ToString('\r');
                                }

                                if (underminingPiecesArray[0] == underminingPiecesArray[1])
                                {
                                    underminingDescription = underminingDescription +
                                                             (((underminingPiecesArray[2] == "?") ||
                                                               (underminingPiecesArray[3] == "?"))
                                                                 ? "Continuous undermining"
                                                                 : string.Format(
                                                                     "Continuous undermining with deepest {0} cm at {1} o'clock",
                                                                     underminingPiecesArray[2],
                                                                     underminingPiecesArray[3]));
                                }
                                else
                                {
                                    underminingDescription = underminingDescription +
                                                             (((underminingPiecesArray[2] == "?") ||
                                                               (underminingPiecesArray[3] == "?"))
                                                                 ? string.Format("From {0} to {1} o'clock",
                                                                     underminingPiecesArray[0],
                                                                     underminingPiecesArray[1])
                                                                 : string.Format(
                                                                     "From {0} to {1} o'clock with deepest {2} cm at {3} o'clock",
                                                                     underminingPiecesArray[0],
                                                                     underminingPiecesArray[1],
                                                                     underminingPiecesArray[2],
                                                                     underminingPiecesArray[3]));
                                }
                            }
                        }
                }
            }

            if (underminingDescription == null)
            {
                underminingDescription = "";
            }

            return underminingDescription;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class IsInErrorColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool? status = value as bool?;

            if (status.HasValue && (status == true))
            {
                return (Brush)System.Windows.Application.Current.Resources["ValidationSummaryBrush1"];
            }

            return (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}