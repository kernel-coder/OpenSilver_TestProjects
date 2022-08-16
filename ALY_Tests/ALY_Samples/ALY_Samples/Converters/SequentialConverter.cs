#region Usings

using System;
using System.Collections.Generic;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class SequentialConverter : List<IValueConverter>, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object returnValue = value;

            foreach (IValueConverter converter in this)
                returnValue = converter.Convert(returnValue, targetType, parameter, culture);

            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //Linkable value converters
    //
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class NegateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    //public class IsDeltaAdminOverrideConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return (WebContext.Current.User.DeltaAdmin) || (bool)value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}
}