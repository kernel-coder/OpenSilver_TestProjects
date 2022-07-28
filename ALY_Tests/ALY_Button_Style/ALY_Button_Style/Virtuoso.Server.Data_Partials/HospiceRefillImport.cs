#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class HospiceRefillImport
    {
        public string PatientDescription
        {
            get
            {
                if (Patient != null)
                {
                    return Patient.FullName;
                }

                var name = string.Format("{0}, {1}", LastName, FirstName).Trim();
                if (name == "," || name == "")
                {
                    name = "?";
                }

                return name;
            }
        }

        public string AdmissionDescription => Admission != null ? Admission.RefillDescription : SOCDescription;

        public string SOCDescription => "SOC: " + (SOCDate == null ? "?" : ((DateTime)SOCDate).ToShortDateString());

        public string MedicationDescription => PatientMedication != null ? PatientMedication.MedicationDescription :
            string.IsNullOrWhiteSpace(DrugName) ? "?" : DrugName;
    }

    public partial class HospiceRefillImportColumnList
    {
        private string _PreviousColumnList;

        public string PreviousColumnList
        {
            get { return _PreviousColumnList; }
            set
            {
                _PreviousColumnList = value;
                RaisePropertyChanged("PreviousColumnList");
            }
        }

        partial void OnLastNameChanged()
        {
            if (IsDeserializing)
            {
            }
        }
    }
}