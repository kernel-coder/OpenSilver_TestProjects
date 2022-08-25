#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Core;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class FindingItem
    {
        public FindingItem(string pLabel)
        {
            Label = pLabel;
            DataPoint = new string[14];
            TooltipBlirb = new string[14];
        }

        public string Label { get; set; }
        public string[] TooltipBlirb { get; set; }
        public string[] DataPoint { get; set; }
    }

    public class HospiceFindingsPopupViewModel : GalaSoft.MvvmLight.ViewModelBase, IDialogService
    {
        public HospiceFindingsPopupViewModel(EntityCollection<EncounterHospiceFindings> pFindings)
        {
            Cancel_Command = new RelayCommand(CancelCommand);
            DataGridSetup(pFindings);
        }

        #region Properties

        public RelayCommand Cancel_Command { get; protected set; }
        private ObservableCollection<FindingItem> _FindingsList;

        public ObservableCollection<FindingItem> FindingsList
        {
            get { return _FindingsList; }
            set
            {
                _FindingsList = value;
                RaisePropertyChanged("FindingsList");
            }
        }

        private FindingItem _Header;

        public FindingItem Header
        {
            get { return _Header; }
            set
            {
                _Header = value;
                RaisePropertyChanged("Header");
            }
        }

        #endregion Properties

        #region methods

        private void CancelCommand()
        {
            DialogResult = false;
        }

        private void DataGridSetup(EntityCollection<EncounterHospiceFindings> findings)
        {
            // pivot the data
            List<EncounterHospiceFindings> fList = findings.OrderByDescending(p => p.EncounterStartDateTime).ToList();
            if (fList.Any() == false)
            {
                return;
            }

            Header = new FindingItem("Dates");
            List<FindingItem> fiList = new List<FindingItem>();
            fiList.Add(new FindingItem("MAC-Right"));
            fiList.Add(new FindingItem("MAC-Left"));
            fiList.Add(new FindingItem("Weight"));
            int i = 0;
            foreach (EncounterHospiceFindings ehf in fList)
            {
                Header.DataPoint[i] = ehf.DateString;
                Header.TooltipBlirb[i] = ehf.TooltipBlirb;
                fiList[0].DataPoint[i] = ehf.MACRight;
                fiList[1].DataPoint[i] = ehf.MACLeft;
                fiList[2].DataPoint[i] = (ehf.WeightValue == null) ? null : ehf.WeightValue + " " + ehf.WeightScale;
                i++;
            }

            FindingsList = fiList.ToObservableCollection();
        }

        #endregion methods

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
            CancelCommand(); // So escape and the 'X' behave like the Cancel button
        }

        public virtual string Caption => "Hospice Findings";
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

        public override void Cleanup()
        {
            base.Cleanup();
            Cancel_Command = null;
            Header = null;
            FindingsList = null;
        }

        #endregion ICleanup
    }
}