#region Usings

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Virtuoso.Core.Controls;

#endregion

namespace Virtuoso.Core.Converters
{
    public class BoolToOppositeBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            //CheckBox.IsChecked is Nullable<bool>
            //if (targetType != typeof(bool))
            //    throw new InvalidOperationException("The target must be a boolean");
            bool v = (value == null) ? false : (bool)value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return !(bool)value;
        }

        #endregion
    }

    //public class BoolTovLabelForceRequiredConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter,
    //        CultureInfo culture)
    //    {
    //        //vLabelForceRequired forcedRequired = vLabelForceRequired.No;

    //        //bool? v = value as bool?;

    //        //if (v.HasValue)
    //        //{
    //        //    if (v.Value)
    //        //    {
    //        //        forcedRequired = vLabelForceRequired.Yes;
    //        //    }
    //        //}

    //        return 0;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter,
    //        CultureInfo culture)
    //    {
    //        bool retVal = false;

    //        vLabelForceRequired? forceRequired = value as vLabelForceRequired?;

    //        if (forceRequired.HasValue)
    //        {
    //            if (forceRequired.Value == vLabelForceRequired.Yes)
    //            {
    //                retVal = false;
    //            }
    //        }

    //        return retVal;
    //    }

    //    #endregion
    //}


    public class ValueToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsNoVcodesSeverityEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            string sValue = value as string;
            if (string.IsNullOrWhiteSpace(sValue))
            {
                return false;
            }

            return (!sValue.Contains("V"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsNoEorVcodesSeverityEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            string sValue = value as string;
            if (string.IsNullOrWhiteSpace(sValue))
            {
                return false;
            }

            if (sValue != null && sValue.Contains("E"))
            {
                return false;
            }

            return sValue != null && (!sValue.Contains("V"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsNoVWXYorZcodesSeverityEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            string icd = value as string;
            if (string.IsNullOrWhiteSpace(icd))
            {
                return false;
            }

            if (icd.ToUpper().StartsWith("V") || icd.ToUpper().StartsWith("W") || icd.ToUpper().StartsWith("X") ||
                icd.ToUpper().StartsWith("Y") || icd.ToUpper().StartsWith("Z"))
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsICDEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            string sValue = value as string;
            return (!string.IsNullOrWhiteSpace(sValue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeValueToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class KeyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (int)value > 0)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class NullableBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var opposite = parameter?.Equals("opposite") ?? false;
            bool? boolValue = value as bool?;
            if (boolValue.HasValue == false)
            {
                return null;
            }

            return (opposite) ? !boolValue : boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var opposite = parameter?.Equals("opposite") ?? false;
            bool? boolValue = value as bool?;
            if (boolValue.HasValue == false)
            {
                return null;
            }

            return (opposite) ? !boolValue : boolValue;
        }
    }

    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (int)value > 0)
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeIntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (int)value > 0)
            {
                return false;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BoolToEitherParameterConverter : IValueConverter
    {
        //Parameter contains two values separated by a pipeline delimiter.
        //The first parameter is returned when converted value is true.
        //The delimiter separate the two parameters.
        //For instance: "truevalue|falsevalue"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] parameters = ParseParameters(parameter);
            if (parameters == null)
            {
                return null; //parameter is ill formed
            }

            if (value == null)
            {
                return null;
            }

            //Return first parameter if value is true otherwise return second parameter
            bool boolValue;
            bool.TryParse(value.ToString(), out boolValue);
            if (boolValue)
            {
                return parameters[0]; //no value to convert 
            }

            return parameters[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] parameters = ParseParameters(parameter);
            if (parameters == null)
            {
                return null; //parameter is ill formed
            }

            if (value == null)
            {
                return null; //no value to convert back
            }

            if (value.Equals(parameters[0]))
            {
                return true;
            }

            if (value.Equals(parameters[1]))
            {
                return false;
            }

            return null;
        }

        //"truevalue|falsevalue" is the parameter format expected
        private static string[] ParseParameters(object parameter)
        {
            if (parameter == null)
            {
                return null; //reject conversion if parameter not provided
            }

            var strParameter = parameter.ToString();
            if (string.IsNullOrEmpty(strParameter))
            {
                return null; //reject conversion if parameter not provided
            }

            var delimiterChar = '|';
            var parameters = strParameter.Split(delimiterChar);
            if (parameters.Length < 2)
            {
                return null; //reject conversion if parameter ill formed
            }

            return parameters;
        }
    }

    public class BoolToYesNoLiteralConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "No";
            }

            int? intValue = value as int?;
            if (intValue != null)
            {
                return (intValue == 0) ? "No" : "Yes";
            }

            bool? boolValue = value as bool?;
            return (boolValue == true) ? "Yes" : "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string stringValue = value as string;
            return (stringValue == "1") ? true : false;
        }
    }

    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            bool? boolValue = value as bool?;
            return (boolValue == true) ? "1" : "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string stringValue = value as string;
            return (stringValue == "1") ? true : false;
        }
    }

    public class StringTrueFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "False";
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "False";
            }

            return value;
        }
    }

    //public class IsDeltaAdminUser : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (WebContext.Current == null)
    //        {
    //            return false;
    //        }

    //        if (WebContext.Current.User == null)
    //        {
    //            return false;
    //        }

    //        return WebContext.Current.User.DeltaAdmin;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return null;
    //    }
    //}

    //public class IsNullValueOrDeltaAdminUser : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (value == null) || WebContext.Current.User.DeltaAdmin;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return null;
    //    }
    //}

    public class InErrorColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isErrors = (bool)value;
            Brush b = (isErrors)
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Color.FromArgb(0xFF, 0x50, 0x00,
                    0x4E)); //<Color x:Key="HighlightDarkColor">#FF50004E</Color>
            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GoalElementColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int goalStatus = (int)value;
            Brush b = new SolidColorBrush(Colors.Transparent);
            if (goalStatus == 1)
            {
                b = (SolidColorBrush)System.Windows.Application.Current.Resources["GoalElementPlannedColorBrush"];
            }

            if (goalStatus == 2)
            {
                b = (SolidColorBrush)System.Windows.Application.Current.Resources["GoalElementAddressedColorBrush"];
            }

            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AdmissionStatusCodeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush b = new SolidColorBrush(Colors.Transparent);
            string admissionStatusCode = value as string;
            if ((string.IsNullOrWhiteSpace(admissionStatusCode) == false) &&
                ((admissionStatusCode == "T") || (admissionStatusCode == "D")))
            {
                b = (SolidColorBrush)System.Windows.Application.Current.Resources["DischargedOrTransferredColorBrush"];
            }
            else
            {
                b = (SolidColorBrush)System.Windows.Application.Current.Resources["TextBrush"];
            }

            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}