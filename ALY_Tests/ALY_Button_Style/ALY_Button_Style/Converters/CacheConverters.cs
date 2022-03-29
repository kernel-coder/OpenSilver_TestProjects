#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Converters
{
    public class UserCacheGetFullNameFromUserIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Guid? userID = value as Guid?;
            if (userID == null)
            {
                return null;
            }

            if (userID == Guid.Empty)
            {
                return "Delta User";
            }

            return UserCache.Current.GetFullNameFromUserId(userID);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class VisibilityAttendingPhysicianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? fk = System.Convert.ToInt32(value);

            int count = FacilityCache.GetFacilityBranches(fk).Count();
            if (count == 0)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class UserCacheGetNameWithSuffixFromUserIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Guid? userID = value as Guid?;
            if (userID == null)
            {
                return null;
            }

            return UserCache.Current.GetFullNameWithSuffixFromUserId(userID);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class UserCacheGetFormalNameFromUserIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Guid? userID = value as Guid?;
            if (userID == null)
            {
                return null;
            }

            return UserCache.Current.GetFormalNameFromUserId(userID);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupHeaderDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return "CodeLookupHeaderDescription Error: No paramater passed";
            }

            return CodeLookupCache.GetCodeLookupHeaderDescriptionFromType(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            if (parameter == null)
            {
                return "CodeLookupConverter Error: No paramater passed";
            }

            string[] parameterSplit = parameter.ToString().Split('.');
            string codeType = parameterSplit[0];
            string codeOrDescription = (parameterSplit.Length > 1) ? parameterSplit[1].ToLower() : "code";
            string code = (string.Compare("code", codeOrDescription) == 0)
                ? CodeLookupCache.GetCodeFromKey(codeType, codeKey)
                : CodeLookupCache.GetCodeDescriptionFromKey(codeType, codeKey);
            if (code == null)
            {
                List<CodeLookup> cls = CodeLookupCache.GetCodeLookupsFromType(codeType);
                if (cls == null)
                {
                    return String.Format("CodeLookupConverter Error: {0} not defined", codeType);
                }

                CodeLookup cl = CodeLookupCache.GetCodeLookupFromKey(codeKey);
                if (cl == null)
                {
                    return String.Format("CodeLookupConverter Error: {0} key={1} not defined", codeType,
                        codeKey.ToString());
                }
            }

            return code;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupCodeFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            return CodeLookupCache.GetCodeFromKey(codeKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceLineGroupingDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int slgKey = System.Convert.ToInt32(value);
                if (slgKey <= 0)
                {
                    return null;
                }

                return ServiceLineCache.GetServiceLineGroupingDescriptionFromKey(slgKey);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceLineGroupingLabelFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int slKey = System.Convert.ToInt32(value);
                if (slKey <= 0)
                {
                    return null;
                }

                ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(slKey);
                return sl?.ServiceLineGroupingLabel;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceLineNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int slKey = System.Convert.ToInt32(value);
                if (slKey <= 0)
                {
                    return null;
                }

                ServiceLine sl = ServiceLineCache.GetServiceLineFromKey(slKey);
                return sl?.Name;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceLineGroupingNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int slgKey = System.Convert.ToInt32(value);
                if (slgKey <= 0)
                {
                    return null;
                }

                ServiceLineGrouping slg = ServiceLineCache.GetServiceLineGroupingFromKey(slgKey);
                return slg?.Name;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceLineGroupingLabelAndNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int slgKey = System.Convert.ToInt32(value);
                if (slgKey <= 0)
                {
                    return null;
                }

                ServiceLineGrouping slg = ServiceLineCache.GetServiceLineGroupingFromKey(slgKey);
                return slg?.LabelAndName;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int codeKey = System.Convert.ToInt32(value);
                if (codeKey <= 0)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeDescriptionFromKey(codeKey);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupDescriptionFromCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            string code = value.ToString().Trim();

            if (parameter == null)
            {
                return "CodeLookupDescriptionFromCodeConverter Error: No paramater passed";
            }

            if (parameter.ToString().Trim() == "")
            {
                return "CodeLookupDescriptionFromCodeConverter Error: No paramater passed";
            }

            string codeType = parameter.ToString().Trim();

            string codeDescription = CodeLookupCache.GetDescriptionFromCode(codeType, code);
            if (codeDescription == null)
            {
                List<CodeLookup> cls = CodeLookupCache.GetCodeLookupsFromType(codeType);
                if (cls == null)
                {
                    return String.Format("CodeLookupDescriptionFromCodeConverter Error: {0} not defined", codeType);
                }
            }

            return codeDescription;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class CodeLookupToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string codeType = value.ToString();
            if (codeType.Trim() == "")
            {
                return null;
            }

            string tooltip = string.Empty;
            List<CodeLookup> cls = CodeLookupCache.GetCodeLookupsFromType(codeType: codeType, sequence: 1000);
            foreach (var item in cls)
                tooltip += (string.IsNullOrEmpty(tooltip) ? "" : "</Paragraph><Paragraph>") + item.CodeDescription;

            return tooltip;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class EquipmentDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            return EquipmentCache.GetDescriptionFromKey(codeKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class NonServiceTypeDescFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int nonServiceTypeKey = System.Convert.ToInt32(value);
            if (nonServiceTypeKey <= 0)
            {
                return null;
            }

            return NonServiceTypeCache.GetNonServiceTypeDescFromKey(nonServiceTypeKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class SupplyDescription1FromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            return SupplyCache.GetSupplyDescription1FromKey(codeKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class VendorNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            var v = VendorCache.GetVendorFromKey(codeKey);
            return v?.VendorName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ServiceTypeDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            return ServiceTypeCache.GetDescriptionFromKey(codeKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DisciplineDescriptionFromServiceTypeKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            int? DiscKey = ServiceTypeCache.GetDisciplineKey(codeKey);
            return DiscKey == null ? "" : DisciplineCache.GetDescriptionFromKey((int)DiscKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class InsuranceNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int insuranceKey = System.Convert.ToInt32(value);
            if (insuranceKey <= 0)
            {
                return null;
            }

            return InsuranceCache.GetInsuranceNameFromKey(insuranceKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class PhysicianNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int physicianKey = System.Convert.ToInt32(value);
            if (physicianKey <= 0)
            {
                return null;
            }

            return PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(physicianKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class FacilityPatientIDLabelFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            Admission a = value as Admission;
            if (a == null)
            {
                return null;
            }

            if (a.FacilityKey.HasValue && a.FacilityKey.Value > 0)
            {
                return FacilityCache.GetPatientIDLabelFromKey(a.FacilityKey.Value, parameter);
            }

            if (a.CurrentReferral != null && a.CurrentReferral.FacilityKey.HasValue &&
                a.CurrentReferral.FacilityKey.Value > 0)
            {
                return FacilityCache.GetPatientIDLabelFromKey(a.CurrentReferral.FacilityKey, parameter);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class FacilityPatientIDLabelFromKeyVisibilityConverterNull : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            Admission a = value as Admission;
            if (a == null)
            {
                return null;
            }

            if (a.FacilityKey.HasValue && a.FacilityKey.Value > 0)
            {
                return (string.IsNullOrEmpty(FacilityCache.GetPatientIDLabelFromKey(a.FacilityKey.Value, parameter))
                    ? Visibility.Collapsed
                    : Visibility.Visible);
            }

            if (a.CurrentReferral != null && a.CurrentReferral.FacilityKey.HasValue &&
                a.CurrentReferral.FacilityKey.Value > 0)
            {
                return (string.IsNullOrEmpty(
                    FacilityCache.GetPatientIDLabelFromKey(a.CurrentReferral.FacilityKey, parameter))
                    ? Visibility.Collapsed
                    : Visibility.Visible);
            }

            return null;
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

    public class EmployerNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            Employer e = EmployerCache.GetEmployerFromKey(codeKey);
            if (e == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(e.EmployerName))
            {
                return null;
            }

            return e.EmployerName.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class FacilityNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            Facility f = FacilityCache.GetFacilityFromKey(codeKey);
            if (f == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(f.Name))
            {
                return null;
            }

            return f.Name.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class FacilityBranchNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int codeKey = System.Convert.ToInt32(value);
            if (codeKey <= 0)
            {
                return null;
            }

            FacilityBranch f = FacilityCache.GetFacilityBranchFromKey(codeKey);
            if (f == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(f.BranchName))
            {
                return null;
            }

            return f.BranchName.Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class TenantSettingsICDVersionVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return TenantSettingsCache.Current.TenantSettingICDVersionVisible;
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

    public class TenantSettingsICDVersionModeVisibility : IValueConverter
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

            if (value.ToString().Trim() == "")
            {
                return Visibility.Collapsed;
            }

            if (value.ToString().Trim() == "Both")
            {
                return Visibility.Collapsed;
            }

            return TenantSettingsCache.Current.TenantSettingICDVersionVisible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return null;
        }
    }

    public class TenantSettingsEnvelopeWindowLeftVisibility : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return (TenantSettingsCache.Current.TenantSettingIsEnvelopeWindowLeft)
                ? Visibility.Visible
                : Visibility.Collapsed;
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

    public class TenantSettingsEnvelopeWindowRightVisibility : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return (TenantSettingsCache.Current.TenantSettingIsEnvelopeWindowRight)
                ? Visibility.Visible
                : Visibility.Collapsed;
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

    public class DisciplineDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int dscpKey = System.Convert.ToInt32(value);
            if (dscpKey <= 0)
            {
                return null;
            }

            return DisciplineCache.GetDescriptionFromKey(dscpKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DisciplineCodeFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int dscpKey = System.Convert.ToInt32(value);
            if (dscpKey <= 0)
            {
                return null;
            }

            return DisciplineCache.GetDescriptionFromKey(dscpKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class RoleNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int rolekey = System.Convert.ToInt32(value);
            if (rolekey <= 0)
            {
                return null;
            }

            return RoleCache.GetRoleNameFromKey(rolekey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class QuestionLabelFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int qKey = System.Convert.ToInt32(value);
            if (qKey <= 0)
            {
                return null;
            }

            return DynamicFormCache.GetQuestionByKey(qKey).Label;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class QuestionKeyFromLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            return DynamicFormCache.GetQuestionKeyByLabel(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class QuestionTemplateFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int qKey = System.Convert.ToInt32(value);
            if (qKey <= 0)
            {
                return null;
            }

            return DynamicFormCache.GetQuestionByKey(qKey).DataTemplate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class PatientInsuranceNameAndNumberFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            Admission a = value as Admission;
            if (a == null)
            {
                return null;
            }

            int? key = a.PatientInsuranceKey;
            if (key == null)
            {
                key = 0;
            }

            if (a.Patient == null)
            {
                return null;
            }

            if (a.Patient.PatientInsurance == null)
            {
                return null;
            }

            PatientInsurance pi = a.Patient.PatientInsurance.FirstOrDefault(i => i.PatientInsuranceKey == key);
            if (pi == null)
            {
                return null;
            }

            return pi.NameAndNumber;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DocumentDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var task = value as Task;
            if (task != null)
            {
                if (task.NonServiceTypeKey.HasValue)
                {
                    var nst = NonServiceTypeCache.GetNonServiceTypeDescFromKey(task.NonServiceTypeKey);
                    if (String.IsNullOrEmpty(nst))
                    {
                        return String.Format("<NO Description for activity code: {0}>", task.NonServiceTypeKey.Value);
                    }

                    return nst;
                }

                if ((task.PatientKey.HasValue) && (task.IsAttempted))
                {
                    return "Attempted Visit";
                }

                if ((task.PatientKey.HasValue) && (task.ServiceTypeKey.HasValue))
                {
                    var sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(task.ServiceTypeKey.Value);
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<NO Description for service type: {0}>", task.ServiceTypeKey.Value);
                    }

                    return sd;
                }

                return "<No description for task activity>";
            }

            var doc = value as DocumentationItem;
            if (doc != null)
            {
                if (doc.ServiceTypeKey.HasValue)
                {
                    string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(doc.ServiceTypeKey.Value);
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<No description for encounter service type: {0}>",
                            doc.ServiceTypeKey.Value);
                    }

                    return sd;
                }
            }
            else
            {
                var addoc = value as AdmissionDocumentationItem;
                if (addoc != null)
                {
                    if (!string.IsNullOrWhiteSpace(addoc.DocumentationFileName))
                    {
                        return addoc.DocumentationFileName;
                    }

                    string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(addoc.ServiceTypeKey.Value);
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<No description for encounter service type: {0}>",
                            doc.ServiceTypeKey.Value);
                    }

                    return sd;
                }
            }

            return "<No description.>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DocumentDescriptionPlusSuffix : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var task = value as Task;
            if (task != null)
            {
                if (task.NonServiceTypeKey.HasValue)
                {
                    var cd = NonServiceTypeCache.GetNonServiceTypeDescFromKey(task.NonServiceTypeKey.Value);
                    if (String.IsNullOrEmpty(cd))
                    {
                        return String.Format("<NO Description for activity code: {0}>", task.NonServiceTypeKey.Value);
                    }

                    return cd;
                }

                if ((task.PatientKey.HasValue) && (task.ServiceTypeKey.HasValue))
                {
                    string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(task.ServiceTypeKey.Value);
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<NO Description for service type: {0}>", task.ServiceTypeKey.Value);
                    }

                    if (string.IsNullOrWhiteSpace(task.AssessmentColor) == false)
                    {
                        var _color = task.AssessmentColor.ToLower();
                        var _count = task.AssessmentVisitNumber.GetValueOrDefault().ToString();
                        var _showCount = false;
                        if (!_color.StartsWith("red") && !_color.StartsWith("purple"))
                        {
                            _showCount = true;
                        }

                        if (_showCount)
                        {
                            return String.Format("{0} {1} ({2})", sd, task.TaskSuffix, _count);
                        }

                        return String.Format("{0} {1}*", sd, task.TaskSuffix);
                    }

                    return sd + " " + task.TaskSuffix;
                }

                return "<No description for task activity>";
            }

            var doc = value as DocumentationItem;
            if (doc != null)
            {
                if (doc.AttachedFormKey.HasValue)
                {
                    string sd = DynamicFormCache.GetFormByKey((int)doc.AttachedFormKey).Description;
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<No description for form: {0}>", doc.AttachedFormKey.Value);
                    }
                }

                if (doc.ServiceTypeKey.HasValue)
                {
                    string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(doc.ServiceTypeKey.Value);
                    if (String.IsNullOrWhiteSpace(sd))
                    {
                        return String.Format("<No description for encounter service type: {0}>",
                            doc.ServiceTypeKey.Value);
                    }

                    return sd + ((doc.Encounter == null) ? "" : " " + doc.Encounter.EncounterSuffix);
                }
            }
            else
            {
                var addoc = value as AdmissionDocumentationItem;
                if (addoc != null)
                {
                    if (!string.IsNullOrWhiteSpace(addoc.DocumentationFileName))
                    {
                        return addoc.DocumentationFileName;
                    }

                    if (addoc.AttachedFormKey.HasValue)
                    {
                        return addoc.AttachedFormDescription;
                    }

                    string sd = ServiceTypeCache.GetDescriptionFromKeyWithOasisOverride(addoc.ServiceTypeKey.Value);
                    return sd + ((addoc.Encounter == null) ? null : " " + addoc.Encounter.EncounterSuffix);
                }
            }

            return "<No description.>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DocumentDescriptionLine2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var task = value as Task;
            if (task != null)
            {
                return null;
            }

            var doc = value as AdmissionDocumentationItem;
            if (doc != null)
            {
                if (doc.Encounter != null)
                {
                    string startDesc = "(Supervised : ";
                    if (doc.Encounter.EncounterSupervision != null && doc.Encounter.EncounterSupervision.Any())
                    {
                        var desc = startDesc;
                        foreach (var sup in doc.Encounter.EncounterSupervision.Where(d => d.DisciplineKey > 0))
                        {
                            if (desc != startDesc)
                            {
                                desc = desc + ", ";
                            }

                            desc = desc + DisciplineCache.GetDisciplineFromKey(sup.DisciplineKey)
                                .SupervisedServiceTypeLabel;
                        }

                        desc = desc + ")";
                        return desc;
                    }

                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class AlertDisplayNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            int alertKey = System.Convert.ToInt32(value);
            if (alertKey <= 0)
            {
                return null;
            }

            return SystemExceptionsAlertCache.GetDisplayNameFromKey(alertKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class ReferralSourceDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            //"other" responses won't have a valid key so just pass back the text
            try
            {
                int codeKey = System.Convert.ToInt32(value);
                if (codeKey <= 0)
                {
                    return null;
                }

                return ReferralSourceCache.GetReferralSourceDescFromKey(codeKey);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DocumentationFormDescriptionFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (value.ToString().Trim() == "")
            {
                return null;
            }

            try
            {
                int codeKey = System.Convert.ToInt32(value);
                if (codeKey <= 0)
                {
                    return null;
                }

                return DynamicFormCache.GetAdmissionDocumentationFormTypeDescriptionByKey(codeKey);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }
}