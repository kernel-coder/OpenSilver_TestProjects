namespace Virtuoso.Server.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public partial class EligibilityRequestJoin
    {
        [Key]
        public int InsuranceKey
        {
            get
            {
                return InsuranceVerificationRequest.InsuranceKey;
            }
        }

        public string PatientCityStateZip
        {
            get
            {
                string patCity = null;
                string patState = null;
                string patZip = null;

                if (!string.IsNullOrEmpty(PatientCity))
                {
                    patCity = PatientCity.Trim() + ", ";
                }

                if (!string.IsNullOrEmpty(PatientState))
                {
                    patState = PatientState.Trim() + " ";
                }

                if (!string.IsNullOrEmpty(PatientZipCode))
                {
                    patZip = PatientZipCode.Trim() + " ";
                }

                return patCity + patState + patZip;
            }
        }

        public string PatientFullName
        {
            get
            {
                return PatientFirstName + " " + (string.IsNullOrEmpty(PatientMiddleName) ? "" : PatientMiddleName + " ") + PatientLastName;
            }
        }


        public string InsuredCityStateZip
        {
            get
            {
                string insCity = null;
                string insState = null;
                string insZip = null;

                if (!string.IsNullOrEmpty(InsuredCity))
                {
                    insCity = InsuredCity.Trim() + ", ";
                }

                if (!string.IsNullOrEmpty(InsuredStateCode))
                {
                    insState = InsuredStateCode.Trim() + " ";
                }

                if (!string.IsNullOrEmpty(InsuredZipCode))
                {
                    insZip = InsuredZipCode.Trim() + " ";
                }

                return insCity + insState + insZip;
            }
        }

        public DateTime? CoverageRequestDate
        {
            get
            {
                return InsuranceVerificationRequest.ParsedUTCDateTime; // PJS Is this accurate enough?
            }
        }
    }
}
