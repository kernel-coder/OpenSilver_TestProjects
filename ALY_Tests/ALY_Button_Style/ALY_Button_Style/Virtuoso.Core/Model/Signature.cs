#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SignatureConsent : QuestionBase
    {
        public SignatureConsent(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void SignatureConsentSetup()
        {
            if (Admission == null)
            {
                return;
            }

            if (EncounterData.IsNew)
            {
                EncounterData.TextData = ServiceLineCache.GetConsentTextFromServiceLineKey(Admission.ServiceLineKey);
            }

            ProtectedOverrideRunTime = SignatureConsentSetupProtectedOverrideRunTime();
        }

        public bool? SignatureConsentSetupProtectedOverrideRunTime()
        {
            if (EncounterData == null)
            {
                return null;
            }

            if (EncounterData.BoolData == true)
            {
                return true;
            }

            return null;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            EncounterData.ValidationErrors.Clear();
            if (EncounterData.IsNew)
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Encounter.FullValidation && (Hidden == false) && (Required || ConditionalRequired) &&
                (Protected == false))
            {
                if ((EncounterData.SignatureData == null) || (EncounterData.DateTimeData.HasValue == false))
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult(
                        "Both a consent signature and date are required.", new[] { "SignatureData", "DateTimeData" }));
                    return false;
                }
            }

            if (Encounter.FullValidation && (Hidden == false) && EncounterData.DateTimeData.HasValue &&
                (Protected == false))
            {
                DateTime date = ((DateTime)EncounterData.DateTimeData).Date;
                if (date > DateTime.Today.Date)
                {
                    EncounterData.ValidationErrors.Add(
                        new ValidationResult("Consent Signature Date cannot be a future date",
                            new[] { "DateTimeData" }));
                    return false;
                }
            }

            return true;
        }

        public override void BackupEntity(bool restore)
        {
            base.BackupEntity(restore);
            RaisePropertyChanged("EncounterData");
            EncounterData.RaiseEvents();
        }
    }

    public class SignatureConsentFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SignatureConsent sc = new SignatureConsent(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            if (sc.EncounterData.IsNew && copyforward)
            {
                bool copiedForward = sc.CopyForwardLastInstance();
                sc.EncounterData.BoolData = copiedForward ? true : (bool?)null;
            }

            sc.SignatureConsentSetup();
            return sc;
        }
    }

    public class SignatureMedicalDirector : QuestionBase
    {
        public SignatureMedicalDirector(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void SignatureMedicalDirectorSetup(int? __FormSectionQuestionKey)
        {
            Messenger.Default.Register<int>(this, "AdmissionPhysician_FormUpdate",
                AdmissionKey => { RefreshPhysicians(); });
            SetupRequired(
                __FormSectionQuestionKey); // Need to set Required up now for RefreshPhysicians() processing - can't wait till we return to dynamic form 
            RefreshPhysicians();
        }

        int? prevSigningPhysicianKey;

        public int? SigningPhysicianKey
        {
            get { return EncounterData?.IntData; }
            set
            {
                if (EncounterData == null)
                {
                    return;
                }

                EncounterData.IntData = value;
                if (EncounterData.IntData == -1)
                {
                    EncounterData.SignatureData = null;
                    EncounterData.DateTimeData = null;
                    Deployment.Current.Dispatcher.BeginInvoke(() => ForceSignatureClear++);
                }

                EncounterData.TextData = (value == -1)
                    ? "Not Applicable"
                    : PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(value);
                if (EncounterData.IntData != null)
                {
                    PhysicianCache.Current.SetTeamMeetingPhysicianKey(EncounterData.IntData);
                }

                this.RaisePropertyChangedLambda(p => p.IsSignatureEnabled);
            }
        }

        int forceSignatureClear;

        public int ForceSignatureClear
        {
            get { return forceSignatureClear; }
            set
            {
                forceSignatureClear = value;
                this.RaisePropertyChangedLambda(p => p.ForceSignatureClear);
            }
        }

        public bool IsSignatureEnabled
        {
            get
            {
                if (Protected)
                {
                    return false;
                }

                if (EncounterData == null)
                {
                    return true;
                }

                return (EncounterData.IntData != -1);
            }
        }

        private List<AdmissionPhysician> internalPhysicianList;
        private CollectionViewSource signingPhysicianList = new CollectionViewSource();
        public ICollectionView SigningPhysicianList => signingPhysicianList.View;

        private void SetupRequired(int? formSectionQuestionKey)
        {
            FormSectionQuestion fsq = DynamicFormCache.GetFormSectionQuestionByKey(formSectionQuestionKey);
            if (fsq != null)
            {
                Required = fsq.Required;
            }
        }

        public void RefreshPhysicians()
        {
            prevSigningPhysicianKey = SigningPhysicianKey;
            SigningPhysicianKey = null;
            SetupSigningPhysicianList();
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianList);
            SigningPhysicianKey = prevSigningPhysicianKey;
            prevSigningPhysicianKey = null;
            // Default Signing Physician if need be
            if (SigningPhysicianKey != null)
            {
                return;
            }

            if (internalPhysicianList == null)
            {
                return;
            }

            if ((internalPhysicianList.Count == 1) && (Required == false))
            {
                SigningPhysicianKey = internalPhysicianList[0].PhysicianKey;
            }
            else
            {
                AdmissionPhysician ap = internalPhysicianList.FirstOrDefault(p => p.PhysicianKey == PhysicianCache.Current.GetTeamMeetingPhysicianKey());
                if (ap?.PhysicianKey != null)
                {
                    SigningPhysicianKey = ap.PhysicianKey;
                }
            }
        }

        private void SetupSigningPhysicianList()
        {
            // Pull Med Director Physicians from the Admission
            internalPhysicianList = new List<AdmissionPhysician>();
            // internalPhysicianList.Add(new AdmissionPhysician() { AdmissionPhysicianKey = 0 }); // PBI 8833 - No null item - is required now
            if (Admission?.AdmissionPhysician != null)
            {
                foreach (AdmissionPhysician ap in Admission.AdmissionPhysician.ToList())
                {
                    if (internalPhysicianList.Any(p => p.PhysicianKey == ap.PhysicianKey))
                    {
                        continue;
                    }

                    if (ValidSigningPhysician(ap))
                    {
                        internalPhysicianList.Add(ap);
                    }
                }
            }

            internalPhysicianList = internalPhysicianList.OrderBy(p => p.MedicalDirectorPhysicianName).ToList();
            // If signature is required - add 'Not Applicable' option 
            if (Required)
            {
                internalPhysicianList.Add(new AdmissionPhysician { PhysicianKey = -1 });
            }

            signingPhysicianList.Source = internalPhysicianList;
            SigningPhysicianList.Refresh();
        }

        private bool ValidSigningPhysician(AdmissionPhysician ap)
        {
            if (ap == null)
            {
                return false;
            }

            if ((prevSigningPhysicianKey != null) && (prevSigningPhysicianKey == ap.PhysicianKey))
            {
                return true;
            }

            if (ap.Inactive)
            {
                return false;
            }

            if (ap.IsMedDirect == false)
            {
                return false;
            }

            DateTime teamMeetingDate = (Encounter?.CreatedDate != null)
                ? ((DateTime)Encounter.CreatedDate).Date
                : DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
            if (ap.PhysicianEffectiveFromDate.Date <= teamMeetingDate == false)
            {
                return false;
            }

            if (((ap.PhysicianEffectiveThruDate == null) ||
                 (ap.PhysicianEffectiveThruDate.Value.Date >= teamMeetingDate)) == false)
            {
                return false;
            }

            return true;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            EncounterData.ValidationErrors.Clear();
            bool allValid = true;
            if (EncounterData.IsNew)
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Required && ((EncounterData.IntData == 0) || (EncounterData.IntData < -1)))
            {
                EncounterData.IntData = null; // 'Not Applicable' (-1) is valid if required
            }

            if ((Required == false) && (EncounterData.IntData <= 0))
            {
                EncounterData.IntData = null;
            }

            // If no physician in drop down or 'Not Applicable' - clear signature as well (new 'Required' rules)
            if ((EncounterData.IntData == null) || (EncounterData.IntData == -1))
            {
                EncounterData.SignatureData = null;
            }

            // return if (full) validation is currently 'off' based on context
            if ((Encounter.FullValidation && (Hidden == false) && (Protected == false)) == false)
            {
                return allValid;
            }

            if (Required && (EncounterData.IntData == null))
            {
                // as of R63 - 'required' ONLY IMPLIES that the physician drop down is populated 
                // and the actual MedDirector electronic signature can be postponed until TM Roster
                EncounterData.ValidationErrors.Add(new ValidationResult(
                    (string.Format("{0} is required", Label.Replace("Signature", ""))), new[] { "IntData" }));
                allValid = false;
            }

            // Post validation - for performance (see spCV_GetTeamMeetingRosterWorkList) - denormolize the med director signature stuff into the EncounterTeamMeeting
            // Also set EncounterData.Bool2Data as a purely persistent forensic data column
            EncounterTeamMeeting etm = Encounter.EncounterTeamMeeting.FirstOrDefault();
            if (etm != null)
            {
                etm.MedDirectorPhysicianKey = EncounterData.IntData;
                etm.MedDirectorSignatureRequired =
                    ((EncounterData.IntData != null) && (EncounterData.IntData != -1) &&
                     (EncounterData.SignatureData == null))
                        ? true
                        : false;
                EncounterData.Bool2Data = etm.MedDirectorSignatureRequired;
            }
            else
            {
                EncounterData.Bool2Data = false;
            }

            return allValid;
        }
    }

    public class SignatureMedicalDirectorFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            int? __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SignatureMedicalDirector smd = new SignatureMedicalDirector(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                EncounterData = ed,
            };
            smd.SignatureMedicalDirectorSetup(__FormSectionQuestionKey);
            return smd;
        }
    }

    public class SignatureDataWithDate : QuestionBase
    {
        public SignatureDataWithDate(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void SignatureDataWithDateSetup()
        {
            if (EncounterData != null)
            {
                EncounterData.TextData = SignatureLabel;
            }
        }

        private new string SignatureLabel
        {
            get
            {
                string label = (Encounter == null)
                    ? null
                    : UserCache.Current.GetFormalNameFromUserId(Encounter.EncounterBy);
                if (Question?.Label == null)
                {
                    return label;
                }

                if (((Question.Label.ToLower() == "patient") || (Question.Label.ToLower() == "patient signature")))
                {
                    label = Patient?.FullName;
                }

                return label;
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            EncounterData.ValidationErrors.Clear();
            bool allValid = true;
            if (EncounterData.IsNew)
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Encounter.FullValidation && (Hidden == false) && (Protected == false))
            {
                if (EncounterData.SignatureData == null)
                {
                    EncounterData.ValidationErrors.Add(
                        new ValidationResult((string.Format("{0} signature is required", Label)),
                            new[] { "SignatureData" }));
                    allValid = false;
                }
            }

            return allValid;
        }
    }

    public class SignatureDataWithDateFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            int? __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SignatureDataWithDate sdwd = new SignatureDataWithDate(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                EncounterData = ed,
            };
            sdwd.SignatureDataWithDateSetup();
            return sdwd;
        }
    }

    public class PatientOrDesigneeSignature : QuestionBase
    {
        public PatientOrDesigneeSignature(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public string PatientName => Patient?.FullNameFormal;
        public ICollectionView FilteredContactItemsSource { get; set; }

        public void ProcessContactFilteredItems()
        {
            if (Patient?.PatientContact != null)
            {
                FilteredContactItemsSource = new PagedCollectionView(Patient?.PatientContact);
                FilteredContactItemsSource.SortDescriptions.Add(new SortDescription("LastName", ListSortDirection.Ascending));
                FilteredContactItemsSource.SortDescriptions.Add(new SortDescription("FirstName", ListSortDirection.Ascending));
                FilteredContactItemsSource.Filter = ContactFilter;
                RaisePropertyChanged("FilteredContactItemsSource");
            }
        }

        public bool ContactFilter(object item)
        {
            PatientContact pc = item as PatientContact;
            if (pc == null)
            {
                return false;
            }

            return (!pc.Inactive) && pc.HistoryKey == null;
        }

        public void PatientOrDesigneeSignatureSetup()
        {
            if (string.IsNullOrWhiteSpace(EncounterData.Text2Data))
            {
                EncounterData.Text2Data = PatientName;
            }

            ProcessContactFilteredItems();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (string.IsNullOrWhiteSpace(EncounterData.Text2Data))
            {
                EncounterData.Text2Data = null;
            }

            if (string.IsNullOrWhiteSpace(EncounterData.Text3Data))
            {
                EncounterData.Text3Data = null;
            }

            EncounterData.TextData = EncounterData.Text3Data + " " + EncounterData.Text2Data;
            if (string.IsNullOrWhiteSpace(EncounterData.TextData))
            {
                EncounterData.TextData = null;
            }

            EncounterData.ValidationErrors.Clear();
            if (EncounterData.IsNew)
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            if (Encounter.FullValidation && (Hidden == false) && (Required || ConditionalRequired) &&
                (Protected == false))
            {
                if (EncounterData.SignatureData == null)
                {
                    EncounterData.ValidationErrors.Add(new ValidationResult(
                        "A Patient or Patient Designee Signature is required.", new[] { "SignatureData" }));
                    return false;
                }
            }

            if ((Encounter.FullValidation && (Hidden == false) && (Required || ConditionalRequired) &&
                 (Protected == false)) ||
                (Encounter.FullValidation && (Hidden == false) && (EncounterData.SignatureData != null) &&
                 (Protected == false)))
            {
                if (EncounterData.Text2Data == null)
                {
                    EncounterData.ValidationErrors.Add(
                        new ValidationResult("A Patient or Patient Designee Name is required.", new[] { "Text2Data" }));
                    return false;
                }
            }

            return true;
        }

        public override void BackupEntity(bool restore)
        {
            base.BackupEntity(restore);
            RaisePropertyChanged("EncounterData");
            EncounterData.RaiseEvents();
        }
    }

    public class PatientOrDesigneeSignatureFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.Section.SectionKey, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PatientOrDesigneeSignature pds = new PatientOrDesigneeSignature(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterData = ed,
                OasisManager = vm.CurrentOasisManager,
                DynamicFormViewModel = vm
            };
            pds.PatientOrDesigneeSignatureSetup();
            return pds;
        }
    }
}