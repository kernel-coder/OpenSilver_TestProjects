#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class GraphItem : VirtuosoEntity
    {
        private Encounter _encounter;
        public Encounter Encounter => _encounter;

        private DateTime _readingDateTime;
        public DateTime ReadingDateTime => _readingDateTime;

        private string _thumbnail;
        public string ReadingDataPointThumbNail => _thumbnail;

        private float? _numericValueAlt;
        public float? ReadingNumericAlt => _numericValueAlt;

        private float? _numericValue;
        public float? ReadingNumeric => _numericValue;

        private bool _useMilitaryTime;

        public bool IsTeleMonitor { get; set; }

        public int ReadingBPSystolic => (int)ReadingNumeric.GetValueOrDefault();
        public int ReadingBPDiastolic => (int)ReadingNumericAlt.GetValueOrDefault();

        public GraphItem()
        {
            _useMilitaryTime = false;
            _encounter = null;
            _numericValue = null;
            _numericValueAlt = null;
            _readingDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            _thumbnail = string.Empty;
            IsTeleMonitor = false;
        }

        private GraphItem(bool useMilitaryTime, Encounter encounter, DateTime? readingDateTime, string thumbNail)
        {
            _useMilitaryTime = useMilitaryTime;
            _encounter = encounter;
            _numericValue = null;
            _numericValueAlt = null;
            _thumbnail = thumbNail ?? string.Empty;

            if (readingDateTime.HasValue)
            {
                _readingDateTime = readingDateTime.Value;
            }
            else
            {
                _readingDateTime = ((encounter == null) || (encounter.EncounterOrTaskStartDateAndTime == null))
                    ? DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                    : encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
            }

            IsTeleMonitor = encounter?.EncounterIsVisitTeleMonitoring ?? false;
        }

        public GraphItem(bool useMilitaryTime, Encounter encounter, DateTime? readingDateTime, string thumbNail,
            int? value)
            : this(useMilitaryTime, encounter, readingDateTime, thumbNail)
        {
            _numericValue = value;
        }

        public GraphItem(bool useMilitaryTime, Encounter encounter, DateTime? readingDateTime, string thumbNail,
            int? value1, int? value2)
            : this(useMilitaryTime, encounter, readingDateTime, thumbNail, value1)
        {
            _numericValueAlt = value2;
        }

        public GraphItem(bool useMilitaryTime, Encounter encounter, DateTime? readingDateTime, string thumbNail,
            float? value)
            : this(useMilitaryTime, encounter, readingDateTime, thumbNail)
        {
            _numericValue = value;
        }

        public string ReadingDataPointThumbNailWithDateTime
        {
            get
            {
                string timeDisplay = string.Format("{0} {1}",
                    ReadingDateTime.ToShortDateString(),
                    (_useMilitaryTime ? ReadingDateTime.ToString("HHmm") : ReadingDateTime.ToShortTimeString()));

                return timeDisplay + "<LineBreak />" + ReadingDataPointThumbNail +
                       (IsTeleMonitor ? "<LineBreak />TeleMonitor" : string.Empty);
            }
        }

        public static bool UsesMilitaryTime => TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
    }

    public class GraphItemSourceMinimumDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<GraphItem> list = value as List<GraphItem>;
            if (list != null && list.Any())
            {
                return list.Min(m => m.ReadingDateTime).Date;
            }

            return DateTime.Now.Date;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GraphItemSourceMaximumDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<GraphItem> list = value as List<GraphItem>;
            if (list != null && list.Any())
            {
                return list.Max(m => m.ReadingDateTime).AddDays(1).Date;
            }

            return DateTime.Now.AddDays(1).Date;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GraphItemSourceMinimumBP : IValueConverter
    {
        // Note: ReadingBPSystolic and ReadingBPDiastolic are separate Line Series on same graph - need min of either of those two values
        // Note: parameter should be interval

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int interval = 20;
            if (parameter != null && string.IsNullOrWhiteSpace(parameter.ToString()) == false)
            {
                int __result = interval;
                if (Int32.TryParse(parameter.ToString(), out __result))
                {
                    interval = __result;
                }
            }

            List<GraphItem> list = value as List<GraphItem>;
            if (list != null && list.Any())
            {
                var min = list.Min(m => Math.Min(m.ReadingBPSystolic, m.ReadingBPDiastolic));
                min -= interval;
                if (min < 0)
                {
                    min = 0;
                }

                return min;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GraphItemSourceMaximumBP : IValueConverter
    {
        // Note: ReadingBPSystolic and ReadingBPDiastolic are separate Line Series on same graph - need max of either of those two values
        // Note: parameter should be interval

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int interval = 20;
            if (parameter != null && string.IsNullOrWhiteSpace(parameter.ToString()) == false)
            {
                int __result = interval;
                if (Int32.TryParse(parameter.ToString(), out __result))
                {
                    interval = __result;
                }
            }

            List<GraphItem> list = value as List<GraphItem>;
            if (list != null && list.Any())
            {
                var max = list.Max(m => Math.Max(m.ReadingBPSystolic, m.ReadingBPDiastolic));
                max += interval;
                return max;
            }

            return 200;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class TelemonitorGraphItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<GraphItem> list = value as List<GraphItem>;
            bool telemonitorItems =
                Boolean.Parse(parameter.ToString()); //You must specify true or false for ConverterParameter
            var ret = list.Where(g => g.IsTeleMonitor == telemonitorItems).ToList();
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GraphItemSourceIntervalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<GraphItem> list = value as List<GraphItem>;

            int x = 1;

            if ((list != null) && (list.Count > 1))
            {
                var fd = list.Min(o => o.ReadingDateTime);
                var ld = list.Max(o => o.ReadingDateTime);
                TimeSpan ts = ld.Subtract(fd);
                if (ts.Days > 1)
                {
                    x = (ts.Days - 1) / 10 + 1;
                }
            }

            return x;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class TelemonitorLegendVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = false;
            List<GraphItem> list = value as List<GraphItem>;

            if (list != null)
            {
                result = list.Any(i => i.IsTeleMonitor);
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}