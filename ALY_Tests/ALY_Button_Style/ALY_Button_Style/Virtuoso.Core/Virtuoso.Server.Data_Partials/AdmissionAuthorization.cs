#region Usings

using System.Linq;
using Virtuoso.Core;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionAuthorization
    {
        #region New Auth Screen

        public string AuthDateRange
        {
            get
            {
                if (IsNew)
                {
                    return "";
                }

                var outString = EffectiveFromDate == null ? "" : EffectiveFromDate.ToShortDateString();
                outString += EffectiveToDate == null ? "" : " - ";
                outString += EffectiveToDate == null ? "" : EffectiveToDate.Value.ToShortDateString();
                return outString;
            }
        }

        #endregion

        public bool CanEditOrder
        {
            get
            {
                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEntryReviewer, false))
                {
                    return true;
                }

                if (RoleAccessHelper.CheckPermission(RoleAccess.OrderEdit, false))
                {
                    return true;
                }

                return false;
            }
        }

        public string InsuranceName
        {
            get
            {
                if (PatientInsuranceKey <= 0)
                {
                    return " ";
                }

                var pi =
                    Patient.PatientInsurance
                        .Where(i => i.PatientInsuranceKey == PatientInsuranceKey).FirstOrDefault();
                return pi.NameAndNumber;
            }
        }

        public string InsuranceNameWithoutNumber
        {
            get
            {
                if (PatientInsuranceKey <= 0)
                {
                    return " ";
                }

                var pi =
                    Patient.PatientInsurance
                        .Where(i => i.PatientInsuranceKey == PatientInsuranceKey).FirstOrDefault();
                if (pi != null)
                {
                    var name = InsuranceCache.GetInsuranceNameFromKey(pi.InsuranceKey);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return " ";
                    }

                    return name;
                }

                return " ";
            }
        }

        public void RaiseChanged()
        {
            RaisePropertyChanged("PatientInsuranceKey");
        }
    }

    public partial class AdmissionAuthorizationInstance
    {
        private Admission _CurrentAdmission;

        public string AuthCount
        {
            get
            {
                if (AdmissionAuthorizationDetail != null && AdmissionAuthorizationDetail
                        .Where(aad => aad.DeletedDate.HasValue == false).Any())
                {
                    var total = AdmissionAuthorizationDetail
                        .Where(aad => aad.DeletedDate.HasValue == false)
                        .Where(aad => aad.AuthCount.HasValue)
                        .Sum(aad => aad.AuthCount);

                    return total.GetValueOrDefault().ToString();
                }

                return "0";
            }
        }

        public string AuthCountLastUpdate
        {
            get
            {
                if (AdmissionAuthorizationDetail != null && AdmissionAuthorizationDetail
                        .Where(aad => aad.DeletedDate.HasValue == false).Any())
                {
                    var max = AdmissionAuthorizationDetail
                        .Where(d => d.DeletedDate.HasValue == false)
                        .Where(aad => aad.AuthCount.HasValue)
                        .Where(aad => aad.AuthCountLastUpdate.HasValue)
                        .Select(d => d.AuthCountLastUpdate)
                        .Max();
                    return max.HasValue ? max.Value.Date.ToString("MM/dd/yyyy") : null;
                }

                return null;
            }
        }

        public Admission CurrentAdmission
        {
            get { return _CurrentAdmission; }
            set
            {
                _CurrentAdmission = value;
                RaisePropertyChanged("Disciplines");
            }
        }

        public bool CanEdit => AdmissionAuthorizationInstanceKey <= 0 ? true : true;

        public string AuthDisciplineDesc
        {
            get
            {
                string desc = null;

                if (AdmissionDisciplineKey.HasValue)
                {
                    desc = DisciplineCache.GetDescriptionFromKey(AdmissionDisciplineKey.Value);
                }
                else if (AuthorizationDiscCode.HasValue)
                {
                    desc = CodeLookupCache.GetCodeDescriptionFromKey(AuthorizationDiscCode.Value);
                }
                else
                {
                    desc = "<General>";
                }

                return desc;
            }
        }

        partial void OnServiceTypeGroupKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            //When ServiceType "Group" specified cannot have a ServiceType "Key" set
            if (ServiceTypeGroupKey != null)
            {
                ServiceTypeKey = null;
            }
        }

        partial void OnServiceTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            //When ServiceType "Key" specified cannot have a ServiceType "Group" set
            if (ServiceTypeKey != null)
            {
                ServiceTypeGroupKey = null;
            }

            RaisePropertyChanged("STKey");
        }
    }

    public partial class AdmissionAuthorizationDetail
    {
        private Admission _CurrentAdmission;

        public Admission CurrentAdmission
        {
            get { return _CurrentAdmission; }
            set
            {
                _CurrentAdmission = value;
                RaisePropertyChanged("Disciplines");
            }
        }

        public bool CanEdit => AdmissionAuthorizationDetailKey <= 0 ? true : true;

        public string AuthDisciplineDesc
        {
            get
            {
                string desc = null;

                if (AdmissionDisciplineKey.HasValue)
                {
                    desc = DisciplineCache.GetDescriptionFromKey(AdmissionDisciplineKey.Value);
                }
                else if (AuthorizationDiscCode.HasValue)
                {
                    desc = CodeLookupCache.GetCodeDescriptionFromKey(AuthorizationDiscCode.Value);
                }
                else
                {
                    desc = "<General>";
                }

                return desc;
            }
        }

        partial void OnServiceTypeGroupKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            //When ServiceType "Group" specified cannot have a ServiceType "Key" set
            if (ServiceTypeGroupKey != null)
            {
                ServiceTypeKey = null;
            }
        }

        partial void OnServiceTypeKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            //When ServiceType "Key" specified cannot have a ServiceType "Group" set
            if (ServiceTypeKey != null)
            {
                ServiceTypeGroupKey = null;
            }

            RaisePropertyChanged("STKey");
        }
    }
}