#region Usings

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class DebugDataBindingConverter : IValueConverter
    {
        /*
         * Step 1 - add XMLNS to XAML
         *    xmlns:VirtuosoCoreConverters="clr-namespace:Virtuoso.Core.Converters;assembly=Virtuoso.Core"
         *
         * Step 2 - add resource to XAML
         *   <Grid.Resources>
         *     <VirtuosoCoreConverters:DebugDataBindingConverter x:Key="DebugBinding"/>
         *   </Grid.Resources>
         *
         * Step 3 - add converter to binding
         *   Example:
         *     <VirtuosoCoreControls:vAsyncComboBox 
         *            x:Name="AddressComboBox"
         *            Grid.Column="1"
         *            Grid.Row="1"
         *            Margin="8,4,8,4"
         *            ItemTemplate="{StaticResource PhysicianAddressDataTemplate}"                                                                                                            
         *            ItemsSource="{Binding PhysicianAddresses}"
         *            SelectedValue="{Binding SelectedItem.PhysicianAddressKey, Mode=TwoWay, ValidatesOnNotifyDataErrors=True, NotifyOnValidationError=True, Converter={StaticResource DebugBinding}}"/>
        */
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }
}