#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class DisciplineSummary : QuestionBase
    {
        public DisciplineSummary(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void SetupDisciplineSummary()
        {
            HiddenOverride = GetHiddenOverride;
            if (HiddenOverride == true)
            {
                return;
            }

            RefreshDisciplineSummaryList();
            Messenger.Default.Register<AdmissionDiscipline>(this,
                string.Format("AdmissionDisciplineAgencyDischargeChanged{0}",
                    AdmissionDiscipline.AdmissionDisciplineKey.ToString().Trim()),
                ad => SetupAdmissionDisciplineAgencyDischargeChanged(ad));
            if ((Admission != null) && (AdmissionDiscipline != null) && (Encounter != null) &&
                (Encounter.EncounterStatus == (int)EncounterStatusType.Edit))
            {
                Admission.CalculateAgencyDischarge(AdmissionDiscipline, AdmissionDiscipline.OverrideAgencyDischarge);
            }
        }

        private bool? PriorAgencyDischarge;

        public void SetupAdmissionDisciplineAgencyDischargeChanged(AdmissionDiscipline ad)
        {
            if ((AdmissionDiscipline != null) && (PriorAgencyDischarge == AdmissionDiscipline.AgencyDischarge))
            {
                return;
            }

            PriorAgencyDischarge = AdmissionDiscipline.AgencyDischarge;
            RefreshDisciplineSummaryList();
        }

        private void RefreshDisciplineSummaryList()
        {
            if ((Encounter == null) || (Encounter.EncounterData == null) || (Admission == null) ||
                (Admission.AdmissionDiscipline == null) || (Encounter.EncounterIsInEdit == false))
            {
                SetupDisciplineSummaryListView();
                return;
            }

            List<AdmissionDiscipline> adList = Admission.AdmissionDiscipline
                .Where(ad => ((ad.HistoryKey == null) && ad.AdmissionDisciplineWasAdmitted))
                .OrderBy(ad => ad.DisciplineAdmitDateTime).ThenBy(ad => ad.DischargeDateTime)
                .ThenBy(ad => ad.AdmissionDisciplineHCFACode).ThenBy(ad => ad.DisciplineDescription)
                .ThenBy(ad => ad.DisciplineAdmitDateTime).ToList();
            if (adList.Count == 0)
            {
                SetupDisciplineSummaryListView();
                return;
            }

            List<EncounterData> edList = Encounter.EncounterData.Where(p => p.QuestionKey == Question.QuestionKey)
                .ToList();
            if (DynamicFormViewModel != null && DynamicFormViewModel.FormModel != null)
            {
                foreach (EncounterData ed in edList) DynamicFormViewModel.FormModel.Remove(ed);
            }

            int sequence = 0;
            foreach (AdmissionDiscipline ad in adList)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = FormSection.SectionKey.Value, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey
                };
                Encounter.EncounterData.Add(ed);
                ed.EncounterKey = Encounter.EncounterKey;
                ed.IntData = ++sequence;
                ed.ExternalKey = ad.AdmissionDisciplineKey;
                ed.TextData = ad.DisciplineDescription;
                ed.Text2Data = "Admitted " + ((DateTime)ad.DisciplineAdmitDateTime).Date.ToShortDateString();
                if (IsDischarge)
                {
                    ed.BoolData = ((ad.AdmissionDisciplineWasDischarged == false) && (AdmissionDiscipline != null) &&
                                   ((ad.AdmissionDisciplineKey == AdmissionDiscipline.AdmissionDisciplineKey) ||
                                    AdmissionDiscipline.AgencyDischarge));
                }
                else
                {
                    ed.BoolData = (ad.AdmissionDisciplineWasDischarged == false); //(assume IsTransfer)
                }

                ed.Text3Data = (ed.BoolData == true)
                    ? "*"
                    : ((ad.DischargeDateTime == null)
                        ? ""
                        : "Discharged " + ((DateTime)ad.DischargeDateTime).Date.ToShortDateString());
                ed.Text4Data = (string.IsNullOrWhiteSpace(ad.SummaryOfCareNarrative))
                    ? null
                    : ad.SummaryOfCareNarrative;
            }

            SetupDisciplineSummaryListView();
        }

        private CollectionViewSource _DisciplineSummaryList;

        public ICollectionView DisciplineSummaryListView =>
            ((_DisciplineSummaryList == null) || (_DisciplineSummaryList.View == null))
                ? null
                : _DisciplineSummaryList.View;

        public bool ShowDisciplineSummaryListView => (_DisciplineSummaryList != null);

        private void SetupDisciplineSummaryListView()
        {
            if (myEncounterData == null)
            {
                _DisciplineSummaryList = null;
            }
            else
            {
                foreach (EncounterData ed in myEncounterData)
                    ed.ViewSummaryOfCareNarrative_Command = new RelayCommand(() =>
                    {
                        ed.ViewSummaryOfCareNarrativeCommand();
                    });

                _DisciplineSummaryList = new CollectionViewSource();
                _DisciplineSummaryList.Source = myEncounterData;
                DisciplineSummaryListView.SortDescriptions.Add(new SortDescription("IntData",
                    ListSortDirection.Ascending));
                DisciplineSummaryListView.Refresh();
            }

            RaisePropertyChanged("DisciplineSummaryListView");
            RaisePropertyChanged("ShowDisciplineSummaryListView");
            RaisePropertyChanged("ShowAsterisk");
            RaisePropertyChanged("AsteriskBlirb");
        }

        private List<EncounterData> myEncounterData
        {
            get
            {
                List<EncounterData> edList = ((Encounter == null) || (Encounter.EncounterData == null))
                    ? null
                    : Encounter.EncounterData.Where(ed =>
                        ((ed.SectionKey == FormSection.SectionKey) && (ed.QuestionGroupKey == QuestionGroupKey) &&
                         (ed.QuestionKey == Question.QuestionKey))).ToList();
                if ((edList == null) || (edList.Any() == false))
                {
                    return null;
                }

                return edList;
            }
        }

        private bool? GetHiddenOverride
        {
            get
            {
                // Hide if not homehealth discharge or transfer form
                if ((Admission == null) || (Admission.IsHomeHealth == false))
                {
                    return true;
                }

                if ((IsDischarge == false) && (IsTransfer == false))
                {
                    return true;
                }

                return null;
            }
        }

        public bool ShowAsterisk
        {
            get
            {
                return ((myEncounterData == null) || (Encounter == null) ||
                        (Encounter.EncounterStatus == (int)EncounterStatusType.Completed))
                    ? false
                    : myEncounterData.Where(e => e.BoolData == true).Any();
            }
        }

        public string AsteriskBlirb => "* -  Indicates this discipline will be discharged when this " +
                                       ((IsDischarge) ? "Discharge" : "Transfer") + " activity is completed.";

        public FormSection FormSection { get; set; }

        private bool IsDischarge => ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null))
            ? false
            : DynamicFormViewModel.CurrentForm.IsDischarge;

        private bool IsTransfer => ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null))
            ? false
            : DynamicFormViewModel.CurrentForm.IsTransfer;

        public override void Cleanup()
        {
            _DisciplineSummaryList = null;
            Messenger.Default.Unregister(this);
            if (myEncounterData != null)
            {
                foreach (EncounterData ed in myEncounterData) ed.ViewSummaryOfCareNarrative_Command = null;
            }

            base.Cleanup();
        }
    }

    public class DisciplineSummaryFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            DisciplineSummary ds = new DisciplineSummary(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                FormSection = formsection,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            ds.SetupDisciplineSummary();
            return ds;
        }
    }
}