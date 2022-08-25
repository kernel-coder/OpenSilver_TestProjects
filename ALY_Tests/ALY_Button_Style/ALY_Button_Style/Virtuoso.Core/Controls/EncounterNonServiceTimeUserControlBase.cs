using System;
using System.Collections.Generic;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;


namespace Virtuoso.Core.Controls
{
    public class EncounterNonServiceTimeUserControlBase : ChildControlBase<EncounterNonServiceTimeUserControl, EncounterNonServiceTime>
    {
        public RelayCommand<EncounterNonServiceTime> EncounterNonServiceTimeEditItem_Command { get; protected set; }
        public IEnumerable<NonServiceType> AvailableNonServiceTypes
        {
            get
            {
                return NonServiceTypeCache.GetNonServiceTypes()
                    .Where(nst => !nst.Inactive)
                    .OrderBy(nst => nst.Description);
            }
        }

        public EncounterNonServiceTimeUserControlBase()
            : base()
        {
            PopupDataTemplate = "NonServiceTimePopupDataTemplate";

            this.AddPressed += new EventHandler<UserControlBaseEventArgs<EncounterNonServiceTime>>(OnAddPressed);
            this.CancelPressed += new EventHandler<UserControlBaseEventArgs<EncounterNonServiceTime>>(OnCancelPressed);
            this.OKPressed += new EventHandler<UserControlBaseEventArgs<EncounterNonServiceTime>>(OnOKPressed);
            this.EditPressed += new EventHandler<UserControlBaseEventArgs<EncounterNonServiceTime>>(OnEditPressed);

            EncounterNonServiceTimeEditItem_Command = new RelayCommand<EncounterNonServiceTime>((item) =>
            {
                if (item == null) return;
                IsEdit = true;
                SelectedItem = item;
                SelectedItem.IgnoreChanges = true;
                SelectedItem.StartDateTimeOffSet = SelectedItem.StartTime.GetValueOrDefault();
                SelectedItem.EndDateTimeOffSet = SelectedItem.EndTime.GetValueOrDefault();
                SelectedItem.IgnoreChanges = false;

                if (SelectedItem != null)
                    SelectedItem.BeginEditting();

                PopupDataTemplate = "NonServiceTimePopupDataTemplate";
                ParentViewModel.PopupDataContext = this;
            });
        }

        public void OnAddPressed(object sender, UserControlBaseEventArgs<EncounterNonServiceTime> e)
        {
            if (e != null)
            {
                SelectedItem = e.Entity;
                SelectedItem.StartDateTimeOffSet = Encounter.EncounterStartDate == null ? DateTimeOffset.Now : (DateTimeOffset)Encounter.EncounterStartDate;
                SelectedItem.EndDateTimeOffSet = Encounter.EncounterStartDate == null ? DateTimeOffset.Now : (DateTimeOffset)Encounter.EncounterStartDate;
                //e.Entity.SequenceNo = Encounter.EncounterSupply.Count();
                //e.Entity.LocationKey = CodeLookupCache.GetKeyFromCode("ILOC", "MAIN");
            }
            ParentViewModel.PopupDataContext = this;
        }

        public void OnCancelPressed(object sender, UserControlBaseEventArgs<EncounterNonServiceTime> e)
        {
            ParentViewModel.PopupDataContext = null;
        }

        public void OnEditPressed(object sender, UserControlBaseEventArgs<EncounterNonServiceTime> e)
        {
            ParentViewModel.PopupDataContext = this;
        }

        public void OnOKPressed(object sender, UserControlBaseEventArgs<EncounterNonServiceTime> e)
        {
            SelectedItem.IgnoreChanges = true;
            SelectedItem.StartTime = (SelectedItem.StartDateTimeOffSet == null ? DateTimeOffset.Now : (DateTimeOffset)SelectedItem.StartDateTimeOffSet);
            SelectedItem.EndTime = (SelectedItem.EndDateTimeOffSet == null ? DateTimeOffset.Now : (DateTimeOffset)SelectedItem.EndDateTimeOffSet);
            if (SelectedItem.MileageIsVisible == false)
            {
                SelectedItem.Distance = null;
                SelectedItem.DistanceScale = null;
            }
            if (string.IsNullOrWhiteSpace(SelectedItem.Notes)) SelectedItem.Notes = null;
            ParentViewModel.PopupDataContext = null;
        }

        public IPatientService Model
        {
            get { return (IPatientService)GetValue(ModelProperty); }
            set
            {
                SetValue(ModelProperty, value);
            }
        }
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IPatientService), typeof(EncounterNonServiceTimeUserControl), null);
        public override void RemoveFromModel(EncounterNonServiceTime entity)
        {
            if (Model == null)
                throw new ArgumentNullException("Model", "Model is NULL");

            Model.Remove(entity);
        }
        public override void SaveModel(UserControlBaseCommandType command)
        {
            //issue SAVE - regardless of whethe command = OK or CANCEL...
            if (Model == null)
                throw new ArgumentNullException("Model", "Model is NULL");
        }
    }

}
