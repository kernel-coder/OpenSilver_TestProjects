#region Usings

using System;
using System.Linq;
using System.Windows.Markup;

#endregion

namespace Virtuoso.Core.MultiBinding
{
    public class BooleanOperationsMultiBindingConverter : MarkupExtension, IMultiValueConverter
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
                return false; //false if list is empty
            }

            if (parameter == null)
            {
                parameter = "TrueWhenAllTrue";
            }

            bool boolResult = false;
            switch (parameter.ToString())
            {
                case "FalseWhenAllFalse":
                    boolResult = AllFalse(nonNullValues);
                    break;
                case "TrueWhenAllTrue":
                    boolResult = AllTrue(nonNullValues);
                    break;
                case "TrueWhenAnyTrue":
                    boolResult = AnyTrue(nonNullValues);
                    break;
                case "FalseWhenAnyFalse":
                    boolResult = AnyFalse(nonNullValues);
                    break;
                default:
                    boolResult = AllTrue(nonNullValues);
                    break;
            }


            return boolResult;
        }

        public static bool AllTrue(object[] values)
        {
            bool boolResult = false;
            boolResult = values.Where(v => v != null).ToList().All(v =>
            {
                bool boolValue;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && boolValue;
            });
            return boolResult;
        }

        public static bool AnyTrue(object[] values)
        {
            bool boolResult = false;
            boolResult = values.Where(v => v != null).ToList().Any(v =>
            {
                bool boolValue;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && boolValue;
            });
            return boolResult;
        }

        public static bool AllFalse(object[] values)
        {
            var boolResult = false;
            boolResult = values.Where(v => v != null).ToList().All(v =>
            {
                bool boolValue;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && !boolValue;
            });
            return boolResult;
        }

        public static bool AnyFalse(object[] values)
        {
            bool boolResult = false;
            boolResult = values.Where(v => v != null).ToList().Any(v =>
            {
                bool boolValue;
                var isBool = bool.TryParse(v.ToString(), out boolValue);
                return isBool && !boolValue;
            });
            return boolResult;
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