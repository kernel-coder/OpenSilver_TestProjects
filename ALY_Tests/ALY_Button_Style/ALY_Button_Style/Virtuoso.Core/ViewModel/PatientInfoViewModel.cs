#region Usings

using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class PatientInfoViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        #region Properties

        protected CommandManager CommandManager { get; set; }
        public PatientInfo PatientInfo { get; set; }
        private readonly string DefaultBusyMessage = "Retrieving patient information...";
        private string _BusyMessage;

        public string BusyMessage
        {
            get { return _BusyMessage; }
            set
            {
                _BusyMessage = value;
                RaisePropertyChanged("BusyMessage");
            }
        }

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        public RelayCommand CloseCommand { get; protected set; }
        public RelayCommand LoadedCommand { get; set; }
        bool? _dialogResult;

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                RaisePropertyChanged("DialogResult");
            }
        }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public PatientInfoViewModel(ILogger logger)
        {
            BusyMessage = DefaultBusyMessage;
            SetupCommands();
            CommandManager = new CommandManager(this);
        }

        #endregion

        #region Methods

        private void ResetPatientInfoDialog()
        {
            DialogResult = null;
            GetPatientInfo();
        }

        private void GetPatientInfo()
        {
        }

        private void SetupCommands()
        {
            LoadedCommand = new RelayCommand(() =>
            {
                //NOTE: application creates one instance of this window - it is hidden and then shown subsequently, so you can use
                //      the loaded event to know that the end user is viewing the dialog...
                if (IsBusy)
                {
                    return;
                }

                IsBusy = true;
                ResetPatientInfoDialog();
                IsBusy = false;
            }, () => true);

            CloseCommand = new RelayCommand(CloseDialog, () => { return true; });
        }

        private void CloseDialog()
        {
            if (IsBusy)
            {
                return;
            }

            DialogResult = true;
        }

        public override void Cleanup()
        {
            CommandManager.CleanUp();
            base.Cleanup();
        }

        #endregion
    }
}