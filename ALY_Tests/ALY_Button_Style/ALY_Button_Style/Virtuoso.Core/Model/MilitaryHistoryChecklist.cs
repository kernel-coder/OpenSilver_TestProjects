#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class MilitaryHistoryChecklist : QuestionBase
    {
        public override void Cleanup()
        {
            try
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        public MilitaryHistoryChecklist(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private MilitaryChecklistHistory _SaveMilitaryChecklistHistory;

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if ((MilitaryChecklistHistory != null) && (_SaveMilitaryChecklistHistory != null))
                {
                    MilitaryChecklistHistory.PatientOrFamilyServed =
                        _SaveMilitaryChecklistHistory.PatientOrFamilyServed;
                    MilitaryChecklistHistory.PatientServed = _SaveMilitaryChecklistHistory.PatientServed;
                    MilitaryChecklistHistory.CombatAssignment = _SaveMilitaryChecklistHistory.CombatAssignment;
                    MilitaryChecklistHistory.HasDischargePapers = _SaveMilitaryChecklistHistory.HasDischargePapers;
                    MilitaryChecklistHistory.SpouseServed = _SaveMilitaryChecklistHistory.SpouseServed;
                    MilitaryChecklistHistory.SpouseServedComments = _SaveMilitaryChecklistHistory.SpouseServedComments;
                    MilitaryChecklistHistory.ImmediateFamilyServed =
                        _SaveMilitaryChecklistHistory.ImmediateFamilyServed;
                    MilitaryChecklistHistory.MilitaryBranch = _SaveMilitaryChecklistHistory.MilitaryBranch;
                    MilitaryChecklistHistory.WarServed = _SaveMilitaryChecklistHistory.WarServed;
                    MilitaryChecklistHistory.WarExperienceComments =
                        _SaveMilitaryChecklistHistory.WarExperienceComments;
                    MilitaryChecklistHistory.MilitaryExperiencedStaffRequest =
                        _SaveMilitaryChecklistHistory.MilitaryExperiencedStaffRequest;
                    MilitaryChecklistHistory.EnrolledInVA = _SaveMilitaryChecklistHistory.EnrolledInVA;
                    MilitaryChecklistHistory.ReceivesBenefits = _SaveMilitaryChecklistHistory.ReceivesBenefits;
                    MilitaryChecklistHistory.ServiceConnectedCondition =
                        _SaveMilitaryChecklistHistory.ServiceConnectedCondition;
                    MilitaryChecklistHistory.RecievesMedicationsFromVA =
                        _SaveMilitaryChecklistHistory.RecievesMedicationsFromVA;
                    MilitaryChecklistHistory.NameOfClinic = _SaveMilitaryChecklistHistory.NameOfClinic;
                    MilitaryChecklistHistory.PrimaryCareProviderName =
                        _SaveMilitaryChecklistHistory.PrimaryCareProviderName;
                    MilitaryChecklistHistory.RequestsBenefitsDiscussion =
                        _SaveMilitaryChecklistHistory.RequestsBenefitsDiscussion;
                }
            }
            else
            {
                _SaveMilitaryChecklistHistory = null;
                if (MilitaryChecklistHistory != null)
                {
                    _SaveMilitaryChecklistHistory = (MilitaryChecklistHistory)Clone(MilitaryChecklistHistory);
                }
            }
        }

        private MilitaryChecklistHistory _MilitaryChecklistHistory;

        public MilitaryChecklistHistory MilitaryChecklistHistory
        {
            get { return _MilitaryChecklistHistory; }
            set
            {
                _MilitaryChecklistHistory = value;
                RaisePropertyChanged("MilitaryChecklistHistory");
            }
        }

        public bool ValidatePopup()
        {
            bool AllValid = true;

            return AllValid;
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            _currentErrors.Clear();

            if (MilitaryChecklistHistory == null)
            {
                return AllValid;
            }

            if (Patient == null)
            {
                return AllValid;
            }

            if (Patient.MilitaryChecklistHistory.Contains(MilitaryChecklistHistory) == false &&
                MilitaryChecklistHistory.IsNew)
            {
                Patient.MilitaryChecklistHistory.Add(MilitaryChecklistHistory);
            }

            return AllValid;
        }
    }

    public class MilitaryHistoryChecklistFactory
    {
        public static MilitaryHistoryChecklist Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            MilitaryHistoryChecklist qb = new MilitaryHistoryChecklist(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission
            };

            MilitaryChecklistHistory me = qb.Patient.MilitaryChecklistHistory.FirstOrDefault();

            if (me == null)
            {
                me = new MilitaryChecklistHistory();
            }

            qb.MilitaryChecklistHistory = me;

            return qb;
        }
    }
}