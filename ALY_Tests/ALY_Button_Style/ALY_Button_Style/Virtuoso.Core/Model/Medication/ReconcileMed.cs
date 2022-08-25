#region Usings

using System.Collections.Generic;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ReconcileMed : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool _ReconcileMedIsChecked;

        public bool ReconcileMedIsChecked
        {
            get { return _ReconcileMedIsChecked; }
            set
            {
                _ReconcileMedIsChecked = value;
                RaisePropertyChanged("ReconcileMedIsChecked");
            }
        }

        private PatientMedication _ReconcileMedPatientMedication;

        public PatientMedication ReconcileMedPatientMedication
        {
            get { return _ReconcileMedPatientMedication; }
            set
            {
                _ReconcileMedPatientMedication = value;
                RaisePropertyChanged("ReconcileMedPatientMedication");
            }
        }
    }
}
