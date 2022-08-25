#region Usings

using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PhysicianPrimary : QuestionUI
    {
        public AdmissionPhysicianFacade AdmissionPhysician { get; set; }

        public PhysicianPrimary(Admission admission, Encounter encounter, int? formSectionQuestionKey) : base(
            formSectionQuestionKey)
        {
            Messenger.Default.Register<int>(this,
                "AdmissionPhysician_FormUpdate",
                AdmissionKey => { AdmissionPhysician.RaiseEvents(); });

            Admission = admission;
            Encounter = encounter;
            AdmissionPhysician = new AdmissionPhysicianFacade
            {
                Admission = Admission,
                Encounter = Encounter
            };
        }

        private EncounterData _EncounterData;

        public EncounterData EncounterData
        {
            get { return _EncounterData; }
            set
            {
                _EncounterData = value;
                this.RaisePropertyChangedLambda(p => p.EncounterData);
            }
        }

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
            }

            base.Cleanup();
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if ((EncounterData != null) && (EncounterData.IsNew))
            {
                Encounter.EncounterData.Add(EncounterData);
            }

            return true;
        }
    }

    public class PhysicianPrimaryFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PhysicianPrimary pp =
                new PhysicianPrimary(vm.CurrentAdmission, vm.CurrentEncounter, __FormSectionQuestionKey)
                {
                    Section = formsection.Section,
                    QuestionGroupKey = qgkey,
                    Question = q,
                    Patient = vm.CurrentPatient,
                    Encounter = vm.CurrentEncounter,
                    Admission = vm.CurrentAdmission,
                    OasisManager = vm.CurrentOasisManager,
                    DynamicFormViewModel = vm
                };
            EncounterData ed = vm.CurrentEncounter.EncounterData
                .FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                                     && x.SectionKey == formsection.Section.SectionKey 
                                     && x.QuestionGroupKey == qgkey 
                                     && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
            }

            pp.EncounterData = ed;
            return pp;
        }
    }
}