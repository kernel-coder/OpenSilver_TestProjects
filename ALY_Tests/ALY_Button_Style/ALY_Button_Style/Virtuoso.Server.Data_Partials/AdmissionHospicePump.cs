#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionHospicePump
    {
        public string PumpMedicationDescription
        {
            get
            {
                var CR = char.ToString('\r');
                var prd = Patient == null ? "Patient ?" :
                    string.IsNullOrWhiteSpace(Patient.RefillDescription) ? "Patient ?" : Patient.RefillDescription;
                var ard = Admission == null ? "Admission ?" :
                    string.IsNullOrWhiteSpace(Admission.RefillDescription) ? "Admission ?" :
                    Admission.RefillDescription;
                var pmrd = PatientMedication == null ? "Medication ?" :
                    string.IsNullOrWhiteSpace(PatientMedication.RefillDescription) ? "Medication ?" :
                    PatientMedication.RefillDescription;
                return prd + ", in " + ard + CR + pmrd;
            }
        }

        public string HospicePumpDescription
        {
            get
            {
                var CR = char.ToString('\r');
                var dose = string.IsNullOrWhiteSpace(Dose) ? "" : "Units of Service: " + Dose;
                var cost = Cost == null ? "" : "Cost: " + string.Format("{0:$0.00}", Cost);
                if (string.IsNullOrWhiteSpace(cost) == false)
                {
                    cost = string.IsNullOrWhiteSpace(dose) ? cost : ",  " + cost;
                }

                return "Pump Info: " + (string.IsNullOrWhiteSpace(PumpInfo) ? "?" : PumpInfo) +
                       ", dispensed on " + (DateFilled == null ? "?" : ((DateTime)DateFilled).ToShortDateString()) +
                       (string.IsNullOrWhiteSpace(dose) && string.IsNullOrWhiteSpace(cost) ? "" : CR) + dose + cost;
            }
        }

        partial void OnCostChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospicePumpDescription");
        }

        partial void OnDateFilledChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospicePumpDescription");
        }

        partial void OnDoseChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospicePumpDescription");
        }

        partial void OnPumpInfoChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospicePumpDescription");
        }
    }
}