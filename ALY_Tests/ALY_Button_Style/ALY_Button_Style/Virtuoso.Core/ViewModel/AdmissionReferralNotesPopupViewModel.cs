#region Usings

using System;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using static System.Diagnostics.Debug;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class AdmissionReferralNotesPopupViewModel : GalaSoft.MvvmLight.ViewModelBase, IDialogService
    {
        public AdmissionReferralNotesPopupViewModel(AdmissionReferral pAdmissionReferral)
        {
            WriteLine($"[2005] {nameof(AdmissionReferralNotesPopupViewModel)}: constructor");
            AdmissionReferral = pAdmissionReferral;
            Cancel_Command = new RelayCommand(CancelCommand);
        }

        #region Properties

        private AdmissionReferral _AdmissionReferral;

        public AdmissionReferral AdmissionReferral
        {
            get { return _AdmissionReferral; }
            set
            {
                _AdmissionReferral = value;
                RaisePropertyChanged("AdmissionReferral");
            }
        }

        public RelayCommand Cancel_Command { get; protected set; }

        #endregion Properties

        #region methods

        private void CancelCommand()
        {
            WriteLine($"[2004] {nameof(AdmissionReferralNotesPopupViewModel)}: {nameof(CancelCommand)}");
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
            return true;
        }

        public virtual void CloseDialog()
        {
            if (DialogResult != true)
            {
                CancelCommand(); // So escape and the 'X' behave like the Cancel button
            }
        }

        public virtual string Caption => "Admission Referral Notes";
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
            Cancel_Command = null;
            AdmissionReferral = null;
        }

        #endregion ICleanup
    }
}