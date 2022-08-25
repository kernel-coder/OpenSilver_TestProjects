#region Usings

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Converters
{
    public class BoolOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return 0;
            }

            bool b = (bool)value;
            if (b)
            {
                return 1;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BranchVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = (string)value;
            if (type != null && type.ToLower() == "branch office")
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ReEvaluateVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = (string)value;
            if (type != null && type.ToLower() == "re-evaluate")
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibilityToNullableBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (((Visibility)value) == Visibility.Visible);
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool?)
            {
                return (((bool?)value) == true ? Visibility.Visible : Visibility.Collapsed);
            }

            if (value is bool)
            {
                return (((bool?)value) == true ? Visibility.Visible : Visibility.Collapsed);
            }

            return false;
        }
    }

    public class VisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var visibility = Visibility.Collapsed;
            if (value == null)
            {
                return visibility;
            }

            bool isVisible;
            if (Boolean.TryParse(value.ToString(), out isVisible))
            {
                if (isVisible)
                {
                    visibility = Visibility.Visible;
                }
            }

            return visibility;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class OppositeVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            bool visibility = (bool)value;
            return visibility ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return visibility != Visibility.Visible;
        }
    }

    public class VisibilityConverterCount : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            int count = System.Convert.ToInt32(value);
            return (count <= 0) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Collapsed) ? 0 : 1;
        }
    }

    public class OppositeVisibilityConverterCount : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Visible;
            }

            int count = System.Convert.ToInt32(value);
            return (count == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Collapsed) ? 1 : 0;
        }
    }

    public class VisibilityConverterNull : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string data = (value == null) ? null : value.ToString();
            data = string.IsNullOrEmpty(data) ? null : data.Trim();
            if (parameter == null)
            {
                return (string.IsNullOrEmpty(data) ? Visibility.Collapsed : Visibility.Visible);
            }

            return (string.IsNullOrEmpty(data) ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class AddAsteriskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "* " + value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibilityConverterNullOrZero : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string data = (value == null) ? null : value.ToString();
            data = string.IsNullOrEmpty(data) ? null : data.Trim();
            bool isNullOrEmptyOrZero = (string.IsNullOrEmpty(data)) ? true : (data == "0") ? true : false;
            if (parameter == null)
            {
                return ((isNullOrEmptyOrZero) ? Visibility.Collapsed : Visibility.Visible);
            }

            return ((isNullOrEmptyOrZero) ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class VisibilityConverterNullOrZeroOrNegative : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string data = (value == null) ? null : value.ToString();
            data = string.IsNullOrEmpty(data) ? null : data.Trim();
            bool isNullOrEmptyOrZero = (string.IsNullOrEmpty(data)) ? true : (data == "0") ? true : false;
            int dataValue = 0;
            try
            {
                dataValue = System.Convert.ToInt32(data);
            }
            catch
            {
            }

            bool isNullEmptyZeroOrNegative = isNullOrEmptyOrZero || dataValue < 0;
            if (parameter == null)
            {
                return ((isNullEmptyZeroOrNegative) ? Visibility.Collapsed : Visibility.Visible);
            }

            return ((isNullEmptyZeroOrNegative) ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class VisibilityConverterIsUTEST : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TenantSettingsCache.Current.TenantSettingIsUTEST) ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public sealed class InvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility mode = (Visibility)value;
            return (mode == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility mode = (Visibility)value;
            return (mode == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed);
        }

        private static Visibility GetVisibilityMode(object parameter)
        {
            // Default to Visible
            Visibility mode = Visibility.Visible;

            // If a parameter is specified, then we'll try to understand it as a Visibility value
            if (parameter != null)
            {
                // If it's already a Visibility value, then just use it
                if (parameter is Visibility)
                {
                    mode = (Visibility)parameter;
                }
                else
                {
                    // Let's try to parse the parameter as a Visibility value, throwing an exception when the parsing fails
                    try
                    {
                        mode = (Visibility)Enum.Parse(typeof(Visibility), parameter.ToString(), true);
                    }
                    catch (FormatException e)
                    {
                        throw new FormatException(
                            "Invalid Visibility specified as the ConverterParameter.  Use Visible or Collapsed.", e);
                    }
                }
            }

            // Return the detected mode
            return mode;
        }
    }

    public class CountVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            int? count = value as int?;
            if (count == null)
            {
                return Visibility.Collapsed;
            }

            if (parameter == null)
            {
                return (count > 0) ? Visibility.Visible : Visibility.Collapsed;
            }

            int minCount = int.Parse(parameter.ToString());
            return (count > minCount) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ConditionalVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (parameter == null)
            {
                return Visibility.Visible;
            }

            string data = value.ToString().ToLower();
            string target = parameter.ToString().ToLower();

            if (string.IsNullOrEmpty(data))
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            string[] targets = target.Split('|');
            Array.Sort(targets);
            if (Array.BinarySearch(targets, data) >= 0)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeConditionalVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (parameter == null)
            {
                return Visibility.Visible;
            }

            string data = ((string)value).ToLower();
            string target = ((string)parameter).ToLower();

            if (string.IsNullOrEmpty(data))
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            string[] targets = target.Split('|');
            foreach (var item in targets)
                if ((item.EndsWith("%") && data.StartsWith(item.Replace('%', ' ')) || item.Equals(data)))
                {
                    return Visibility.Collapsed;
                }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeConditionalCodeLookupOnKeyVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (parameter == null)
            {
                return Visibility.Visible;
            }

            string data = "";
            int? insKey = value as int?;
            if (insKey.HasValue)
            {
                data = CodeLookupCache.GetCodeFromKey(insKey);
                if (data == null)
                {
                    data = "";
                }
            }

            data = data.ToLower();
            string target = ((string)parameter).ToLower();

            if (string.IsNullOrEmpty(data))
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            string[] targets = target.Split('|');
            foreach (var item in targets)
                if ((item.EndsWith("%") && data.StartsWith(item.Replace("%", "")) || item.Equals(data)))
                {
                    return Visibility.Collapsed;
                }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupConditionalVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            int key = (int)value;
            string target = (string)parameter;
            if (key <= 0) //if (key == 0)
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            if (target.Equals(CodeLookupCache.GetCodeFromKey(key), StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeCodeLookupConditionalVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            int key = 0;
            try
            {
                key = System.Convert.ToInt32(value.ToString());
            }
            catch
            {
            }

            string target = (string)parameter;
            if (key <= 0) //if (key == 0)
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            if (target.Equals(CodeLookupCache.GetCodeFromKey(key), StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeCodeLookupConditionalVisibility2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Visible;
            }

            int key = 0;
            try
            {
                key = System.Convert.ToInt32(value.ToString());
            }
            catch
            {
            }

            string target = (string)parameter;
            if (key <= 0)
            {
                return Visibility.Visible;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            if (target.Equals(CodeLookupCache.GetCodeFromKey(key), StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupConditionalVisibilityOnCode : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            CodeLookup cl = value as CodeLookup;
            int key = 0;
            try
            {
                key = (cl == null) ? System.Convert.ToInt32(value.ToString()) : cl.CodeLookupKey;
            }
            catch
            {
            }

            string target = (string)parameter;
            if (key <= 0)
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            string[] targets = target.ToLower().Split('|');
            for (int i = 1; i < targets.Length; i++)
                if (targets[i].Equals(CodeLookupCache.GetCodeFromKey(targets[0], key),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
                }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ConditionalVisibilityOnCode : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            string code = null;
            try
            {
                code = value.ToString();
            }
            catch
            {
            }

            string target = (string)parameter;
            if (string.IsNullOrWhiteSpace(code))
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(target))
            {
                return Visibility.Visible;
            }

            string[] targets = target.ToLower().Split('|');
            for (int i = 1; i < targets.Length; i++)
                if (targets[i].Equals(code, StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
                }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ViewModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string data = value.ToString();
            if (string.IsNullOrEmpty(data))
            {
                return Visibility.Collapsed;
            }

            if (data.ToLower().Equals("viewmode"))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OppositeViewModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string data = value.ToString();
            if (string.IsNullOrEmpty(data))
            {
                return Visibility.Collapsed;
            }

            if (data.ToLower().Equals("viewmode"))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsDemoEnvironmentVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.IsDemoEnvironment
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ContractServiceProviderVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.ContractServiceProvider
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class UsingSHPIntegratedAlertsVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool use = (TenantSettingsCache.Current.TenantSetting.UsingSHPIntegratedAlerts == null)
                ? false
                : (bool)TenantSettingsCache.Current.TenantSetting.UsingSHPIntegratedAlerts;
            return (use) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class UsingPPSPlusVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.UsingPPSPlus ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedMediSpanVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedMediSpan
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedHospiceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSettingPurchasedHospice
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedHomeHealthVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSettingPurchasedHomeHealth
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedHomeCareVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSettingPurchasedHomeCare
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ShowServiceLineTypeOptionsVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool HasMultiple = TenantSettingsCache.Current.TenantSettingHasMultipleServiceLineTypeOptions;
            return HasMultiple ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ServiceLineTypeIsHospiceOnlyVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? h = null;
            if (value != null)
            {
                try
                {
                    h = Int32.Parse(value.ToString());
                }
                catch
                {
                    h = null;
                }
            }

            return (h == 4) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ServiceLineTypeIncludesHospiceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? h = null;
            if (value != null)
            {
                try
                {
                    h = Int32.Parse(value.ToString());
                }
                catch
                {
                    h = null;
                }
            }

            return (h & 4) == 4 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedTeleMonitoringVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedTeleMonitoring
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FormMaintenanceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedFormMaint
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AssessmentMaintenanceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedAssessmentMaint
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ReportMaintenanceVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedReportMaint
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return RoleAccessHelper.CheckPermission(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessPurchasedInsuranceEligibilityVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (RoleAccessHelper.CheckPermission(parameter.ToString()))
            {
                if (TenantSettingsCache.Current.TenantSetting.PurchasedInsuranceEligibility)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessPurchasedHospiceEligibilityVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (RoleAccessHelper.CheckPermission(parameter.ToString()))
            {
                if (TenantSettingsCache.Current.TenantSettingIsHomeHealthOnly == false)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return RoleAccessHelper.CheckPermission(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessPlusIntVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(value.ToString()))
            {
                return Visibility.Collapsed;
            }

            if (System.Convert.ToInt32(value) <= 0)
            {
                return Visibility.Collapsed;
            }

            return RoleAccessHelper.CheckPermission(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessPlusBoolVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!System.Convert.ToBoolean(value))
            {
                return Visibility.Collapsed;
            }

            return RoleAccessHelper.CheckPermission(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RoleAccessPlusOppositeBoolVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value))
            {
                return Visibility.Collapsed;
            }

            return RoleAccessHelper.CheckPermission(parameter.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsDeltaAdminUserVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (WebContext.Current.User.DeltaAdmin) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PurchasedCrescendoConnectVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSetting.PurchasedCrescendoConnect
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsLoggedInUser : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Guid selecteduser = new Guid(value.ToString());

            return selecteduser.Equals(WebContext.Current.User.MemberID) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class VisibilityConverterOasisAlertTypeChanged : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EncounterOasisAlert eoa = value as EncounterOasisAlert;
            if (eoa == null)
            {
                return Visibility.Collapsed;
            }

            if (eoa.Encounter == null)
            {
                return Visibility.Collapsed;
            }

            if (eoa.Encounter.EncounterOasisAlert == null)
            {
                return Visibility.Collapsed;
            }

            EncounterOasisAlert eoaPrev = eoa.Encounter.EncounterOasisAlert
                .Where(a => (a.OasisAlertSequence < eoa.OasisAlertSequence))
                .OrderByDescending(a => (a.OasisAlertSequence)).FirstOrDefault();
            if (eoaPrev == null)
            {
                return Visibility.Visible;
            }

            return (eoa.OasisAlertType.Equals(eoaPrev.OasisAlertType)) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class VisibilityConverterOasisAlertDomainChanged : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EncounterOasisAlert eoa = value as EncounterOasisAlert;
            if (eoa == null)
            {
                return Visibility.Collapsed;
            }

            if (eoa.Encounter == null)
            {
                return Visibility.Collapsed;
            }

            if (eoa.Encounter.EncounterOasisAlert == null)
            {
                return Visibility.Collapsed;
            }

            EncounterOasisAlert eoaPrev = eoa.Encounter.EncounterOasisAlert
                .Where(a => (a.OasisAlertSequence < eoa.OasisAlertSequence))
                .OrderByDescending(a => (a.OasisAlertSequence)).FirstOrDefault();
            if (eoaPrev == null)
            {
                return Visibility.Visible;
            }

            return (eoa.OasisAlertDomain.Equals(eoaPrev.OasisAlertDomain)) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IsGroupOwnerVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return (WebContext.Current.User.GroupOwner) ? Visibility.Visible : Visibility.Collapsed;
            }

            return (WebContext.Current.User.GroupOwner) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PrintCollectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tmpList = value as IEnumerable;
            if (tmpList == null)
            {
                return Visibility.Collapsed;
            }

            if (tmpList.Cast<object>().Any() == false)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class OppositePrintCollectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tmpList = value as IEnumerable;
            if (tmpList == null)
            {
                return Visibility.Visible;
            }

            if (tmpList.Cast<object>().Any() == false)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class VisibilityToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var MyVis = (Visibility)value;
                if (MyVis == Visibility.Visible)
                {
                    return true;
                }

                return false;
            }
            catch
            {
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class StringVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }

    public class InsuranceTypeStateCodeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = Visibility.Collapsed;
            int? insKey = value as int?;
            if (insKey.HasValue)
            {
                string insType = CodeLookupCache.GetCodeDescriptionFromKey(insKey);
                if (insType.ToLower().Contains("medicaid"))
                {
                    vis = Visibility.Visible;
                }
            }

            return vis;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class VisibleForMedicareOrMedicaid : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = Visibility.Collapsed;
            int? insKey = value as int?;
            if (insKey.HasValue)
            {
                Insurance ins = InsuranceCache.GetInsuranceFromKey(insKey);

                if (ins != null)
                {
                    string insType = CodeLookupCache.GetCodeDescriptionFromKey(ins.InsuranceType);

                    if (insType.ToLower().Contains("medicare")
                        || insType.ToLower().Contains("medicaid")
                       )
                    {
                        vis = Visibility.Visible;
                    }
                }
            }

            return vis;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            return (visibility == Visibility.Visible);
        }
    }

    public class PhysicianServicesVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var obj = value as Admission.DisciplineGroups;
            var visibility = Boolean.Parse(parameter.ToString());

            switch (obj.CurrentAdmissionDiscipline.AdmissionDisciplineHCFACode)
            {
                case "P": //Hopice Physician Services
                    return (visibility) ? Visibility.Visible : Visibility.Collapsed;
                default:
                    return (visibility) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}