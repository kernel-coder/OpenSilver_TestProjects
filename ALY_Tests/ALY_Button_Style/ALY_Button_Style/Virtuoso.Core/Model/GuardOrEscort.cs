#region Usings

using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class GuardOrEscort : QuestionBase
    {
        public GuardOrEscort(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }
    }

    public class GuardOrEscortFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            GuardOrEscort qb = new GuardOrEscort(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
            };

            qb.ProtectedOverrideRunTime = qb.SetupOrderEntryProtectedOverrideRunTime();
            qb.ProtectedOverrideRunTime = qb.SetupServiceDateProtectedOverrideRunTime();
            if (qb.ProtectedOverrideRunTime == null)
            {
                qb.ProtectedOverrideRunTime = qb.ProtectDischargeDateOverrideRunTimeIndr();
            }

            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey
                };
                qb.EncounterData = ed;
                qb.ApplyDefaults();

                if (qb.Encounter.IsNew && copyforward)
                {
                    qb.CopyForwardLastInstance();
                }
            }
            else
            {
                qb.EncounterData = ed;
            }

            ed.PropertyChanged += qb.EncounterData_PropertyChanged;
            qb.Encounter.PropertyChanged += qb.Encounter_PropertyChanged;

            qb.Setup();

            if ((vm.CurrentEncounter != null)
                && (vm.CurrentEncounter.EncounterStatus != (int)EncounterStatusType.Completed)
               )
            {
                UserProfile up = UserCache.Current.GetUserProfileFromUserId(vm.CurrentEncounter.EncounterBy);
                if (up != null)
                {
                    string code = CodeLookupCache.GetCodeFromKey(up.MethodOfPayKey);

                    if (!string.IsNullOrEmpty(code))
                    {
                        // since employees that have a MethodOfPay of Salary can change their method of pay for each encounter through the UI, we only
                        // want to default the method of pay for those employees for new encounters.  the non Salary employees cannot change their method
                        // of pay, so we always want to default their method of pay.
                        if ((vm.CurrentEncounter.EncounterKey <= 0)
                            || (code.ToUpper() != "SAL")
                           )
                        {
                            qb.EncounterData.IntData = up.MethodOfPayKey;
                        }
                    }
                }
            }

            return qb;
        }
    }
}