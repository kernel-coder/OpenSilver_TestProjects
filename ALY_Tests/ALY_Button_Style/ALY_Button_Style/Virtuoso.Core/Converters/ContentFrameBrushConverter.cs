#region Usings

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

#endregion

namespace Virtuoso.Core.Converters
{
    public class TherapyTaskFillColorConverter : IValueConverter
    {
        public SolidColorBrush DefaultColor { get; set; }
        public SolidColorBrush RedColor { get; set; }
        public SolidColorBrush YellowColor { get; set; }
        public SolidColorBrush GreenColor { get; set; }
        public SolidColorBrush PurpleColor { get; set; }

        public TherapyTaskFillColorConverter()
        {
            DefaultColor = new SolidColorBrush(Colors.Transparent);
            RedColor = new SolidColorBrush(Color.FromArgb(255, 212, 0, 184));
            YellowColor = new SolidColorBrush(Color.FromArgb(255, 255, 194, 15));
            GreenColor = new SolidColorBrush(Color.FromArgb(255, 163, 212, 0));
            PurpleColor = (SolidColorBrush)System.Windows.Application.Current.Resources["PurpleBrush"];
        }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var task = value as Virtuoso.Server.Data.Task;
            var _assessmentColor = value == null ? null : value.ToString();
            if (_assessmentColor != null)
            {
                if (!string.IsNullOrEmpty(_assessmentColor))
                {
                    if (_assessmentColor.ToLower().StartsWith("red")) //To find RED and RED30
                    {
                        return RedColor;
                    }

                    if (_assessmentColor.ToLower().StartsWith("yellow"))
                    {
                        return YellowColor;
                    }

                    if (_assessmentColor.ToLower().StartsWith("green"))
                    {
                        return GreenColor;
                    }

                    if (_assessmentColor.ToLower().StartsWith("purple"))
                    {
                        return PurpleColor;
                    }

                    return DefaultColor;
                }

                return DefaultColor;
            }

            return DefaultColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class TherapyTaskMessageVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var _assessmentColor = value?.ToString();
            if (_assessmentColor != null)
            {
                if (!string.IsNullOrEmpty(_assessmentColor))
                {
                    if (_assessmentColor.ToLower().StartsWith("red")) //To find RED and RED30
                    {
                        return Visibility.Visible;
                    }

                    if (_assessmentColor.ToLower().StartsWith("purple"))
                    {
                        return Visibility.Visible;
                    }

                    return Visibility.Collapsed;
                }

                return Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentFrameBrushConverter : IValueConverter
    {
        public object True { get; set; }

        public object False { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return ((bool)value) ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}