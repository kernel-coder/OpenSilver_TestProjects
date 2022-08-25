#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class HospiceQuestionBase : QuestionBase
    {
        public HospiceQuestionBase(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public virtual void OnProcessHospiceQuestion()
        {
        }

        public void ProcessHospiceQuestion()
        {
            OnProcessHospiceQuestion();
        }
    }

    public class HospiceQuestionBaseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceQuestionBase hq = new HospiceQuestionBase(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            hq.ProcessHospiceQuestion();
            return hq;
        }
    }

    public class HospiceDisciplineDischarge : HospiceQuestionBase
    {
        private AdmissionDiscipline CurrentAdmissionDiscipline;

        private Virtuoso.Server.Data.HospiceDisciplineDischarge _CurrentHospiceDisciplineDischarge;

        public Virtuoso.Server.Data.HospiceDisciplineDischarge CurrentHospiceDisciplineDischarge
        {
            get { return _CurrentHospiceDisciplineDischarge; }
            set
            {
                _CurrentHospiceDisciplineDischarge = value;
                RaisePropertyChanged("CurrentHospiceDisciplineDischarge");
                RaisePropertyChanged("PhysicianName");
                RaisePropertyChanged("HospiceDisciplineDischargeDate");
            }
        }

        public HospiceDisciplineDischarge(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public DateTime? HospiceDisciplineDischargeDate
        {
            // Using a pass through property so we can write out DischargeDate to both our 
            // HospiceDisciplineDischarge and AdmissionDiscipline entities
            get
            {
                return (CurrentHospiceDisciplineDischarge == null)
                    ? null
                    : CurrentHospiceDisciplineDischarge.DisciplineDischargeDate;
            }
            set
            {
                if ((value.HasValue) && (CurrentHospiceDisciplineDischarge != null))
                {
                    CurrentHospiceDisciplineDischarge.DisciplineDischargeDate = value.Value.Date;
                }
                else if ((value.HasValue == false) && (CurrentHospiceDisciplineDischarge != null))
                {
                    CurrentHospiceDisciplineDischarge.DisciplineDischargeDate = null;
                }

                RaisePropertyChanged("PhysicianName");
                RaisePropertyChanged("HospiceDisciplineDischargeDate");
            }
        }

        public string PhysicianName
        {
            get
            {
                // Attempt to find the active physician as of the Discharge Date
                if ((CurrentHospiceDisciplineDischarge == null) ||
                    (CurrentHospiceDisciplineDischarge.DisciplineDischargeDate == null))
                {
                    if (CurrentHospiceDisciplineDischarge != null)
                    {
                        CurrentHospiceDisciplineDischarge.AdmissionPhysicianKey = null;
                    }

                    return "";
                }

                AdmissionPhysician myPhysician = null;
                if ((Admission != null) && (Admission.AdmissionPhysician != null) &&
                    (CurrentHospiceDisciplineDischarge != null))
                {
                    myPhysician = Admission.AdmissionPhysician.Where(a => a.Inactive == false)
                        .Where(a => CodeLookupCache.GetCodeFromKey(a.PhysicianType) == "PCP")
                        .Where(a => a.PhysicianEffectiveFromDate.Date <=
                                    CurrentHospiceDisciplineDischarge.DisciplineDischargeDate)
                        .Where(a => a.PhysicianEffectiveThruDate == null || (a.PhysicianEffectiveThruDate.Value.Date >=
                                                                             CurrentHospiceDisciplineDischarge
                                                                                 .DisciplineDischargeDate))
                        .OrderByDescending(a => a.PhysicianEffectiveFromDate).FirstOrDefault();
                }

                if (myPhysician == null)
                {
                    if (CurrentHospiceDisciplineDischarge != null)
                    {
                        CurrentHospiceDisciplineDischarge.AdmissionPhysicianKey = null;
                    }

                    return "No active Attending/Primary Care physician as of the Discharge Date is found.";
                }

                if (CurrentHospiceDisciplineDischarge != null)
                {
                    CurrentHospiceDisciplineDischarge.AdmissionPhysicianKey = myPhysician.AdmissionPhysicianKey;
                }

                return myPhysician.PhysicianName;
            }
        }

        public string DischargedHospiceDisciplineText
        {
            get
            {
                string MyText = "";

                if (Encounter != null)
                {
                    MyText += DisciplineCache.GetDescriptionFromKey(Encounter.DisciplineKey.Value) +
                              "              Discipline Admission Date: " + Admission.AdmissionDiscipline
                                  .Where(a => a.DisciplineKey == Encounter.DisciplineKey.Value).FirstOrDefault()
                                  .DisciplineAdmitDateTime.Value.Date.ToString("MM/dd/yyyy");
                }

                return MyText;
            }
        }

        internal void SetupData()
        {
            CurrentAdmissionDiscipline = Admission.AdmissionDiscipline
                .Where(a => a.DisciplineKey == Encounter.DisciplineKey).FirstOrDefault();

            if (CurrentAdmissionDiscipline != null)
            {
                CurrentHospiceDisciplineDischarge = CurrentAdmissionDiscipline.HospiceDisciplineDischarge
                    .Where(a => a.AddedFromEncounterKey == Encounter.EncounterKey).FirstOrDefault();
            }

            if (CurrentHospiceDisciplineDischarge == null)
            {
                AddNewHospiceDisciplineDischargeToContext();
            }
        }

        private void AddNewHospiceDisciplineDischargeToContext()
        {
            var newHospiceDisciplineDischarge = new Virtuoso.Server.Data.HospiceDisciplineDischarge();

            newHospiceDisciplineDischarge.TenantID = CurrentAdmissionDiscipline.TenantID;
            newHospiceDisciplineDischarge.AdmissionDisciplineKey = CurrentAdmissionDiscipline.AdmissionDisciplineKey;
            newHospiceDisciplineDischarge.AdmissionDiscipline = CurrentAdmissionDiscipline;
            newHospiceDisciplineDischarge.PatientKey = Patient.PatientKey;
            newHospiceDisciplineDischarge.AddedFromEncounterKey = Encounter.EncounterKey;
            newHospiceDisciplineDischarge.UpdatedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            newHospiceDisciplineDischarge.InactiveDate = null;

            CurrentAdmissionDiscipline.HospiceDisciplineDischarge.Add(newHospiceDisciplineDischarge);

            // Also set data for binding
            CurrentHospiceDisciplineDischarge = newHospiceDisciplineDischarge;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (CurrentHospiceDisciplineDischarge == null)
            {
                return true;
            }

            CurrentHospiceDisciplineDischarge.ValidationErrors.Clear();

            if (((Encounter.FullValidation) && (Hidden == false) && (Protected == false)) == false)
            {
                return true;
            }

            bool allValid = true;

            if ((CurrentHospiceDisciplineDischarge.DisciplineDischargeReason == null) ||
                (CurrentHospiceDisciplineDischarge.DisciplineDischargeReason == 0))
            {
                CurrentHospiceDisciplineDischarge.ValidationErrors.Add(new ValidationResult(
                    "The Discipline Discharge Reason field is required.", new[] { "DisciplineDischargeReason" }));
                allValid = false;
            }

            if (CurrentHospiceDisciplineDischarge.DisciplineDischargeDate == null)
            {
                CurrentHospiceDisciplineDischarge.ValidationErrors.Add(new ValidationResult(
                    "The Discipline Discharge Date field is required.", new[] { "DisciplineDischargeDate" }));
                allValid = false;
            }

            if (string.IsNullOrWhiteSpace(CurrentHospiceDisciplineDischarge.DisciplineDischargeSummary))
            {
                CurrentHospiceDisciplineDischarge.ValidationErrors.Add(new ValidationResult(
                    "The Discipline Discharge Summary field is required.", new[] { "DisciplineDischargeSummary" }));
                allValid = false;
            }

            return allValid;
        }
    }

    public class HospiceDisciplineDischargeFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceDisciplineDischarge hq = new HospiceDisciplineDischarge(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };

            hq.SetupData();
            return hq;
        }
    }

    public class HospiceElectionStatement : HospiceQuestionBase
    {
        public HospiceElectionStatement(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void Cleanup()
        {
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }

            base.Cleanup();
        }

        public override void OnProcessHospiceQuestion()
        {
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged += EncounterData_PropertyChanged;
            }
        }

        public new void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TextData"))
            {
                QuestionValueChanged.Execute(sender);
            }
        }

        public bool HospiceElectionStatementHidden
        {
            get
            {
                if (EncounterData == null)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(EncounterData.TextData) == false)
                {
                    return false; // ElectionStatement data already exists - show it
                }

                // collect ElectionStatement data only if the (controlling payer) insurance requires it
                if ((Patient == null) || (Admission == null))
                {
                    return true;
                }

                if ((Admission.PatientInsuranceKey == null) || (Patient.PatientInsurance == null))
                {
                    return true;
                }

                PatientInsurance pi = Patient.PatientInsurance
                    .Where(p => p.PatientInsuranceKey == Admission.PatientInsuranceKey).FirstOrDefault();
                if (pi == null)
                {
                    return true;
                }

                Insurance i = InsuranceCache.GetInsuranceFromKey(pi.InsuranceKey);
                if (i == null)
                {
                    return true;
                }

                return (i.RequireBenefitElection) ? false : true;
            }
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (EncounterData == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(EncounterData.TextData))
            {
                if (Required && Encounter.FullValidation && (HospiceElectionStatementHidden == false))
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult((string.Format("{0} is required", Label)),
                        new[] { "TextData" }));
                    return false;
                }

                if (EncounterData.EntityState == EntityState.Modified)
                {
                    Encounter.EncounterData.Remove(EncounterData);
                    EncounterData = new EncounterData
                    {
                        SectionKey = EncounterData.SectionKey, QuestionGroupKey = EncounterData.QuestionGroupKey,
                        QuestionKey = EncounterData.QuestionKey
                    };
                }
            }
            else
            {
                if (EncounterData.TextData == "0")
                {
                    EncounterData.Int2Data = 0;
                }

                if (EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(EncounterData);
                }
            }

            return true;
        }
    }

    public class HospiceElectionStatementFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceElectionStatement hq = new HospiceElectionStatement(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey,
                    BoolData = true
                };
                hq.EncounterData = ed;
                hq.ApplyDefaults();

                hq.CopyForwardLastInstance();
                hq.EncounterData.BoolData = (hq.EncounterData.TextData == "1") ? false : true;
            }
            else
            {
                hq.EncounterData = ed;
            }

            hq.ProcessHospiceQuestion();
            return hq;
        }
    }

    public class HospicePatientRiskAssessment : HospiceQuestionBase
    {
        public HospicePatientRiskAssessment(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region RiskAssessment

        public string ReevaluatePopupLabel => (CurrentRiskAssessment == null)
            ? "Bereavement Risk Assessment"
            : CurrentRiskAssessment.Label;

        private string _PopupDataTemplate = "ReEvaluatePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }
                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public IDynamicFormService ReEvalFormModel { get; set; }

        ObservableCollection<SectionUI> _ReEvalSections;

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSections);
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSection);
            }
        }

        private bool _OpenReEval;

        public bool OpenReEval
        {
            get { return _OpenReEval; }
            set
            {
                if (_OpenReEval != value)
                {
                    _OpenReEval = value;
                    this.RaisePropertyChangedLambda(p => p.OpenReEval);
                }
            }
        }

        public bool Loading { get; set; }

        public RelayCommand ReEvaluateCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }

        private int RiskAssessmentInUse;

        private void ReEvaluateSetup()
        {
            ReEvaluateCommand = new RelayCommand(() =>
            {
                if (ReEvalSection == null)
                {
                    RiskAssessment ra = CurrentRiskAssessment;
                    if (ra == null)
                    {
                        MessageBox.Show("Bereavement Risk Assessment definition not found.  contact AlayaCare support.");
                        return;
                    }

                    RiskAssessmentInUse = ra.RiskAssessmentKey;
                    FormSection formsection = new FormSection
                    {
                        RiskAssessmentKey = ra.RiskAssessmentKey, RiskAssessment = ra, FormSectionKey = 0,
                        FormKey = DynamicFormViewModel.CurrentForm.FormKey, Section = null, SectionKey = 0, Sequence = 1
                    };

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection, ReEvalSections, false, false, true);
                    ReEvalSection = ReEvalSections.FirstOrDefault();

                    if (!Convert.ToBoolean(EncounterData.BoolData))
                    {
                        //since we are re-evaluating a section we need to go and find the latest data
                        //look for a previous visit first and default to eval and no re-eval found
                        Encounter en = Admission.Encounter.Where(p => (!p.IsNew))
                            .OrderBy(p => p.EncounterOrTaskStartDateAndTime).FirstOrDefault();

                        foreach (var item in
                                 Admission.Encounter
                                     .Where(e => e.Form != null)
                                     .Where(p =>
                                         !p.IsNew &&
                                         !p.Form.IsPlanOfCare &&
                                         !p.Form.IsTransfer)
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            EncounterRisk er = item.EncounterRisk.Where(p =>
                                    ((p.RiskAssessmentKey == ra.RiskAssessmentKey) && (p.RiskForID == null)))
                                .FirstOrDefault();
                            if (er != null)
                            {
                                en = item;
                                break;
                            }
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvalSection.Questions) q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvalSection.Questions) q.PreProcessing();
                    Loading = false;
                }
                else
                {
                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    //backup original in case we need to revert back to it
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(() =>
            {
                if (!Protected)
                {
                    if (EncounterData.IntData == null)
                    {
                        EncounterData.IntData = RiskAssessmentInUse;
                    }

                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    EncounterData.BoolData = true;
                    FetchRiskAssessmentScore();
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                foreach (QuestionUI qui in ReEvalSection.Questions)
                {
                    Risk quiR = qui as Risk;
                    if (quiR != null)
                    {
                        quiR.RestoreRiskAssessmentEncounterRisks2();
                    }
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });
        }

        private void FetchRiskAssessmentScore()
        {
            if (ReEvalSection != null)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    Risk r = q as Risk;
                    if (r != null)
                    {
                        EncounterData.TextData = "Risk Assessment - Done" +
                                                 ((string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                                     ? ""
                                                     : ((CurrentRiskAssessment.IncludeComment) ? ", w/comments" : ""));
                        if ((r.CurrentRiskRange != null) && (r.TotalRecord != null))
                        {
                            string score = (r.TotalRecord.Score == null)
                                ? ""
                                : r.TotalRecord.Score.ToString().Trim() + " - ";
                            EncounterData.Text2Data = score + r.CurrentRiskRange.Label;
                        }
                    }
                }
            }
        }

        private RiskAssessment CurrentRiskAssessment
        {
            get
            {
                RiskAssessment ra = null;
                if ((EncounterData != null) && (EncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)EncounterData.IntData);
                    if (ra != null)
                    {
                        return ra;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra;
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel("Bereavement Risk Assessment");
                return ra;
            }
        }

        private int RiskAssessmentKey => (CurrentRiskAssessment == null) ? 0 : CurrentRiskAssessment.RiskAssessmentKey;

        public override void PreProcessing()
        {
            if (Hidden)
            {
                return;
            }

            if (EncounterData.BoolData.HasValue)
            {
                Loading = true;
                ReEvaluateCommand.Execute(null);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any())
                {
                    if (OasisManager != null)
                    {
                        OasisManager.QuestionOasisMappingChanged(Question, EncounterData);
                    }
                }
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (Hidden)
            {
                return true;
            }

            if (OpenReEval)
            {
                OK_Command.Execute(null);
            }

            bool AllValid = base.Validate(out SubSections);

            if (ReEvalSection != null && AllValid && EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    string ErrorSection = string.Empty;

                    if (!q.Validate(out ErrorSection))
                    {
                        if (string.IsNullOrEmpty(SubSections))
                        {
                            SubSections = ReEvalSection.Label;
                        }

                        AllValid = false;
                    }
                }
            }

            return AllValid;
        }

        public void RiskCleanup()
        {
            if (ReEvalSection != null && ReEvalSection.Questions != null)
            {
                foreach (var q in ReEvalSection.Questions) q.Cleanup();
            }
        }

        #endregion

        public override void Cleanup()
        {
            RiskCleanup();
            base.Cleanup();
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }
        }

        public void HospicePatientRiskAssessmentSetup()
        {
            EncounterData ed = Encounter.EncounterData.Where(x =>
                x.EncounterKey == Encounter.EncounterKey && x.SectionKey == Section.SectionKey &&
                x.QuestionGroupKey == QuestionGroupKey && x.QuestionKey == Question.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                if ((CanCopyForwardLastInstance() == false) &&
                    ((DynamicFormViewModel != null) && (DynamicFormViewModel.CurrentForm != null) &&
                     DynamicFormViewModel.CurrentForm.FormContainsQuestion(
                         DynamicFormCache.GetSingleQuestionByDataTemplate("HospiceAnticipatoryRiskAssessment"))))
                {
                    // this form contains the new bereaved question as well (like in model Teammeeting) - but contains no legacy data - so hide it, in favor of the new question
                    HiddenOverride = true;
                    return;
                }

                ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = "Risk Assessment - Not Done", Text2Data = "None"
                };
                EncounterData = ed;
                CopyForwardLastInstance();
            }
            else
            {
                EncounterData = ed;
            }

            ProcessHospiceQuestion();
            ReEvaluateSetup();
        }

        public bool CanCopyForwardLastInstance()
        {
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterData previousED = previousE.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                    .FirstOrDefault();
                if (previousED != null)
                {
                    EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                        x.RiskForID == null && x.IsTotal && x.RiskAssessmentKey == RiskAssessmentKey &&
                        !x.RiskGroupKey.HasValue).FirstOrDefault();
                    if (previousER != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterData previousED = previousE.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                    .FirstOrDefault();
                if (previousED != null)
                {
                    EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                        x.RiskForID == null && x.IsTotal && x.RiskAssessmentKey == RiskAssessmentKey &&
                        !x.RiskGroupKey.HasValue).FirstOrDefault();
                    if (previousER != null)
                    {
                        CopyProperties(previousED);
                        Encounter.CopyForwardRiskAssessment(previousE, RiskAssessmentKey, null);
                        return true;
                    }
                }
            }

            return false;
        }

        public override void OnProcessHospiceQuestion()
        {
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged += EncounterData_PropertyChanged;
            }
        }

        public new void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TextData"))
            {
                QuestionValueChanged.Execute(sender);
            }
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }
    }

    public class HospicePatientRiskAssessmentFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospicePatientRiskAssessment hq = new HospicePatientRiskAssessment(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            hq.HospicePatientRiskAssessmentSetup();
            return hq;
        }
    }

    public class HospiceAnticipatoryRiskAssessment : HospiceQuestionBase
    {
        public HospiceAnticipatoryRiskAssessment(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region RiskAssessment

        public string ReevaluatePopupLabel => CurrentRiskAssessmentString;
        private string _PopupDataTemplate = "ReEvaluatePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }
                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public IDynamicFormService ReEvalFormModel { get; set; }

        ObservableCollection<SectionUI> _ReEvalSections;

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSections);
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                this.RaisePropertyChangedLambda(p => p.ReEvalSection);
            }
        }

        private bool _OpenReEval;

        public bool OpenReEval
        {
            get { return _OpenReEval; }
            set
            {
                if (_OpenReEval != value)
                {
                    _OpenReEval = value;
                    this.RaisePropertyChangedLambda(p => p.OpenReEval);
                }
            }
        }

        public bool Loading { get; set; }

        public RelayCommand ReEvaluateCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }

        private int RiskAssessmentInUse;

        private void ReEvaluateSetup()
        {
            ReEvaluateCommand = new RelayCommand(() =>
            {
                if (ReEvalSection == null)
                {
                    RiskAssessment ra = CurrentRiskAssessment;
                    if (ra == null)
                    {
                        MessageBox.Show(CurrentRiskAssessmentString + " definition not found.  contact AlayaCare support.");
                        return;
                    }

                    RiskAssessmentInUse = ra.RiskAssessmentKey;
                    FormSection formsection = new FormSection
                    {
                        RiskAssessmentKey = ra.RiskAssessmentKey, RiskAssessment = ra, FormSectionKey = 0,
                        FormKey = DynamicFormViewModel.CurrentForm.FormKey, Section = null, SectionKey = 0, Sequence = 1
                    };

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection, ReEvalSections, false, false, true);
                    ReEvalSection = ReEvalSections.FirstOrDefault();

                    if (!Convert.ToBoolean(EncounterData.BoolData))
                    {
                        //since we are re-evaluating a section we need to go and find the latest data
                        //look for a previous visit first and default to eval and no re-eval found
                        Encounter en = Admission.Encounter.Where(p => (!p.IsNew))
                            .OrderBy(p => p.EncounterOrTaskStartDateAndTime).FirstOrDefault();

                        foreach (var item in
                                 Admission.Encounter
                                     .Where(e => e.Form != null)
                                     .Where(p =>
                                         !p.IsNew &&
                                         !p.Form.IsPlanOfCare &&
                                         !p.Form.IsTransfer)
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            EncounterRisk er = item.EncounterRisk.Where(p =>
                                    ((p.RiskAssessmentKey == ra.RiskAssessmentKey) && (p.RiskForID == null)))
                                .FirstOrDefault();
                            if (er != null)
                            {
                                en = item;
                                break;
                            }
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvalSection.Questions) q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvalSection.Questions) q.PreProcessing();
                    Loading = false;
                }
                else
                {
                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    //backup original in case we need to revert back to it
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(() =>
            {
                if (!Protected)
                {
                    if (EncounterData.IntData == null)
                    {
                        EncounterData.IntData = RiskAssessmentInUse;
                    }

                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            if (quiR.ValidateRequireResponseOnPopup() == false)
                            {
                                return;
                            }

                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    EncounterData.BoolData = true;
                    FetchRiskAssessmentScore();
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                foreach (QuestionUI qui in ReEvalSection.Questions)
                {
                    Risk quiR = qui as Risk;
                    if (quiR != null)
                    {
                        quiR.RestoreRiskAssessmentEncounterRisks2();
                    }
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });
        }

        private void FetchRiskAssessmentScore()
        {
            if (ReEvalSection != null)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    Risk r = q as Risk;
                    if (r != null)
                    {
                        EncounterData.TextData = CurrentRiskAssessmentString + " - Done" +
                                                 ((string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                                     ? ""
                                                     : ((CurrentRiskAssessment.IncludeComment) ? ", w/comments" : ""));
                        if ((r.CurrentRiskRange != null) && (r.TotalRecord != null))
                        {
                            string score = (r.TotalRecord.Score == null)
                                ? ""
                                : r.TotalRecord.Score.ToString().Trim() + " - ";
                            EncounterData.Text2Data = score + r.CurrentRiskRange.Label;
                        }
                    }
                }
            }
        }

        private string CurrentRiskAssessmentString
        {
            get
            {
                RiskAssessment ra = null;
                if ((EncounterData != null) && (EncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)EncounterData.IntData);
                    if (ra != null)
                    {
                        return ra.Label;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra.Label;
                }

                return "Bereavement Risk Assessment";
            }
        }

        private RiskAssessment CurrentRiskAssessment
        {
            get
            {
                RiskAssessment ra = null;
                if ((EncounterData != null) && (EncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)EncounterData.IntData);
                    if (ra != null)
                    {
                        return ra;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra;
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel("Bereavement Risk Assessment");
                return ra;
            }
        }

        private int RiskAssessmentKey => (CurrentRiskAssessment == null) ? 0 : CurrentRiskAssessment.RiskAssessmentKey;

        public override void PreProcessing()
        {
            if (EncounterData.BoolData.HasValue)
            {
                Loading = true;
                ReEvaluateCommand.Execute(null);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any())
                {
                    if (OasisManager != null)
                    {
                        OasisManager.QuestionOasisMappingChanged(Question, EncounterData);
                    }
                }
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (OpenReEval)
            {
                OK_Command.Execute(null);
            }

            bool AllValid = base.Validate(out SubSections);

            if (ReEvalSection != null && AllValid && EncounterData.BoolData.HasValue && EncounterData.BoolData.Value)
            {
                foreach (var q in ReEvalSection.Questions)
                {
                    string ErrorSection = string.Empty;

                    if (!q.Validate(out ErrorSection))
                    {
                        if (string.IsNullOrEmpty(SubSections))
                        {
                            SubSections = ReEvalSection.Label;
                        }

                        AllValid = false;
                    }
                }
            }

            return AllValid;
        }

        public void RiskCleanup()
        {
            if (ReEvalSection != null && ReEvalSection.Questions != null)
            {
                foreach (var q in ReEvalSection.Questions) q.Cleanup();
            }
        }

        #endregion

        public override void Cleanup()
        {
            RiskCleanup();
            base.Cleanup();
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }
        }

        public void HospiceAnticipatoryRiskAssessmentSetup()
        {
            EncounterData ed = Encounter.EncounterData.Where(x =>
                x.EncounterKey == Encounter.EncounterKey && x.SectionKey == Section.SectionKey &&
                x.QuestionGroupKey == QuestionGroupKey && x.QuestionKey == Question.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = CurrentRiskAssessmentString + " - Not Done",
                    Text2Data = "None"
                };
                EncounterData = ed;
                CopyForwardLastInstance();
            }
            else
            {
                EncounterData = ed;
            }

            ProcessHospiceQuestion();
            ReEvaluateSetup();
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterData previousED = previousE.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                    .FirstOrDefault();
                if (previousED != null)
                {
                    EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                        x.RiskForID == null && x.IsTotal && x.RiskAssessmentKey == RiskAssessmentKey &&
                        !x.RiskGroupKey.HasValue).FirstOrDefault();
                    if (previousER != null)
                    {
                        CopyProperties(previousED);
                        Encounter.CopyForwardRiskAssessment(previousE, RiskAssessmentKey, null);
                        return true;
                    }
                }
            }

            return false;
        }

        public override void OnProcessHospiceQuestion()
        {
            if (EncounterData != null)
            {
                EncounterData.PropertyChanged += EncounterData_PropertyChanged;
            }
        }

        public new void EncounterData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TextData"))
            {
                QuestionValueChanged.Execute(sender);
            }
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }
    }

    public class HospiceAnticipatoryRiskAssessmentFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceAnticipatoryRiskAssessment hq = new HospiceAnticipatoryRiskAssessment(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            hq.HospiceAnticipatoryRiskAssessmentSetup();
            return hq;
        }
    }

    public class PalliativePerformanceScaleItem
    {
        public string PPSLevel { get; set; }
        public string Ambulation { get; set; }
        public string Activity { get; set; }
        public string SelfCare { get; set; }
        public string Intake { get; set; }
        public string ConsciousLevel { get; set; }
    }

    public class HospicePalliativePerformanceScale : HospiceQuestionBase
    {
        public RelayCommand PPSOK_Command { get; set; }
        public RelayCommand PPSCancel_Command { get; set; }

        public HospicePalliativePerformanceScale(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            PPSv2Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                EncounterData.BeginEditting();
                DynamicFormViewModel.PopupDataContext = this;
                if (EncounterData.TextData == null)
                {
                    EncounterData.TextData = "100%";
                }

                PPSSelectedItem = PPSList2.Where(a => a.PPSLevel == EncounterData.TextData).FirstOrDefault();
            });

            PPSOK_Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                if (EncounterData.Validate())
                {
                    string score = PPSSelectedItem.PPSLevel;
                    if (EncounterData.TextData != score)
                    {
                        if (!Encounter.EncounterData.Any(ed => ed.EncounterDataKey == EncounterData.EncounterDataKey))
                        {
                            Encounter.EncounterData.Add(EncounterData);
                        }

                        EncounterData.TextData = score;
                    }

                    DynamicFormViewModel.PopupDataContext = null;
                    EncounterData.EndEditting();
                }
            });

            PPSCancel_Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.PopupDataContext = null;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                EncounterData.CancelEditting();
            });
        }

        private string _PopupDataTemplate = "PPSPopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        private List<PalliativePerformanceScaleItem> _PPSList;

        public List<PalliativePerformanceScaleItem> PPSList
        {
            get { return _PPSList; }
            set
            {
                _PPSList = value;
                this.RaisePropertyChangedLambda(p => p.PPSList);
            }
        }

        private List<PalliativePerformanceScaleItem> _PPSList2;

        public List<PalliativePerformanceScaleItem> PPSList2
        {
            get { return _PPSList2; }
            set
            {
                _PPSList2 = value;
                this.RaisePropertyChangedLambda(p => p.PPSList2);
            }
        }

        private PalliativePerformanceScaleItem _PPSSelectedItem;

        public PalliativePerformanceScaleItem PPSSelectedItem
        {
            get { return _PPSSelectedItem; }
            set
            {
                _PPSSelectedItem = value;
                if ((PPSSelectedItem != null) && (string.IsNullOrWhiteSpace(PPSSelectedItem.PPSLevel)))
                {
                    PPSSelectedItem = null;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.RaisePropertyChangedLambda(p => p.PPSSelectedItem);
                });
            }
        }

        public RelayCommand PPSv2Command { get; set; }

        public override void OnProcessHospiceQuestion()
        {
            // populate PalliativePerformanceScale 
            PPSList = new List<PalliativePerformanceScaleItem>();
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = " ",
                Ambulation = " ",
                Activity = " ",
                SelfCare = " ",
                Intake = " ",
                ConsciousLevel = " "
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "100%",
                Ambulation = "Full",
                Activity = "Normal activity & work.  No evidence of disease.",
                SelfCare = "Full",
                Intake = "Normal",
                ConsciousLevel = "Full"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "90%",
                Ambulation = "Full",
                Activity = "Normal activity & work.  Some evidence of disease.",
                SelfCare = "Full",
                Intake = "Normal",
                ConsciousLevel = "Full"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "80%",
                Ambulation = "Full",
                Activity = "Normal activity with Effort.  Some evidence of disease.",
                SelfCare = "Full",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "70%",
                Ambulation = "Reduced",
                Activity = "Unable Normal Job/Work.  Significant disease.",
                SelfCare = "Full",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "60%",
                Ambulation = "Reduced",
                Activity = "Unable hobby/house work.  Significant disease.",
                SelfCare = "Occasional assistance necessary",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full or Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "50%",
                Ambulation = "Mainly Sit/Lie",
                Activity = "Unable to do any work.  Extensive disease.",
                SelfCare = "Considerable assistance required",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full or Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "40%",
                Ambulation = "Mainly in Bed",
                Activity = "Unable to do most activity.  Extensive disease.",
                SelfCare = "Mainly assistance",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full or Drowsy  +/- Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "30%",
                Ambulation = "Totally Bed Bound",
                Activity = "Unable to do any activity.  Extensive disease.",
                SelfCare = "Total Care",
                Intake = "Normal or reduced",
                ConsciousLevel = "Full or Drowsy  +/- Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "20%",
                Ambulation = "Totally Bed Bound",
                Activity = "Unable to do any activity.  Extensive disease.",
                SelfCare = "Total Care",
                Intake = "Minimal to sips",
                ConsciousLevel = "Full or Drowsy  +/- Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "10%",
                Ambulation = "Totally Bed Bound",
                Activity = "Unable to do any activity.  Extensive disease.",
                SelfCare = "Total Care",
                Intake = "Mouth care only",
                ConsciousLevel = "Drowsy or Coma +/- Confusion"
            });
            PPSList.Add(new PalliativePerformanceScaleItem
            {
                PPSLevel = "0%",
                Ambulation = "Death",
                Activity = "-",
                SelfCare = "-",
                Intake = "-",
                ConsciousLevel = "-"
            });
            PPSList2 = new List<PalliativePerformanceScaleItem>();
            foreach (PalliativePerformanceScaleItem i in PPSList)
                if (string.IsNullOrWhiteSpace(i.PPSLevel) == false)
                {
                    PPSList2.Add(i);
                }

            this.RaisePropertyChangedLambda(p => p.PPSList);
            this.RaisePropertyChangedLambda(p => p.PPSList2);
            if (EncounterData != null)
            {
                PPSSelectedItem = PPSList.Where(p => p.PPSLevel == EncounterData.TextData).FirstOrDefault();
            }
        }
    }

    public class HospicePalliativePerformanceScaleFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospicePalliativePerformanceScale hq = new HospicePalliativePerformanceScale(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                hq.EncounterData = ed;
                hq.ApplyDefaults();

                hq.CopyForwardLastInstance();
            }
            else
            {
                hq.EncounterData = ed;
            }

            hq.ProcessHospiceQuestion();
            return hq;
        }
    }

    public class PreEvalStatusItem
    {
        public string Status { get; set; }
    }

    public class PreEvalStatus : QuestionUI
    {
        public string NotTakenReasonCodeType => (Admission == null)
            ? "NotTakenReason"
            : (Admission.HospiceAdmission ? "NotTakenReason" : "HospiceNotTakeReason");

        public PreEvalStatus(Admission admission, Encounter encounter, int? formSectionQuestionKey) : base(
            formSectionQuestionKey)
        {
            Admission = admission;
            Encounter = encounter;
            Admission.AdmissionGroupDate = Encounter.EncounterStartDate.HasValue
                ? Encounter.EncounterStartDate.Value.DateTime
                : DateTime.Today;
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        private List<PreEvalStatusItem> _PreEvalStatusList;

        public List<PreEvalStatusItem> PreEvalStatusList
        {
            get { return _PreEvalStatusList; }
            set
            {
                _PreEvalStatusList = value;
                this.RaisePropertyChangedLambda(p => p.PreEvalStatusList);
            }
        }

        public void Setup()
        {
            PreEvalStatusList = new List<PreEvalStatusItem>();
            PreEvalStatusList.Add(new PreEvalStatusItem { Status = Admission.PreEvalStatusPlanToAdmit });
            PreEvalStatusList.Add(new PreEvalStatusItem { Status = Admission.PreEvalStatusDoNotAdmit });
            PreEvalStatusList.Add(new PreEvalStatusItem { Status = Admission.PreEvalStatusOnHold });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            Admission.ValidationErrors.Clear();

            Admission.ValidateState_IsPreEval =
                ((Encounter.FullValidation) && DynamicFormViewModel.CurrentForm.IsPreEval);

            if (!Admission.Validate())
            {
                Admission.ValidateState_IsPreEval = false;
                return false;
            }

            return true;
        }
    }

    public class PreEvalStatusFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PreEvalStatus a = new PreEvalStatus(vm.CurrentAdmission, vm.CurrentEncounter, __FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Admission = vm.CurrentAdmission,
                DynamicFormViewModel = vm,
                OasisManager = vm.CurrentOasisManager,
            };
            a.Setup();
            return a;
        }
    }


    public class HospiceWorksheetItem
    {
        public string Worksheet { get; set; }
    }

    public class HospiceWorksheet : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public HospiceWorksheet(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        public void HospiceWorksheetSetup()
        {
            AddHospiceWorksheetCommand = new RelayCommand<HospiceWorksheet>(hospiceWorksheet =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick();
                }
            });
            DeleteHospiceWorksheetCommand = new RelayCommand<QuestionBase>(hospiceWorksheetQuestion =>
            {
                QuestionBase q = hospiceWorksheetQuestion;
                if (q == null)
                {
                    return;
                }

                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

                HospiceWorksheets.Remove(q);
                SetupHospiceWorksheetList();
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });

            HospiceWorksheets = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                             x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                             x.SectionKey == Section.SectionKey &&
                             x.QuestionGroupKey == QuestionGroupKey && x.QuestionKey == Question.QuestionKey)
                         .OrderBy(x => x.AddedDateTime))
                HospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            if ((HospiceWorksheets.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }

            SetupHospiceWorksheetList();
        }

        private void DefaultHospiceWorksheets()
        {
            //default with one and allow more to be added
            if (HospiceWorksheets.Any())
            {
                return;
            }

            EncounterData ed = new EncounterData
            {
                SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                QuestionKey = Question.QuestionKey, TextData = "Decline in clinical status", IntData = 0,
                Text2Data = null, Int2Data = 0, BoolData = true, AddedDateTime = DateTime.UtcNow
            };
            HospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
            {
                Admission = Admission, Encounter = Encounter, EncounterData = ed, Question = Question,
                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
            });
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }

            _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
        }

        private void SetupHospiceWorksheetList()
        {
            HospiceWorksheetList = new List<HospiceWorksheetItem>();
            List<CodeLookup> clList = CodeLookupCache.GetCodeLookupsFromType("HospiceWorksheet");
            foreach (CodeLookup cl in clList)
                if (HospiceWorksheets != null)
                {
                    if (HospiceWorksheets.Where(h => h.EncounterData.TextData == cl.CodeDescription).FirstOrDefault() ==
                        null)
                    {
                        HospiceWorksheetList.Add(new HospiceWorksheetItem { Worksheet = cl.CodeDescription });
                    }
                }

            this.RaisePropertyChangedLambda(p => p.HospiceWorksheetList);
        }

        public ObservableCollection<QuestionBase> HospiceWorksheets { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceWorksheets { get; set; }

        public RelayCommand<HospiceWorksheet> AddHospiceWorksheetCommand { get; set; }
        public RelayCommand<QuestionBase> DeleteHospiceWorksheetCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }

        private List<HospiceWorksheetItem> _HospiceWorksheetList;

        public List<HospiceWorksheetItem> HospiceWorksheetList
        {
            get { return _HospiceWorksheetList; }
            set
            {
                _HospiceWorksheetList = value;
                this.RaisePropertyChangedLambda(p => p.HospiceWorksheetList);
            }
        }

        private HospiceWorksheetItem _SelectedHospiceWorksheetItem;

        public HospiceWorksheetItem SelectedHospiceWorksheetItem
        {
            get { return _SelectedHospiceWorksheetItem; }
            set
            {
                _SelectedHospiceWorksheetItem = value;
                if (_popupProvider != null)
                {
                    _popupProvider.BeginClosingPopup();
                }

                if (_SelectedHospiceWorksheetItem != null)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                        QuestionKey = Question.QuestionKey, TextData = _SelectedHospiceWorksheetItem.Worksheet,
                        IntData = 0, Text2Data = null, Int2Data = 0, BoolData = true, AddedDateTime = DateTime.UtcNow
                    };
                    HospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = DynamicFormViewModel.CurrentEncounter, EncounterData = ed,
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                SetupHospiceWorksheetList();
                this.RaisePropertyChangedLambda(p => p.SelectedHospiceWorksheetItem);
            }
        }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            EncounterData.BoolData =
                (EncounterData.IntData == 1)
                    ? false
                    : true; // CAN edit/delete NOT completed worksheets, CANNOT edit/delete COMPLETED worksheets
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            HospiceWorksheets = new ObservableCollection<QuestionBase>();

            bool found = false;
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                             .OrderBy(d => d.AddedDateTime))
                    if (ed != null)
                    {
                        HospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                            Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
                        found = true;
                    }

                if (found)
                {
                    break;
                }
            }

            DefaultHospiceWorksheets();
            SetupHospiceWorksheetList();
            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceWorksheets != null)
                {
                    foreach (var item in HospiceWorksheets)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceWorksheets = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceWorksheets)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceWorksheets);
            }
            else
            {
                BackupHospiceWorksheets = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceWorksheets)
                    BackupHospiceWorksheets.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                        ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceWorksheets)
            {
                item.EncounterData.ValidationErrors.Clear();
                if (item.EncounterData.IntData == 0)
                {
                    item.EncounterData.Text2Data = null;
                    item.EncounterData.Int2Data = 0;
                }
                else if ((Encounter.FullValidation) && (item.EncounterData.IntData == 1) &&
                         string.IsNullOrWhiteSpace(item.EncounterData.Text2Data))
                {
                    item.EncounterData.ValidationErrors.Add(
                        new ValidationResult("Status is required on completed worksheets", new[] { "Text2Data" }));
                    AllValid = false;
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceWorksheetFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceWorksheet hw = new HospiceWorksheet(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hw.HospiceWorksheetSetup();
            return hw;
        }
    }

    public class HospiceSymptom : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public HospiceSymptom(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        public void HospiceSymptomSetup()
        {
            AddHospiceSymptomCommand = new RelayCommand<HospiceSymptom>(hospiceSymptom =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick();
                }
            });
            DeleteHospiceSymptomCommand = new RelayCommand<QuestionBase>(hospiceSymptomQuestion =>
            {
                QuestionBase q = hospiceSymptomQuestion;
                if (q == null)
                {
                    return;
                }

                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

                HospiceSymptoms.Remove(q);
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildTextBoxInputLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChildTextBoxInput(frameworkElement as TextBox);
                }
            });
            PopupChildTextBoxInputUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });

            HospiceSymptoms = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                         x.SectionKey == Section.SectionKey && x.QuestionGroupKey == QuestionGroupKey &&
                         x.QuestionKey == Question.QuestionKey).OrderBy(x => x.AddedDateTime).ThenBy(x => x.TextData))
                HospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            if ((HospiceSymptoms.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }
        }

        private void DefaultHospiceSymptoms()
        {
            if (HospiceSymptoms.Any())
            {
                return;
            }

            List<CodeLookup> clList = CodeLookupCache.GetCodeLookupsFromType("HospiceSymptom");
            if (clList == null)
            {
                return;
            }

            if (clList.Any() == false)
            {
                return;
            }

            foreach (CodeLookup cl in clList)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = cl.CodeDescription, Text2Data = null,
                    BoolData = false, AddedDateTime = DateTime.UtcNow
                };
                HospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            }

            this.RaisePropertyChangedLambda(p => p.HospiceSymptoms);
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }

            _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
        }

        public ObservableCollection<QuestionBase> HospiceSymptoms { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceSymptoms { get; set; }

        public RelayCommand<HospiceSymptom> AddHospiceSymptomCommand { get; set; }
        public RelayCommand<QuestionBase> DeleteHospiceSymptomCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputUnLoaded { get; set; }

        public bool CanAddHospiceSymptom
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return (DynamicFormCache.IsPreEval(DynamicFormViewModel.CurrentForm.FormKey)) ? false : true;
            }
        }

        private String _AddSymptom;

        public String AddSymptom
        {
            get { return _AddSymptom; }
            set
            {
                _AddSymptom = value;
                if (_popupProvider != null)
                {
                    _popupProvider.BeginClosingPopup();
                }

                if (string.IsNullOrWhiteSpace(AddSymptom) == false)
                {
                    // Only add the symptom if its new
                    QuestionBase q = HospiceSymptoms
                        .Where(h => h.EncounterData.TextData.ToLower() == AddSymptom.ToLower().Trim()).FirstOrDefault();
                    if (q == null)
                    {
                        EncounterData ed = new EncounterData
                        {
                            SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                            QuestionKey = Question.QuestionKey, TextData = AddSymptom.Trim(), Text2Data = null,
                            BoolData = true, AddedDateTime = DateTime.UtcNow
                        };
                        HospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = DynamicFormViewModel.CurrentEncounter,
                            EncounterData = ed, Question = Question,
                            ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
                    }
                }

                if (AddSymptom != null)
                {
                    AddSymptom = null;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.RaisePropertyChangedLambda(p => p.AddSymptom);
                });
            }
        }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = false; // can edit/delete
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            HospiceSymptoms = new ObservableCollection<QuestionBase>();

            bool found = false;
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                             .OrderBy(d => d.AddedDateTime).ThenBy(d => d.TextData))
                    if (ed != null)
                    {
                        HospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                            Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
                        found = true;
                    }

                if (found)
                {
                    break;
                }
            }

            DefaultHospiceSymptoms();
            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceSymptoms != null)
                {
                    foreach (var item in HospiceSymptoms)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceSymptoms = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceSymptoms)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceSymptoms);
            }
            else
            {
                BackupHospiceSymptoms = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceSymptoms)
                    BackupHospiceSymptoms.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                        ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceSymptoms)
            {
                item.EncounterData.ValidationErrors.Clear();
                if ((Encounter.FullValidation) && string.IsNullOrWhiteSpace(item.EncounterData.Text2Data))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Symptom measurment is required",
                        new[] { "Text2Data" }));
                    AllValid = false;
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceSymptomFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceSymptom hw = new HospiceSymptom(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hw.HospiceSymptomSetup();
            return hw;
        }
    }

    public class HospiceSignificantDate : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public HospiceSignificantDate(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        public void HospiceSignificantDateSetup()
        {
            AddHospiceSignificantDateCommand = new RelayCommand<HospiceSignificantDate>(hospiceSignificantDate =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick();
                }
            });
            DeleteHospiceSignificantDateCommand = new RelayCommand<QuestionBase>(hospiceSignificantDateQuestion =>
            {
                QuestionBase q = hospiceSignificantDateQuestion;
                if (q == null)
                {
                    return;
                }

                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

                HospiceSignificantDates.Remove(q);
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildTextBoxInputLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChildTextBoxInput(frameworkElement as TextBox);
                }
            });
            PopupChildTextBoxInputUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });

            HospiceSignificantDates = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                         x.SectionKey == Section.SectionKey && x.QuestionGroupKey == QuestionGroupKey &&
                         x.QuestionKey == Question.QuestionKey).OrderBy(x => x.AddedDateTime).ThenBy(x => x.TextData))
                HospiceSignificantDates.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            if ((HospiceSignificantDates.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }
        }

        private void DefaultHospiceSignificantDates()
        {
            if (HospiceSignificantDates.Any())
            {
                return;
            }

            this.RaisePropertyChangedLambda(p => p.HospiceSignificantDates);
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }

            _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
        }

        public ObservableCollection<QuestionBase> HospiceSignificantDates { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceSignificantDates { get; set; }

        public RelayCommand<HospiceSignificantDate> AddHospiceSignificantDateCommand { get; set; }
        public RelayCommand<QuestionBase> DeleteHospiceSignificantDateCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputUnLoaded { get; set; }

        public bool CanAddHospiceSignificantDate
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return true;
            }
        }

        private String _AddEvent;

        public String AddEvent
        {
            get { return _AddEvent; }
            set
            {
                _AddEvent = value;
                if (_popupProvider != null)
                {
                    _popupProvider.BeginClosingPopup();
                }

                if (string.IsNullOrWhiteSpace(AddEvent) == false)
                {
                    // Only add the event if its new
                    QuestionBase q = HospiceSignificantDates
                        .Where(h => h.EncounterData.TextData.ToLower() == AddEvent.ToLower().Trim()).FirstOrDefault();
                    if (q == null)
                    {
                        EncounterData ed = new EncounterData
                        {
                            SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                            QuestionKey = Question.QuestionKey, TextData = AddEvent.Trim(), Text2Data = null,
                            BoolData = true, AddedDateTime = DateTime.UtcNow
                        };
                        HospiceSignificantDates.Add(
                            new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = DynamicFormViewModel.CurrentEncounter,
                                EncounterData = ed, Question = Question,
                                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                            });
                    }
                }

                if (AddEvent != null)
                {
                    AddEvent = null;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => { this.RaisePropertyChangedLambda(p => p.AddEvent); });
            }
        }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = false; // can edit/delete
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            HospiceSignificantDates = new ObservableCollection<QuestionBase>();

            bool found = false;
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                             .OrderBy(d => d.AddedDateTime).ThenBy(d => d.TextData))
                    if (ed != null)
                    {
                        HospiceSignificantDates.Add(
                            new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                                Question = Question,
                                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                            });
                        found = true;
                    }

                if (found)
                {
                    break;
                }
            }

            DefaultHospiceSignificantDates();
            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceSignificantDates != null)
                {
                    foreach (var item in HospiceSignificantDates)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceSignificantDates = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceSignificantDates)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceSignificantDates.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceSignificantDates);
            }
            else
            {
                BackupHospiceSignificantDates = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceSignificantDates)
                    BackupHospiceSignificantDates.Add(
                        new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter,
                            EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                            ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceSignificantDates)
            {
                item.EncounterData.ValidationErrors.Clear();
                if ((Encounter.FullValidation) && (item.EncounterData.DateTimeData == null))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Event date is required",
                        new[] { "DateTimeData" }));
                    AllValid = false;
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceSignificantDateFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceSignificantDate hsd = new HospiceSignificantDate(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hsd.HospiceSignificantDateSetup();
            return hsd;
        }
    }


    public class HospiceFuncScale : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public RelayCommand SelectedFuncScaleChanged { get; set; }

        public HospiceFuncScale(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SelectedFuncScaleChanged = new RelayCommand(() =>
            {

            });
        }

        public void HospiceFuncScaleSetup()
        {
            AddHospiceFuncScaleCommand = new RelayCommand<HospiceFuncScale>(hospiceFuncScale =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick();
                }
            });
            DeleteHospiceFuncScaleCommand = new RelayCommand<QuestionBase>(hospiceFuncScaleQuestion =>
            {
                QuestionBase q = hospiceFuncScaleQuestion;
                if (q == null)
                {
                    return;
                }

                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

                HospiceFuncScales.Remove(q);
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildTextBoxInputLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChildComboBoxInput(frameworkElement as ComboBox);
                }
            });
            PopupChildTextBoxInputUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildCodeLookupMultiPopupOpened = new RelayCommand(() =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.StopClosingSubPopup();
                }
            });
            PopupChildCodeLookupMultiPopupClosed = new RelayCommand(() =>
            {
                if (_popupProvider != null)
                {
                    AddNewFuncScale();
                    _popupProvider.BeginClosingSubPopup();
                }
            });

            HospiceFuncScales = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                         x.SectionKey == Section.SectionKey && x.QuestionGroupKey == QuestionGroupKey &&
                         x.QuestionKey == Question.QuestionKey).OrderBy(x => x.AddedDateTime))
                HospiceFuncScales.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            if ((HospiceFuncScales.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }
        }

        private void DefaultHospiceFuncScales()
        {
            if (HospiceFuncScales.Any())
            {
            }

            // no defaulting of functional scales
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }

            _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
        }

        public ObservableCollection<QuestionBase> HospiceFuncScales { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceFuncScales { get; set; }

        public RelayCommand<HospiceFuncScale> AddHospiceFuncScaleCommand { get; set; }
        public RelayCommand<QuestionBase> DeleteHospiceFuncScaleCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputUnLoaded { get; set; }
        public RelayCommand PopupChildCodeLookupMultiPopupOpened { get; set; }
        public RelayCommand PopupChildCodeLookupMultiPopupClosed { get; set; }

        public bool CanAddHospiceFuncScale
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return (DynamicFormCache.IsPreEval(DynamicFormViewModel.CurrentForm.FormKey)) ? false : true;
            }
        }

        private void AddNewFuncScale()
        {
            if (string.IsNullOrWhiteSpace(AddFuncScale) == false)
            {
                if (_popupProvider != null)
                {
                    _popupProvider.BeginClosingPopup();
                }

                // Only add the functional scale if its new
                QuestionBase q = HospiceFuncScales
                    .Where(h => h.EncounterData.TextData.ToLower() == AddFuncScale.ToLower().Trim()).FirstOrDefault();
                if (q == null)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                        QuestionKey = Question.QuestionKey, TextData = AddFuncScale.Trim(), Text2Data = null,
                        BoolData = true, AddedDateTime = DateTime.UtcNow
                    };
                    HospiceFuncScales.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = DynamicFormViewModel.CurrentEncounter, EncounterData = ed,
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }
            }

            if (AddFuncScale != null)
            {
                AddFuncScale = null;
            }
        }

        private String _AddFuncScale;

        public String AddFuncScale
        {
            get { return _AddFuncScale; }
            set
            {
                _AddFuncScale = value;
                if (string.IsNullOrWhiteSpace(AddFuncScale))
                {
                    _AddFuncScale = null;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.RaisePropertyChangedLambda(p => p.AddFuncScale);
                });
            }
        }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = false; // can edit/delete
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            HospiceFuncScales = new ObservableCollection<QuestionBase>();

            bool found = false;
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                             .OrderBy(d => d.AddedDateTime))
                    if (ed != null)
                    {
                        HospiceFuncScales.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                            Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
                        found = true;
                    }

                if (found)
                {
                    break;
                }
            }

            DefaultHospiceFuncScales();
            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceFuncScales != null)
                {
                    foreach (var item in HospiceFuncScales)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceFuncScales = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceFuncScales)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceFuncScales.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceFuncScales);
            }
            else
            {
                BackupHospiceFuncScales = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceFuncScales)
                    BackupHospiceFuncScales.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                        ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceFuncScales)
            {
                item.EncounterData.ValidationErrors.Clear();
                if ((Encounter.FullValidation) && string.IsNullOrWhiteSpace(item.EncounterData.Text2Data))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Scale measurment is required",
                        new[] { "Text2Data" }));
                    AllValid = false;
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceFuncScaleFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceFuncScale hfs = new HospiceFuncScale(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hfs.HospiceFuncScaleSetup();
            return hfs;
        }
    }


    public class HospiceCommunityResource : QuestionUI
    {
        public override void Cleanup()
        {
            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildComboBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChildTextBoxInput(null);
            }

            if (_popupProvider != null)
            {
                _popupProvider.SetPopupChild(null);
            }

            TriggerButton = null;
            PopupControl = null;
            SetupPopupProvider();
            base.Cleanup();
        }

        public HospiceCommunityResource(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        public void HospiceCommunityResourceSetup()
        {
            AddHospiceCommunityResourceCommand = new RelayCommand<HospiceCommunityResource>(hospiceCommunityResource =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.TriggerClick();
                }
            });
            DeleteHospiceCommunityResourceCommand = new RelayCommand<QuestionBase>(hospiceCommunityResourceQuestion =>
            {
                QuestionBase q = hospiceCommunityResourceQuestion;
                if (q == null)
                {
                    return;
                }

                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

                HospiceCommunityResources.Remove(q);
            });
            PopupTriggerLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                TriggerButton = frameworkElement;
                SetupPopupProvider();
            });
            PopupTriggerUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                PopupControl = frameworkElement as Popup;
                SetupPopupProvider();
            });
            PopupUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChild(frameworkElement);
                }
            });
            PopupChildUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });
            PopupChildTextBoxInputLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {
                if (_popupProvider != null)
                {
                    _popupProvider.SetPopupChildTextBoxInput(frameworkElement as TextBox);
                }
            });
            PopupChildTextBoxInputUnLoaded = new RelayCommand<FrameworkElement>(frameworkElement =>
            {

            });

            HospiceCommunityResources = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                         x.SectionKey == Section.SectionKey && x.QuestionGroupKey == QuestionGroupKey &&
                         x.QuestionKey == Question.QuestionKey).OrderBy(x => x.AddedDateTime))
                HospiceCommunityResources.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });

            if ((HospiceCommunityResources.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }
        }

        private void DefaultHospiceCommunityResources()
        {
        }

        private FrameworkElement TriggerButton;
        private Popup PopupControl;
        private PopupProvider _popupProvider;

        private void SetupPopupProvider()
        {
            if ((TriggerButton == null) || (PopupControl == null))
            {
                _popupProvider = null;
                return;
            }

            _popupProvider = new PopupProvider(TriggerButton, TriggerButton, PopupControl, null, Direction.Bottom);
        }

        public ObservableCollection<QuestionBase> HospiceCommunityResources { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceCommunityResources { get; set; }

        public RelayCommand<HospiceCommunityResource> AddHospiceCommunityResourceCommand { get; set; }
        public RelayCommand<QuestionBase> DeleteHospiceCommunityResourceCommand { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupTriggerUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildUnLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputLoaded { get; set; }
        public RelayCommand<FrameworkElement> PopupChildTextBoxInputUnLoaded { get; set; }

        public bool CanAddHospiceCommunityResource
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                return (DynamicFormCache.IsPreEval(DynamicFormViewModel.CurrentForm.FormKey)) ? false : true;
            }
        }

        private String _AddCommunityResource;

        public String AddCommunityResource
        {
            get { return _AddCommunityResource; }
            set
            {
                _AddCommunityResource = value;
                if (_popupProvider != null)
                {
                    _popupProvider.BeginClosingPopup();
                }

                if (string.IsNullOrWhiteSpace(AddCommunityResource) == false)
                {
                    // Only add the resource if its new
                    QuestionBase q = HospiceCommunityResources
                        .Where(h => h.EncounterData.TextData.ToLower() == AddCommunityResource.ToLower().Trim())
                        .FirstOrDefault();
                    if (q == null)
                    {
                        EncounterData ed = new EncounterData
                        {
                            SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                            QuestionKey = Question.QuestionKey, TextData = AddCommunityResource.Trim(),
                            Text2Data = null, BoolData = true, AddedDateTime = DateTime.UtcNow
                        };
                        HospiceCommunityResources.Add(
                            new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = DynamicFormViewModel.CurrentEncounter,
                                EncounterData = ed, Question = Question,
                                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                            });
                    }
                }

                if (AddCommunityResource != null)
                {
                    AddCommunityResource = null;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.RaisePropertyChangedLambda(p => p.AddCommunityResource);
                });
            }
        }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = true; // can edit/delete
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            HospiceCommunityResources = new ObservableCollection<QuestionBase>();

            bool found = false;
            foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey)
                             .OrderBy(d => d.AddedDateTime))
                    if (ed != null)
                    {
                        HospiceCommunityResources.Add(
                            new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                                Question = Question,
                                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                            });
                        found = true;
                    }

                if (found)
                {
                    break;
                }
            }

            DefaultHospiceCommunityResources();
            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceCommunityResources != null)
                {
                    foreach (var item in HospiceCommunityResources)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceCommunityResources = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceCommunityResources)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceCommunityResources.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceCommunityResources);
            }
            else
            {
                BackupHospiceCommunityResources = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceCommunityResources)
                    BackupHospiceCommunityResources.Add(
                        new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter,
                            EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                            ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                        });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceCommunityResources)
            {
                item.EncounterData.ValidationErrors.Clear();
                if ((Encounter.FullValidation) && string.IsNullOrWhiteSpace(item.EncounterData.TextData))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Agency/provider is required",
                        new[] { "TextData" }));
                    AllValid = false;
                }
                else if ((Encounter.FullValidation) &&
                         (string.IsNullOrWhiteSpace(item.EncounterData.TextData) == false))
                {
                    int count = HospiceCommunityResources.Where(h =>
                            h.EncounterData.TextData.Trim().ToLower() == item.EncounterData.TextData.Trim().ToLower())
                        .Count();
                    if (count > 1)
                    {
                        item.EncounterData.ValidationErrors.Add(new ValidationResult("Agency/provider must be unique",
                            new[] { "TextData" }));
                        AllValid = false;
                    }
                }

                if ((Encounter.FullValidation) && (string.IsNullOrWhiteSpace(item.EncounterData.Text3Data) == false))
                {
                    string phone = item.EncounterData.Text3Data.Replace(".", "");
                    if ((phone.Length != 7) && (phone.Length != 10))
                    {
                        item.EncounterData.ValidationErrors.Add(new ValidationResult(
                            "Invalid phone number format, must be 999.9999 or 999.999.9999", new[] { "Text3Data" }));
                        AllValid = false;
                    }
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceCommunityResourceFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceCommunityResource hcr = new HospiceCommunityResource(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hcr.HospiceCommunityResourceSetup();
            return hcr;
        }
    }

    public class HospiceCOTI : QuestionUI
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister(this);

            if (AdmissionPhysician != null)
            {
                if (AdmissionPhysician.Encounter != null)
                {
                    AdmissionPhysician.Encounter.Cleanup();
                }

                if (AdmissionPhysician.Admission != null)
                {
                    AdmissionPhysician.Admission.Cleanup();
                }

                AdmissionPhysician = null;
            }

            base.Cleanup();
        }

        public HospiceCOTI(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() => { });
        }

        public void HospiceCOTISetup()
        {
            // setup AdmissionPhysician facade
            AdmissionPhysician = new AdmissionPhysicianFacade();
            AdmissionPhysician.Admission = Admission;
            AdmissionPhysician.Encounter = Encounter;
            Messenger.Default.Register<int>(this,
                "AdmissionPhysician_FormUpdate",
                AdmissionKey =>
                {
                    if (Admission == null)
                    {
                        return;
                    }

                    if (AdmissionPhysician == null)
                    {
                        return;
                    }

                    if (Admission.AdmissionKey != AdmissionKey)
                    {
                        return;
                    }

                    AdmissionPhysician.RaiseEvents();
                    DefaultHospiceCOTIs();
                });
            HospiceCOTIs = new ObservableCollection<QuestionBase>();
            foreach (var item in DynamicFormViewModel.CurrentEncounter.EncounterData.Where(x =>
                         x.EncounterKey == DynamicFormViewModel.CurrentEncounter.EncounterKey &&
                         x.SectionKey == Section.SectionKey && x.QuestionGroupKey == QuestionGroupKey &&
                         x.QuestionKey == Question.QuestionKey))
                HospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = item, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            if ((HospiceCOTIs.Any() == false) && (IsNewEncounterOrSection))
            {
                CopyForwardLastInstance(); // Will copy or default
            }

            DefaultHospiceCOTIs();
        }

        private bool UseMedicalDirectoryOnly
        {
            get
            {
                if (Admission == null)
                {
                    return false;
                }

                if (Admission.TransferHospice)
                {
                    return true;
                }

                if (Admission.CurrentCertPeriodNumber == null)
                {
                    return false;
                }

                if (Admission.CurrentCertPeriodNumber == 1)
                {
                    return false;
                }

                return true; // Not a transfer and CurrentCertPeriodNumber > 1
            }
        }

        private Physician CurrentAttendingPhysician =>
            (AdmissionPhysician == null) ? null : AdmissionPhysician.AttendingPhysician;

        private string AttendingPhysicianRole => CodeLookupCache.GetDescriptionFromCode("PHTP", "PCP");

        private Physician CurrentMedicalDirector =>
            (AdmissionPhysician == null) ? null : AdmissionPhysician.MedicalDirector;

        private string MedicalDirectorRole => CodeLookupCache.GetDescriptionFromCode("PHTP", "MedDirect");

        private string GetPhysicianFullNameInformalWithSuffixFromKey(int? key)
        {
            string name = PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(key);
            return (string.IsNullOrWhiteSpace(name)) ? NONAME : name;
        }

        private string CR = char.ToString('\r');
        private string NONAME = "No active physicians";
        private string NOROLE = "None";

        private bool EncounterDataDifferent(EncounterData ed, int? physicianKey, string physicianName,
            string physicianRole)
        {
            if (ed == null)
            {
                return true;
            }

            if (ed.IntData != physicianKey)
            {
                return true;
            }

            if (ed.TextData != physicianName)
            {
                return true;
            }

            if (ed.Text2Data != physicianRole)
            {
                return true;
            }

            return false;
        }

        private void DefaultHospiceCOTIs()
        {
            // Do not overwrite COTIs on completed forms
            if (DynamicFormViewModel == null)
            {
                return;
            }

            if (DynamicFormViewModel.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return;
            }

            if (Protected)
            {
                return;
            }

            int? currentAttendingPhysicianKey = (UseMedicalDirectoryOnly)
                ? null
                : ((CurrentAttendingPhysician == null) ? (int?)null : CurrentAttendingPhysician.PhysicianKey);
            int? currentMedicalDirectorKey =
                (CurrentMedicalDirector == null) ? (int?)null : CurrentMedicalDirector.PhysicianKey;
            int? physician1Key = null;
            int? physician2Key = null;
            string physician1Name = null;
            string physician2Name = null;
            string physician1Role = null;
            string physician2Role = null;
            if ((UseMedicalDirectoryOnly) ||
                ((currentAttendingPhysicianKey == null) && (currentMedicalDirectorKey == null)))
            {
                physician1Key = currentMedicalDirectorKey;
                physician1Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician1Key);
                physician1Role = (physician1Key == null) ? NOROLE : MedicalDirectorRole;
            }
            else if ((currentAttendingPhysicianKey != null) && (currentMedicalDirectorKey != null) &&
                     (currentAttendingPhysicianKey != currentMedicalDirectorKey))
            {
                physician1Key = currentAttendingPhysicianKey;
                physician1Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician1Key);
                physician1Role = (physician1Key == null) ? NOROLE : AttendingPhysicianRole;

                physician2Key = currentMedicalDirectorKey;
                physician2Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician2Key);
                physician2Role = (physician1Key == null) ? NOROLE : MedicalDirectorRole;
            }
            else if ((currentAttendingPhysicianKey != null) && (currentMedicalDirectorKey != null) &&
                     (currentAttendingPhysicianKey == currentMedicalDirectorKey))
            {
                physician1Key = currentMedicalDirectorKey;
                physician1Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician1Key);
                physician1Role = (physician1Key == null) ? NOROLE : AttendingPhysicianRole + CR + MedicalDirectorRole;
            }
            else if ((currentAttendingPhysicianKey != null) && (currentMedicalDirectorKey == null))
            {
                physician1Key = currentAttendingPhysicianKey;
                physician1Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician1Key);
                physician1Role = (physician1Key == null) ? NOROLE : AttendingPhysicianRole;
            }
            else
            {
                physician1Key = currentMedicalDirectorKey;
                physician1Name = GetPhysicianFullNameInformalWithSuffixFromKey(physician1Key);
                physician1Role = (physician1Key == null) ? NOROLE : MedicalDirectorRole;
            }

            bool reDefault = false;
            if (HospiceCOTIs.Any() == false)
            {
                reDefault = true;
            }
            else if ((HospiceCOTIs.Count == 1) && (physician2Key != null))
            {
                reDefault = true;
            }
            else if ((HospiceCOTIs.Count == 1) && (physician2Key == null) &&
                     (EncounterDataDifferent(HospiceCOTIs[0].EncounterData, physician1Key, physician1Name,
                         physician1Role)))
            {
                reDefault = true;
            }
            else if ((HospiceCOTIs.Count == 2) && (physician2Key == null))
            {
                reDefault = true;
            }
            else if ((HospiceCOTIs.Count == 2) && (physician2Key != null))
            {
                var medDir = HospiceCOTIs.Where(h => h.EncounterData.Text2Data == MedicalDirectorRole);
                var attend = HospiceCOTIs.Where(h => h.EncounterData.Text2Data == AttendingPhysicianRole);
                if (medDir.Any() && attend.Any())
                {
                    if (EncounterDataDifferent(attend.First().EncounterData, physician1Key, physician1Name,
                            physician1Role)
                        || EncounterDataDifferent(medDir.First().EncounterData, physician2Key, physician2Name,
                            physician2Role))
                    {
                        reDefault = true;
                    }
                }
            }

            if (reDefault == false)
            {
                return;
            }

            // Refactor for DE 38422 to redefault status and date if same physician on file
            EncounterData ed1 = null;
            EncounterData ed2 = null;
            EncounterData edOld = null;
            ed1 = new EncounterData
            {
                SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                QuestionKey = Question.QuestionKey, IntData = physician1Key, TextData = physician1Name,
                Text2Data = physician1Role
            };
            edOld = HospiceCOTIs.Where(h => h.EncounterData.IntData == ed1.IntData).Select(h => h.EncounterData)
                .FirstOrDefault();
            if (edOld != null)
            {
                ed1.Int2Data = edOld.Int2Data;
                ed1.DateTimeData = edOld.DateTimeData;
            } // Redefault status and date if same physician on file

            if (physician2Key != null)
            {
                ed2 = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, IntData = physician2Key, TextData = physician2Name,
                    Text2Data = physician2Role
                };
                edOld = HospiceCOTIs.Where(h => h.EncounterData.IntData == ed2.IntData).Select(h => h.EncounterData)
                    .FirstOrDefault();
                if (edOld != null)
                {
                    ed2.Int2Data = edOld.Int2Data;
                    ed2.DateTimeData = edOld.DateTimeData;
                } // Redefault status and date if same physician on file
            }

            foreach (QuestionBase q in HospiceCOTIs)
                if (q.EncounterData.IsNew == false)
                {
                    ((IPatientService)DynamicFormViewModel.FormModel).Remove(q.EncounterData);
                }

            HospiceCOTIs = new ObservableCollection<QuestionBase>();
            HospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
            {
                Admission = Admission, Encounter = Encounter, EncounterData = ed1, Question = Question,
                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
            });
            if (ed2 != null)
            {
                HospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed2, Question = Question,
                    ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                });
            }

            this.RaisePropertyChangedLambda(p => p.HospiceCOTIs);
        }

        public AdmissionPhysicianFacade AdmissionPhysician { get; set; }
        public ObservableCollection<QuestionBase> HospiceCOTIs { get; set; }
        public ObservableCollection<QuestionBase> BackupHospiceCOTIs { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = false; // can edit/delete
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        private bool CanCopyForward
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.CurrentForm.IsPreEval)
                {
                    return false;
                }

                return true;
            }
        }

        public override bool CopyForwardLastInstance()
        {
            bool found = false;
            if (CanCopyForward)
            {
                HospiceCOTIs = new ObservableCollection<QuestionBase>();
                foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                             .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    foreach (var ed in item.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey))
                        if (ed != null)
                        {
                            HospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                                Question = Question,
                                ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                            });
                            found = true;
                        }

                    if (found)
                    {
                        break;
                    }
                }
            }

            return found;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            // called from re-evaluate
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (HospiceCOTIs != null)
                {
                    foreach (var item in HospiceCOTIs)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                HospiceCOTIs = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupHospiceCOTIs)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    HospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Question = Question, ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
                }

                this.RaisePropertyChangedLambda(p => p.HospiceCOTIs);
            }
            else
            {
                BackupHospiceCOTIs = new ObservableCollection<QuestionBase>();
                foreach (var item in HospiceCOTIs)
                    BackupHospiceCOTIs.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Question = Question,
                        ProcessGoals = new RelayCommand(() => { ProcessGoals.Execute(null); })
                    });
            }
        }

        private bool IsCOTIStatusNotDone(int? status)
        {
            if (status == null)
            {
                return false;
            }

            string statusCode = CodeLookupCache.GetCodeFromKey(status);
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                return false;
            }

            return (statusCode.ToLower().Trim() == "not done") ? true : false;
        }

        private bool IsCOTIStatusVerbal(int? status)
        {
            if (status == null)
            {
                return false;
            }

            string statusCode = CodeLookupCache.GetCodeFromKey(status);
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                return false;
            }

            return (statusCode.ToLower().Trim() == "verbal") ? true : false;
        }

        private bool IsCOTIStatusSigned(int? status)
        {
            if (status == null)
            {
                return false;
            }

            string statusCode = CodeLookupCache.GetCodeFromKey(status);
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                return false;
            }

            return (statusCode.ToLower().Trim() == "signed") ? true : false;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in HospiceCOTIs)
            {
                item.EncounterData.ValidationErrors.Clear();
                if ((Encounter.FullValidation) &&
                    ((item.EncounterData.Int2Data == null) || (item.EncounterData.Int2Data == 0)))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Status is required",
                        new[] { "Int2Data" }));
                    AllValid = false;
                }
                else if ((Encounter.FullValidation) && (item.EncounterData.IntData == null) &&
                         (IsCOTIStatusNotDone(item.EncounterData.Int2Data) == false))
                {
                    item.EncounterData.ValidationErrors.Add(
                        new ValidationResult(NONAME + ", the only allowable status is Not Done", new[] { "Int2Data" }));
                    AllValid = false;
                }

                if ((Encounter.FullValidation) && (IsCOTIStatusVerbal(item.EncounterData.Int2Data)) &&
                    ((item.EncounterData.DateTimeData == null) ||
                     (item.EncounterData.DateTimeData == DateTime.MinValue)))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Verbal Date is required",
                        new[] { "DateTimeData" }));
                    AllValid = false;
                }

                if ((Encounter.FullValidation) && (IsCOTIStatusSigned(item.EncounterData.Int2Data)) &&
                    ((item.EncounterData.DateTimeData == null) ||
                     (item.EncounterData.DateTimeData == DateTime.MinValue)))
                {
                    item.EncounterData.ValidationErrors.Add(new ValidationResult("Signed Date is required",
                        new[] { "DateTimeData" }));
                    AllValid = false;
                }

                if ((IsCOTIStatusVerbal(item.EncounterData.Int2Data) == false) &&
                    (IsCOTIStatusSigned(item.EncounterData.Int2Data) == false))
                {
                    item.EncounterData.DateTimeData = null;
                }

                if (item.EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(item.EncounterData);
                }
            }

            return AllValid;
        }
    }

    public class HospiceCOTIFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceCOTI hw = new HospiceCOTI(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
            };
            hw.HospiceCOTISetup();
            return hw;
        }
    }

    public class HospiceFamilyRelationship : QuestionUI
    {
        private string _PopupDataTemplate;

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public RelayCommand<EncounterData> PrimaryCaregiverCheckBoxChecked { get; set; }

        public HospiceFamilyRelationship(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void HospiceFamilyRelationshipSetup()
        {
            PrimaryCaregiverCheckBoxChecked = new RelayCommand<EncounterData>(encounterData =>
            {
                if (encounterData == null)
                {
                    return;
                }

                if (Contacts == null)
                {
                    return;
                }

                if (encounterData.GuidData == null)
                {
                    encounterData.BoolData = false;
                    return;
                }

                // Only one primary caregiver - uncheck all others
                List<QuestionBase> gList = Contacts.Where(c => c.EncounterData != encounterData).ToList();
                foreach (QuestionBase qb in gList)
                {
                    if (qb.EncounterData.BoolData == true)
                    {
                        qb.EncounterData.BoolData = false;
                    }
                }
            });
            Setup_PatientContact();
        }

        public ObservableCollection<QuestionBase> Contacts { get; set; }
        public ObservableCollection<QuestionBase> BackupContacts { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = source.BoolData;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            return CopyPatientContactCareGivers();
        }

        private PatientContact GetPatientContact(Guid? contactGuid)
        {
            if ((Patient == null) || (Patient.PatientContact == null))
            {
                return null;
            }

            return Patient.PatientContact
                .Where(p => ((p.Caregiver == 1) && (p.Inactive == false) && (p.ContactGuid == contactGuid)))
                .FirstOrDefault();
        }

        public bool CopyPatientContactCareGivers()
        {
            Contacts = new ObservableCollection<QuestionBase>();
            bool Break = false;
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (var previousED in previousE.EncounterData.Where(d => d.QuestionKey == Question.QuestionKey))
                    if (previousED != null)
                    {
                        // Copy forward if-and-only-if this contact is still an active caregiver - (to copy forward Primary caregiver)
                        if (GetPatientContact(previousED.GuidData) != null)
                        {
                            Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter,
                                EncounterData = CopyProperties(previousED), Patient = Patient, Question = Question
                            });
                        }

                        Break = true;
                    }

                if (Break)
                {
                    break;
                }
            }

            // Also include and non-copy forward contacts that are now active caregivers
            if ((Patient != null) && (Patient.PatientContact != null))
            {
                foreach (PatientContact pc in Patient.PatientContact.Where(p => ((p.Caregiver == 1)) && (p.Inactive == false)))
                {
                    if ((pc != null) &&
                        (Contacts.Where(c => c.EncounterData.GuidData == pc.ContactGuid).FirstOrDefault() == null))
                    {
                        EncounterData newED = new EncounterData
                        {
                            SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                            QuestionKey = Question.QuestionKey, GuidData = pc.ContactGuid, BoolData = false
                        };
                        Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = Admission, Encounter = Encounter, EncounterData = newED, Patient = Patient,
                            Question = Question
                        });
                    }
                }
            }

            //default with one and allow more to be added
            if (Contacts.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, BoolData = false
                };
                Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Patient = Patient,
                    Question = Question
                });
            }

            RaisePropertyChanged("Contacts");
            return true;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {

        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (Contacts != null)
                {
                    foreach (var item in Contacts)
                    {
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                    }
                }

                Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupContacts)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Patient = Patient, Question = Question
                    });
                }

                this.RaisePropertyChangedLambda(p => p.Contacts);
            }
            else
            {
                BackupContacts = new ObservableCollection<QuestionBase>();
                foreach (var item in Contacts)
                    BackupContacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Patient = Patient, Question = Question
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Contacts)
            {
                item.EncounterData.ValidationErrors.Clear();

                PatientContact pc = Patient.PatientContact.Where(p => p.ContactGuid == item.EncounterData.GuidData)
                    .FirstOrDefault();
                if (item.EncounterData.GuidData != null)
                {
                    if (pc != null)
                    {
                        pc.Caregiver = 1;
                    }

                    if (item.EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(item.EncounterData);
                    }
                }
                else
                {
                    if (pc != null)
                    {
                        pc.Caregiver = 0;
                    }

                    if (item.EncounterData.EntityState == EntityState.Modified)
                    {
                        Encounter.EncounterData.Remove(item.EncounterData);
                    }
                }
            }

            return AllValid;
        }

        #region PatientContact

        public bool IsEdit => true;
        private PatientContact _SelectedItem;

        public PatientContact SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                _SelectedItem = value;
                this.RaisePropertyChangedLambda(p => p.SelectedItem);
            }
        }

        public RelayCommand AddContactCommand { get; set; }
        public RelayCommand<HospiceFamilyRelationship> AddBehaviorCommand { get; set; }
        public RelayCommand<Guid?> PatientContactDetailsCommand { get; protected set; }

        public RelayCommand CancelPatientContactCommand { get; protected set; }
        public RelayCommand OKPatientContactCommand { get; protected set; }

        private void Setup_PatientContact()
        {
            AddContactCommand = new RelayCommand(() =>
            {
                SelectedItem = new PatientContact();
                SelectedItem.IsDynamicForm = true;
                SelectedItem.ContactGuid = Guid.NewGuid();
                int? TypeKey = CodeLookupCache.GetKeyFromCode("PATCONTACTADDRESS", "Patient");
                SelectedItem.ContactTypeKey = TypeKey == null ? 0 : (int)TypeKey;
                PopupDataTemplate = "PatientContactPopupDataTemplate";
                DynamicFormViewModel.PopupDataContext = this;
            });

            PatientContactDetailsCommand = new RelayCommand<Guid?>(patientContactGuid =>
            {
                if (patientContactGuid == null)
                {
                    return;
                }

                if (patientContactGuid == Guid.Empty)
                {
                    return;
                }

                if (Patient == null)
                {
                    return;
                }

                if (Patient.PatientContact == null)
                {
                    return;
                }

                PatientContact pc = Patient.PatientContact.Where(c => c.ContactGuid == patientContactGuid)
                    .FirstOrDefault();
                if (pc == null)
                {
                    return;
                }

                PatientContactDetailsDialog cw = new PatientContactDetailsDialog(pc);
                cw.Show();
            });

            OKPatientContactCommand = new RelayCommand(() =>
            {
                if (SelectedItem == null)
                {
                    return;
                }

                SelectedItem.ValidationErrors.Clear();
                if (SelectedItem.Validate())
                {
                    Patient.PatientContact.Add(SelectedItem);
                    DynamicFormViewModel.PopupDataContext = null;
                    PopupDataTemplate = null;
                }
            });

            CancelPatientContactCommand = new RelayCommand(() =>
            {
                DynamicFormViewModel.PopupDataContext = null;
                PopupDataTemplate = null;
            });
        }

        #endregion
    }

    public class HospiceFamilyRelationshipFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceFamilyRelationship fr = new HospiceFamilyRelationship(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm,
                AddBehaviorCommand = new RelayCommand<HospiceFamilyRelationship>(familyRelationship =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, BoolData = false
                    };
                    familyRelationship.Contacts.Add(
                        new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                        {
                            Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                            Patient = vm.CurrentPatient, Question = q
                        });
                }),
            };
            fr.HospiceFamilyRelationshipSetup();
            EncounterData eData = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (eData == null)
            {
                fr.CopyPatientContactCareGivers();
            }
            else
            {
                fr.Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in vm.CurrentEncounter.EncounterData.Where(x =>
                             x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                             x.SectionKey == formsection.Section.SectionKey &&
                             x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey))
                {
                    fr.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = item,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }

                //default with one and allow more to be added
                if (fr.Contacts.Any() == false)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, BoolData = false
                    };
                    fr.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }
            }

            return fr;
        }
    }

    public class HospiceBereaved : QuestionUI
    {
        public HospiceBereaved(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region RiskAssessment

        public string ReevaluatePopupLabel => (CurrentRiskAssessment == null)
            ? "Bereavement Risk Assessment"
            : CurrentRiskAssessment.Label;

        private string _PopupDataTemplate = "ReEvaluatePopupDataTemplate";

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public IDynamicFormService ReEvalFormModel { get; set; }

        ObservableCollection<SectionUI> _ReEvalSections;

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                RaisePropertyChanged("ReEvalSections");
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                RaisePropertyChanged("ReEvalSection");
            }
        }

        private bool _OpenReEval;

        public bool OpenReEval
        {
            get { return _OpenReEval; }
            set
            {
                if (_OpenReEval != value)
                {
                    _OpenReEval = value;
                    this.RaisePropertyChangedLambda(p => p.OpenReEval);
                }
            }
        }

        public bool Loading { get; set; }

        public RelayCommand<QuestionBase> ReEvaluateCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }
        private EncounterData ReEvaluateEncounterData;
        private int RiskAssessmentInUse;

        private void ReEvaluateSetup()
        {
            ReEvaluateCommand = new RelayCommand<QuestionBase>(hospiceBereavedQuestion =>
            {
                QuestionBase qb = hospiceBereavedQuestion;
                if (qb == null)
                {
                    return;
                }

                ReEvaluateEncounterData = qb.EncounterData;
                if (ReEvaluateEncounterData == null)
                {
                    return;
                }

                if (ReEvaluateEncounterData.ReEvalSection == null)
                {
                    RiskAssessment ra = CurrentRiskAssessment;
                    if (ra == null)
                    {
                        MessageBox.Show("Bereavement Risk Assessment definition not found.  contact AlayaCare support.");
                        return;
                    }

                    RiskAssessmentInUse = ra.RiskAssessmentKey;
                    FormSection formsection = new FormSection
                    {
                        RiskAssessmentKey = ra.RiskAssessmentKey, RiskAssessment = ra, FormSectionKey = 0,
                        FormKey = DynamicFormViewModel.CurrentForm.FormKey, Section = null, SectionKey = 0, Sequence = 1
                    };

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvaluateEncounterData.ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection,
                        ReEvaluateEncounterData.ReEvalSections, false, false, true, ReEvaluateEncounterData.GuidData);
                    ReEvaluateEncounterData.ReEvalSection = ReEvaluateEncounterData.ReEvalSections.FirstOrDefault();

                    ReEvalSections = ReEvaluateEncounterData.ReEvalSections;
                    ReEvalSection = ReEvaluateEncounterData.ReEvalSection;

                    if (!Convert.ToBoolean(ReEvaluateEncounterData.BoolData))
                    {
                        //since we are re-evaluating a section we need to go and find the latest data
                        //look for a previous visit first and default to eval and no re-eval found
                        Encounter en = Admission.Encounter.Where(p => (!p.IsNew))
                            .OrderBy(p => p.EncounterOrTaskStartDateAndTime).FirstOrDefault();

                        foreach (var item in
                                 Admission.Encounter
                                     .Where(e => e.Form != null)
                                     .Where(p =>
                                         !p.IsNew &&
                                         !p.Form.IsPlanOfCare &&
                                         !p.Form.IsTransfer)
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            EncounterRisk er = item.EncounterRisk.Where(p =>
                                ((p.RiskAssessmentKey == ra.RiskAssessmentKey) &&
                                 (p.RiskForID == ReEvaluateEncounterData.GuidData))).FirstOrDefault();
                            if (er != null)
                            {
                                en = item;
                                break;
                            }
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions)
                                q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions) q.PreProcessing();
                    Loading = false;
                }
                else
                {
                    ReEvalSections = ReEvaluateEncounterData.ReEvalSections;
                    ReEvalSection = ReEvaluateEncounterData.ReEvalSection;

                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    //backup original in case we need to revert back to it
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(() =>
            {
                if ((!Protected) && (ReEvaluateEncounterData != null))
                {
                    if (ReEvaluateEncounterData.IntData == null)
                    {
                        ReEvaluateEncounterData.IntData = RiskAssessmentInUse;
                    }

                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    ReEvaluateEncounterData.BoolData = true;
                    FetchRiskAssessmentScore();
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                if (ReEvaluateEncounterData != null)
                {
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.RestoreRiskAssessmentEncounterRisks2();
                        }
                    }
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
            });
        }

        private void FetchRiskAssessmentScore()
        {
            if ((ReEvaluateEncounterData != null) && (ReEvaluateEncounterData.ReEvalSection != null))
            {
                foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions)
                {
                    Risk r = q as Risk;
                    if ((r != null) && (ReEvaluateEncounterData != null))
                    {
                        ReEvaluateEncounterData.TextData = "Risk Assessment - Done" +
                                                           ((string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                                               ? ""
                                                               : " w/cmts");
                        if ((r.CurrentRiskRange != null) && (r.TotalRecord != null))
                        {
                            string score = (r.TotalRecord.Score == null)
                                ? ""
                                : r.TotalRecord.Score.ToString().Trim() + " - ";
                            ReEvaluateEncounterData.Text2Data = score + r.CurrentRiskRange.Label;
                        }
                    }
                }
            }
        }

        private RiskAssessment CurrentRiskAssessment
        {
            get
            {
                RiskAssessment ra = null;

                if ((ReEvaluateEncounterData != null) && (ReEvaluateEncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)ReEvaluateEncounterData.IntData);
                    if (ra != null)
                    {
                        return ra;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra;
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel("Bereavement Risk Assessment");
                return ra;
            }
        }

        private int RiskAssessmentKey => (CurrentRiskAssessment == null) ? 0 : CurrentRiskAssessment.RiskAssessmentKey;

        public override void PreProcessing()
        {
            if (Hidden)
            {
                return;
            }

            if (ReEvaluateEncounterData == null)
            {
                return;
            }

            if (ReEvaluateEncounterData.BoolData.HasValue)
            {
                Loading = true;
                ReEvaluateCommand.Execute(null);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any())
                {
                    if (OasisManager != null)
                    {
                        OasisManager.QuestionOasisMappingChanged(Question, ReEvaluateEncounterData);
                    }
                }
            }
        }

        public bool ReEvaluateValidate(out string SubSections)
        {
            SubSections = string.Empty;

            if (OpenReEval)
            {
                OK_Command.Execute(null);
            }

            bool AllValid = base.Validate(out SubSections);

            foreach (var item in Contacts)
                if (item.EncounterData.ReEvalSection != null && AllValid && item.EncounterData.BoolData.HasValue &&
                    item.EncounterData.BoolData.Value)
                {
                    foreach (var q in item.EncounterData.ReEvalSection.Questions)
                    {
                        string ErrorSection = string.Empty;

                        if (!q.Validate(out ErrorSection))
                        {
                            if (string.IsNullOrEmpty(SubSections))
                            {
                                SubSections = item.EncounterData.ReEvalSection.Label;
                            }

                            AllValid = false;
                        }
                    }
                }

            return AllValid;
        }

        public void RiskCleanup()
        {
            if (Contacts == null)
            {
                return;
            }

            foreach (var item in Contacts)
                if (item.EncounterData.ReEvalSection != null)
                {
                    foreach (var q in item.EncounterData.ReEvalSection.Questions) q.Cleanup();
                }
        }

        #endregion

        public void HospiceBereavedSetup()
        {
            AddContactCommand = new RelayCommand(() =>
            {
                AddContactVisible = true;
                NewContact = new PatientContact();
                NewContact.ContactGuid = Guid.NewGuid();
                int? TypeKey = CodeLookupCache.GetKeyFromCode("PATCONTACTADDRESS", "Patient");
                NewContact.ContactTypeKey = TypeKey == null ? 0 : (int)TypeKey;
            });

            ConfirmContactCommand = new RelayCommand(() =>
            {
                if (NewContact.Validate())
                {
                    Patient.PatientContact.Add(NewContact);
                    AddContactVisible = false;
                }
            });

            PatientContactDetailsCommand = new RelayCommand<Guid?>(patientContactGuid =>
            {
                if (patientContactGuid == null)
                {
                    return;
                }

                if (patientContactGuid == Guid.Empty)
                {
                    return;
                }

                if (Patient == null)
                {
                    return;
                }

                if (Patient.PatientContact == null)
                {
                    return;
                }

                PatientContact pc = Patient.PatientContact.Where(c => c.ContactGuid == patientContactGuid)
                    .FirstOrDefault();
                if (pc == null)
                {
                    return;
                }

                PatientContactDetailsDialog cw = new PatientContactDetailsDialog(pc);
                cw.Show();
            });

            ReEvaluateSetup();

            NewContact = new PatientContact();
            NewContact.ContactGuid = Guid.NewGuid();
        }

        public override void Cleanup()
        {
            RiskCleanup();
            base.Cleanup();
        }

        public ObservableCollection<QuestionBase> Contacts { get; set; }
        public ObservableCollection<QuestionBase> BackupContacts { get; set; }

        public RelayCommand AddContactCommand { get; set; }
        public RelayCommand ConfirmContactCommand { get; set; }
        public RelayCommand<HospiceBereaved> AddBehaviorCommand { get; set; }
        public RelayCommand<Guid?> PatientContactDetailsCommand { get; protected set; }

        private PatientContact _NewContact;

        public PatientContact NewContact
        {
            get { return _NewContact; }
            set
            {
                _NewContact = value;
                this.RaisePropertyChangedLambda(p => p.NewContact);
            }
        }

        private bool _AddContactVisible;

        public bool AddContactVisible
        {
            get { return _AddContactVisible; }
            set
            {
                _AddContactVisible = value;
                this.RaisePropertyChangedLambda(p => p.AddContactVisible);
            }
        }


        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = source.BoolData;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            return CopyPatientContactBereaved();
        }

        private PatientContact GetPatientContact(Guid? contactGuid)
        {
            if ((Patient == null) || (Patient.PatientContact == null))
            {
                return null;
            }

            return Patient.PatientContact.Where(p => ((p.Inactive == false) && (p.ContactGuid == contactGuid)))
                .FirstOrDefault();
        }

        public bool CanCopyPatientContactBereaved()
        {
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (EncounterData previousED in previousE.EncounterData.Where(d =>
                             d.QuestionKey == Question.QuestionKey))
                    if (previousED != null)
                    {
                        EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                            x.RiskForID == previousED.GuidData && x.IsTotal &&
                            x.RiskAssessmentKey == RiskAssessmentKey && !x.RiskGroupKey.HasValue).FirstOrDefault();
                        if (previousER != null)
                        {
                            return true;
                        }
                    }
            }

            return false;
        }

        public bool CopyPatientContactBereaved()
        {
            Contacts = new ObservableCollection<QuestionBase>();
            bool Break = false;
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (EncounterData previousED in previousE.EncounterData.Where(d =>
                             d.QuestionKey == Question.QuestionKey))
                    if (previousED != null)
                    {
                        // Copy forward if-and-only-if this contact is still active
                        if (GetPatientContact(previousED.GuidData) != null)
                        {
                            Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter,
                                EncounterData = CopyProperties(previousED), Patient = Patient, Question = Question
                            });
                        }

                        EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                            x.RiskForID == previousED.GuidData && x.IsTotal &&
                            x.RiskAssessmentKey == RiskAssessmentKey && !x.RiskGroupKey.HasValue).FirstOrDefault();
                        if (previousER != null)
                        {
                            Encounter.CopyForwardRiskAssessment(previousE, RiskAssessmentKey, previousED.GuidData);
                        }

                        Break = true;
                    }

                if (Break)
                {
                    break;
                }
            }

            //default with one and allow more to be added
            if (Contacts.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = "Risk Assessment - Not Done", Text2Data = "None"
                };
                Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Patient = Patient,
                    Question = Question
                });
            }

            RaisePropertyChanged("Contacts");
            return true;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            CopyPatientContactBereaved();
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (Contacts != null)
                {
                    foreach (var item in Contacts)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupContacts)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Patient = Patient, Question = Question
                    });
                }

                this.RaisePropertyChangedLambda(p => p.Contacts);
            }
            else
            {
                BackupContacts = new ObservableCollection<QuestionBase>();
                foreach (var item in Contacts)
                    BackupContacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Patient = Patient, Question = Question
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (Hidden)
            {
                return true;
            }

            bool AllValid = true;
            foreach (var item in Contacts)
            {
                item.EncounterData.ValidationErrors.Clear();

                if (item.EncounterData.GuidData != null)
                {
                    if (item.EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(item.EncounterData);
                    }
                }
                else
                {
                    if (item.EncounterData.EntityState == EntityState.Modified)
                    {
                        Encounter.EncounterData.Remove(item.EncounterData);
                    }
                }
            }

            AllValid = ReEvaluateValidate(out SubSections);
            return AllValid;
        }
    }

    public class HospiceBereavedFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceBereaved hb = new HospiceBereaved(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                AddBehaviorCommand = new RelayCommand<HospiceBereaved>(bereaved =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, TextData = "Risk Assessment - Not Done", Text2Data = "None"
                    };
                    bereaved.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }),
            };

            EncounterData eData = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (eData == null)
            {
                if ((hb.CanCopyPatientContactBereaved() == false) &&
                    ((vm != null) && (vm.CurrentForm != null) &&
                     vm.CurrentForm.FormContainsQuestion(
                         DynamicFormCache.GetSingleQuestionByDataTemplate("HospiceBereavedSurvivors"))))
                {
                    // this form contains the new bereaved question as well (like in model Teammeeting) - but contains no legacy data - so hide it, in favor of the new question
                    hb.HiddenOverride = true;
                    return hb;
                }

                hb.CopyPatientContactBereaved();
            }
            else
            {
                hb.Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in vm.CurrentEncounter.EncounterData.Where(x =>
                             x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                             x.SectionKey == formsection.Section.SectionKey &&
                             x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey))
                    hb.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = item,
                        Patient = vm.CurrentPatient, Question = q
                    });

                //default with one and allow more to be added
                if (hb.Contacts.Any() == false)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, TextData = "Risk Assessment - Not Done", Text2Data = "None"
                    };
                    hb.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }
            }

            hb.HospiceBereavedSetup();
            return hb;
        }
    }

    public class HospiceBereavedSurvivors : QuestionUI
    {
        public HospiceBereavedSurvivors(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region RiskAssessment

        public string ReevaluatePopupLabel => CurrentRiskAssessmentString;
        private string _PopupDataTemplate;

        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                RaisePropertyChanged("PopupDataTemplate");
                RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }

        private DataTemplateHelper DataTemplateHelper;

        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null)
                {
                    DataTemplateHelper = new DataTemplateHelper();
                }

                return DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }

        public IDynamicFormService ReEvalFormModel { get; set; }

        ObservableCollection<SectionUI> _ReEvalSections;

        public ObservableCollection<SectionUI> ReEvalSections
        {
            get { return _ReEvalSections; }
            set
            {
                _ReEvalSections = value;
                RaisePropertyChanged("ReEvalSections");
            }
        }

        SectionUI _ReEvalSection;

        public SectionUI ReEvalSection
        {
            get { return _ReEvalSection; }
            set
            {
                _ReEvalSection = value;
                RaisePropertyChanged("ReEvalSection");
            }
        }

        private bool _OpenReEval;

        public bool OpenReEval
        {
            get { return _OpenReEval; }
            set
            {
                if (_OpenReEval != value)
                {
                    _OpenReEval = value;
                    this.RaisePropertyChangedLambda(p => p.OpenReEval);
                }
            }
        }

        public bool Loading { get; set; }

        public RelayCommand<QuestionBase> ReEvaluateCommand { get; protected set; }

        public RelayCommand OK_Command { get; protected set; }

        public RelayCommand Cancel_Command { get; protected set; }
        private EncounterData ReEvaluateEncounterData;
        private int RiskAssessmentInUse;

        private void ReEvaluateSetup()
        {
            ReEvaluateCommand = new RelayCommand<QuestionBase>(hospiceBereavedQuestion =>
            {
                QuestionBase qb = hospiceBereavedQuestion;
                if (qb == null)
                {
                    return;
                }

                ReEvaluateEncounterData = qb.EncounterData;
                if (ReEvaluateEncounterData == null)
                {
                    return;
                }

                if (ReEvaluateEncounterData.ReEvalSection == null)
                {
                    RiskAssessment ra = CurrentRiskAssessment;
                    if (ra == null)
                    {
                        MessageBox.Show(CurrentRiskAssessmentString + " definition not found.  contact AlayaCare support.");
                        return;
                    }

                    RiskAssessmentInUse = ra.RiskAssessmentKey;
                    FormSection formsection = new FormSection
                    {
                        RiskAssessmentKey = ra.RiskAssessmentKey, RiskAssessment = ra, FormSectionKey = 0,
                        FormKey = DynamicFormViewModel.CurrentForm.FormKey, Section = null, SectionKey = 0, Sequence = 1
                    };

                    if (!Loading)
                    {
                        OpenReEval = true;
                    }

                    ReEvaluateEncounterData.ReEvalSections = new ObservableCollection<SectionUI>();
                    DynamicFormViewModel.ProcessFormSectionQuestions(formsection,
                        ReEvaluateEncounterData.ReEvalSections, false, false, true, ReEvaluateEncounterData.GuidData);
                    ReEvaluateEncounterData.ReEvalSection = ReEvaluateEncounterData.ReEvalSections.FirstOrDefault();

                    ReEvalSections = ReEvaluateEncounterData.ReEvalSections;
                    ReEvalSection = ReEvaluateEncounterData.ReEvalSection;

                    if (!Convert.ToBoolean(ReEvaluateEncounterData.BoolData))
                    {
                        //since we are re-evaluating a section we need to go and find the latest data
                        //look for a previous visit first and default to eval and no re-eval found
                        Encounter en = Admission.Encounter.Where(p => (!p.IsNew))
                            .OrderBy(p => p.EncounterOrTaskStartDateAndTime).FirstOrDefault();

                        foreach (var item in
                                 Admission.Encounter
                                     .Where(e => e.Form != null)
                                     .Where(p =>
                                         !p.IsNew &&
                                         !p.Form.IsPlanOfCare &&
                                         !p.Form.IsTransfer)
                                     .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                        {
                            EncounterRisk er = item.EncounterRisk.Where(p =>
                                ((p.RiskAssessmentKey == ra.RiskAssessmentKey) &&
                                 (p.RiskForID == ReEvaluateEncounterData.GuidData))).FirstOrDefault();
                            if (er != null)
                            {
                                en = item;
                                break;
                            }
                        }

                        if (en != null)
                        {
                            foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions)
                                q.CopyForwardfromEncounter(en);
                        }
                    }

                    foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions) q.PreProcessing();
                    Loading = false;
                }
                else
                {
                    ReEvalSections = ReEvaluateEncounterData.ReEvalSections;
                    ReEvalSection = ReEvaluateEncounterData.ReEvalSection;

                    OpenReEval = true;
                }

                if (OpenReEval)
                {
                    //backup original in case we need to revert back to it
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    PopupDataTemplate = "ReEvaluatePopupDataTemplate";
                    DynamicFormViewModel.PopupDataContext = this;
                }
            });

            OK_Command = new RelayCommand(() =>
            {
                if ((!Protected) && (ReEvaluateEncounterData != null))
                {
                    if (ReEvaluateEncounterData.IntData == null)
                    {
                        ReEvaluateEncounterData.IntData = RiskAssessmentInUse;
                    }

                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            if (quiR.ValidateRequireResponseOnPopup() == false)
                            {
                                return;
                            }

                            quiR.SaveRiskAssessmentEncounterRisks2();
                        }
                    }

                    ReEvaluateEncounterData.BoolData = true;
                    FetchRiskAssessmentScore();
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
                PopupDataTemplate = null;
            });

            Cancel_Command = new RelayCommand(() =>
            {
                if (ReEvaluateEncounterData != null)
                {
                    foreach (QuestionUI qui in ReEvalSection.Questions)
                    {
                        Risk quiR = qui as Risk;
                        if (quiR != null)
                        {
                            quiR.RestoreRiskAssessmentEncounterRisks2();
                        }
                    }
                }

                OpenReEval = false;
                DynamicFormViewModel.PopupDataContext = null;
                PopupDataTemplate = null;
            });
        }

        private void FetchRiskAssessmentScore()
        {
            if ((ReEvaluateEncounterData != null) && (ReEvaluateEncounterData.ReEvalSection != null))
            {
                foreach (var q in ReEvaluateEncounterData.ReEvalSection.Questions)
                {
                    Risk r = q as Risk;
                    if ((r != null) && (ReEvaluateEncounterData != null))
                    {
                        ReEvaluateEncounterData.TextData = CurrentRiskAssessmentString + " - Done" +
                                                           ((string.IsNullOrWhiteSpace(r.TotalRecord.Comment))
                                                               ? ""
                                                               : " w/cmts");
                        if ((r.CurrentRiskRange != null) && (r.TotalRecord != null))
                        {
                            string score = (r.TotalRecord.Score == null)
                                ? ""
                                : r.TotalRecord.Score.ToString().Trim() + " - ";
                            ReEvaluateEncounterData.Text2Data = score + r.CurrentRiskRange.Label;
                        }
                    }
                }
            }
        }

        private string CurrentRiskAssessmentString
        {
            get
            {
                RiskAssessment ra = null;
                if ((ReEvaluateEncounterData != null) && (ReEvaluateEncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)ReEvaluateEncounterData.IntData);
                    if (ra != null)
                    {
                        return ra.Label;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra.Label;
                }

                return "Bereavement Risk Assessment";
            }
        }

        private RiskAssessment CurrentRiskAssessment
        {
            get
            {
                RiskAssessment ra = null;
                if ((ReEvaluateEncounterData != null) && (ReEvaluateEncounterData.IntData != null))
                {
                    ra = DynamicFormCache.GetRiskAssessmentByKey((int)ReEvaluateEncounterData.IntData);
                    if (ra != null)
                    {
                        return ra;
                    }
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel(Question.LookupType);
                if (ra != null)
                {
                    return ra;
                }

                ra = DynamicFormCache.GetRiskAssessmentByLabel("Bereavement Risk Assessment");
                return ra;
            }
        }

        private int RiskAssessmentKey => (CurrentRiskAssessment == null) ? 0 : CurrentRiskAssessment.RiskAssessmentKey;

        public override void PreProcessing()
        {
            if (ReEvaluateEncounterData == null)
            {
                return;
            }

            if (ReEvaluateEncounterData.BoolData.HasValue)
            {
                Loading = true;
                ReEvaluateCommand.Execute(null);
            }

            if ((Question.QuestionOasisMapping != null) && (Encounter.IsNew))
            {
                if (Question.QuestionOasisMapping.Any())
                {
                    if (OasisManager != null)
                    {
                        OasisManager.QuestionOasisMappingChanged(Question, ReEvaluateEncounterData);
                    }
                }
            }
        }

        public bool ReEvaluateValidate(out string SubSections)
        {
            SubSections = string.Empty;

            if (OpenReEval)
            {
                OK_Command.Execute(null);
            }

            bool AllValid = base.Validate(out SubSections);

            foreach (var item in Contacts)
                if (item.EncounterData.ReEvalSection != null && AllValid && item.EncounterData.BoolData.HasValue &&
                    item.EncounterData.BoolData.Value)
                {
                    foreach (var q in item.EncounterData.ReEvalSection.Questions)
                    {
                        string ErrorSection = string.Empty;

                        if (!q.Validate(out ErrorSection))
                        {
                            if (string.IsNullOrEmpty(SubSections))
                            {
                                SubSections = item.EncounterData.ReEvalSection.Label;
                            }

                            AllValid = false;
                        }
                    }
                }

            return AllValid;
        }

        public void RiskCleanup()
        {
            if (Contacts == null)
            {
                return;
            }

            foreach (var item in Contacts)
                if (item.EncounterData.ReEvalSection != null)
                {
                    foreach (var q in item.EncounterData.ReEvalSection.Questions) q.Cleanup();
                }
        }

        #endregion

        public void HospiceBereavedSurvivorsSetup()
        {
            Setup_PatientContact();
            Setup_Protected();
            ReEvaluateSetup();
        }

        public override void Cleanup()
        {
            RiskCleanup();
            base.Cleanup();
        }

        public void Setup_Protected()
        {

        }

        public ObservableCollection<QuestionBase> Contacts { get; set; }
        public ObservableCollection<QuestionBase> BackupContacts { get; set; }

        EncounterData CopyProperties(EncounterData source)
        {
            EncounterData EncounterData = new EncounterData();
            EncounterData.TextData = source.TextData;
            EncounterData.Text2Data = source.Text2Data;
            EncounterData.Text3Data = source.Text3Data;
            EncounterData.Text4Data = source.Text4Data;
            EncounterData.AddedDateTime = source.AddedDateTime;
            EncounterData.GuidData = source.GuidData;
            EncounterData.IntData = source.IntData;
            EncounterData.Int2Data = source.Int2Data;
            EncounterData.BoolData = source.BoolData;
            EncounterData.DateTimeData = source.DateTimeData;
            EncounterData.RealData = source.RealData;
            EncounterData.FuncDeficit = source.FuncDeficit;
            EncounterData.SignatureData = source.SignatureData;
            EncounterData.SectionKey = Section.SectionKey;
            EncounterData.QuestionGroupKey = QuestionGroupKey;
            EncounterData.QuestionKey = Question.QuestionKey;
            return EncounterData;
        }

        public override bool CopyForwardLastInstance()
        {
            return CopyPatientContactBereaved();
        }

        private PatientContact GetPatientContact(Guid? contactGuid)
        {
            if ((Patient == null) || (Patient.PatientContact == null))
            {
                return null;
            }

            return Patient.PatientContact.Where(p => ((p.Inactive == false) && (p.ContactGuid == contactGuid)))
                .FirstOrDefault();
        }

        public bool CopyPatientContactBereaved()
        {
            Contacts = new ObservableCollection<QuestionBase>();
            bool Break = false;
            foreach (Encounter previousE in Admission.Encounter.Where(p => !p.IsNew)
                         .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                foreach (EncounterData previousED in previousE.EncounterData
                             .Where(d => d.QuestionKey == Question.QuestionKey).OrderBy(d => d.EncounterDataKey))
                    if (previousED != null)
                    {
                        // Copy forward if-and-only-if this contact is still active
                        if (GetPatientContact(previousED.GuidData) != null)
                        {
                            Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                            {
                                Admission = Admission, Encounter = Encounter,
                                EncounterData = CopyProperties(previousED), Patient = Patient, Question = Question
                            });
                        }

                        EncounterRisk previousER = previousE.EncounterRisk.Where(x =>
                            x.RiskForID == previousED.GuidData && x.IsTotal &&
                            x.RiskAssessmentKey == RiskAssessmentKey && !x.RiskGroupKey.HasValue).FirstOrDefault();
                        if (previousER != null)
                        {
                            Encounter.CopyForwardRiskAssessment(previousE, RiskAssessmentKey, previousED.GuidData);
                        }

                        Break = true;
                    }

                if (Break)
                {
                    break;
                }
            }

            //default with one and allow more to be added
            if (Contacts.Any() == false)
            {
                EncounterData ed = new EncounterData
                {
                    SectionKey = Section.SectionKey, QuestionGroupKey = QuestionGroupKey,
                    QuestionKey = Question.QuestionKey, TextData = CurrentRiskAssessmentString + " - Not Done",
                    Text2Data = "None"
                };
                Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                {
                    Admission = Admission, Encounter = Encounter, EncounterData = ed, Patient = Patient,
                    Question = Question
                });
            }

            RaisePropertyChanged("Contacts");
            return true;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            CopyPatientContactBereaved();
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                if (Contacts != null)
                {
                    foreach (var item in Contacts)
                        try
                        {
                            ((IPatientService)DynamicFormViewModel.FormModel).Remove(item.EncounterData);
                        }
                        catch
                        {
                        }
                }

                Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in BackupContacts)
                {
                    //need to copy so raise property changes gets called - can't just copy the entire object
                    EncounterData ed = (EncounterData)Clone(item.EncounterData);
                    Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter, EncounterData = CopyProperties(ed),
                        Patient = Patient, Question = Question
                    });
                }

                this.RaisePropertyChangedLambda(p => p.Contacts);
            }
            else
            {
                BackupContacts = new ObservableCollection<QuestionBase>();
                foreach (var item in Contacts)
                    BackupContacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = Admission, Encounter = Encounter,
                        EncounterData = (EncounterData)Clone(item.EncounterData), Patient = Patient, Question = Question
                    });
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            bool AllValid = true;
            foreach (var item in Contacts)
            {
                item.EncounterData.ValidationErrors.Clear();

                if (item.EncounterData.GuidData != null)
                {
                    // Setup LevelOfBereavementServices CodeDescription for print
                    if (item.EncounterData.Int2Data == 0)
                    {
                        item.EncounterData.Int2Data = null;
                    }

                    item.EncounterData.Text3Data = (item.EncounterData.Int2Data == null)
                        ? null
                        : CodeLookupCache.GetCodeDescriptionFromKey(item.EncounterData.Int2Data);
                    ;
                    if (item.EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(item.EncounterData);
                    }
                }
                else
                {
                    if (item.EncounterData.EntityState == EntityState.Modified)
                    {
                        Encounter.EncounterData.Remove(item.EncounterData);
                    }
                }
            }

            AllValid = ReEvaluateValidate(out SubSections);
            return AllValid;
        }

        #region PatientContact

        public bool IsEdit => true;
        private PatientContact _SelectedItem;

        public PatientContact SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                _SelectedItem = value;
                this.RaisePropertyChangedLambda(p => p.SelectedItem);
            }
        }

        public RelayCommand AddContactCommand { get; set; }
        public RelayCommand<HospiceBereavedSurvivors> AddBehaviorCommand { get; set; }
        public RelayCommand<Guid?> PatientContactDetailsCommand { get; protected set; }

        public RelayCommand CancelPatientContactCommand { get; protected set; }
        public RelayCommand OKPatientContactCommand { get; protected set; }

        private void Setup_PatientContact()
        {
            AddContactCommand = new RelayCommand(() =>
            {
                SelectedItem = new PatientContact();
                SelectedItem.IsDynamicForm = true;
                SelectedItem.ContactGuid = Guid.NewGuid();
                int? TypeKey = CodeLookupCache.GetKeyFromCode("PATCONTACTADDRESS", "Patient");
                SelectedItem.ContactTypeKey = TypeKey == null ? 0 : (int)TypeKey;
                PopupDataTemplate = "PatientContactPopupDataTemplate";
                DynamicFormViewModel.PopupDataContext = this;
            });

            PatientContactDetailsCommand = new RelayCommand<Guid?>(patientContactGuid =>
            {
                if (patientContactGuid == null)
                {
                    return;
                }

                if (patientContactGuid == Guid.Empty)
                {
                    return;
                }

                if (Patient == null)
                {
                    return;
                }

                if (Patient.PatientContact == null)
                {
                    return;
                }

                PatientContact pc = Patient.PatientContact.Where(c => c.ContactGuid == patientContactGuid)
                    .FirstOrDefault();
                if (pc == null)
                {
                    return;
                }

                PatientContactDetailsDialog cw = new PatientContactDetailsDialog(pc);
                cw.Show();
            });

            OKPatientContactCommand = new RelayCommand(() =>
            {
                if (SelectedItem == null)
                {
                    return;
                }

                SelectedItem.ValidationErrors.Clear();
                if (SelectedItem.Validate())
                {
                    Patient.PatientContact.Add(SelectedItem);
                    DynamicFormViewModel.PopupDataContext = null;
                    PopupDataTemplate = null;
                }
            });

            CancelPatientContactCommand = new RelayCommand(() =>
            {
                DynamicFormViewModel.PopupDataContext = null;
                PopupDataTemplate = null;
            });
        }

        #endregion
    }

    public class HospiceBereavedSurvivorsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceBereavedSurvivors hb = new HospiceBereavedSurvivors(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                AddBehaviorCommand = new RelayCommand<HospiceBereavedSurvivors>(bereaved =>
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, TextData = q.LookupType + " - Not Done", Text2Data = "None"
                    };
                    bereaved.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }),
            };

            EncounterData eData = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (eData == null)
            {
                hb.CopyPatientContactBereaved();
            }
            else
            {
                hb.Contacts = new ObservableCollection<QuestionBase>();
                foreach (var item in vm.CurrentEncounter.EncounterData.Where(x =>
                             x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                             x.SectionKey == formsection.Section.SectionKey &&
                             x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey))
                    hb.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = item,
                        Patient = vm.CurrentPatient, Question = q
                    });

                //default with one and allow more to be added
                if (hb.Contacts.Any() == false)
                {
                    EncounterData ed = new EncounterData
                    {
                        SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey,
                        QuestionKey = q.QuestionKey, TextData = q.LookupType + " - Not Done", Text2Data = "None"
                    };
                    hb.Contacts.Add(new QuestionBase(Constants.DynamicForm.NonValidFormSectionQuestionKey)
                    {
                        Admission = vm.CurrentAdmission, Encounter = vm.CurrentEncounter, EncounterData = ed,
                        Patient = vm.CurrentPatient, Question = q
                    });
                }
            }

            hb.HospiceBereavedSurvivorsSetup();
            return hb;
        }
    }


    public enum Direction
    {
        Left,
        Top,
        Right,
        Bottom
    }

    public class PopupProvider
    {
        #region Constructors

        /// http://www.codeproject.com/Articles/26284/Reusable-Silverlight-Popup-Logic
        /// <summary>
        /// Encapsulates logic for popup controls so the code does not need to be rewritten.
        /// </summary>
        /// <param name="owner">
        /// The owner is the FrameworkElement that will trigger the popup to close it the
        /// mouse leaves its screan area.  The popup will only remain open after leaving the
        /// owner if the popupChild element is supplied in this constructor and the mouse 
        /// immediately enters the screen area of the popupChild element after leaving the owner.
        /// </param>
        /// <param name="trigger">
        /// The trigger is the framework element that triggers the popup panel to open.
        /// The popup will open on the MouseLeftButtonUp routed event of the trigger.
        /// </param>
        /// <param name="popup">
        /// The popup is the Popup primitive control that contains the content to be displayed.
        /// </param>
        /// <param name="popupChild">
        /// The popupChild is the child control of the popup panel.  The Popup control does not 
        /// raise MouseEnter and MouseLeave events so the child control must be used to detect
        /// if the popup should remain open or closed in conjuction with the owner element.
        /// This value may be left null to create situations where only the owner element
        /// controls whether the popup closes or not.  e.g. an image could trigger a popup that
        /// describes the image and the popup closes when the mouse leaves the image regardless
        /// of whether the mouse enters the description. 
        /// </param>
        /// <param name="placement">
        /// Determines which side of the owner element the popup will appear on.
        /// </param>
        public PopupProvider(FrameworkElement owner, FrameworkElement trigger, Popup popup, FrameworkElement popupChild,
            Direction placement)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (trigger == null)
            {
                throw new ArgumentNullException("trigger");
            }

            if (popup == null)
            {
                throw new ArgumentNullException("popup");
            }

            _owner = owner;
            _placement = placement;
            _popup = popup;
            _popupChild = popupChild;
            _popupChildTextBoxInput = null;
            _trigger = trigger;

            if (_popup != null)
            {
                _popup.Opened += _popup_Opened;
            }

            _owner.MouseEnter += _owner_MouseEnter;
            _owner.MouseLeave += _owner_MouseLeave;
            if (_popupChild != null)
            {
                _popupChild.MouseEnter += _popupChild_MouseEnter;
                _popupChild.MouseLeave += _popupChild_MouseLeave;
            }

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = new TimeSpan(0, 0, 0, 0, CloseDelay);
            _closeTimer.Tick += _closeTimer_Tick;
        }

        public void Cleanup()
        {
            if (_popup != null)
            {
                try
                {
                    _popup.Opened -= _popup_Opened;
                }
                catch
                {
                }
            }

            if (_owner != null)
            {
                try
                {
                    _owner.MouseEnter -= _owner_MouseEnter;
                }
                catch
                {
                }

                try
                {
                    _owner.MouseLeave -= _owner_MouseLeave;
                }
                catch
                {
                }
            }

            if (_popupChild != null)
            {
                try
                {
                    _popupChild.MouseEnter -= _popupChild_MouseEnter;
                }
                catch
                {
                }

                try
                {
                    _popupChild.MouseLeave -= _popupChild_MouseLeave;
                }
                catch
                {
                }
            }

            if (_popupChildTextBoxInput != null)
            {
                try
                {
                    _popupChildTextBoxInput.KeyDown -= _popupChildTextBoxInput_KeyDown;
                }
                catch
                {
                }
            }

            if (_popupChildComboBoxInput != null)
            {
                try
                {
                    _popupChildComboBoxInput.MouseEnter -= _popupChild_MouseEnter;
                }
                catch
                {
                }

                try
                {
                    _popupChildComboBoxInput.MouseLeave -= _popupChild_MouseLeave;
                }
                catch
                {
                }
            }

            _popup = null;
            _owner = null;
            _popupChild = null;
            _popupChildTextBoxInput = null;
            _popupChildComboBoxInput = null;
            _trigger = null;
            _closeTimer = null;
        }

        public void SetPopupChild(FrameworkElement popupChild)
        {
            if (_popupChild != null)
            {
                try
                {
                    _popupChild.MouseEnter -= _popupChild_MouseEnter;
                }
                catch
                {
                }

                try
                {
                    _popupChild.MouseLeave -= _popupChild_MouseLeave;
                }
                catch
                {
                }
            }

            _popupChild = popupChild;
            if (_popupChild != null)
            {
                _popupChild.MouseEnter += _popupChild_MouseEnter;
                _popupChild.MouseLeave += _popupChild_MouseLeave;
            }
        }

        public void SetPopupChildTextBoxInput(TextBox popupChildTextBoxInput)
        {
            if (_popupChildTextBoxInput != null)
            {
                try
                {
                    _popupChildTextBoxInput.KeyDown -= _popupChildTextBoxInput_KeyDown;
                }
                catch
                {
                }
            }

            _popupChildTextBoxInput = popupChildTextBoxInput;
            if (_popupChildTextBoxInput != null)
            {
                _popupChildTextBoxInput.KeyDown += _popupChildTextBoxInput_KeyDown;
                Deployment.Current.Dispatcher.BeginInvoke(() => { _popupChildTextBoxInput.Focus(); });
            }
        }

        public void SetPopupChildComboBoxInput(ComboBox popupChildComboBoxInput)
        {
            if (_popupChildComboBoxInput != null)
            {
                try
                {
                    _popupChildComboBoxInput.MouseEnter -= _popupChild_MouseEnter;
                }
                catch
                {
                }

                try
                {
                    _popupChildComboBoxInput.MouseLeave -= _popupChild_MouseLeave;
                }
                catch
                {
                }
            }

            _popupChildComboBoxInput = popupChildComboBoxInput;
            if (_popupChildComboBoxInput != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { _popupChildComboBoxInput.Focus(); });
            }
        }

        #endregion

        #region Event Handlers

        void _popup_Opened(object sender, EventArgs e)
        {
            if (_popupChildTextBoxInput != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { _popupChildTextBoxInput.Focus(); });
            }
        }

        void _closeTimer_Tick(object sender, EventArgs e)
        {
            DebugMessage("_closeTimer_Tick");
            ClosePopup();
        }

        void _owner_MouseEnter(object sender, MouseEventArgs e)
        {
            DebugMessage("_owner_MouseEnter");
            StopClosingPopup();
        }

        void _owner_MouseLeave(object sender, MouseEventArgs e)
        {
            DebugMessage("_owner_MouseLeave");
            BeginClosingPopup();
        }

        void _popupChild_MouseEnter(object sender, MouseEventArgs e)
        {
            DebugMessage("_popupLayout_MouseEnter");
            StopClosingPopup();
        }

        void _popupChild_MouseLeave(object sender, MouseEventArgs e)
        {
            DebugMessage("_popupLayout_MouseLeave");
            BeginClosingPopup();
        }

        void _popupChildTextBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) || (e.Key == Key.Escape) || (e.Key == Key.Tab))
            {
                e.Handled = true;
                BeginClosingPopup();
            }
        }

        void _trigger_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DebugMessage("_trigger_MouseLeftButtonUp");
            TriggerClick();
        }

        public void TriggerClick()
        {
            if (!_isPopupOpen)
            {
                GeneralTransform gt = _owner.TransformToVisual(null);
                Point p;
                switch (_placement)
                {
                    case Direction.Left:
                        p = gt.Transform(new Point(_popup.ActualWidth, 0));
                        break;
                    case Direction.Top:
                        p = gt.Transform(new Point(0, -_popup.ActualHeight + 2));
                        break;
                    case Direction.Bottom:
                        p = gt.Transform(new Point(0, _owner.ActualHeight - 2));
                        break;
                    case Direction.Right:
                        p = gt.Transform(new Point(_owner.ActualWidth, 0));
                        break;
                    default:
                        throw new InvalidOperationException("Placement of popup not defined.");
                }

                _isPopupOpen = _popup.IsOpen = true;
            }
            else
            {
                BeginClosingPopup();
            }
        }

        private void DebugMessage(string methodName)
        {
            System.Diagnostics.Debug.WriteLine(
                "{0}: _isPopupOpen({1}) _isPopupClosing({2}) _popup.IsOpen({3}) _closeTimer.IsEnabled({4})", methodName,
                _isPopupOpen, _isPopupClosing, _popup.IsOpen, _closeTimer.IsEnabled);
        }

        #endregion

        #region Private Fields

        private const int CloseDelay = 100;

        private DispatcherTimer _closeTimer;
        private bool _isPopupOpen;
        private bool _isPopupClosing;
        private FrameworkElement _owner;
        private Direction _placement;
        private Popup _popup;
        private FrameworkElement _popupChild;
        private TextBox _popupChildTextBoxInput;
        private ComboBox _popupChildComboBoxInput;
        private FrameworkElement _trigger;

        #endregion

        #region Private Methods

        public void BeginClosingSubPopup()
        {
            inSubPopup = false;
            BeginClosingPopup();
        }

        public void BeginClosingPopup()
        {
            if (_isPopupOpen && !_isPopupClosing)
            {
                _isPopupClosing = true;
                _closeTimer.Start();
            }
        }

        public void ForceClosePopup()
        {
            _closeTimer.Stop();
            _popup.IsOpen = false;
            _isPopupOpen = false;
            _isPopupClosing = false;
            inSubPopup = false;
        }

        public void ClosePopup()
        {
            if (inSubPopup)
            {
                return;
            }

            if (_isPopupOpen && _isPopupClosing)
            {
                _closeTimer.Stop();
                _isPopupOpen = _isPopupClosing = _popup.IsOpen = false;
            }
        }

        private bool inSubPopup;

        public void StopClosingSubPopup()
        {
            inSubPopup = true;
            StopClosingPopup();
        }

        public void StopClosingPopup()
        {
            if (_isPopupOpen && _isPopupClosing)
            {
                _closeTimer.Stop();
                _isPopupClosing = false;
            }
        }

        #endregion
    }

    public class HospicePainComfort : HospiceQuestionBase
    {
        public HospicePainComfort(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        public override void OnProcessHospiceQuestion()
        {
        }

        public bool AskInitialPainQuestion
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                if (EncounterData == null)
                {
                    return false;
                }

                if (Patient == null)
                {
                    return false;
                }

                if (Patient.IsUnder18)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return false;
                }

                if (Encounter.EncounterIsEval == false)
                {
                    return false;
                }

                if (Encounter.IsSkilledNursingServiceType == false)
                {
                    return false;
                }

                if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    // Set/reset Initial Pain Question's service datetime
                    EncounterData.DateTimeData = (Encounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.Now
                        : Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
                    // Set/reset this Pain Question's service datetime
                    EncounterData.AddedDateTime = (Encounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.Now
                        : Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
                }

                return true;
            }
        }

        public bool AskFollowupPainQuestion
        {
            get
            {
                if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    // Set/reset this Pain Question's service datetime
                    EncounterData.AddedDateTime = (Encounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.Now
                        : Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
                }

                if (AskInitialPainQuestion)
                {
                    return false;
                }

                if (Encounter == null)
                {
                    return false;
                }

                if (EncounterData == null)
                {
                    return false;
                }

                if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    EncounterData.AddedDateTime = (Encounter.EncounterOrTaskStartDateAndTime == null)
                        ? DateTime.Now
                        : Encounter.EncounterOrTaskStartDateAndTime.GetValueOrDefault().DateTime;
                }
                //GetPreviousPainFollowupEncounterData -- need this filter iff only ask once in the 48-72 hour window

                // if initial response was Yes and in 48 - 72 hour (to end of day) window - ask followup
                // Note - from the copy forward we have the AskInitialPainQuestion data in TextData and Text2Data
                if ((string.IsNullOrWhiteSpace(EncounterData.TextData) == false) &&
                    (EncounterData.TextData.ToLower().Trim() == "yes"))
                {
                    if (EncounterData.IsPainFollowupQuestionIn48To72HourWindow)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private EncounterData GetPreviousPainFollowupEncounterData
        {
            get
            {
                if (EncounterData == null)
                {
                    return null;
                }

                foreach (var item in Admission.Encounter.Where(p => !p.IsNew)
                             .OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    EncounterData previous = item.EncounterData.Where(d =>
                        d.QuestionKey == Question.QuestionKey && d.EncounterKey != EncounterData.EncounterKey &&
                        d.BoolData == false).FirstOrDefault();
                    if (previous != null)
                    {
                        return previous;
                    }
                }

                return null;
            }
        }

        public override bool Validate(out string SubSections)
        {
            // To reset EncounterData dates based on servicedate
            RaisePropertyChanged("AskInitialPainQuestion");
            RaisePropertyChanged("AskFollowupPainQuestion");

            SubSections = string.Empty;
            bool AllValid = true;
            UpdateEncounterData();
            if (EncounterData == null)
            {
                return true;
            }

            EncounterData.ValidationErrors.Clear();
            if (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed)
            {
                return true;
            }

            if (AskInitialPainQuestion && Encounter.FullValidation)
            {
                if (string.IsNullOrWhiteSpace(EncounterData.TextData))
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult((string.Format("{0} is required", Label)),
                        new[] { "TextData" }));
                    AllValid = false;
                }
                else if (EncounterData.TextData.ToLower().Trim() == "unabletoselfreport")
                {
                    if (string.IsNullOrWhiteSpace(EncounterData.Text2Data))
                    {
                        EncounterData.ValidationErrors.Add(
                            new ValidationResult("Reason unable to self-report is required", new[] { "Text2Data" }));
                        AllValid = false;
                    }
                }

                if ((string.IsNullOrWhiteSpace(EncounterData.TextData)) ||
                    (EncounterData.TextData.ToLower().Trim() != "unabletoselfreport"))
                {
                    EncounterData.Text2Data = null;
                }

                EncounterData.Text3Data = null;
            }
            else if (AskFollowupPainQuestion && Encounter.FullValidation)
            {
                if (string.IsNullOrWhiteSpace(EncounterData.Text3Data))
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult(
                        "Does the patient feel that pain has been brought to a comfortable level? is required",
                        new[] { "Text3Data" }));
                    AllValid = false;
                }
            }

            if ((AskInitialPainQuestion || AskFollowupPainQuestion))
            {
                if (EncounterData.IsNew)
                {
                    if (EncounterData.BoolData != null)
                    {
                        Encounter.EncounterData.Add(EncounterData);
                    }
                }
            }
            else
            {
                try
                {
                    Encounter.EncounterData.Remove(EncounterData);
                }
                catch
                {
                }

                GetNewEncounterData();
            }

            return AllValid;
        }

        public FormSection FormSection { get; set; }

        public void GetNewEncounterData()
        {
            EncounterData = new EncounterData
            {
                SectionKey = FormSection.SectionKey.Value, QuestionGroupKey = QuestionGroupKey,
                QuestionKey = Question.QuestionKey
            };
            ApplyDefaults();
            CopyForwardLastInstance();
            UpdateEncounterData();
        }

        public void UpdateEncounterData()
        {
            if (EncounterData == null)
            {
                GetNewEncounterData();
            }

            if (AskInitialPainQuestion)
            {
                EncounterData.BoolData = true;
            }
            else if (AskFollowupPainQuestion)
            {
                EncounterData.BoolData = false;
            }
            else
            {
                EncounterData.BoolData = null;
            }

            RaisePropertyChanged("AskInitialPainQuestion");
            RaisePropertyChanged("AskFollowupPainQuestion");
        }
    }

    public class HospicePainComfortFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospicePainComfort hpc = new HospicePainComfort(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                FormSection = formsection,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                hpc.GetNewEncounterData();
            }
            else
            {
                hpc.EncounterData = ed;
                hpc.UpdateEncounterData();
            }

            hpc.ProcessHospiceQuestion();
            return hpc;
        }
    }

    public class HospiceAmputation : QuestionBase
    {
        public HospiceAmputation(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public RelayCommand RightAmputationChanged { get; set; }
        public RelayCommand LeftAmputationChanged { get; set; }

        public override void PreProcessing()
        {
            if (RightAmputationChanged != null)
            {
                RightAmputationChanged.Execute(this);
            }

            if (LeftAmputationChanged != null)
            {
                LeftAmputationChanged.Execute(this);
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterData.ValidationErrors.Clear();

            if (EncounterData.IntData.HasValue || EncounterData.Int2Data.HasValue)
            {
                if (EncounterData.IsNew)
                {
                    Encounter.EncounterData.Add(EncounterData);
                }

                return true;
            }

            if (EncounterData.EntityState == EntityState.Modified)
            {
                Encounter.EncounterData.Remove(EncounterData);
                EncounterData = new EncounterData
                {
                    SectionKey = EncounterData.SectionKey, QuestionGroupKey = EncounterData.QuestionGroupKey,
                    QuestionKey = EncounterData.QuestionKey
                };
            }

            return true;
        }
    }

    public class HospiceAmputationFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            return new HospiceAmputation(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
                RightAmputationChanged = new RelayCommand(() =>
                {
                    foreach (var item in q.SourceQuestionNotification)
                    {
                        int amp = 0;
                        if (ed.IntData > 0)
                        {
                            amp = CodeLookupCache.GetSequenceFromKey(ed.IntData).Value;
                        }
                        else
                        {
                            amp = 0;
                        }

                        Messenger.Default.Send(new int[3] { qgkey, 0, amp },
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, vm.CurrentEncounter.AdmissionKey,
                                vm.CurrentEncounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                    }
                }),
                LeftAmputationChanged = new RelayCommand(() =>
                {
                    foreach (var item in q.SourceQuestionNotification)
                    {
                        int amp = 0;
                        if (ed.Int2Data > 0)
                        {
                            amp = CodeLookupCache.GetSequenceFromKey(ed.Int2Data).Value;
                        }
                        else
                        {
                            amp = 0;
                        }

                        Messenger.Default.Send(new int[3] { qgkey, 1, amp },
                            string.Format("{0}|{1}|{2}|{3}|{4}", item.MessageType, vm.CurrentEncounter.AdmissionKey,
                                vm.CurrentEncounter.EncounterID, item.SourceQuestionKey, item.DestinationQuestionKey));
                    }
                })
            };
        }
    }

    public class HospiceSimpleROM : QuestionBase
    {
        public HospiceSimpleROM(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private bool _QuestionHidden;

        public bool QuestionHidden
        {
            get { return RightHidden && LeftHidden; }
            set
            {
                if (_QuestionHidden != value)
                {
                    _QuestionHidden = value;

                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        private bool _RightHidden;

        public bool RightHidden
        {
            get { return _RightHidden; }
            set
            {
                if (_RightHidden != value)
                {
                    _RightHidden = value;

                    this.RaisePropertyChangedLambda(p => p.RightHidden);
                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        private bool _LeftHidden;

        public bool LeftHidden
        {
            get { return _LeftHidden; }
            set
            {
                if (_LeftHidden != value)
                {
                    _LeftHidden = value;

                    this.RaisePropertyChangedLambda(p => p.LeftHidden);
                    this.RaisePropertyChangedLambda(p => p.QuestionHidden);
                }
            }
        }

        public override void ProcessAmputationMessage(int[] message)
        {
            int value = message[2];

            if (value > 0)
            {
                value--;
            }

            if (value == 4)
            {
                value = 3;
            }

            if (message[1] == 0)
            {
                if (value == 0 || value >= Sequence - 1)
                {
                    RightHidden = false;
                }
                else
                {
                    RightHidden = true;
                    EncounterData.TextData = null;
                }
            }
            else
            {
                if (value == 0 || value >= Sequence - 1)
                {
                    LeftHidden = false;
                }
                else
                {
                    LeftHidden = true;
                    EncounterData.Text2Data = null;
                }
            }
        }
    }

    public class HospiceSimpleROMFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceSimpleROM sr = new HospiceSimpleROM(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
            };

            var qg = formsection.FormSectionQuestion.Where(p => p.QuestionGroupKey == qgkey)
                .Select(p => p.QuestionGroup).FirstOrDefault();
            if (qg != null)
            {
                var seq = qg.QuestionGroupQuestion.Where(p => p.QuestionKey == q.QuestionKey).FirstOrDefault().Sequence;
                sr.Sequence = seq;
            }


            return sr;
        }
    }

    public class DisciplineSynopsis : HospiceQuestionBase
    {
        public EncounterSynopsis EncounterSynopsis { get; set; }

        public AdmissionTeamMeeting ATM
        {
            get
            {
                if ((Admission == null) || (Admission.AdmissionTeamMeeting == null))
                {
                    return null;
                }

                if ((EncounterSynopsis != null) && (EncounterSynopsis.AdmissionTeamMeetingKey != null))
                {
                    AdmissionTeamMeeting atm = Admission.AdmissionTeamMeeting
                        .Where(a => a.AdmissionTeamKey == EncounterSynopsis.AdmissionTeamMeetingKey).FirstOrDefault();
                    if (atm != null)
                    {
                        return atm;
                    }
                }

                DateTime encounterDate = Encounter == null || Encounter.EncounterOrTaskStartDateAndTime == null
                    ? DateTime.Today
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;

                return Admission.GetAdmissionTeamMeetingForDate(encounterDate);
            }
        }

        public RelayCommand LaunchSynopsisHistory { get; protected set; }

        public bool ShowSynopsisHistory
        {
            get
            {
                if (Encounter == null)
                {
                    return false;
                }

                return (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) ? false : true;
            }
        }

        public DisciplineSynopsis(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            LaunchSynopsisHistory = new RelayCommand(() =>
            {
                AdmissionTeamMeeting encounterATM = ATM;
                DateTime fromDT = (encounterATM == null || encounterATM.LastTeamMeetingDate == null)
                    ? Convert.ToDateTime("01/01/2000")
                    : (DateTime)encounterATM.LastTeamMeetingDate;
                DateTime throughDT = (encounterATM == null || encounterATM.NextTeamMeetingDate == null)
                    ? Convert.ToDateTime("01/01/2115")
                    : (DateTime)encounterATM.NextTeamMeetingDate;
                if ((encounterATM == null) && (Admission != null) || (Admission.AdmissionTeamMeeting != null))
                {
                    DateTime encounterDate = Encounter == null || Encounter.EncounterOrTaskStartDateAndTime == null
                        ? DateTime.Today
                        : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                    AdmissionTeamMeeting mostRecentATM = Admission.AdmissionTeamMeeting.Where(atm =>
                            ((atm.Inactive == false) &&
                             ((atm.LastTeamMeetingDate != null) &&
                              (encounterDate >= ((DateTime)atm.LastTeamMeetingDate).Date))))
                        .OrderByDescending(atm => atm.LastTeamMeetingDate).FirstOrDefault();
                    fromDT = (mostRecentATM == null || mostRecentATM.LastTeamMeetingDate == null)
                        ? fromDT
                        : (DateTime)mostRecentATM.LastTeamMeetingDate;
                }

                InformationButtonDialog d = new InformationButtonDialog("Synopsis History",
                    Admission.GetDisciplineAgnosticSynopsisHistory(fromDT, throughDT, Encounter));
                d.Show();
            });
        }

        public string NextTeamMeetingDateText
        {
            get
            {
                DateTime nextTeamMeetingDate =
                    ((EncounterSynopsis == null) || (EncounterSynopsis.NextTeamMeetingDate == null))
                        ? NextTeamMeetingDate
                        : ((DateTime)EncounterSynopsis.NextTeamMeetingDate).Date;

                return string.Format("Next team meeting date {0}", nextTeamMeetingDate.ToShortDateString());
            }
        }

        public DateTime NextTeamMeetingDate
        {
            get
            {
                AdmissionTeamMeeting encounterATM = ATM;
                if (encounterATM != null && encounterATM.NextTeamMeetingDate != null)
                {
                    return ((DateTime)encounterATM.NextTeamMeetingDate).Date;
                }

                // Any time we can't find an AdmissionTeamMeeting calculate one
                DateTime encounterDate = Encounter == null || Encounter.EncounterOrTaskStartDateAndTime == null
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;

                if ((encounterATM == null) && (Admission != null) || (Admission.AdmissionTeamMeeting != null))
                {
                    // see if next is past due - if so, display the past due date
                    AdmissionTeamMeeting mostRecentATM = Admission.AdmissionTeamMeeting.Where(atm =>
                            ((atm.Inactive == false) &&
                             ((atm.LastTeamMeetingDate != null) &&
                              (encounterDate >= ((DateTime)atm.LastTeamMeetingDate).Date))))
                        .OrderByDescending(atm => atm.LastTeamMeetingDate).FirstOrDefault();
                    if ((mostRecentATM != null) && (mostRecentATM.NextTeamMeetingDate != null))
                    {
                        return ((DateTime)mostRecentATM.NextTeamMeetingDate).Date;
                    }
                }

                // punt - calculate when it should be based on the encounterDate/Socdate
                AdmissionGroup ag = Admission.GetServiceLineGroupingForTeamMeeting(encounterDate);
                if (ag == null)
                {
                    return encounterDate.AddDays(14);
                }

                DateTime tomorrow = DateTime.Today.AddDays(1).Date;
                DateTime earliestPossibleDate = (CurrentAdmission.SOCDate == null)
                    ? ((encounterDate.Date < tomorrow) ? tomorrow : encounterDate.Date)
                    : ((((DateTime)CurrentAdmission.SOCDate).Date < tomorrow)
                        ? tomorrow
                        : ((DateTime)CurrentAdmission.SOCDate).Date);

                DateTime? calcDate = DateCalculationHelper.CalculateNextTeamMeetingDate(earliestPossibleDate,
                    ag.ServiceLineGroupingKey, Patient.LastName, true);
                return (calcDate == null) ? earliestPossibleDate.AddDays(14) : ((DateTime)calcDate).Date;
            }
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;

            SubSections = string.Empty;

            if (Encounter.FullValidation && Required && string.IsNullOrEmpty(EncounterSynopsis.SynopsisText))
            {
                EncounterSynopsis.ValidationErrors.Add(new ValidationResult("Synopsis text is required.",
                    new[] { "SynopsisText" }));
                AllValid = false;
            }

            return AllValid;
        }
    }

    public class DisciplineSynopsisFactory
    {
        public static HospiceQuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            DisciplineSynopsis qb = new DisciplineSynopsis(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline
            };

            EncounterSynopsis ed = vm.CurrentEncounter.EncounterSynopsis.FirstOrDefault();

            int? discKey = 0;
            if (qb.Encounter.ServiceTypeKey != null)
            {
                discKey = ServiceTypeCache.GetDisciplineKey((int)qb.Encounter.ServiceTypeKey);
            }

            if (ed == null)
            {
                ed = new EncounterSynopsis
                    { EncounterKey = qb.Encounter.EncounterKey, DisciplineKey = (discKey == null ? 0 : (int)discKey) };
                qb.Encounter.EncounterSynopsis.Add(ed);
            }

            qb.EncounterSynopsis = ed;
            if (qb.Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Edit)
            {
                qb.EncounterSynopsis.NextTeamMeetingDate = qb.NextTeamMeetingDate;
            }

            return qb;
        }
    }

    public class ObsoleteTeamMeetingSynopsis : HospiceQuestionBase
    {
        public DateTime LastTeamMeetEndDate { get; set; }
        public DateTime LastTeamMeetStartDate { get; set; }
        public DateTime TeamMeetStartDate { get; set; }
        public DateTime TeamMeetEndDate { get; set; }
        public RelayCommand<int> LaunchSynopsisHistory { get; protected set; }

        public ObsoleteTeamMeetingSynopsis(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            LaunchSynopsisHistory = new RelayCommand<int>(discParm =>
            {
                InformationButtonDialog d = new InformationButtonDialog("Synopsis History",
                    Admission.GetDisciplineSynopsisHistory(LastTeamMeetStartDate, LastTeamMeetEndDate.AddDays(1),
                        discParm, Encounter));
                d.Show();
            });
        }

        public void SetLastTeamMeetingDates()
        {
            // value of previous LastTeamMeetingDate
            LastTeamMeetStartDate = Convert.ToDateTime("01/01/2000");
            // value of previous NextTeamMeetingDate
            LastTeamMeetEndDate = TeamMeetStartDate == null ? Convert.ToDateTime("01/01/2200") : TeamMeetStartDate;

            var lastTM = Admission.AdmissionTeamMeeting.Where(a =>
                    (Encounter.IsNew && a.Superceded == false) || (Encounter.IsNew && a.Superceded))
                .OrderByDescending(atm => atm.AdmissionTeamKey).FirstOrDefault();
            if (lastTM != null)
            {
                if (lastTM.LastTeamMeetingDate != null)
                {
                    LastTeamMeetStartDate = (DateTime)lastTM.LastTeamMeetingDate;
                }

                if (lastTM.NextTeamMeetingDate != null)
                {
                    LastTeamMeetEndDate = (DateTime)lastTM.NextTeamMeetingDate;
                }
            }
        }

        public void BuildSynopsisList()
        {
            if (Encounter == null || !Encounter.IsNew)
            {
                return;
            }

            foreach (var ad in Admission.AdmissionDiscipline.Where(a => (
                         (a.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Admitted) ||
                         (a.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Transferred) ||
                         (a.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Resumed) ||
                         ((a.AdmissionStatus == AdmissionStatusHelper.AdmissionStatus_Discharged) &&
                          (a.DischargeDateTime != null) &&
                          (a.DischargeDateTime.Value.Date >= LastTeamMeetStartDate.Date) &&
                          (a.DischargeDateTime.Value.Date <= LastTeamMeetEndDate.AddDays(1).Date)))))
            {
                var encList = Admission.Encounter.Where(e => (
                        e.EncounterKey != Encounter.EncounterKey && e.EncounterSynopsis != null &&
                        e.EncounterSynopsis.Any()
                        && e.Form != null && !e.Form.IsTeamMeeting && e.DisciplineKey == ad.DisciplineKey
                        && ((e.EncounterOrTaskStartDateAndTime != null) &&
                            (e.EncounterOrTaskStartDateAndTime.Value.Date >= LastTeamMeetStartDate.Date))
                        && ((e.EncounterOrTaskStartDateAndTime != null) &&
                            (e.EncounterOrTaskStartDateAndTime.Value.Date <=
                             LastTeamMeetEndDate.AddDays(1).Date))))
                    .OrderByDescending(e => e.EncounterOrTaskStartDateAndTime);

                EncounterSynopsis es = null;
                if (encList != null)
                {
                    Encounter fe = encList.FirstOrDefault();
                    if (fe != null)
                    {
                        es = fe.EncounterSynopsis.FirstOrDefault();
                    }
                }

                EncounterSynopsis TeamES = new EncounterSynopsis
                    { DisciplineKey = ad.DisciplineKey, SynopsisText = ((es == null) ? null : es.SynopsisText) };
                Encounter.EncounterSynopsis.Add(TeamES);
            }
        }

        public DateTime GetTeamMeetingStartDateToUse(AdmissionTeamMeeting ATMParm)
        {
            // Any time we can't find an AdmissionTeamMeetingn row we should pass true to the calculate to use the earliest possible date instead of the schedule.
            if (ATMParm != null && ATMParm.NextTeamMeetingDate != null)
            {
                return (DateTime)ATMParm.NextTeamMeetingDate;
            }

            if ((ATMParm == null || (ATMParm != null && ATMParm.NextTeamMeetingDate == null))
                && AdmissionDiscipline != null && AdmissionDiscipline.DisciplineAdmitDateTime != null)
            {
                var slg = Admission.GetServiceLineGroupingForTeamMeeting(
                    (DateTime)AdmissionDiscipline.DisciplineAdmitDateTime);
                var calcDate = slg == null
                    ? (DateTime)AdmissionDiscipline.DisciplineAdmitDateTime
                    : DateCalculationHelper.CalculateNextTeamMeetingDate(
                        (DateTime)AdmissionDiscipline.DisciplineAdmitDateTime, slg.ServiceLineGroupingKey,
                        Patient.LastName, true);
                return calcDate != null ? (DateTime)calcDate : (DateTime)AdmissionDiscipline.DisciplineAdmitDateTime;
            }

            return DateTime.Today;
        }
    }

    public class ObsoleteTeamMeetingSynopsisFactory
    {
        public static HospiceQuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ObsoleteTeamMeetingSynopsis qb = new ObsoleteTeamMeetingSynopsis(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline
            };

            AdmissionTeamMeeting ATM = null;
            if (qb.Encounter.EncounterTeamMeeting != null && qb.Encounter.EncounterTeamMeeting.Any())
            {
                EncounterTeamMeeting etm = qb.Encounter.EncounterTeamMeeting.FirstOrDefault();
                if (etm != null)
                {
                    ATM = etm.AdmissionTeamMeeting;
                    if (ATM.LastTeamMeetingDate != null)
                    {
                        qb.TeamMeetStartDate = (DateTime)ATM.LastTeamMeetingDate;
                    }

                    if (ATM.NextTeamMeetingDate != null)
                    {
                        qb.TeamMeetEndDate = (DateTime)ATM.NextTeamMeetingDate;
                    }
                }
            }

            if (ATM == null)
            {
                ATM = qb.Admission.AdmissionTeamMeeting.Where(a => a.Superceded == false)
                    .OrderByDescending(b => b.NextTeamMeetingDate).FirstOrDefault();

                qb.TeamMeetStartDate = qb.GetTeamMeetingStartDateToUse(ATM);
                // Get the proper service line grouping row to use.
                var slg = qb.Admission.GetServiceLineGroupingForTeamMeeting(qb.TeamMeetStartDate);
                // The first team meeting row should have been created, so we pass in false to the calculate to use the schedule.
                var tempDate = slg == null
                    ? qb.TeamMeetStartDate
                    : DateCalculationHelper.CalculateNextTeamMeetingDate(qb.TeamMeetStartDate,
                        slg.ServiceLineGroupingKey, qb.Patient.LastName, false);
                qb.TeamMeetEndDate = (tempDate == null)
                    ? qb.TeamMeetStartDate.AddDays(DateCalculationHelper.DefaultTeamMeetingSpan)
                    : (DateTime)tempDate;
            }

            qb.SetLastTeamMeetingDates();
            qb.BuildSynopsisList();
            return qb;
        }
    }

    public class IDTSynopsis : HospiceQuestionBase
    {
        private EncounterSynopsis encounterSynopsis;

        public EncounterSynopsis EncounterSynopsis
        {
            get { return encounterSynopsis; }
            set
            {
                encounterSynopsis = value;
                this.RaisePropertyChangedLambda(p => p.EncounterSynopsis);
            }
        }

        public string SynopsisHistoryList => (Admission == null)
            ? "No synopses are on file."
            : Admission.GetTeamMeetingSynopsisHistory(SynopsisHistoryStartDate, SynopsisHistoryEndDate,
                ThisAdmissionTeamMeeting, AssignAdmissionTeamMeeting);

        public IDTSynopsis(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private bool AssignAdmissionTeamMeeting =>
            ((Encounter == null) || (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed))
                ? false
                : true;

        private DateTime SynopsisHistoryStartDate
        {
            get
            {
                DateTime startDate = Convert.ToDateTime("01/01/1900");
                if ((Encounter != null) && (Encounter.EncounterTeamMeeting != null) && (Admission != null) &&
                    (Admission.AdmissionTeamMeeting != null))
                {
                    DateTime thisTMDate = SynopsisHistoryEndDate;
                    AdmissionTeamMeeting prevATM = Admission.AdmissionTeamMeeting
                        .Where(a => a.LastTeamMeetingDate < thisTMDate).OrderByDescending(b => b.NextTeamMeetingDate)
                        .FirstOrDefault();
                    if ((prevATM != null) && (prevATM.LastTeamMeetingDate != null))
                    {
                        startDate = ((DateTime)prevATM.LastTeamMeetingDate).Date;
                    }
                }

                return startDate;
            }
        }

        private DateTime SynopsisHistoryEndDate
        {
            get
            {
                DateTime endDate = DateTime.Today.Date;
                AdmissionTeamMeeting thisATM = ThisAdmissionTeamMeeting;
                if ((thisATM != null) && (thisATM.LastTeamMeetingDate != null))
                {
                    endDate = ((DateTime)thisATM.LastTeamMeetingDate).Date;
                }

                return endDate;
            }
        }

        private AdmissionTeamMeeting ThisAdmissionTeamMeeting
        {
            get
            {
                // AdmissionTeamMeeting is setup by the TeamMeetingDates factory - and is assumed to be here when referenced
                // as TeamMeetingDates and this question IDTSynopsis (and IDTSynopsisHistory) are always pary of a team meeting form
                if ((Encounter == null) || (Encounter.EncounterTeamMeeting == null))
                {
                    return null;
                }

                EncounterTeamMeeting ed = Encounter.EncounterTeamMeeting.FirstOrDefault();
                return (ed == null) ? null : ed.AdmissionTeamMeeting;
            }
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;

            SubSections = string.Empty;

            if ((Question != null) && (Question.DataTemplate == "IDTSynopsis") && (Encounter.FullValidation) &&
                (Required) && (EncounterSynopsis != null) && (string.IsNullOrEmpty(EncounterSynopsis.SynopsisText)))
            {
                EncounterSynopsis.ValidationErrors.Add(new ValidationResult("IDT Synopsis is required.",
                    new[] { "SynopsisText" }));
                AllValid = false;
            }

            return AllValid;
        }
    }

    public class IDTSynopsisFactory
    {
        public static HospiceQuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterSynopsis es = null;
            if ((vm != null) && (vm.CurrentEncounter != null) && (vm.CurrentEncounter.EncounterSynopsis != null) &&
                (vm.CurrentAdmissionDiscipline != null))
            {
                es = vm.CurrentEncounter.EncounterSynopsis.FirstOrDefault();
                if (es == null)
                {
                    es = new EncounterSynopsis
                        { DisciplineKey = vm.CurrentAdmissionDiscipline.DisciplineKey, SynopsisText = null };
                    vm.CurrentEncounter.EncounterSynopsis.Add(es);
                }
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            IDTSynopsis qb = new IDTSynopsis(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                EncounterSynopsis = es,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline
            };

            return qb;
        }
    }

    public class TeamMeetingDates : HospiceQuestionBase
    {
        public AdmissionTeamMeeting AdmissionTeamMeeting { get; set; }

        public TeamMeetingDates(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public DateTime? LastTeamMeetingDateDisplay
        {
            get
            {
                if (AdmissionTeamMeeting == null)
                {
                    return null;
                }

                return AdmissionTeamMeeting.LastTeamMeetingDate;
            }
            set
            {
                if (AdmissionTeamMeeting == null)
                {
                    return;
                }

                AdmissionTeamMeeting.LastTeamMeetingDate = value;
                if (Encounter.PreviousEncounterStatus != (int)EncounterStatusType.Completed)
                {
                    NextTeamMeetingDateDisplay = ReCalculateNextTeamMeetingDate;
                }

                this.RaisePropertyChangedLambda(p => p.LastTeamMeetingDateDisplay);
                this.RaisePropertyChangedLambda(p => p.ScheduleNextTeamMeeting);
            }
        }

        public DateTime? NextTeamMeetingDateDisplay
        {
            get
            {
                if (AdmissionTeamMeeting == null)
                {
                    return null;
                }

                return AdmissionTeamMeeting.NextTeamMeetingDate;
            }
            set
            {
                if (AdmissionTeamMeeting == null)
                {
                    return;
                }

                if (AdmissionTeamMeeting != null)
                {
                    AdmissionTeamMeeting.NextTeamMeetingDate = value;
                }

                this.RaisePropertyChangedLambda(p => p.NextTeamMeetingDateDisplay);
            }
        }

        public bool ScheduleNextTeamMeeting
        {
            get
            {
                // We only need one team meeting after the discharge, transfer or death date.  After it's been done, stop creating team meetings.
                if (Admission == null)
                {
                    return true;
                }

                if (!LastTeamMeetingDateDisplay.HasValue)
                {
                    return true;
                }

                if (Admission.DeathDate.HasValue &&
                    Admission.DeathDate.Value.Date <= LastTeamMeetingDateDisplay.Value.Date)
                {
                    return false;
                }

                if (Admission.DischargeDateTime.HasValue &&
                    Admission.DischargeDateTime.Value.Date <= LastTeamMeetingDateDisplay.Value.Date)
                {
                    return false;
                }

                if (Admission.IsAdmissionStatusTransferred)
                {
                    var MRTransfer = Admission.MostRecentTransfer;
                    if (MRTransfer != null && MRTransfer.TransferDate != null &&
                        MRTransfer.TransferDate.Date <= LastTeamMeetingDateDisplay.Value.Date)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public DateTime? ReCalculateNextTeamMeetingDate
        {
            get
            {
                DateTime lastDate =
                    ((AdmissionTeamMeeting == null) || (AdmissionTeamMeeting.LastTeamMeetingDate == null))
                        ? DateTime.Today.Date
                        : ((DateTime)AdmissionTeamMeeting.LastTeamMeetingDate).Date;
                AdmissionGroup ag = Admission.GetServiceLineGroupingForTeamMeeting(lastDate);

                DateTime? nextDate = (ag == null)
                    ? lastDate.AddDays(14)
                    : DateCalculationHelper.CalculateNextTeamMeetingDate(lastDate, ag.ServiceLineGroupingKey,
                        Patient.LastName, false);
                if (nextDate == null)
                {
                    nextDate = lastDate.AddDays(14);
                }

                return (ScheduleNextTeamMeeting == false) ? null : nextDate;
            }
        }
    }

    public class TeamMeetingDatesFactory
    {
        public static HospiceQuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            TeamMeetingDates qb = new TeamMeetingDates(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline
            };
            EncounterTeamMeeting eTM = vm.CurrentEncounter.EncounterTeamMeeting.FirstOrDefault();
            if ((eTM != null) && (eTM.AdmissionTeamMeeting != null))
            {
                // existing (saved) TM encounter
                qb.AdmissionTeamMeeting = eTM.AdmissionTeamMeeting;
                qb.NextTeamMeetingDateDisplay =
                    qb.ScheduleNextTeamMeeting ? eTM.AdmissionTeamMeeting.NextTeamMeetingDate : null;
            }
            else if (eTM != null)
            {
                // New TM encounter - try to find the last one on the Admission
                qb.AdmissionTeamMeeting = qb.Admission.AdmissionTeamMeeting.Where(a => a.Superceded == false)
                    .OrderByDescending(b => b.NextTeamMeetingDate).FirstOrDefault();

                // If No AdmissionTeamMeeting exists on the Admission, or we need to advance the TM Dates
                // create a new one, add it to the admission and set or advance the dates (same thing)
                if ((qb.AdmissionTeamMeeting == null) ||
                    ((qb.AdmissionTeamMeeting != null) &&
                     ((qb.AdmissionTeamMeeting.LastTeamMeetingDate != DateTime.Today) ||
                      (qb.AdmissionTeamMeeting.NextTeamMeetingDate != qb.ReCalculateNextTeamMeetingDate))))
                {
                    if (qb.AdmissionTeamMeeting != null)
                    {
                        qb.AdmissionTeamMeeting.Superceded = true;
                    }

                    qb.AdmissionTeamMeeting = new AdmissionTeamMeeting();
                    qb.Admission.AdmissionTeamMeeting.Add(qb.AdmissionTeamMeeting);
                    qb.LastTeamMeetingDateDisplay = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                }

                // Add the AdmissionTeamMeeting to the EncounterTeamMeeting
                qb.AdmissionTeamMeeting.EncounterTeamMeeting.Add(eTM);
            }

            return qb;
        }
    }

    public class AdmissionHospiceDischarge : HospiceQuestionBase
    {
        private Virtuoso.Server.Data.AdmissionHospiceDischarge _CurrentAdmissionHospiceDischarge;

        public Virtuoso.Server.Data.AdmissionHospiceDischarge CurrentAdmissionHospiceDischarge
        {
            get { return _CurrentAdmissionHospiceDischarge; }
            set
            {
                _CurrentAdmissionHospiceDischarge = value;
                RaisePropertyChanged("CurrentAdmissionHospiceDischarge");
            }
        }

        public int? DischargeReason
        {
            get
            {
                return (CurrentAdmissionHospiceDischarge == null)
                    ? null
                    : CurrentAdmissionHospiceDischarge.DischargeReason;
            }
            set
            {
                if (CurrentAdmissionHospiceDischarge == null)
                {
                    return;
                }

                if (value.HasValue)
                {
                    CurrentAdmissionHospiceDischarge.DischargeReason = value.Value;
                }
                else
                {
                    CurrentAdmissionHospiceDischarge.DischargeReason = null;
                }

                RaisePropertyChanged("DischargeReason");
                RaisePropertyChanged("ShowAdministrativeMessage");
                RaisePropertyChanged("AHDischargeTemplate");
            }
        }

        public bool ShowAdministrativeMessage
        {
            get
            {
                if ((CurrentAdmissionHospiceDischarge == null) ||
                    (CurrentAdmissionHospiceDischarge.DischargeReason == null))
                {
                    return false;
                }

                return CurrentAdmissionHospiceDischarge.DischargeReasonCodeIsAdministrative;
            }
        }

        public string AHDischargeTemplate
        {
            get
            {
                if (CurrentAdmissionHospiceDischarge == null)
                {
                    return "AHDischargeTemplateNone";
                }

                switch (CurrentAdmissionHospiceDischarge.DischargeReasonCode)
                {
                    case "Expired":
                        return "AHDischargeTemplateExpired";
                    case "Revoked":
                        return "AHDischargeTemplateRevoked";
                    case "NotTerminal":
                        return "AHDischargeTemplateNotTerminal";
                    case "Moved":
                        return "AHDischargeTemplateMoved";
                    case "Transferred":
                        return "AHDischargeTemplateTransferred";
                    case "ForCause":
                        return "AHDischargeTemplateForCause";
                    case "Administrative":
                        return "AHDischargeTemplateAdministrative";
                    default:
                        return "AHDischargeTemplateNone";
                }
            }
        }

        public string AHDischargeTemplatePrint => AHDischargeTemplate + "Print";

        public string SetupDataError
        {
            get
            {
                if ((Admission == null) || (Encounter == null))
                {
                    return "AdmissionHospiceDischarge Error: No Admission or Encounter defined.";
                }

                if (Admission.HospiceAdmission == false)
                {
                    return
                        "AdmissionHospiceDischarge Error: Only Hospice Admissions can perform Hospice Agency Discharge.";
                }

                AdmissionDiscipline ad = Admission.AdmissionDiscipline
                    .Where(a => a.DisciplineKey == Encounter.DisciplineKey).FirstOrDefault();
                if (ad == null)
                {
                    return "AdmissionHospiceDischarge Error: No AdmissionDiscipline defined.";
                }

                if (ad.AdmissionDisciplineIsSN == false)
                {
                    return
                        "AdmissionHospiceDischarge Error: ServiceType setup error: Only skilled nursing disciplines can perform Hospice Agency Discharge.";
                }

                if (isDischargeForm == false)
                {
                    return
                        "AdmissionHospiceDischarge Error: From setup error: The Hospice Agency Discharge question is only valid in a discharge form.";
                }

                return null;
            }
        }

        public bool ShowSetupDataError => (string.IsNullOrWhiteSpace(SetupDataError)) ? false : true;

        private bool isDischargeForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsDischarge == false))
                {
                    return false;
                }

                return true;
            }
        }

        public AdmissionHospiceDischarge(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        internal void SetupData()
        {
            if ((Admission != null) && (Encounter != null))
            {
                CurrentAdmissionHospiceDischarge = Admission.AdmissionHospiceDischarge
                    .Where(a => a.AddedFromEncounterKey == Encounter.EncounterKey).FirstOrDefault();
            }

            if (CurrentAdmissionHospiceDischarge == null)
            {
                AddNewAdmissionHospiceDischarge();
            }
        }

        private void AddNewAdmissionHospiceDischarge()
        {
            if (ShowSetupDataError)
            {
                MessageBox.Show(SetupDataError);
                return;
            }

            Virtuoso.Server.Data.AdmissionHospiceDischarge
                newAHD = new Virtuoso.Server.Data.AdmissionHospiceDischarge();
            newAHD.Version = 2;
            newAHD.PatientKey = Patient.PatientKey;
            newAHD.AdmissionKey = Admission.AdmissionKey;
            newAHD.AddedFromEncounterKey = Encounter.EncounterKey;
            newAHD.TenantID = Admission.TenantID;
            newAHD.UpdatedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            Encounter.AdmissionHospiceDischarge.Add(newAHD);
            CurrentAdmissionHospiceDischarge = newAHD;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (CurrentAdmissionHospiceDischarge == null)
            {
                return false;
            }

            CurrentAdmissionHospiceDischarge.TidyUpData();

            CurrentAdmissionHospiceDischarge.ValidationErrors.Clear();

            if (((Encounter.FullValidation) && (Hidden == false) && (Protected == false)) == false)
            {
                return true;
            }

            // Need a reason to do anything
            if (CurrentAdmissionHospiceDischarge.DischargeReason == null)
            {
                CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                    new ValidationResult("The Hospice Discharge Reason field is required.",
                        new[] { "DischargeReason" }));
                return false;
            }

            bool allValid = true;
            switch (CurrentAdmissionHospiceDischarge.DischargeReasonCode)
            {
                case "Expired":
                    if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Present at Death field is required.",
                                new[] { "HospicePresentAtDeath" }));
                        return false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == true)
                    {
                        if (CurrentAdmissionHospiceDischarge.DeathPronouncement == null)
                        {
                            CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                                new ValidationResult("The Death Pronouncement field is required.",
                                    new[] { "DeathPronouncement" }));
                            allValid = false;
                        }

                        if (CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath == null)
                        {
                            CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                                new ValidationResult("The Persons Present At Death field is required.",
                                    new[] { "PersonsPresentAtDeath" }));
                            allValid = false;
                        }
                    }

                    if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == false)
                    {
                        if (CurrentAdmissionHospiceDischarge.HowInformedOfDeath == null)
                        {
                            CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                                new ValidationResult("The How was Hospice Informed of the Death? field is required.",
                                    new[] { "HowInformedOfDeath" }));
                            allValid = false;
                        }
                    }

                    if (CurrentAdmissionHospiceDischarge.DeathDateTime == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Date/Time of Death field is required.",
                                new[] { "DeathDateTime" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DeathDateTime.HasValue &&
                        CurrentAdmissionHospiceDischarge.DeathDateTime.Value > DateTimeOffset.Now)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Date/Time of Death field cannot be a future date",
                                new[] { "DeathDateTime" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.LocationOfDeath == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Location of Death field is required.",
                                new[] { "LocationOfDeath" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.DischargeDate =
                            ((DateTimeOffset)CurrentAdmissionHospiceDischarge.DeathDateTime).Date;
                        if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == true)
                        {
                            CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        }

                        if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == false)
                        {
                            CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        }

                        if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == false)
                        {
                            CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        }

                        if (CurrentAdmissionHospiceDischarge.HospicePresentAtDeath == false)
                        {
                            CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        }

                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision = null;
                        CurrentAdmissionHospiceDischarge.HospiceDCPlan = null;
                        CurrentAdmissionHospiceDischarge.HospiceDCPlanComment = null;
                        CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                    }

                    break;
                case "Revoked":
                    if ((CurrentAdmissionHospiceDischarge.ShowRevokeReason) &&
                        (CurrentAdmissionHospiceDischarge.RevokeReason == null))
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Reason for Revocation field is required.",
                                new[] { "RevokeReason" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.RevocationEffectiveDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Effective Date of Signed Statement of Revocation field is required.",
                                new[] { "RevocationEffectiveDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision = null;
                        CurrentAdmissionHospiceDischarge.HospiceDCPlan = null;
                        CurrentAdmissionHospiceDischarge.HospiceDCPlanComment = null;
                        CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfStateDate = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                    }

                    break;
                case "NotTerminal":
                    if (CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Summary of IDT Decision field is required.",
                                new[] { "SummaryOfIDTDecision" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospiceDCPlan == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Planning field is required.",
                                new[] { "HospiceDCPlan" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Date of Physician's Order for Discharge field is required.",
                                new[] { "PhysicianOrderForDischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NOMNCDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Date NOMNC Presented/Signed by Patient/Responsible Party field is required.",
                                new[] { "NOMNCDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfStateDate = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                    }

                    break;
                case "Moved":
                    if (CurrentAdmissionHospiceDischarge.ExplanationOfRelocation == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Explanation Regarding Relocation Outside of Service Area field is required.",
                                new[] { "ExplanationOfRelocation" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospiceDCPlan == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Planning field is required.",
                                new[] { "HospiceDCPlan" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision = null;
                        CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfStateDate = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                    }

                    break;
                case "Transferred":
                    if ((CurrentAdmissionHospiceDischarge.ShowTransferReason) &&
                        (CurrentAdmissionHospiceDischarge.TransferReason == null))
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Reason for Transfer field is required.",
                                new[] { "TransferReason" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Date of Receipt of Patient Statement Requesting Transfer to Another Hospice field is required.",
                                new[] { "TransferRequestReceiptDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.ReceivingHospiceContact == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Receiving Hospice Name, Address, Phone Number and Contact field is required.",
                                new[] { "ReceivingHospiceContact" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospiceTransferType == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Type of Hospice Transfer field is required.",
                                new[] { "HospiceTransferType" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospiceDCPlan == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Planning field is required.",
                                new[] { "HospiceDCPlan" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision = null;
                        CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfStateDate = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                    }

                    break;
                case "ForCause":
                    if (CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Summary of IDT Decision field is required.",
                                new[] { "SummaryOfIDTDecision" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Date of Physician's Order for Discharge field is required.",
                                new[] { "PhysicianOrderForDischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.HospiceDCPlan == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Planning field is required.",
                                new[] { "HospiceDCPlan" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Notification of Medicare Administrative Contractor field is required.",
                                new[] { "NotificationOfMedicareDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfStateDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of State Survey Office field is required.",
                                new[] { "NotificationOfStateDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeForCauseDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult(
                                "The Date Patient Received Written Notification of Discharge for Cause field is required.",
                                new[] { "DischargeForCauseDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                    }

                    break;
                case "Administrative":
                    if (CurrentAdmissionHospiceDischarge.DischargeDate == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Hospice Discharge Date field is required.",
                                new[] { "DischargeDate" }));
                        allValid = false;
                    }

                    if (CurrentAdmissionHospiceDischarge.NotificationOfHospiceDischarge == null)
                    {
                        CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                            new ValidationResult("The Notification of Hospice Discharge field is required.",
                                new[] { "NotificationOfHospiceDischarge" }));
                        allValid = false;
                    }

                    if (allValid)
                    {
                        CurrentAdmissionHospiceDischarge.HowInformedOfDeath = null;
                        CurrentAdmissionHospiceDischarge.HospicePresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncement = null;
                        CurrentAdmissionHospiceDischarge.DeathPronouncementComment = null;
                        CurrentAdmissionHospiceDischarge.PersonsPresentAtDeath = null;
                        CurrentAdmissionHospiceDischarge.DeathDateTime = null;
                        CurrentAdmissionHospiceDischarge.LocationOfDeath = null;
                        CurrentAdmissionHospiceDischarge.RevocationEffectiveDate = null;
                        CurrentAdmissionHospiceDischarge.SummaryOfIDTDecision = null;
                        CurrentAdmissionHospiceDischarge.PhysicianOrderForDischargeDate = null;
                        CurrentAdmissionHospiceDischarge.NOMNCDate = null;
                        CurrentAdmissionHospiceDischarge.TransferRequestReceiptDate = null;
                        CurrentAdmissionHospiceDischarge.ReceivingHospiceContact = null;
                        CurrentAdmissionHospiceDischarge.HospiceTransferType = null;
                        CurrentAdmissionHospiceDischarge.DischargeForCauseDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfMedicareDate = null;
                        CurrentAdmissionHospiceDischarge.NotificationOfStateDate = null;
                        CurrentAdmissionHospiceDischarge.RevokeReason = null;
                        CurrentAdmissionHospiceDischarge.TransferReason = null;
                        CurrentAdmissionHospiceDischarge.ExplanationOfRelocation = null;
                        CurrentAdmissionHospiceDischarge.HospiceDCPlan = null;
                    }

                    break;
                default:
                    CurrentAdmissionHospiceDischarge.ValidationErrors.Add(
                        new ValidationResult("The Hospice Discharge Reason field is required.",
                            new[] { "DischargeReason" }));
                    allValid = false;
                    break;
            }

            return allValid;
        }
    }

    public class AdmissionHospiceDischargeFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            AdmissionHospiceDischarge ahd = new AdmissionHospiceDischarge(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };

            ahd.SetupData();
            return ahd;
        }
    }

    public class HospiceVisits : HospiceQuestionBase
    {
        public HospiceVisits(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

        private List<EncounterData> _A0270List;

        public List<EncounterData> A0270List
        {
            get { return _A0270List; }
            set
            {
                _A0270List = value;
                RaisePropertyChanged("A0270List");
            }
        }

        private string _DischargeBlirb;

        public string DischargeBlirb
        {
            get { return _DischargeBlirb; }
            set
            {
                _DischargeBlirb = value;
                RaisePropertyChanged("DischargeBlirb");
            }
        }

        private string A0270blirb(DateTime A0270, DateTime encounterDate)
        {
            if (A0270 == encounterDate)
            {
                return "A0270";
            }

            if (A0270.AddDays(-1) == encounterDate)
            {
                return "A0270-1";
            }

            if (A0270.AddDays(-2) == encounterDate)
            {
                return "A0270-2";
            }

            if (A0270.AddDays(-3) == encounterDate)
            {
                return "A0270-3";
            }

            if (A0270.AddDays(-4) == encounterDate)
            {
                return "A0270-4";
            }

            if (A0270.AddDays(-5) == encounterDate)
            {
                return "A0270-5";
            }

            if (A0270.AddDays(-6) == encounterDate)
            {
                return "A0270-6";
            }

            return null;
        }

        private int O5000sortOrder(Encounter e)
        {
            if (e.IsHCFACode("A") && (e.RNorLPN == "RN"))
            {
                return 1;
            }

            if (e.IsHCFACode("P"))
            {
                return 2;
            }

            if (e.IsHCFACode("E"))
            {
                return 3;
            }

            if (e.IsHCFACode("S"))
            {
                return 4;
            }

            if (e.IsHCFACode("A") && (e.RNorLPN == "LPN"))
            {
                return 5;
            }

            if (e.IsHCFACode("S"))
            {
                return 6;
            }

            return 7;
        }

        public override void OnProcessHospiceQuestion()
        {
            // Setup Grid
            if ((OasisManager != null) && (OasisManager.IsHISVersion2orHigher == false))
            {
                Hidden = true;
            }

            if ((CurrentAdmission == null) || (Encounter == null))
            {
                return;
            }

            DateTime A0270 = (CurrentAdmission.DischargeDateTime == null)
                ? DateTime.Today.Date
                : ((DateTime)CurrentAdmission.DischargeDateTime).Date;
            DischargeBlirb = string.Format("Discharged on {0}     Encounters within Last 7 Days of Life:  {1} - {2}",
                CurrentAdmission.DischargeDateTimeDisplay, A0270.AddDays(-6).ToShortDateString(),
                A0270.ToShortDateString());
            List<EncounterData> a0270List = Encounter.EncounterData.Where(x =>
                x.EncounterKey == Encounter.EncounterKey && x.QuestionKey == Question.QuestionKey).ToList();
            if (a0270List == null)
            {
                a0270List = new List<EncounterData>();
            }

            foreach (EncounterData e in a0270List) e.BoolData = false;
            List<Encounter> eList = CurrentAdmission.GetEvalAndVisitEncountersInclusive(A0270.AddDays(-6), A0270);
            if (eList != null)
            {
                foreach (Encounter e in eList)
                {
                    EncounterData ed = a0270List.Where(x => x.IntData == e.EncounterKey).FirstOrDefault();
                    if (ed == null)
                    {
                        ed = new EncounterData
                        {
                            SectionKey = FormSection.SectionKey.Value, QuestionGroupKey = QuestionGroupKey,
                            QuestionKey = Question.QuestionKey
                        };
                        Encounter.EncounterData.Add(ed);
                    }

                    ed.BoolData = true;
                    ed.IntData = e.EncounterKey;
                    ed.Int2Data = O5000sortOrder(e);
                    ed.TextData = e.DisciplineCode;
                    ed.DateTimeData = e.EncounterOrTaskStartDateAndTime.GetValueOrDefault().Date;
                    ed.Text2Data = A0270blirb(A0270, (DateTime)ed.DateTimeData);
                    ed.GuidData = e.EncounterBy;
                    ed.Text3Data = (e.EncounterIsEval && e.ServiceTypeDescriptionContains("eval"))
                        ? "Evaluation"
                        : ((e.EncounterIsEval) ? "Assessment" : ((e.EncounterIsVisit) ? "Visit" : null));
                    ed.Text4Data = e.RNorLPN;
                    ed.Text5Data = e.EncounterStatusDescription2;
                }
            }

            List<EncounterData> removeList = a0270List.Where(e => e.BoolData == false).ToList();
            if (removeList != null)
            {
                foreach (EncounterData e in removeList) Encounter.EncounterData.Remove(e);
            }

            A0270List = Encounter.EncounterData
                .Where(x => x.EncounterKey == Encounter.EncounterKey && x.QuestionKey == Question.QuestionKey)
                .OrderBy(x => x.Int2Data).OrderBy(x => x.Int2Data).ThenBy(x => x.Text2Data).ToList();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }

        public FormSection FormSection { get; set; }
    }

    public class HospiceVisitsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceVisits hv = new HospiceVisits(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                FormSection = formsection,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };
            hv.ProcessHospiceQuestion();
            return hv;
        }
    }

    public class HospiceElectionAddendumMedicationItem
    {
        private string _MedicationDescription;

        public string MedicationDescription
        {
            get { return _MedicationDescription; }
            set { _MedicationDescription = value; }
        }
    }

    public class HospiceElectionAddendum : HospiceQuestionBase
    {
        private EncounterHospiceElectionAddendum _CurrentEncounterHospiceElectionAddendum;

        public EncounterHospiceElectionAddendum CurrentEncounterHospiceElectionAddendum
        {
            get { return _CurrentEncounterHospiceElectionAddendum; }
            set { _CurrentEncounterHospiceElectionAddendum = value; }
        }

        public List<EncounterHospiceElectionAddendumDiagnosis> DiagnosisList
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumDiagnosis == null))
                {
                    return null;
                }

                return Encounter.EncounterHospiceElectionAddendumDiagnosis.OrderBy(p => p.Sequence).ToList();
            }
        }

        public List<EncounterHospiceElectionAddendumMedication> MedicationList
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumMedication == null))
                {
                    return null;
                }

                return Encounter.EncounterHospiceElectionAddendumMedication.OrderBy(p => p.Sequence).ToList();
            }
        }

        public List<EncounterHospiceElectionAddendumService> ServiceList
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumService == null))
                {
                    return null;
                }

                return Encounter.EncounterHospiceElectionAddendumService.OrderBy(p => p.Sequence).ToList();
            }
        }

        public HospiceElectionAddendum(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        internal void SetupData()
        {
            SetupCommands();

            CurrentEncounterHospiceElectionAddendum = null;
            if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendum == null))
            {
                return;
            }

            CurrentEncounterHospiceElectionAddendum = Encounter.EncounterHospiceElectionAddendum
                .Where(eh => eh.EncounterKey == Encounter.EncounterKey).FirstOrDefault();
            if (CurrentEncounterHospiceElectionAddendum != null)
            {
                CurrentEncounterHospiceElectionAddendum.PropertyChanged +=
                    CurrentEncounterHospiceElectionAddendum_PropertyChanged;
                SetupPatientMedications();
                return;
            }

            EncounterHospiceElectionAddendum encounterHospiceElectionAddendum = new EncounterHospiceElectionAddendum();

            encounterHospiceElectionAddendum.Version =
                TenantSettingsCache.Current.TenantSettingHospiceElectionAddendumVersion;
            encounterHospiceElectionAddendum.CreateDate =
                DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            encounterHospiceElectionAddendum.FieldAgencyName = SetFieldAgencyName;
            encounterHospiceElectionAddendum.FieldAgencyAddress1 = SetFieldAgencyAddress1;
            encounterHospiceElectionAddendum.FieldAgencyAddress2 = SetFieldAgencyAddress2;
            encounterHospiceElectionAddendum.FieldAgencyCityStateZip = SetFieldAgencyCityStateZip;
            encounterHospiceElectionAddendum.FieldAgencyPhone = SetFieldAgencyPhone;
            encounterHospiceElectionAddendum.FieldPatientFullName = SetFieldPatientFullName;
            encounterHospiceElectionAddendum.FieldPatientMRNAdmissionID = SetFieldPatientMRNAdmissionID;

            Encounter.EncounterHospiceElectionAddendum.Add(encounterHospiceElectionAddendum);

            CurrentEncounterHospiceElectionAddendum = encounterHospiceElectionAddendum;
            CurrentEncounterHospiceElectionAddendum.PropertyChanged +=
                CurrentEncounterHospiceElectionAddendum_PropertyChanged;
            SetupPatientMedications();

            SetupDiagnosis();
        }

        public RelayCommand AddMedication_Command { get; protected set; }
        public RelayCommand<EncounterHospiceElectionAddendumMedication> DeleteMedication_Command { get; protected set; }
        public RelayCommand AddService_Command { get; protected set; }
        public RelayCommand<EncounterHospiceElectionAddendumService> DeleteService_Command { get; protected set; }

        private void SetupCommands()
        {
            AddMedication_Command = new RelayCommand(AddMedicationCommand);
            DeleteMedication_Command =
                new RelayCommand<EncounterHospiceElectionAddendumMedication>(em => { DeleteMedicationCommand(em); });
            AddService_Command = new RelayCommand(AddServiceCommand);
            DeleteService_Command =
                new RelayCommand<EncounterHospiceElectionAddendumService>(es => { DeleteServiceCommand(es); });
        }

        private void AddMedicationCommand()
        {
            if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumMedication == null))
            {
                return;
            }

            EncounterHospiceElectionAddendumMedication lastEM = Encounter.EncounterHospiceElectionAddendumMedication
                .OrderByDescending(p => p.Sequence).FirstOrDefault();
            int sequence = (lastEM == null) ? 0 : lastEM.Sequence;
            sequence++;
            EncounterHospiceElectionAddendumMedication em = new EncounterHospiceElectionAddendumMedication();
            em.EncounterKey = Encounter.EncounterKey;
            em.Sequence = sequence;
            Encounter.EncounterHospiceElectionAddendumMedication.Add(em);
            RaisePropertyChanged("MedicationList");
        }

        private void DeleteMedicationCommand(EncounterHospiceElectionAddendumMedication em)
        {
            if (em == null)
            {
                return;
            }

            if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumMedication == null))
            {
                return;
            }

            if ((DynamicFormViewModel == null) || (DynamicFormViewModel.FormModel == null))
            {
                return;
            }

            Encounter.EncounterHospiceElectionAddendumMedication.Remove(em);
            DynamicFormViewModel.FormModel.RemoveEncounterHospiceElectionAddendumMedication(em);
            RaisePropertyChanged("MedicationList");
        }

        private void AddServiceCommand()
        {
            if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumService == null))
            {
                return;
            }

            EncounterHospiceElectionAddendumService lastES = Encounter.EncounterHospiceElectionAddendumService
                .OrderByDescending(p => p.Sequence).FirstOrDefault();
            int sequence = (lastES == null) ? 0 : lastES.Sequence;
            sequence++;
            EncounterHospiceElectionAddendumService es = new EncounterHospiceElectionAddendumService();
            es.EncounterKey = Encounter.EncounterKey;
            es.Sequence = sequence;
            Encounter.EncounterHospiceElectionAddendumService.Add(es);
            RaisePropertyChanged("ServiceList");
        }

        private void DeleteServiceCommand(EncounterHospiceElectionAddendumService es)
        {
            if (es == null)
            {
                return;
            }

            if ((Encounter == null) || (Encounter.EncounterHospiceElectionAddendumService == null))
            {
                return;
            }

            if ((DynamicFormViewModel == null) || (DynamicFormViewModel.FormModel == null))
            {
                return;
            }

            Encounter.EncounterHospiceElectionAddendumService.Remove(es);
            DynamicFormViewModel.FormModel.RemoveEncounterHospiceElectionAddendumService(es);
            RaisePropertyChanged("ServiceList");
        }

        public void CurrentEncounterHospiceElectionAddendum_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("RequestSOC"))
            {
                if (CurrentEncounterHospiceElectionAddendum != null)
                {
                    if (CurrentEncounterHospiceElectionAddendum.RequestSOC == false)
                    {
                        CurrentEncounterHospiceElectionAddendum.SOCDate = null;
                    }

                    if (CurrentEncounterHospiceElectionAddendum.RequestSOC)
                    {
                        CurrentEncounterHospiceElectionAddendum.RequestDate = null;
                        if (Admission != null)
                        {
                            CurrentEncounterHospiceElectionAddendum.SOCDate = Admission.SOCDate;
                        }
                    }
                }
            }
        }

        private ServiceLineGrouping _ServiceLineGroupingZero;

        private ServiceLineGrouping ServiceLineGroupingZero
        {
            get
            {
                if (_ServiceLineGroupingZero != null)
                {
                    return _ServiceLineGroupingZero;
                }

                if ((Admission == null) || (Encounter == null))
                {
                    return null;
                }

                DateTime admissionGroupDate = (Encounter.EncounterOrTaskStartDateAndTime == null)
                    ? DateTime.Today.Date
                    : Encounter.EncounterOrTaskStartDateAndTime.Value.Date;
                AdmissionGroup ag = Admission.GetNthCurrentGroup(0, admissionGroupDate);
                if (ag == null)
                {
                    return null;
                }

                _ServiceLineGroupingZero = ServiceLineCache.GetServiceLineGroupingFromKey(ag.ServiceLineGroupingKey);
                return _ServiceLineGroupingZero;
            }
        }

        private string SetFieldAgencyName
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyName) == false))
                {
                    return ServiceLineGroupingZero.AgencyName;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Name))
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Name;
            }
        }

        private string SetFieldAgencyAddress1
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return ServiceLineGroupingZero.AgencyAddress1;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address1))
                    ? "?"
                    : TenantSettingsCache.Current.TenantSetting.Address1;
            }
        }

        private string SetFieldAgencyAddress2
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress2))
                        ? null
                        : ServiceLineGroupingZero.AgencyAddress2;
                }

                return (string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.Address2))
                    ? null
                    : TenantSettingsCache.Current.TenantSetting.Address2;
            }
        }

        private string SetFieldAgencyCityStateZip
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyAddress1) == false))
                {
                    return string.Format("{0}, {1}  {2}",
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyCity))
                            ? "City ?"
                            : ServiceLineGroupingZero.AgencyCity),
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyStateCodeCode))
                            ? "State ?"
                            : ServiceLineGroupingZero.AgencyStateCodeCode),
                        ((string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyZipCode))
                            ? "ZipCode ?"
                            : ServiceLineGroupingZero.AgencyZipCode));
                }

                return string.Format("{0}, {1}  {2}",
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.City))
                        ? "City ?"
                        : TenantSettingsCache.Current.TenantSetting.City),
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.StateCodeCode))
                        ? "State ?"
                        : TenantSettingsCache.Current.TenantSetting.StateCodeCode),
                    ((string.IsNullOrWhiteSpace(TenantSettingsCache.Current.TenantSetting.ZipCode))
                        ? "ZipCode ?"
                        : TenantSettingsCache.Current.TenantSetting.ZipCode));
            }
        }

        private string SetFieldAgencyPhone
        {
            get
            {
                if ((ServiceLineGroupingZero != null) &&
                    (string.IsNullOrWhiteSpace(ServiceLineGroupingZero.AgencyPhoneNumber) == false))
                {
                    return FormatPhoneNumber(ServiceLineGroupingZero.AgencyPhoneNumber);
                }

                return null;
            }
        }

        private string SetFieldPatientFullName
        {
            get
            {
                if (Patient != null)
                {
                    return (string.IsNullOrWhiteSpace(Patient.FullNameWithMiddleInitial)
                        ? "?"
                        : Patient.FullNameWithMiddleInitial);
                }

                return "?";
            }
        }

        private string SetFieldPatientMRNAdmissionID
        {
            get
            {
                if ((Patient != null) && (Admission != null))
                {
                    return string.Format("{0}{1}",
                        ((string.IsNullOrWhiteSpace(Patient.MRN)) ? "?" : Patient.MRN),
                        ((string.IsNullOrWhiteSpace(Admission.AdmissionID)) ? "" : "-" + Admission.AdmissionID));
                }

                return "?";
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return null;
            }

            PhoneConverter pc = new PhoneConverter();
            if (pc == null)
            {
                return null;
            }

            object phoneObject = pc.Convert(phoneNumber, null, null, null);
            if (phoneObject != null)
            {
                if (string.IsNullOrWhiteSpace(phoneObject.ToString()) == false)
                {
                    return phoneObject.ToString();
                }
            }

            return null;
        }

        private void SetupDiagnosis()
        {
            if ((Encounter == null) || (Admission == null) || (Admission.AdmissionDiagnosis == null))
            {
                return;
            }

            CollectionViewSource adViewSource = new CollectionViewSource();
            adViewSource.Source = Admission.AdmissionDiagnosis;
            adViewSource.View.SortDescriptions.Add(new SortDescription("DiagnosisStatus", ListSortDirection.Ascending));
            adViewSource.View.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
            adViewSource.View.Filter = FilterItems;
            adViewSource.View.Refresh();
            if (adViewSource.View == null)
            {
                return;
            }

            int i = 1;
            foreach (AdmissionDiagnosis ad in adViewSource.View)
            {
                EncounterHospiceElectionAddendumDiagnosis ed = new EncounterHospiceElectionAddendumDiagnosis();
                ed.Sequence = i++;
                ed.Version = ad.Version;
                ed.Code = ad.Code;
                ed.Description = ad.Description;
                Encounter.EncounterHospiceElectionAddendumDiagnosis.Add(ed);
            }
        }

        private List<HospiceElectionAddendumMedicationItem> _PatientMedicationList;

        public List<HospiceElectionAddendumMedicationItem> PatientMedicationList
        {
            get { return _PatientMedicationList; }
            set { _PatientMedicationList = value; }
        }

        private void SetupPatientMedications()
        {
            if ((Encounter == null) || (Patient == null) || (Patient.PatientMedication == null))
            {
                return;
            }

            CollectionViewSource pmViewSource = new CollectionViewSource();
            pmViewSource.Source = Patient.PatientMedication;
            pmViewSource.View.SortDescriptions.Add(new SortDescription("MedicationStatus",
                ListSortDirection.Ascending));
            pmViewSource.View.SortDescriptions.Add(new SortDescription("MedicationName", ListSortDirection.Ascending));
            pmViewSource.View.Filter = FilterItems;
            pmViewSource.View.Refresh();
            List<HospiceElectionAddendumMedicationItem> medList = new List<HospiceElectionAddendumMedicationItem>();
            HospiceElectionAddendumMedicationItem b = new HospiceElectionAddendumMedicationItem
                { MedicationDescription = " " };
            medList.Add(b);
            foreach (PatientMedication pm in pmViewSource.View)
            {
                HospiceElectionAddendumMedicationItem i = new HospiceElectionAddendumMedicationItem
                    { MedicationDescription = pm.DescriptionPlusHospiceIndicator };
                medList.Add(i);
            }

            PatientMedicationList = medList;
            RaisePropertyChanged("PatientMedicationList");
        }

        private bool FilterItems(object item)
        {
            var properties = item.GetType().GetProperties();

            VirtuosoEntity v = item as VirtuosoEntity;
            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            if ((Encounter != null) && (!v.IsNew))
            {
                AdmissionDiagnosis ad = item as AdmissionDiagnosis;
                if (ad != null)
                {
                    if (ad.RemovedDate != null)
                    {
                        return false;
                    }

                    if (ad.Diagnosis == false)
                    {
                        return false;
                    }

                    EncounterDiagnosis ed = Encounter.EncounterDiagnosis
                        .Where(p => p.AdmissionDiagnosis.AdmissionDiagnosisKey == ad.AdmissionDiagnosisKey)
                        .FirstOrDefault();
                    if (ed == null)
                    {
                        return false;
                    }
                }

                PatientMedication pm = item as PatientMedication;
                if (pm != null)
                {
                    EncounterMedication em = Encounter.EncounterMedication
                        .Where(p => p.PatientMedication.PatientMedicationKey == pm.PatientMedicationKey)
                        .FirstOrDefault();
                    if (em == null)
                    {
                        return false;
                    }
                }
            }

            var prop = properties.Where(p => p.Name.Equals("EffectiveFrom", StringComparison.OrdinalIgnoreCase) ||
                                             p.Name.EndsWith("StartDate", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (prop != null)
            {
                var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                if (value != null && ((DateTime)value).Date >
                    ((DateTime)CurrentEncounterHospiceElectionAddendum.CreateDate).Date)
                {
                    return false;
                }
            }

            prop = properties.Where(p => p.Name.Equals("EffectiveThru", StringComparison.OrdinalIgnoreCase) ||
                                         p.Name.EndsWith("EndDate", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (prop != null)
            {
                var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                if ((value != null) && (((DateTime)value).Date <
                                        ((DateTime)CurrentEncounterHospiceElectionAddendum.CreateDate).Date))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            if (CurrentEncounterHospiceElectionAddendum == null)
            {
                return true;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestSOC == false)
            {
                CurrentEncounterHospiceElectionAddendum.SOCDate = null;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestOther == false)
            {
                CurrentEncounterHospiceElectionAddendum.RequestDate = null;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestDate == DateTime.MinValue)
            {
                CurrentEncounterHospiceElectionAddendum.RequestDate = null;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestDate != null)
            {
                CurrentEncounterHospiceElectionAddendum.RequestDate =
                    ((DateTime)CurrentEncounterHospiceElectionAddendum.RequestDate).Date;
            }

            CurrentEncounterHospiceElectionAddendum.ValidationErrors.Clear();

            if (((Encounter.FullValidation) && (Hidden == false) && (Protected == false)) == false)
            {
                return true;
            }

            bool allValid = true;

            if (string.IsNullOrEmpty(CurrentEncounterHospiceElectionAddendum.RequestedBy))
            {
                CurrentEncounterHospiceElectionAddendum.RequestedBy = null;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestedBy == null)
            {
                CurrentEncounterHospiceElectionAddendum.ValidationErrors.Add(
                    new ValidationResult("The Requested By is required.", new[] { "RequestedBy" }));
                allValid = false;
            }

            if ((CurrentEncounterHospiceElectionAddendum.RequestSOC == false) &&
                (CurrentEncounterHospiceElectionAddendum.RequestOther == false))
            {
                CurrentEncounterHospiceElectionAddendum.ValidationErrors.Add(
                    new ValidationResult("A Date of Request Type is required.",
                        new[] { "RequestSOC", "RequestOther" }));
                allValid = false;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestOther &&
                (CurrentEncounterHospiceElectionAddendum.RequestDate == null))
            {
                CurrentEncounterHospiceElectionAddendum.ValidationErrors.Add(
                    new ValidationResult("A Request Date is required.", new[] { "RequestDate" }));
                allValid = false;
            }

            if (CurrentEncounterHospiceElectionAddendum.RequestOther &&
                (CurrentEncounterHospiceElectionAddendum.RequestDate != null) &&
                (CurrentEncounterHospiceElectionAddendum.RequestDate >
                 DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date))
            {
                CurrentEncounterHospiceElectionAddendum.ValidationErrors.Add(
                    new ValidationResult("Referral Date cannot be a future date", new[] { "RequestDate" }));
                allValid = false;
            }

            if ((Admission != null) &&
                (Admission.SOCDate != null) &&
                CurrentEncounterHospiceElectionAddendum.RequestOther &&
                (CurrentEncounterHospiceElectionAddendum.RequestDate != null) &&
                (CurrentEncounterHospiceElectionAddendum.RequestDate < ((DateTime)Admission.SOCDate).Date))
            {
                CurrentEncounterHospiceElectionAddendum.ValidationErrors.Add(
                    new ValidationResult("Referral Date cannot be before the Start of Service date",
                        new[] { "RequestDate" }));
                allValid = false;
            }

            foreach (EncounterHospiceElectionAddendumDiagnosis ed in DiagnosisList)
            {
                ed.ValidationErrors.Clear();
                if ((ed.Related == false) && (ed.Unrelated == false))
                {
                    ed.ValidationErrors.Add(new ValidationResult(
                        "Related or Unrelated must be selected for each diagnosis", new[] { "Related", "Unrelated" }));
                    allValid = false;
                }
            }

            foreach (EncounterHospiceElectionAddendumMedication em in MedicationList)
            {
                em.ValidationErrors.Clear();
                if (string.IsNullOrWhiteSpace(em.Description))
                {
                    em.Description = null;
                }

                if (string.IsNullOrWhiteSpace(em.ReasonNotCovered))
                {
                    em.ReasonNotCovered = null;
                }

                if ((em.Description == null))
                {
                    em.ValidationErrors.Add(new ValidationResult("A Medication is required.", new[] { "Description" }));
                    allValid = false;
                }

                if ((em.ReasonNotCovered == null))
                {
                    em.ValidationErrors.Add(new ValidationResult("A Reason for Non-Coverage is required.",
                        new[] { "ReasonNotCovered" }));
                    allValid = false;
                }
            }

            foreach (EncounterHospiceElectionAddendumService es in ServiceList)
            {
                es.ValidationErrors.Clear();
                if (string.IsNullOrWhiteSpace(es.Description))
                {
                    es.Description = null;
                }

                if (string.IsNullOrWhiteSpace(es.ReasonNotCovered))
                {
                    es.ReasonNotCovered = null;
                }

                if ((es.Description == null))
                {
                    es.ValidationErrors.Add(
                        new ValidationResult("A Service/Item is required.", new[] { "Description" }));
                    allValid = false;
                }

                if ((es.ReasonNotCovered == null))
                {
                    es.ValidationErrors.Add(new ValidationResult("A Reason for Non-Coverage is required.",
                        new[] { "ReasonNotCovered" }));
                    allValid = false;
                }
            }

            return allValid;
        }

        public override void Cleanup()
        {
            if (CurrentEncounterHospiceElectionAddendum != null)
            {
                CurrentEncounterHospiceElectionAddendum.PropertyChanged -=
                    CurrentEncounterHospiceElectionAddendum_PropertyChanged;
            }

            base.Cleanup();
        }
    }

    public class HospiceElectionAddendumFactory
    {
        public static HospiceQuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm,
            FormSection formsection, int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            HospiceElectionAddendum hea = new HospiceElectionAddendum(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Label = q.Label,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = null,
                OasisManager = vm.CurrentOasisManager
            };

            hea.SetupData();
            return hea;
        }
    }
}