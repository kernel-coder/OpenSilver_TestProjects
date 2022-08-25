#region Usings

using System.Collections.Generic;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class AdministrationMed : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool _AdministrationMedIsChecked;

        public bool AdministrationMedIsChecked
        {
            get { return _AdministrationMedIsChecked; }
            set
            {
                _AdministrationMedIsChecked = value;
                RaisePropertyChanged("AdministrationMedIsChecked");
            }
        }

        private PatientMedication _AdministrationMedPatientMedication;

        public PatientMedication AdministrationMedPatientMedication
        {
            get { return _AdministrationMedPatientMedication; }
            set
            {
                _AdministrationMedPatientMedication = value;
                RaisePropertyChanged("AdministrationMedPatientMedication");
            }
        }
    }
}
