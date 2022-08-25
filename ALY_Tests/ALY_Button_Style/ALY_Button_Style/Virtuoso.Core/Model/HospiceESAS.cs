#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ESASGraph : QuestionBase
    {
        public RelayCommand ESASGraphCommand { get; protected set; }
        public RelayCommand ESASGraphOK_Command { get; protected set; }
        public RelayCommand ESASGraphCancel_Command { get; protected set; }
        private string _PopupDataTemplate = "ESASGraphPopupDataTemplate";

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

        public ESASGraph(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SetupCommands();
        }

        public ESASGraphContext ESASPain { get; set; }
        public ESASGraphContext ESASNausea { get; set; }
        public ESASGraphContext ESASAppetite { get; set; }
        public ESASGraphContext ESASConstipation { get; set; }
        public ESASGraphContext ESASBreath { get; set; }
        public ESASGraphContext ESASTiredness { get; set; }
        public ESASGraphContext ESASDrowsiness { get; set; }
        public ESASGraphContext ESASDepression { get; set; }
        public ESASGraphContext ESASAnxiety { get; set; }
        public ESASGraphContext ESASWellbeing { get; set; }

        public ESASGraphContext ReportedBy { get; set; }

        public ESASGraphContext HospicePPS { get; set; }
        public ESASGraphContext HospiceFAST { get; set; }
        public ESASGraphContext HospiceGDS { get; set; }
        public ESASGraphContext HospiceMMSE { get; set; }

        public void SetupData()
        {
            //Perception of NAUSEA (based on ESAS-r)
            //Perception of LACK OF APPETITE (based on ESAS-r)
            //Perception of PAIN (based on ESAS-r)
            //Perception of SHORTNESS OF BREATH (based on ESAS-r)
            //Perception of TIREDNESS - Lack of Energy (based on ESAS-r)
            //Perception of DROWSINESS - Feeling Sleepy (based on ESAS-r)
            //Perception of DEPRESSION - Feeling Sad (based on ESAS-r)
            //Perception of ANXIETY - Feeling Nervous (based on ESAS-r)
            //Perception of WELLBEING - Feeling Overall (based on ESAS-r)
            //Perception of CONSTIPATION (based on ESAS-r) 

            var ESASQuestions =
                DynamicFormCache.GetQuestionByDataTemplate("PerceptionOf"); //Perception of PAIN (based on ESAS-r)

            ESASPain = GetESASData(ESASQuestions, "PAIN", "Pain");
            ESASNausea = GetESASData(ESASQuestions, "NAUSEA", "Nausea");
            ESASAppetite = GetESASData(ESASQuestions, "APPETITE", "Lack of Appetite");
            ESASConstipation = GetESASData(ESASQuestions, "CONSTIPATION", "Constipation");
            ESASBreath = GetESASData(ESASQuestions, "BREATH", "Shortness of Breath");
            ESASTiredness = GetESASData(ESASQuestions, "TIREDNESS", "Tiredness");
            ESASDrowsiness = GetESASData(ESASQuestions, "DROWSINESS", "Drowsiness");
            ESASDepression = GetESASData(ESASQuestions, "DEPRESSION", "Depression");
            ESASAnxiety = GetESASData(ESASQuestions, "ANXIETY", "Anxiety");
            ESASWellbeing = GetESASData(ESASQuestions, "WELLBEING", "Wellbeing");

            ReportedBy = GetReportedByData("Reported By");

            HospicePPS = GetHospicePPSData("PPS");
            HospiceFAST = GetHospiceStageData("FAST", "HospiceFASTStaging");
            HospiceGDS = GetHospiceStageData("GDS", "HospiceGDSStaging");

            HospiceMMSE = GetHospiceMMSEData("MMSE");
        }

        private ESASGraphContext GetHospiceMMSEData(string graphTitle)
        {
            var encounters = GetLast14Encounters();

            var questionKey = DynamicFormCache.GetQuestionByDataTemplate("MMSE")
                .Select(q => q.QuestionKey)
                .FirstOrDefault();

            var questions = GetGraphPointsForEncounters(encounters, questionKey, ed =>
            {
                try
                {
                    var v = ed.TextData;
                    return Int32.Parse(v);
                }
                catch (Exception)
                {
                    return 0;
                }
            });
            var ret = new ESASGraphContext(graphTitle, questions);
            ret.SetDateRange(encounters.OrderBy(e => e.EncounterDateTime));
            return ret;
        }

        private ESASGraphContext GetHospicePPSData(string graphTitle)
        {
            var encounters = GetLast14Encounters();

            var questions = GetGraphPointsForEncounters(encounters,
                GetQuestionKeyForDataTemplateAndBackingFactory("HospicePalliativePerformanceScale",
                    "HospicePalliativePerformanceScale"), ed =>
                {
                    try
                    {
                        var v = ed.TextData.Replace("%", string.Empty);
                        return Int32.Parse(v);
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                });

            var ret = new ESASGraphContext(graphTitle, questions);

            ret.SetDateRange(encounters.OrderBy(e => e.EncounterDateTime));

            return ret;
        }

        private ESASGraphContext GetHospiceStageData(string graphTitle, string lookupType)
        {
            var encounters = GetLast14Encounters();

            var questions = GetGraphPointsForEncounters(encounters,
                GetQuestionKeyForDataTemplateAndLookupType("CodeLookup", lookupType), ed =>
                {
                    try
                    {
                        var v = ed.IntData.GetValueOrDefault();
                        var retInt = ConvertReportedByToInteger(GetESASReportedCodes(lookupType), v);
                        return retInt;
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                });

            var ret = new ESASGraphContext(graphTitle, questions);

            ret.SetDateRange(encounters.OrderBy(e => e.EncounterDateTime));

            return ret;
        }

        private ESASGraphContext GetReportedByData(string graphTitle)
        {
            var encounters = GetLast14Encounters();

            var data = GetLast14ReportedByData(encounters);

            var ret = new ESASGraphContext(graphTitle, data);

            ret.SetDateRange(encounters.OrderBy(e => e.EncounterDateTime));

            return ret;
        }

        private ESASGraphContext GetESASData(List<Question> ESASQuestions, string questionLabel, string graphTitle)
        {
            var encounters = GetLast14Encounters();

            var questions = GetGraphPointsForEncounters(encounters,
                GetQuestionKeyForESASLabel(ESASQuestions, questionLabel), ed =>
                {
                    var v = ed.IntData.GetValueOrDefault();
                    var code = CodeLookupCache.GetCodeFromKey(v);
                    return Int32.Parse(code);
                });

            var ret = new ESASGraphContext(graphTitle, questions);

            ret.SetDateRange(encounters.OrderBy(e => e.EncounterDateTime));

            return ret;
        }

        private IEnumerable<Encounter> GetLast14Encounters()
        {
            var encounters = Admission.Encounter
                .Where(e => e.EncounterStatus != (int)EncounterStatusType.None)
                .OrderByDescending(e => e.EncounterDateTime)
                .Take(14);
            return encounters;
        }

        private int GetQuestionKeyForDataTemplateAndBackingFactory(string dataTemplate, string backingFactory)
        {
            var questions = DynamicFormCache.GetQuestionByDataTemplate(dataTemplate);
            if (questions != null)
            {
                var filteredByFactory =
                    questions.Where(q => q.BackingFactory.ToLower().Equals(backingFactory.ToLower())).ToList();
                if (filteredByFactory != null && filteredByFactory.Any())
                {
                    var ret = filteredByFactory
                        .Select(q => q.QuestionKey)
                        .FirstOrDefault();
                    return ret;
                }
            }

            return 0;
        }

        private int GetQuestionKeyForDataTemplateAndLookupType(string dataTemplate, string lookupType)
        {
            var questions = DynamicFormCache.GetQuestionByDataTemplate(dataTemplate);
            if (questions != null)
            {
                var filteredByFactory = questions
                    .Where(q => q.LookupType != null && q.LookupType.ToLower().Equals(lookupType.ToLower())).ToList();
                if (filteredByFactory != null && filteredByFactory.Any())
                {
                    var ret = filteredByFactory
                        .Select(q => q.QuestionKey)
                        .FirstOrDefault();
                    return ret;
                }
            }

            return 0;
        }

        private int GetQuestionKeyForESASLabel(List<Question> ESASQuestions, string label)
        {
            var ret = ESASQuestions
                .Where(q => q.Label.ToLower().Contains(label.ToLower()))
                .Select(q => q.QuestionKey)
                .FirstOrDefault();
            return ret;
        }

        private List<ESASGraphPoint> GetLast14ReportedByData(IEnumerable<Encounter> encounters) //string questionLabel)
        {
            var last14ReportedByValues = new List<ESASGraphPoint>();

            foreach (var encounter in encounters)
                if (encounter.ReportedBy.HasValue)
                {
                    var rpt = new ESASGraphPoint
                    {
                        ReadingDateTime = encounter.EncounterDateTime,
                        ReadingNumeric = ConvertReportedByToInteger(GetESASReportedCodes("ESASReported"), encounter.ReportedBy.GetValueOrDefault())
                    };

                    last14ReportedByValues.Add(rpt);
                }

            return last14ReportedByValues;
        }

        private int ConvertReportedByToInteger(CodeLookup[] codeLookups, int reportedBy)
        {
            try
            {
                var idx = Array.IndexOf(codeLookups, codeLookups.FirstOrDefault(c => c.CodeLookupKey == reportedBy));
                return idx;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private CodeLookup[] GetESASReportedCodes(string codeType)
        {
            // 'P' = Patient
            // 'F' = Family Careiver
            // 'H' = HCP Caregiver
            // 'C" = Caregiver Assisted

            //CodeLookupKey CodeLookupHeaderKey TenantID    Code   CodeDescription
            //------------- ------------------- ----------- ------ ----------------------------
            //136439        16968               6           P      Patient
            //136441        16968               6           F      Family Caregiver
            //136443        16968               6           H      Health Care Professional
            //136445        16968               6           C      Caregiver Assisted
            return CodeLookupCache.GetCodeLookupsFromType(codeType).ToArray();
        }

        private List<ESASGraphPoint> GetGraphPointsForEncounters(IEnumerable<Encounter> encounters, int questionKey, Func<EncounterData, int> getDataPoint)
        {
            var last14ESASQuestions = new List<ESASGraphPoint>();

            foreach (var encounter in encounters)
            {
                var eds = encounter.EncounterData
                    .Where(ed => ed.QuestionKey == questionKey)
                    .Select(ed => new ESASGraphPoint
                    {
                        ReadingDateTime = encounter.EncounterDateTime,
                        QuestionKey = questionKey,
                        ReadingNumeric = getDataPoint(ed)
                    }).FirstOrDefault();

                //Do not add a data point if not answered
                if (eds == null)
                {
                    eds = new ESASGraphPoint
                    {
                        ReadingDateTime = encounter.EncounterDateTime,
                        QuestionKey = questionKey,
                        ReadingNumeric = 0
                    };
                }

                last14ESASQuestions.Add(eds);
            }

            return last14ESASQuestions;
        }

        private void SetupCommands()
        {
            ESASGraphCommand = new RelayCommand(() =>
            {
                SetupData();
                DynamicFormViewModel.PopupDataContext = this;
            });
            ESASGraphOK_Command = new RelayCommand(() => { DynamicFormViewModel.PopupDataContext = null; });
            ESASGraphCancel_Command = new RelayCommand(() => { DynamicFormViewModel.PopupDataContext = null; });
        }

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
            return AllValid;
        }

        public class ESASGraphContext
        {
            public ESASGraphContext(string title, IEnumerable<ESASGraphPoint> dataset)
            {
                Title = title;
                ChartItemSource = dataset;
            }

            public string Title { get; private set; }

            public DateTime GraphItemSourceMinimumDate { get; set; }
            public DateTime GraphItemSourceMaximumDate { get; set; }
            public int GraphItemSourceInterval { get; set; }

            public IEnumerable<ESASGraphPoint> ChartItemSource { get; private set; }

            public void SetDateRange(IEnumerable<Encounter> encounters)
            {
                GraphItemSourceMinimumDate =
                    SetGraphItemSourceMinimumDate(encounters.OrderBy(e => e.EncounterDateTime));
                GraphItemSourceMaximumDate =
                    SetGraphItemSourceMaximumDate(encounters.OrderBy(e => e.EncounterDateTime));
                GraphItemSourceInterval = SetGraphItemSourceInterval(encounters.OrderBy(e => e.EncounterDateTime));
            }

            private DateTime SetGraphItemSourceMinimumDate(IEnumerable<Encounter> list)
            {
                if (list == null)
                {
                    return DateTime.Now.Date;
                }

                if (list.Any() == false)
                {
                    return DateTime.Now.Date;
                }

                return list.First().EncounterDateTime.Date;
            }

            private DateTime SetGraphItemSourceMaximumDate(IEnumerable<Encounter> list)
            {
                if (list == null)
                {
                    return DateTime.Now.AddDays(1).Date;
                }

                if (list.Any() == false)
                {
                    return DateTime.Now.AddDays(1).Date;
                }

                return list.Last().EncounterDateTime.AddDays(1).Date;
            }

            private int SetGraphItemSourceInterval(IEnumerable<Encounter> list)
            {
                if (list == null)
                {
                    return 1;
                }

                if (list.Count() < 2)
                {
                    return 1;
                }

                var first = list.First();
                var last = list.Last();
                if (first == null || last == null)
                {
                    return 1;
                }

                if (first.EncounterDateTime == last.EncounterDateTime)
                {
                    return 1;
                }

                TimeSpan ts = last.EncounterDateTime.Subtract(first.EncounterDateTime);
                if (ts == null)
                {
                    return 1;
                }

                if (ts.Days == 0)
                {
                    return 1;
                }

                int x = ((ts.Days - 1) / 10) + 1;
                return x;
            }
        }

        public class ESASGraphPoint
        {
            public int ReadingNumeric { get; set; }
            public DateTime ReadingDateTime { get; set; }
            public int QuestionKey { get; set; }
            public string ReadingDataPointThumbNail => ReadingNumeric.ToString();

            public string ReadingDataPointThumbNailWithDateTime
            {
                get
                {
                    bool useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                    return string.Format("{0} {1}<LineBreak />{2}",
                        ReadingDateTime.ToShortDateString(),
                        ((useMilitaryTime) ? ReadingDateTime.ToString("HHmm") : ReadingDateTime.ToShortTimeString()),
                        ReadingDataPointThumbNail);
                }
            }
        }
    }

    public class ESASGraphFactory
    {
        public static ESASGraph Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ESASGraph qb = new ESASGraph(__FormSectionQuestionKey)
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

            return qb;
        }
    }

    public class ESASFlowSheet : QuestionBase, INotifyDataErrorInfo
    {
        public ESASFlowSheet(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        #region Properties

        public RelayCommand ESASFindings_Command { get; protected set; }
        public RelayCommand HospiceFindings_Command { get; protected set; }

        #endregion Properties

        #region Methods

        public void SetupESASFlowSheet()
        {
            ESASFindings_Command = new RelayCommand(() => { ESASFindingsCommand(); });
            HospiceFindings_Command = new RelayCommand(() => { HospiceFindingsCommand(); });
        }

        private void ESASFindingsCommand()
        {
            bool any = ((Encounter?.EncounterESASFindings == null) ? false : Encounter.EncounterESASFindings.Any());
            if (any == false)
            {
                NavigateCloseDialog d = new NavigateCloseDialog();
                d.Width = double.NaN;
                d.Height = double.NaN;
                d.ErrorMessage = "No ESAS-r Findings available.";
                d.ErrorQuestion = null;
                d.Title = "ESAS-r Findings";
                d.HasCloseButton = false;
                d.OKLabel = "OK";
                d.NoVisible = false;
                d.Show();
                return;
            }

            ESASFindingsPopupViewModel viewModel = new ESASFindingsPopupViewModel(Encounter?.EncounterESASFindings);
            DialogService ds = new DialogService();
            ds.ShowDialog(viewModel, ret =>
            {
                viewModel?.Cleanup();
                viewModel = null;
            });
        }

        private void HospiceFindingsCommand()
        {
            bool any =
                ((Encounter?.EncounterHospiceFindings == null) ? false : Encounter.EncounterHospiceFindings.Any());
            if (any == false)
            {
                NavigateCloseDialog d = new NavigateCloseDialog();
                d.Width = double.NaN;
                d.Height = double.NaN;
                d.ErrorMessage = "No Hospice Findings available.";
                d.ErrorQuestion = null;
                d.Title = "Hospice Findings";
                d.HasCloseButton = false;
                d.OKLabel = "OK";
                d.NoVisible = false;
                d.Show();
                return;
            }

            HospiceFindingsPopupViewModel viewModel =
                new HospiceFindingsPopupViewModel(Encounter?.EncounterHospiceFindings);
            DialogService ds = new DialogService();
            ds.ShowDialog(viewModel, ret =>
            {
                viewModel?.Cleanup();
                viewModel = null;
            });
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }

        #endregion Methods

        #region ICleanup

        public override void Cleanup()
        {
            ESASFindings_Command = null;
            HospiceFindings_Command = null;
            base.Cleanup();
        }

        #endregion ICleanup
    }

    public class ESASFlowSheetFactory
    {
        public static ESASFlowSheet Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            ESASFlowSheet efs = new ESASFlowSheet(__FormSectionQuestionKey)
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
            efs.SetupESASFlowSheet();
            return efs;
        }
    }
}