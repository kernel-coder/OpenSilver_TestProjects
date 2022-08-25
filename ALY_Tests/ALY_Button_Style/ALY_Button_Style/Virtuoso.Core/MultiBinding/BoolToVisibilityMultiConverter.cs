#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

#endregion

namespace Virtuoso.Core.MultiBinding
{
    public class BoolToVisibilityMultiConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (values == null)
            {
                return null;
            }

            var nonNullValues = values.Where(v => v != null).ToArray();
            if (nonNullValues.Any() == false)
            {
                return Visibility.Collapsed; //Is not visible if list is empty
            }

            if (parameter == null)
            {
                parameter = "VisibleWhenAllTrue";
            }

            bool IsVisible = false;
            switch (parameter.ToString())
            {
                case "VisibleWhenAllFalse":
                    IsVisible = AllFalse(nonNullValues);
                    break;
                case "VisibleWhenAllTrue":
                    IsVisible = AllTrue(nonNullValues);
                    break;
                case "VisibleWhenAnyTrue":
                    IsVisible = AnyTrue(nonNullValues);
                    break;
                case "VisibleWhenAnyFalse":
                    IsVisible = AnyFalse(nonNullValues);
                    break;
                default:
                    IsVisible = AllTrue(nonNullValues);
                    break;
            }


            return IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static bool AllTrue(object[] values)
        {
            bool IsVisible = false;
            IsVisible = values.Where(v => v != null).ToList().All(v =>
            {
                var boolValue = false;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && boolValue;
            });
            return IsVisible;
        }

        public static bool AnyTrue(object[] values)
        {
            bool IsVisible = false;
            IsVisible = values.Where(v => v != null).ToList().Any(v =>
            {
                var boolValue = false;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && boolValue;
            });
            return IsVisible;
        }

        public static bool AllFalse(object[] values)
        {
            var isVisible = false;
            isVisible = values.Where(v => v != null).ToList().All(v =>
            {
                var boolValue = true;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && !boolValue;
            });
            return isVisible;
        }

        public static bool AnyFalse(object[] values)
        {
            bool IsVisible = false;
            IsVisible = values.Where(v => v != null).ToList().Any(v =>
            {
                var boolValue = false;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && !boolValue;
            });
            return IsVisible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override Object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}