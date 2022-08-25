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
    public class ESASFindingsPopupViewModel : GalaSoft.MvvmLight.ViewModelBase, IDialogService
    {
        public ESASFindingsPopupViewModel(EntityCollection<EncounterESASFindings> pFindings)
        {
            DataGridSetup(pFindings);
            Cancel_Command = new RelayCommand(CancelCommand);
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

        private void DataGridSetup(EntityCollection<EncounterESASFindings> findings)
        {
            // pivot the data
            List<EncounterESASFindings> fList = findings.OrderByDescending(p => p.EncounterStartDateTime).ToList();
            if (fList.Any() == false)
            {
                return;
            }

            Header = new FindingItem("Dates");
            List<FindingItem> fiList = new List<FindingItem>
            {
                new FindingItem("Pain"),
                new FindingItem("Nausea"),
                new FindingItem("Lack of Appetite"),
                new FindingItem("Constipation"),
                new FindingItem("Shortness of Breath"),
                new FindingItem("Tiredness"),
                new FindingItem("Drowsiness"),
                new FindingItem("Depression"),
                new FindingItem("Anxiety"),
                new FindingItem("Wellbeing"),
                new FindingItem("Reported By *")
            };
            int i = 0;
            foreach (EncounterESASFindings eef in fList)
            {
                Header.DataPoint[i] = eef.DateString;
                Header.TooltipBlirb[i] = eef.TooltipBlirb;
                fiList[0].DataPoint[i] = (eef.Pain == null) ? null : eef.Pain.ToString();
                fiList[1].DataPoint[i] = (eef.Nausea == null) ? null : eef.Nausea.ToString();
                fiList[2].DataPoint[i] = (eef.Appetite == null) ? null : eef.Appetite.ToString();
                fiList[3].DataPoint[i] = (eef.Constipation == null) ? null : eef.Constipation.ToString();
                fiList[4].DataPoint[i] = (eef.Breath == null) ? null : eef.Breath.ToString();
                fiList[5].DataPoint[i] = (eef.Tiredness == null) ? null : eef.Tiredness.ToString();
                fiList[6].DataPoint[i] = (eef.Drowsiness == null) ? null : eef.Drowsiness.ToString();
                fiList[7].DataPoint[i] = (eef.Depression == null) ? null : eef.Depression.ToString();
                fiList[8].DataPoint[i] = (eef.Anxiety == null) ? null : eef.Anxiety.ToString();
                fiList[9].DataPoint[i] = (eef.Wellbeing == null) ? null : eef.Wellbeing.ToString();
                fiList[10].DataPoint[i] = (eef.ReportedBy == null) ? null : eef.ReportedBy;
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

        public virtual string Caption => "ESAS-r Symptom Findings";
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