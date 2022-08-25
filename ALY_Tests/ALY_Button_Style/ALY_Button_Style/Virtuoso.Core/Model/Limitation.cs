#region Usings

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Limitation
    {
        public string Description { get; set; }
    }

    public class POCLimitations : QuestionUI
    {
        public POCLimitations(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public ObservableCollection<Limitation> Limitations { get; set; }
        private String _LimitationString;

        public String LimitationsString
        {
            get { return _LimitationString; }
            set
            {
                _LimitationString = value;
                if (epc != null)
                {
                    epc.POCLimitations = value;
                    PopulateLimitationsList();
                }
            }
        }

        public void PopulateLimitationsList()
        {
            string[] delimit = { "|" };
            if (epc == null)
            {
                return;
            }

            if (Limitations == null)
            {
                return;
            }

            string[] lSplit = epc.POCLimitations.Split(delimit, StringSplitOptions.RemoveEmptyEntries);
            Limitations.Clear();
            foreach (string l in lSplit)
            {
                Limitation lo = new Limitation { Description = l };
                Limitations.Add(lo);
            }
        }

        public EncounterPlanOfCare epc;

        public override bool Validate(out string SubSections)
        {
            if (epc != null)
            {
                epc.POCLimitations = LimitationsString;
            }

            return base.Validate(out SubSections);
        }
    }

    public class POCLimitationsFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POCLimitations pl = new POCLimitations(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };
            pl.Limitations = new ObservableCollection<Limitation>();
            pl.epc = vm.CurrentEncounter.EncounterPlanOfCare.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            if ((pl.epc != null) && (pl.epc.POCLimitations != null))
            {
                pl.LimitationsString = pl.epc.POCLimitations;
                pl.LimitationsString = pl.LimitationsString.TrimStart(Convert.ToChar("|"));
                pl.LimitationsString = pl.LimitationsString.Replace("|", " - ");
                pl.PopulateLimitationsList();
            }

            return pl;
        }
    }
}