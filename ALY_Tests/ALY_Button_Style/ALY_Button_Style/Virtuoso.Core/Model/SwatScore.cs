#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class SwatScore : QuestionBase
    {
        public class SwatScoreData : GenericBase, INotifyDataErrorInfo
        {
            public SwatScoreData(string label, string AnswerGroupName, SwatScore myContext, string answer)
            {
                Label = label;
                this.AnswerGroupName = AnswerGroupName;
                ParentSwatScoreQuestion = myContext;
                InitializeRadioButtonAnswers(answer);
            }

            public override void Cleanup()
            {
                Label = null;
                AnswerGroupName = null;
                ParentSwatScoreQuestion = null;
                Comment = null;
            }

            private void InitializeRadioButtonAnswers(string answer)
            {
                switch (answer)
                {
                    case "1":
                        Answer1 = true;
                        break;
                    case "2":
                        Answer2 = true;
                        break;
                    case "3":
                        Answer3 = true;
                        break;
                    case "4":
                        Answer4 = true;
                        break;
                    case "5":
                        Answer5 = true;
                        break;
                }
            }

            private string _Label = "";

            public string Label
            {
                get { return _Label; }
                set { _Label = value; }
            }

            private string _AnswerGroupName = "";

            public string AnswerGroupName
            {
                get { return _AnswerGroupName; }
                set
                {
                    _AnswerGroupName = value;
                    RaisePropertyChanged("AnswerGroupName");
                }
            }

            private string _Comment = "";

            public string Comment
            {
                get { return _Comment; }
                set { _Comment = value; }
            }

            private bool _Answer1;

            public bool Answer1
            {
                get { return _Answer1; }
                set
                {
                    _Answer1 = value;
                    ParentSwatScoreQuestion.ReevaluateScore();
                }
            }

            private bool _Answer2;

            public bool Answer2
            {
                get { return _Answer2; }
                set
                {
                    _Answer2 = value;
                    ParentSwatScoreQuestion.ReevaluateScore();
                }
            }

            private bool _Answer3;

            public bool Answer3
            {
                get { return _Answer3; }
                set
                {
                    _Answer3 = value;
                    ParentSwatScoreQuestion.ReevaluateScore();
                }
            }

            private bool _Answer4;

            public bool Answer4
            {
                get { return _Answer4; }
                set
                {
                    _Answer4 = value;
                    ParentSwatScoreQuestion.ReevaluateScore();
                }
            }

            private bool _Answer5;

            public bool Answer5
            {
                get { return _Answer5; }
                set
                {
                    _Answer5 = value;
                    ParentSwatScoreQuestion.ReevaluateScore();
                }
            }

            public SwatScore ParentSwatScoreQuestion { get; set; }

            #region INotifyDataErrorInfo

            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
            readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

            public IEnumerable GetErrors(string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    //FYI: if you are not supporting entity level errors, it is acceptable to return null
                    var ret = _currentErrors.Values.Where(c => c.Any());
                    return ret.Any() ? ret : null;
                }

                MakeOrCreatePropertyErrorList(propertyName);
                if (_currentErrors[propertyName].Any())
                {
                    return _currentErrors[propertyName];
                }

                return null;
            }

            public bool HasErrors
            {
                get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
            }

            void FireErrorsChanged(string property)
            {
                if (ErrorsChanged != null)
                {
                    ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
                }
            }

            public void ClearErrorFromProperty(string property)
            {
                MakeOrCreatePropertyErrorList(property);
                _currentErrors[property].Clear();
                FireErrorsChanged(property);
            }

            public void AddErrorForProperty(string property, string error)
            {
                MakeOrCreatePropertyErrorList(property);
                _currentErrors[property].Add(error);
                FireErrorsChanged(property);
            }

            void MakeOrCreatePropertyErrorList(string propertyName)
            {
                if (!_currentErrors.ContainsKey(propertyName))
                {
                    _currentErrors[propertyName] = new List<string>();
                }
            }

            #endregion
        }

        public string SwatScorePopupLabel => SwatScorePatient ? "PATIENT" : "PRIMARY CAREGIVER";
        private string _SwatScoreComments;

        public string SwatScoreComments
        {
            get { return _SwatScoreComments; }
            set
            {
                _SwatScoreComments = value;
                RaisePropertyChanged("SwatScoreComments");
            }
        }

        private bool _SwatScorePatient;

        public bool SwatScorePatient
        {
            get { return _SwatScorePatient; }
            set
            {
                _SwatScorePatient = value;
                RaisePropertyChanged("SwatScorePatient");
                RaisePropertyChanged("SwatScorePopupLabel");
            }
        }

        private ObservableCollection<SwatScoreData> _SwatScoreDatas;

        public ObservableCollection<SwatScoreData> SwatScoreDatas
        {
            get { return _SwatScoreDatas; }
            set
            {
                _SwatScoreDatas = value;
                RaisePropertyChanged("SwatScoreDatas");
            }
        }

        private readonly string[] defaultAnswers = { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };

        private ObservableCollection<SwatScoreData> InitializeSwatScoreDatas()
        {
            string[] answers = null;
            if (SwatScorePatient)
            {
                answers = (EncounterData.TextData != null) ? EncounterData.TextData.Split('|') : defaultAnswers;
                SwatScoreComments = EncounterData.Text3Data;
            }
            else
            {
                answers = (EncounterData.Text2Data != null) ? EncounterData.Text2Data.Split('|') : defaultAnswers;
                SwatScoreComments = EncounterData.Text4Data;
            }

            List<SwatScoreData> myList = new List<SwatScoreData>
            {
                new SwatScoreData("1. End of life decisions consistent with their religious and cultural norms", "GroupLabel1", 
                    this, answers[0]),
                new SwatScoreData("2. Patient thoughts of suicide or wanting to hasten death", "GroupLabel2", 
                    this, answers[1]),
                new SwatScoreData("3. Anxiety about death", "GroupLabel3", 
                    this, answers[2]),
                new SwatScoreData("4. Preferences about environment (e.g., pets, own bed, etc.)", "GroupLabel4", 
                    this, answers[3]),
                new SwatScoreData("5. Social support", "GroupLabel5", 
                    this, answers[4]),
                new SwatScoreData("6. Financial resources", "GroupLabel6", 
                    this, answers[5]),
                new SwatScoreData("7. Safety issues", "GroupLabel7", 
                    this, answers[6]),
                new SwatScoreData("8. Comfort issues", "GroupLabel8", 
                    this, answers[7]),
                new SwatScoreData("9. Complicated anticipatory grief (e.g., guilt, depression, etc.)", "GroupLabel9", 
                    this, answers[8]),
                new SwatScoreData("10. Awareness of prognosis", "roupLabel10", 
                    this, answers[9]),
                new SwatScoreData("11. Spirituality (e.g., higher purpose in life, sense of connection with all, etc.)", "GroupLabel11", 
                    this, answers[10])
            };
            return myList.ToObservableCollection();
        }

        public void ReevaluateScore()
        {
            RaisePropertyChanged("PatientCaregiverScore");
        }

        public RelayCommand SWATScoreOK_Command { get; protected set; }
        public RelayCommand SWATScoreCancel_Command { get; protected set; }

        public override void Cleanup()
        {
            try
            {
                EncounterData.PropertyChanged -= EncounterData_PropertyChanged;
            }
            catch (Exception)
            {
            }

            try
            {
                _SwatScoreDatas = null;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        public void SetupCommands()
        {
            SwatScorePatientCommand = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                SwatScorePatient = true;
                SwatScoreDatas = InitializeSwatScoreDatas();
                RaisePropertyChanged("PatientCaregiverScore");
                EncounterData.BeginEditting();
                DynamicFormViewModel.PopupDataContext = this;
            });
            SwatScoreCaregiverCommand = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                SwatScorePatient = false;
                SwatScoreDatas = InitializeSwatScoreDatas();
                RaisePropertyChanged("PatientCaregiverScore");
                EncounterData.BeginEditting();
                DynamicFormViewModel.PopupDataContext = this;
            });
            SWATScoreOK_Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                if (ValidatePopup() == false)
                {
                    return;
                }

                string pipeDelimitedAnswers = "";
                List<string> nonSerializedComments = new List<string>();

                foreach (var item in SwatScoreDatas)
                {
                    if (item.Answer1)
                    {
                        pipeDelimitedAnswers += "1|";
                    }
                    else if (item.Answer2)
                    {
                        pipeDelimitedAnswers += "2|";
                    }
                    else if (item.Answer3)
                    {
                        pipeDelimitedAnswers += "3|";
                    }
                    else if (item.Answer4)
                    {
                        pipeDelimitedAnswers += "4|";
                    }
                    else if (item.Answer5)
                    {
                        pipeDelimitedAnswers += "5|";
                    }

                    if (item.Comment == null)
                    {
                        nonSerializedComments.Add("");
                    }
                    else
                    {
                        nonSerializedComments.Add(item.Comment);
                    }
                }

                if (SwatScorePatient)
                {
                    EncounterData.IntData = PatientCaregiverScore;
                    EncounterData.TextData = pipeDelimitedAnswers;
                    EncounterData.Text3Data = SwatScoreComments;
                }
                else
                {
                    EncounterData.Text2Data = pipeDelimitedAnswers;
                    EncounterData.Int2Data = PatientCaregiverScore;
                    EncounterData.Text4Data = SwatScoreComments;
                }

                DynamicFormViewModel.PopupDataContext = null;
                EncounterData.EndEditting();
            });
            SWATScoreCancel_Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel != null)
                {
                    DynamicFormViewModel.PopupDataContext = null;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                ClearErrors();
                EncounterData.CancelEditting();
            });
        }

        public int PatientCaregiverScore
        {
            get
            {
                int score = 0;

                foreach (var item in SwatScoreDatas)
                    if (item.Answer1)
                    {
                        score += 1;
                    }
                    else if (item.Answer2)
                    {
                        score += 2;
                    }
                    else if (item.Answer3)
                    {
                        score += 3;
                    }
                    else if (item.Answer4)
                    {
                        score += 4;
                    }
                    else if (item.Answer5)
                    {
                        score += 5;
                    }

                return score;
            }
        }

        public RelayCommand SwatScorePatientCommand { get; set; }
        public RelayCommand SwatScoreCaregiverCommand { get; set; }
        private string _PopupDataTemplate = "SwatScorePopupDataTemplate";

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

        public SwatScore(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SetupCommands();
        }

        public bool ValidatePopup()
        {
            bool AllValid = true;

            ClearErrors();
            if (EncounterData == null)
            {
                return AllValid;
            }

            if (Encounter == null)
            {
                return AllValid;
            }

            foreach (var item in SwatScoreDatas)
                if ((item.Answer1 == false) && (item.Answer2 == false) && (item.Answer3 == false) &&
                    (item.Answer4 == false) && (item.Answer5 == false))
                {
                    AllValid = false;
                    item.AddErrorForProperty("Answer1", "An answer must be selected for: " + item.Label);
                }

            return AllValid;
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;
            if (EncounterData == null)
            {
                return AllValid;
            }

            if (Encounter == null)
            {
                return AllValid;
            }

            if (!Encounter.FullValidation)
            {
                return AllValid;
            }

            if (Required && ((EncounterData.IntData == null) || (EncounterData.IntData == 0)))
            {
                EncounterData.ValidationErrors.Add(new ValidationResult("A Patient SWAT score is required",
                    new[] { "IntData" }));
                AllValid = false;
            }

            return AllValid;
        }
    }

    public class SwatScoreFactory
    {
        public static SwatScore Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            SwatScore qb = new SwatScore(__FormSectionQuestionKey)
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
            EncounterData ed = vm.CurrentEncounter.EncounterData.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey 
                && x.SectionKey == formsection.Section.SectionKey 
                && x.QuestionGroupKey == qgkey 
                && x.QuestionKey == q.QuestionKey);
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey,
                    BoolData = false
                };

                qb.EncounterData = ed;

                if (qb.IsNewEncounterOrSection && copyforward)
                {
                    qb.CopyForwardLastInstance();
                }

                vm.CurrentEncounter.EncounterData.Add(ed);
            }
            else
            {
                qb.EncounterData = ed;
            }

            return qb;
        }
    }
}