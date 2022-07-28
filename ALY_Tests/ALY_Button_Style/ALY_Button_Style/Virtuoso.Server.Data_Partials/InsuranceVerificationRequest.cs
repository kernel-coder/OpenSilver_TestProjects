#region Usings

using System;
using System.ComponentModel;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class InsuranceVerificationRequest : INotifyPropertyChanged
    {
        public enum NewVerificationStatus
        {
            Verified,
            NotVerified,
            Inconclusive
        }

        public bool IsLoaded => true;

        public bool NegativeValue => false;

        public bool CanProcess => ParsedUTCDateTime.HasValue && !WasProcessed;


        public string PatientFullName => FormatHelper.FormatName(PatientLastName, PatientFirstName, PatientMiddleName);

        public string InsuredFullName => FormatHelper.FormatName(InsuredLastName, InsuredFirstName, InsuredMiddleName);

        public string PatientCityStateZip => FormatHelper.FormatCityStateZip(PatientCity, PatientState, PatientZipCode);

        public string InsuredCityStateZip => FormatHelper.FormatCityStateZip(InsuredCity, InsuredState, InsuredZipCode);

        public string InsuranceID => string.IsNullOrEmpty(PatientInsuranceID) ? InsuredInsuranceID : PatientInsuranceID;

        public bool HasPatientAddress2 => !string.IsNullOrEmpty(PatientAddress2);

        public bool HasInsuredAddress2 => !string.IsNullOrEmpty(InsuredAddress2) && !PatientIsTheInsured;

        public DateTime DisplayDate
        {
            get
            {
                if (ParsedUTCDateTime.HasValue)
                {
                    if (WasProcessed)
                    {
                        return UpdatedUTCDateTime;
                    }

                    return ParsedUTCDateTime.Value;
                }

                return CreatedUTCDateTime;
            }
        }

        public string DisplayStatus
        {
            get
            {
                if (ParsedUTCDateTime.HasValue)
                {
                    if (WasProcessed)
                    {
                        return WasVerified ? "Verified" : "Not Verified";
                    }

                    return "Action Needed";
                }

                if (WasProcessed)
                {
                    return "Errored";
                }

                return "In Process";
            }
        }

        public void Process(NewVerificationStatus NewVerifiedStatus)
        {
            UpdateProperties();
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged("DisplayStatus");
            RaisePropertyChanged("DisplayDate");
            RaisePropertyChanged("WasVerified");
            RaisePropertyChanged("WasProcessed");
            RaisePropertyChanged("CanProcess");
            RaisePropertyChanged("IsLoaded");
            RaisePropertyChanged("NegativeValue");
            RaisePropertyChanged("DisplayStatus");
        }
    }
}