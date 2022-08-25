#region Usings

using System.Collections.Generic;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class TeachingMed : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool _TeachingMedIsChecked;

        public bool TeachingMedIsChecked
        {
            get { return _TeachingMedIsChecked; }
            set
            {
                _TeachingMedIsChecked = value;
                RaisePropertyChanged("TeachingMedIsChecked");
            }
        }

        private PatientMedication _TeachingMedPatientMedication;

        public PatientMedication TeachingMedPatientMedication
        {
            get { return _TeachingMedPatientMedication; }
            set
            {
                _TeachingMedPatientMedication = value;
                RaisePropertyChanged("TeachingMedPatientMedication");
            }
        }
    }
}
