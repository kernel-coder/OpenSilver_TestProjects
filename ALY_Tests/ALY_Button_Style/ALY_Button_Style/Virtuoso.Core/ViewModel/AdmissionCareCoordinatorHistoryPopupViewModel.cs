#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using static System.Diagnostics.Debug;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class AdmissionCareCoordinatorHistoryPopupViewModel : GalaSoft.MvvmLight.ViewModelBase, IDialogService
    {
        public AdmissionCareCoordinatorHistoryPopupViewModel(IPatientService pServiceModel, Admission pAdmission)
        {
            WriteLine($"[2005] {nameof(AdmissionCareCoordinatorHistoryPopupViewModel)}: constructor");

            if (pServiceModel == null)
            {
                return;
            }

            _ServiceModel = pServiceModel;
            _Admission = pAdmission;
            Cancel_Command = new RelayCommand(CancelCommand);
            if (EntityManager.Current.IsOnline)
            {
                _ServiceModel.OnGetAdmissionCareCoordinatorHistoryLoaded +=
                    Model_OnGetAdmissionCareCoordinatorHistoryLoaded;
                SetIsBusy(true, BUSYMESSAGELoading);
                int key = _Admission?.AdmissionKey ?? 0;
                _ServiceModel.GetAdmissionCareCoordinatorHistoryAsync(key);
            }
            else
            {
                // off line - may have one handly - if in dynamic form
                SetupHistory(_Admission?.AdmissionCareCoordinatorHistoryPOCO.ToList());
            }
        }

        #region Properties

        private readonly string BUSYMESSAGELoading = "Loading Care Coordinator History...";
        private IPatientService _ServiceModel;
        private Admission _Admission;
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

        private bool _IsLoaded;

        public bool IsLoaded
        {
            get { return _IsLoaded; }
            set
            {
                _IsLoaded = value;
                RaisePropertyChanged("IsLoaded");
            }
        }

        public RelayCommand Cancel_Command { get; protected set; }

        private ObservableCollection<AdmissionCareCoordinatorHistoryPOCO> _AdmissionCareCoordinatorHistoryList;

        public ObservableCollection<AdmissionCareCoordinatorHistoryPOCO> AdmissionCareCoordinatorHistoryList
        {
            get { return _AdmissionCareCoordinatorHistoryList; }
            set
            {
                _AdmissionCareCoordinatorHistoryList = value;
                RaisePropertyChanged("AdmissionCareCoordinatorHistoryList");
                RaisePropertyChanged("ShowResults");
            }
        }

        public bool ShowResults => (AdmissionCareCoordinatorHistoryList != null);

        #endregion Properties

        #region methods

        public void SetIsBusy(bool isBusy, string busyMessage = "Please wait...")
        {
            BusyMessage = busyMessage;
            IsBusy = isBusy;
        }

        private void Model_OnGetAdmissionCareCoordinatorHistoryLoaded(object sender,
            EntityEventArgs<AdmissionCareCoordinatorHistoryPOCO> e)
        {
            if (_ServiceModel != null)
            {
                _ServiceModel.OnGetAdmissionCareCoordinatorHistoryLoaded -=
                    Model_OnGetAdmissionCareCoordinatorHistoryLoaded;
            }

            SetIsBusy(false);
            if (e == null)
            {
                return;
            }

            if (e.Error != null)
            {
                if (e.EntityErrors.Any() == false)
                {
                    ShowErrorMessage(e.Error.Message);
                }
                else
                {
                    foreach (string error in e.EntityErrors) ShowErrorMessage(error);
                }

                IsLoaded = true;
                return;
            }

            List<AdmissionCareCoordinatorHistoryPOCO> acchpList = e.Results?.ToList();
            SetupHistory(acchpList);
        }

        private void SetupHistory(List<AdmissionCareCoordinatorHistoryPOCO> acchpList)
        {
            List<AdmissionCareCoordinatorHistoryPOCO> list = acchpList?.OrderByDescending(p => p.PopulatedDateTime).ToList();
            if (list != null && list.Any() == false)
            {
                list = null;
            }

            AdmissionCareCoordinatorHistoryList = list?.ToObservableCollection();
            IsLoaded = true;
        }

        private void ShowErrorMessage(string message)
        {
            NavigateCloseDialog d = new NavigateCloseDialog
            {
                Width = double.NaN,
                Height = double.NaN,
                ErrorMessage = message,
                ErrorQuestion = null,
                Title = "Care Coordinator History Load Error",
                HasCloseButton = false,
                OKLabel = "OK",
                NoVisible = false
            };
            d.Show();
        }

        private void CancelCommand()
        {
            WriteLine($"[2004] {nameof(AdmissionCareCoordinatorHistoryPopupViewModel)}: {nameof(CancelCommand)}");
            DialogResult = false;
        }

        #endregion Methods

        #region IDialogService

        protected static readonly DialogService _dialogService = new DialogService();
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

        public virtual bool CanClose()
        {
            if ((_ServiceModel == null) || (_ServiceModel.Context == null))
            {
                return false;
            }

            return (IsBusy == false);
        }

        public virtual void CloseDialog()
        {
            if (DialogResult != true)
            {
                CancelCommand(); // So escape and the 'X' behave like the Cancel button
            }
        }

        public virtual string Caption => "Care Coordinator History";
        public virtual bool ResizeWindow => true;
        public virtual bool DynamicSize => true;
        public virtual bool SetMaxWidthAndHeight => true;
        public virtual double? Height => null;
        public virtual double? Width => null;
        public virtual double? MinHeight => 360;
        public virtual double? MinWidth => 720;

        protected Maybe<Action> EditableWidgetSelector = Maybe<Action>.None;

        public void SetSelectFirstEditableWidgetAction(Action editableWidgetSelector)
        {
            if (editableWidgetSelector != null)
            {
                EditableWidgetSelector = editableWidgetSelector.ToMaybe();
            }
        }

        #endregion IDialogService

        #region ICleanup

        public new void Cleanup()
        {
            base.Cleanup();
            if (_ServiceModel != null)
            {
                _ServiceModel.OnGetAdmissionCareCoordinatorHistoryLoaded -=
                    Model_OnGetAdmissionCareCoordinatorHistoryLoaded;
            }

            Cancel_Command = null;

            AdmissionCareCoordinatorHistoryList = null;
            _Admission = null;
        }

        #endregion ICleanup
    }
}