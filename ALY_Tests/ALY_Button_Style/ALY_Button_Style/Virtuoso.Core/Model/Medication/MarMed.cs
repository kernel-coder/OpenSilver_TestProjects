#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class MARMed : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool _PRN;
        public bool PRN
        {
            get { return _PRN; }
            set
            {
                _PRN = value;
                RaisePropertyChanged("PRN");
            }
        }

        private PatientMedication _MARPatientMedication;
        public PatientMedication MARPatientMedication
        {
            get { return _MARPatientMedication; }
            set
            {
                _MARPatientMedication = value;
                RaisePropertyChanged("MARPatientMedication");
            }
        }

        private IEnumerable<AdmissionMedicationMAR> _AdmissionMedicationMARList;
        public IEnumerable<AdmissionMedicationMAR> AdmissionMedicationMARList
        {
            get { return _AdmissionMedicationMARList; }
            set
            {
                _AdmissionMedicationMARList = value;
                RaisePropertyChanged("AdmissionMedicationMARList");
            }
        }

        // Only relevant for STD Meds - NOT PRN
        private IEnumerable<AdmissionMedicationMAR> _PriorAdministrationList = new List<AdmissionMedicationMAR>();
        public IEnumerable<AdmissionMedicationMAR> PriorAdministrationList
        {
            get { return _PriorAdministrationList; }
            set
            {
                _PriorAdministrationList = value;
                RaisePropertyChanged("PriorAdministrationList");
            }
        }
    }
}