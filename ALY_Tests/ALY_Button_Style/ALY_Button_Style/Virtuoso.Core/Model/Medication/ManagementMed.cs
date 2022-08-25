#region Usings

using System.Collections.Generic;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ManagementMed : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool _ManagementMedIsChecked;

        public bool ManagementMedIsChecked
        {
            get { return _ManagementMedIsChecked; }
            set
            {
                _ManagementMedIsChecked = value;
                RaisePropertyChanged("ManagementMedIsChecked");
            }
        }

        private PatientMedication _ManagementMedPatientMedication;

        public PatientMedication ManagementMedPatientMedication
        {
            get { return _ManagementMedPatientMedication; }
            set
            {
                _ManagementMedPatientMedication = value;
                RaisePropertyChanged("ManagementMedPatientMedication");
            }
        }
    }
}
