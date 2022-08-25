#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Model;
using Virtuoso.Server.Data;

#endregion

//using System.Diagnostics;

namespace Virtuoso.Core.Converters
{
    public static class ItemSourceHelpers<E>
    {
        public static List<E> GetCollectionViewList(object value)
        {
            if (value == null)
            {
                return null;
            }

            List<E> list = new List<E>();
            try
            {
                foreach (E pd in value as ICollectionView) list.Add(pd);
            }
            catch
            {
            }

            return (list == null) ? null : (list.Any() == false) ? null : list;
        }
        
        public static List<AdmissionDiagnosis> GetCurrentDiagnosisCollectionViewList(object value)
        {
            List<AdmissionDiagnosis> pd = ItemSourceHelpers<AdmissionDiagnosis>.GetCollectionViewList(value);
            if (pd == null)
            {
                return null;
            }

            List<AdmissionDiagnosis> list = pd.Where(i => (i.DiagnosisStatus == 0)).ToList();
            return (list == null) ? null : (list.Any() == false) ? null : list;
        }

        public static List<AdmissionDiagnosis> GetDiagnosisCollectionViewList(object value)
        {
            List<AdmissionDiagnosis> pd = ItemSourceHelpers<AdmissionDiagnosis>.GetCollectionViewList(value);
            if (pd == null)
            {
                return null;
            }

            List<AdmissionDiagnosis> list = pd.ToList();
            return (list == null) ? null : (list.Any() == false) ? null : list;
        }
    }

    public class CodeTypeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var ret = CodeLookupCache.Current.Context.CodeLookupHeaders.OrderBy(p => p.CodeType).ToList();
                bool includeEmpty = System.Convert.ToBoolean(parameter);
                if (includeEmpty)
                {
                    ret.Insert(0,
                        new CodeLookupHeader { CodeType = " ", CodeTypeDescription = " ", CodeLookupHeaderKey = 0 });
                }

