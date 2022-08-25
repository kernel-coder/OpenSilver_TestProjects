#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionCoverageInsurance
    {
        public string InsuranceName
        {
            get
            {
                string insuranceName = null;
                insuranceName = InsuranceCache.GetInsuranceNameFromKey(PatientInsurance.InsuranceKey);
                return insuranceName;
            }
        }

        partial void OnPatientInsuranceKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("InsuranceName");
        }
    }
}