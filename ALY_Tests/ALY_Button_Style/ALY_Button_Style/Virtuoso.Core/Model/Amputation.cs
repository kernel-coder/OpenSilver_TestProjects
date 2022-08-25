#region Usings

using System.Linq;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Amputation : QuestionBase
    {
        public Amputation(int? formSectionQuestionKey) : base(formSectionQuestionKey)
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

    public class AmputationFactory
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

            return new Amputation(__FormSectionQuestionKey)
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
                            var cl = CodeLookupCache.GetSequenceFromKey(ed.IntData);
                            amp = cl.HasValue ? cl.Value : 0;
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
                            var cl = CodeLookupCache.GetSequenceFromKey(ed.Int2Data);
                            amp = cl.HasValue ? cl.Value : 0;
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
}