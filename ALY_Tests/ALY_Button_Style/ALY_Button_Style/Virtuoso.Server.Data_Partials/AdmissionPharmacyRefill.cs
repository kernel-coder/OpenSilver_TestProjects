#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionPharmacyRefill
    {
        private string _RefillDescription;

        public string RefillDescription
        {
            get { return _RefillDescription; }
            set
            {
                _RefillDescription = value;
                RaisePropertyChanged("RefillDescription");
            }
        }

        public string CompoundCode => Compound == true ? "Yes" : "No";

        public string RefillMedicationDescription
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

        public string RefillPharmacyDescription
        {
            get
            {
                var CR = char.ToString('\r');
                var NDCquantityUnits = string.IsNullOrWhiteSpace(Quantity) && string.IsNullOrWhiteSpace(Unit)
                    ? ""
                    : "NDC quantity: " + (string.IsNullOrWhiteSpace(Quantity) ? "?" : Quantity) + " " +
                      (string.IsNullOrWhiteSpace(Unit) ? "units" : Unit);
                var HCPCSquantity = string.IsNullOrWhiteSpace(Dose) ? "" : "HCPCS quantity: " + Dose;
                if (string.IsNullOrWhiteSpace(HCPCSquantity) == false)
                {
                    HCPCSquantity = string.IsNullOrWhiteSpace(NDCquantityUnits) ? HCPCSquantity : ",  " + HCPCSquantity;
                }

                return
                    "RX #: " + (string.IsNullOrWhiteSpace(PrescriptionNumber) ? "?" : PrescriptionNumber) +
                    ",  NDC Code: " + (string.IsNullOrWhiteSpace(NDCCode) ? "?" : NDCCode) + CR +
                    NDCquantityUnits + HCPCSquantity + ",  dispensed on " +
                    (DateFilled == null ? "?" : ((DateTime)DateFilled).ToShortDateString());
            }
        }

        partial void OnCompoundChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CompoundCode");
        }

        partial void OnPrescriptionNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }

        partial void OnNDCCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }

        partial void OnDateFilledChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }

        partial void OnDoseChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }

        partial void OnUnitChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }

        partial void OnQuantityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("RefillPharmacyDescription");
        }
    }
}