                return ret;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    //Bind to the CodeLookupHeader.CodeType
    public class CodeLookupHeaderItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return CodeLookupCache.GetCodeLookupHeaders();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value != null) || (parameter != null))
            {
                var _code = value ?? parameter;
                var tmpCodes = CodeLookupCache.GetCodeLookupsFromType(_code.ToString(), false, false,
                    System.Convert.ToBoolean(parameter));
                return tmpCodes;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupItemSourcePlusMeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return null;
            }

            string pString = parameter.ToString().Trim();
            if (string.IsNullOrWhiteSpace(pString))
            {
                return null;
            }

            string[] pList = pString.Split('|');
            if (pList == null)
            {
                return null;
            }

            if (pList.Length == 0)
            {
                return null;
            }

            string codeType = pList[0].Trim();
            if (string.IsNullOrWhiteSpace(codeType))
            {
                return null;
            }

            bool includeEmpty = false;
            if (pList.Length > 1)
            {
                try
                {
                    includeEmpty = System.Convert.ToBoolean(pList[1].Trim());
                }
                catch
                {
                }
            }

            int plusMeKey = 0;
            try
            {
                plusMeKey = System.Convert.ToInt32(value);
            }
            catch
            {
            }

            List<CodeLookup> tmpCodes =
                CodeLookupCache.GetCodeLookupsFromType(codeType, false, false, includeEmpty, null, plusMeKey);
            return tmpCodes;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupQuestionUIItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            QuestionBase qui = value as QuestionBase;
            if (qui != null)
            {
                int? plusMeKey = qui.EncounterData?.IntData;
                if (plusMeKey == null)
                {
                    plusMeKey = 0;
                }

                if (!string.IsNullOrWhiteSpace(qui.Question.LookupType))
                {
                    return CodeLookupCache.GetCodeLookupsFromType(qui.Question.LookupType, false, false, !qui.Required,
                        null, (int)plusMeKey);
                }

                if (parameter != null)
                {
                    return CodeLookupCache.GetCodeLookupsFromType(parameter.ToString(), false, false, false, null,
                        (int)plusMeKey);
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupMultiItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<CodeLookup> items = null;
            string codeTypeString = parameter.ToString().Trim();
            var codeTypes = codeTypeString.Split('|');
            foreach (var codeType in codeTypes)
            {
                var lst = CodeLookupCache.GetCodeLookupsFromType(codeType);
                if (items == null)
                {
                    items = lst;
                }
                else
                {
                    items = items.Concat(lst);
                }
            }

            if (items != null)
            {
                return items;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupRequiredToIncludeNullItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool required = System.Convert.ToBoolean(value);
            return required ? "False" : "True";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CodeLookupTextToFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return CodeLookupFormat.Description;
            }

            string format = value.ToString();
            if (string.IsNullOrWhiteSpace(format))
            {
                return "Description";
            }

            format = format.Trim().ToLower();
            if (format.Equals("code"))
            {
                return CodeLookupFormat.Code;
            }

            if (format.Equals("codedashdescription"))
            {
                return CodeLookupFormat.CodeDashDescription;
            }

            return CodeLookupFormat.Description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EquipmentItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return EquipmentCache.GetEquipmentByType(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EmployerItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return EmployerCache.GetActiveEmployers(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GoalElementResponseTypeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CodeLookupCache.GetGoalElementResponseTypes();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FacilityItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FacilityCache.GetActiveFacilities(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FacilityBranchItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FacilityCache.GetActiveBranches();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FacilityTypeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CodeLookupCache.GetCodeLookupsFromType("FacilityType");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ReferralSourceItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ReferralSourceCache.GetReferralSources(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class GoalItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GoalCache.GetActiveGoals(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PhysicianItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PhysicianCache.Current.GetActivePhysicians(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DisciplineItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DisciplineCache.GetActiveDisciplines(System.Convert.ToBoolean(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ServiceTypeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int || value is long)
            {
                return ServiceTypeCache.GetServiceTypesFilterByLikeFormAndAssistant(
                    System.Convert.ToInt32(value)); //STs for DynamicForm
            }

            return ServiceTypeCache.GetActiveServiceTypes(); //return service types for DSCP maint.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ActiveServiceTypesPlusMeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int || value is long)
            {
                return ServiceTypeCache.GetActiveServiceTypesPlusMe(System.Convert.ToInt32(value));
            }

            return ServiceTypeCache.GetActiveServiceTypes(); //return service types for DSCP maint.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InsuranceItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return InsuranceCache.GetActiveInsurances();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class UserItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UserCache.Current.GetUsers();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class UserByDisciplineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return UserCache.Current.GetUserProfileByDisciplineKey((int)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AdmissionDiagnosisPrimaryItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<AdmissionDiagnosis> pd =
                ItemSourceHelpers<AdmissionDiagnosis>.GetCurrentDiagnosisCollectionViewList(value);
            if (pd == null)
            {
                return null;
            }

            if (pd.Count() == 1)
            {
                return pd;
            }

            int min_sequence = 0;
            try
            {
                min_sequence = pd.Min(i => i.Sequence);
            }
            catch
            {
            }

            List<AdmissionDiagnosis> list = pd.Where(i => (i.Sequence == min_sequence)).ToList();
            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AdmissionDiagnosisSecondaryItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<AdmissionDiagnosis> pd =
                ItemSourceHelpers<AdmissionDiagnosis>.GetCurrentDiagnosisCollectionViewList(value);
            if (pd == null)
            {
                return null;
            }

            if (pd.Count() < 2)
            {
                return null;
            }

            int min_sequence = 0;
            try
            {
                min_sequence = pd.Min(i => i.Sequence);
            }
            catch
            {
            }

            List<AdmissionDiagnosis> list = pd.Where(i => (i.Sequence != min_sequence)).OrderBy(i => i.Sequence)
                .ToList();
            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AdmissionDiagnosisSecondaryVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            List<AdmissionDiagnosis> pd =
                ItemSourceHelpers<AdmissionDiagnosis>.GetCurrentDiagnosisCollectionViewList(value);
            if (pd == null)
            {
                return Visibility.Collapsed;
            }

            if (pd.Any() == false)
            {
                return Visibility.Collapsed;
            }

            int min_sequence = 0;
            try
            {
                min_sequence = pd.Min(i => i.Sequence);
            }
            catch
            {
            }

            List<AdmissionDiagnosis> list = pd.Where(i => (i.Sequence != min_sequence)).ToList();
            return (list == null) ? Visibility.Collapsed :
                (list.Any() == false) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }

    public class AdmissionDiagnosisSurgicalVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            List<AdmissionDiagnosis> pd = ItemSourceHelpers<AdmissionDiagnosis>.GetDiagnosisCollectionViewList(value);
            if (pd == null)
            {
                return Visibility.Collapsed;
            }

            if (pd.Any() == false)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Visibility.Collapsed;
        }
    }

    public class AdmissionDisciplineLookupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            AdmissionDiscipline a = value as AdmissionDiscipline;
            if (a == null)
            {
                return null;
            }

            int? key = a.DisciplineKey;
            if (key == null)
            {
                key = 0;
            }

            int DiscKey = a.DisciplineKey;

            return DiscKey;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ActiveAdmissionDisciplinePlusMeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            QuestionBase qui = value as QuestionBase;
            if (qui != null)
            {
                int? plusMeKey = qui.EncounterData?.IntData;
                if (plusMeKey == null)
                {
                    plusMeKey = 0;
                }

                List<AdmissionDiscipline> ad = qui.Admission.AdmissionDiscipline
                    .Where(p => (!p.DischargeDateTime.HasValue && !p.NotTakenDateTime.HasValue) ||
                                p.AdmissionDisciplineKey == plusMeKey)
                    .OrderBy(p => p.ReferDateTime).ToList();

                ad.Insert(0, new AdmissionDiscipline { DisciplineKey = -1, AdmissionDisciplineKey = 0 });

                return ad;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InsuranceThatRequireAuthorizationsPlusMeItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            QuestionBase qui = value as QuestionBase;
            if (qui != null)
            {
                int? plusMeKey = qui.EncounterData?.IntData;
                if (plusMeKey == null)
                {
                    plusMeKey = 0;
                }

                List<AdmissionAuthorization> ad = qui.Admission.AdmissionAuthorization
                    .Where(aa =>
                        aa.PatientInsurance.Insurance.Authorizations || (aa.AdmissionAuthorizationKey == plusMeKey))
                    .ToList();

                ad.Insert(0, new AdmissionAuthorization { AdmissionAuthorizationKey = 0, PatientInsuranceKey = 0 });

                return ad;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ServiceTypeNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return ServiceTypeCache.GetDescriptionFromKey((int)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class DisciplineNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "<General>";
            }

            string description = DisciplineCache.GetDescriptionFromKey((int)value);
            return (string.IsNullOrWhiteSpace(description)) ? "<General>" : description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class GenAuthCountKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            ObservableCollection<GeneralAuthCounts> ac = (value) as ObservableCollection<GeneralAuthCounts>;

            List<GeneralAuthCounts> accc = ac.ToList();
            return accc.AsEnumerable();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class AuthCountKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            ObservableCollection<AuthCounts> ac = (value) as ObservableCollection<AuthCounts>;

            List<AuthCounts> accc = ac.ToList();
            return accc.AsEnumerable();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // OneWay converter
            return null;
        }
    }

    public class PatientInsuranceNameFromKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            AdmissionAuthorization a = value as AdmissionAuthorization;
            if (a == null)
            {
                return null;
            }

            int? key = a.PatientInsuranceKey;

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
            return null;
        }
    }

    public class AuthorizedByValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class SupplyItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return SupplyCache.GetSupplies();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}