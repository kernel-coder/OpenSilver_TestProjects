#region Usings

using System;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PatientDemographics : QuestionUI
    {
        public PatientDemographics(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private EncounterData _FuneralData;

        public EncounterData FuneralData
        {
            get
            {
                if (_FuneralData == null)
                {
                    _FuneralData = InitializeFuneralData();
                }

                return _FuneralData;
            }
        }

        private EncounterData InitializeFuneralData()
        {
            var ques = DynamicFormCache.GetQuestionByDataTemplate("FuneralArrangements");
            if (ques == null || ques.Any() == false)
            {
                return null;
            }

            var myQuestion = ques.First();
            var myEncs = Admission.Encounter
                .Where(e => !e.Inactive && e.EncounterData.Any(a => a.QuestionKey == myQuestion.QuestionKey))
                .OrderByDescending(e => e.EncounterDateTime);

            var e1 = myEncs.FirstOrDefault();
            if (e1 != null)
            {
                var e2 = e1.EncounterData;
                if (e2 != null)
                {
                    return e2.FirstOrDefault(ed => ed.QuestionKey == myQuestion.QuestionKey);
                }
            }

            return null;
        }

        public bool ShouldShowFuneralHomeInfo =>
            // Only show FuneralHome info on PatientDemographics if we are in an encounter where the admission is a Hospice Admission
            Admission.HospiceAdmission;

        public string FuneralName
        {
            get
            {
                var f = FuneralData;
                return f == null ? string.Empty : f.TextData;
            }
        }

        public string FuneralPhone
        {
            get
            {
                var f = FuneralData;
                return f == null ? string.Empty : f.Text2Data;
            }
        }

        public string FuneralExtension
        {
            get
            {
                var f = FuneralData;
                return f == null ? string.Empty : f.Text3Data;
            }
        }

        public string AdditionalText
        {
            get
            {
                var f = FuneralData;
                return f == null ? string.Empty : f.Text4Data;
            }
        }

        public override void ClearEntity()
        {
        }

        public void SetupPatientAddress()
        {
            _PatientAddress = null;
            if ((Patient != null) && (Patient.PatientAddress != null))
            {
                Encounter e = new Encounter { EncounterStartDate = DateTimeOffset.Now };
                e.SetupPatientAddressCollectionView(Patient.PatientAddress);
                e.FilterPatientAddressCollectionView();
                if (e.PatientAddressKey != null)
                {
                    _PatientAddress = Patient.PatientAddress.FirstOrDefault(pa => pa.PatientAddressKey == e.PatientAddressKey);
                }
            }

            PhysicianDetailsCommand = new RelayCommand<AdmissionPhysician>(physician =>
            {
                if (physician == null)
                {
                    return;
                }

                PhysicianDisplay pd = new PhysicianDisplay
                    { Physician = physician.PhysicianProxy, AdmissionPhysician = physician };
                PhysicianDetailsDialog d = new PhysicianDetailsDialog(pd);
                d.Show();
            });
        }

        public override void Cleanup()
        {
            PhysicianDetailsCommand = null;
            base.Cleanup();
        }

        public RelayCommand<AdmissionPhysician> PhysicianDetailsCommand { get; set; }

        private PatientAddress _PatientAddress;
        public PatientAddress PatientAddress => _PatientAddress;
        public bool IsPatientAddress => (_PatientAddress != null);
    }

    public class PatientDemographicsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PatientDemographics pd = new PatientDemographics(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Admission = vm.CurrentAdmission,
                Encounter = vm.CurrentEncounter,
                Patient = vm.CurrentPatient,
                OasisManager = vm.CurrentOasisManager,
            };
            pd.SetupPatientAddress();
            return pd;
        }
    }
}