#region Usings

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Core.Converters
{
    public class DataTemplateConverter : IValueConverter
    {
        public IResourceDictionary _AppResourceDictionary { get; set; }

        private ResourceDictionary ControlResourceDictionary;
        private bool initialized;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                //Would be preferrable to do this in constuctor, but cannot, because this object exists in App.xaml and is constructed
                //before VirtuosoAppService - which sets up the MEF container for the application...
                if (initialized == false)
                {
                    _AppResourceDictionary = VirtuosoContainer.Current.GetExport<IResourceDictionary>().Value;
                    initialized = true;
                }

                ControlResourceDictionary = _AppResourceDictionary.CurrentResourceDictionary;

                // Change up the binding so that you can use value to determine the data template that gets loaded. 
                string datatype = value.ToString();

                DataTemplate dataTemplate = ControlResourceDictionary[datatype] as DataTemplate;
                var loadedTemplate = dataTemplate?.LoadContent();
                return loadedTemplate;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RehabTemplateConverter : IValueConverter
    {
        public IResourceDictionary _AppResourceDictionary { get; set; }

        private ResourceDictionary ControlResourceDictionary;
        private bool initialized;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            //Would be preferrable to do this in constuctor, but cannot, because this object exists in App.xaml and is constructed
            //before VirtuosoAppService - which sets up the MEF container for the application...
            if (initialized == false)
            {
                _AppResourceDictionary = VirtuosoContainer.Current.GetExport<IResourceDictionary>().Value;
                initialized = true;
            }

            ControlResourceDictionary = _AppResourceDictionary.CurrentResourceDictionary;

            // Change up the binding so that you can use value to determine the data template that gets loaded. 
            string datatype = value + "Rehab";

            DataTemplate dataTemplate = ControlResourceDictionary[datatype] as DataTemplate;
            var loadedTemplate = dataTemplate?.LoadContent();
            return loadedTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DataTemplateMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                double d = System.Convert.ToDouble(value) * 25;
                return new Thickness(d, 0, 0, 0);
            }

            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}