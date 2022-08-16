#region Usings

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class MedicationFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // MedicationStatus 0=current=Normal, 1=future=Italic, 2=discontinued=Italic
            if (value == null)
            {
                return FontStyles.Normal;
            }

            int i = 0;
            try
            {
                i = Int32.Parse(value.ToString().Trim());
            }
            catch
            {
            }

            return (i == 0) ? FontStyles.Normal : FontStyles.Italic;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MedicationFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // MedicationStatus 0=current=Normal, 1=future=Medium, 2=discontinued=Light
            if (value == null)
            {
                return FontWeights.Normal;
            }

            int i = 0;
            try
            {
                i = Int32.Parse(value.ToString().Trim());
            }
            catch
            {
            }

            return (i == 0) ? FontWeights.Normal : (i == 1) ? FontWeights.Normal : FontWeights.Thin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MedicationOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // MedicationStatus 0=current=Normal, 1=future=Medium, 2=discontinued=Light
            if (value == null)
            {
                return 1.0;
            }

            int i = 0;
            try
            {
                i = Int32.Parse(value.ToString().Trim());
            }
            catch
            {
            }

            return (i == 0) ? 1.0 : (i == 1) ? 0.7 : 0.4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class PatientDemographicsFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // MedicationStatus 0=current=Normal, 1=future=Medium, 2=discontinued=Light
            if (value == null)
            {
                return FontWeights.Normal;
            }

            bool isPatientDemographics = (bool)value;
            return (isPatientDemographics) ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}