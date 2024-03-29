﻿#region Usings

using System;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class ServiceLineTypeCheckCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            int mask = ConvertParameter(parameter.ToString());
            int chk = (int)value;

            return (chk & mask) > 0;
        }

        private int ConvertParameter(string parameter)
        {
            switch (parameter)
            {
                //case "eServiceLineType.HomeCare":
                //    return (int)eServiceLineType.HomeCare;
                //case "eServiceLineType.HomeHealth":
                //    return (int)eServiceLineType.HomeHealth;
                //case "eServiceLineType.Hospice":
                //    return (int)eServiceLineType.Hospice;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            int mask = ConvertParameter(parameter.ToString());
            string result = mask.ToString();
            return ((bool)value && result != "0") ? result : null;
        }
    }
}