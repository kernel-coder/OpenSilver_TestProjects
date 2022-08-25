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
using Virtuoso.Client.Core;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class zOBSOLETESwatScore : QuestionBase
    {
        public class zOBSOLETESwatScoreData : GenericBase, INotifyDataErrorInfo
        {
            public bool ErrorProp => true;

            public zOBSOLETESwatScoreData(string label, string PatientAnswerGroupLabel,
                string ClinicianAnswerGroupLabel, zOBSOLETESwatScore myContext, string patientAns, string clinAns,
                string comment)
            {
                Label = label;
                PatientAnswerGroupName = PatientAnswerGroupLabel;
                ClinicianAnswerGroupName = ClinicianAnswerGroupLabel;
                ParentSwatScore = myContext;
                Comment = comment;

                InitializeRadioButtonAnswers(patientAns, clinAns);
            }

            public override void Cleanup()
            {
                Label = null;
                PatientAnswerGroupName = null;
                ClinicianAnswerGroupName = null;
                ParentSwatScore = null;
                Comment = null;
            }

            private void InitializeRadioButtonAnswers(string patientAns, string clinAns)
            {
                switch (patientAns)
                {
                    case "1":
                        PatientAnswer1 = true;
                        break;
                    case "2":
                        PatientAnswer2 = true;
                        break;
                    case "3":
                        PatientAnswer3 = true;
                        break;
                    case "4":
                        PatientAnswer4 = true;
                        break;
                    case "5":
                        PatientAnswer5 = true;
                        break;
                }

                switch (clinAns)
                {
                    case "1":
                        ClinicianAnswer1 = true;
                        break;
                    case "2":
                        ClinicianAnswer2 = true;
                        break;
                    case "3":
                        ClinicianAnswer3 = true;
                        break;
                    case "4":
                        ClinicianAnswer4 = true;
                        break;
                    case "5":
                        ClinicianAnswer5 = true;
                        break;
                }
            }

            private string _Label = "";

            public string Label
            {
                get { return _Label; }
                set { _Label = value; }
            }

            private string _ClinicianAnswerGroupLabel = "";

            public string ClinicianAnswerGroupName
            {
                get { return _ClinicianAnswerGroupLabel; }
                set
                {
                    _ClinicianAnswerGroupLabel = value;
                    RaisePropertyChanged("ClinicianAnswerGroupName");
                }
            }

            private string _PatientAnswerGroupLabel = "";

            public string PatientAnswerGroupName
            {
                get { return _PatientAnswerGroupLabel; }
                set
                {
                    _PatientAnswerGroupLabel = value;
                    RaisePropertyChanged("PatientAnswerGroupName");
                }
            }

            private string _Comment = "";

            public string Comment
            {
                get { return _Comment; }
                set { _Comment = value; }
            }

            private bool _PatientAnswer1;

            public bool PatientAnswer1
            {
                get { return _PatientAnswer1; }
                set
                {
                    _PatientAnswer1 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _PatientAnswer2;

            public bool PatientAnswer2
            {
                get { return _PatientAnswer2; }
                set
                {
                    _PatientAnswer2 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _PatientAnswer3;

            public bool PatientAnswer3
            {
                get { return _PatientAnswer3; }
                set
                {
                    _PatientAnswer3 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _PatientAnswer4;

            public bool PatientAnswer4
            {
                get { return _PatientAnswer4; }
                set
                {
                    _PatientAnswer4 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _PatientAnswer5;

            public bool PatientAnswer5
            {
                get { return _PatientAnswer5; }
                set
                {
                    _PatientAnswer5 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _ClinicianAnswer1;

            public bool ClinicianAnswer1
            {
                get { return _ClinicianAnswer1; }
                set
                {
                    _ClinicianAnswer1 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _ClinicianAnswer2;

            public bool ClinicianAnswer2
            {
                get { return _ClinicianAnswer2; }
                set
                {
                    _ClinicianAnswer2 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _ClinicianAnswer3;

            public bool ClinicianAnswer3
            {
                get { return _ClinicianAnswer3; }
                set
                {
                    _ClinicianAnswer3 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _ClinicianAnswer4;

            public bool ClinicianAnswer4
            {
                get { return _ClinicianAnswer4; }
                set
                {
                    _ClinicianAnswer4 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            private bool _ClinicianAnswer5;

            public bool ClinicianAnswer5
            {
                get { return _ClinicianAnswer5; }
                set
                {
                    _ClinicianAnswer5 = value;
                    ParentSwatScore.ReevaluateScore();
                }
            }

            public zOBSOLETESwatScore ParentSwatScore { get; set; }

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

        private ObservableCollection<zOBSOLETESwatScoreData> _zOBSOLETESwatScoreDatas;

        public ObservableCollection<zOBSOLETESwatScoreData> zOBSOLETESwatScoreDatas
        {
            get
            {
                if (_zOBSOLETESwatScoreDatas == null)
                {
                    _zOBSOLETESwatScoreDatas = InitializeSwatScoreQuestions();
                }

                return _zOBSOLETESwatScoreDatas;
            }
        }

        private ObservableCollection<zOBSOLETESwatScoreData> InitializeSwatScoreQuestions()
        {
            string[] patientAns = { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] clinAns = { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
            string[] comments = { "", "", "", "", "", "", "", "", "", "", "" };

            if (EncounterData.TextData != null)
            {
                patientAns = EncounterData.TextData.Split('|');
            }

            if (EncounterData.Text2Data != null)
            {
                clinAns = EncounterData.Text2Data.Split('|');
            }

            if (EncounterData.Text3Data != null)
            {
                comments = JSONSerializer.Deserialize<string[]>(EncounterData.Text3Data);
            }

            List<zOBSOLETESwatScoreData> myList = new List<zOBSOLETESwatScoreData>();

            myList.Add(new zOBSOLETESwatScoreData(
                "1. End of life decisions consistent with their religious and cultural norms", "PatientGroupLabel1",
                "ClinicianGroupLabel1", this, patientAns[0], clinAns[0], comments[0]));
            myList.Add(new zOBSOLETESwatScoreData("2. Patient thoughts of suicide or wanting to hasten death",
                "PatientGroupLabel2", "ClinicianGroupLabel2", this, patientAns[1], clinAns[1], comments[1]));
            myList.Add(new zOBSOLETESwatScoreData("3. Anxiety about death", "PatientGroupLabel3",
                "ClinicianGroupLabel3", this, patientAns[2], clinAns[2], comments[2]));
            myList.Add(new zOBSOLETESwatScoreData("4. Preferences about environment (e.g., pets, own bed, etc.)",
                "PatientGroupLabel4", "ClinicianGroupLabel4", this, patientAns[3], clinAns[3], comments[3]));
            myList.Add(new zOBSOLETESwatScoreData("5. Social support", "PatientGroupLabel5", "ClinicianGroupLabel5",
                this, patientAns[4], clinAns[4], comments[4]));
            myList.Add(new zOBSOLETESwatScoreData("6. Financial resources", "PatientGroupLabel6",
                "ClinicianGroupLabel6", this, patientAns[5], clinAns[5], comments[5]));
            myList.Add(new zOBSOLETESwatScoreData("7. Safety issues", "PatientGroupLabel7", "ClinicianGroupLabel7",
                this, patientAns[6], clinAns[6], comments[6]));
            myList.Add(new zOBSOLETESwatScoreData("8. Comfort issues", "PatientGroupLabel8", "ClinicianGroupLabel8",
                this, patientAns[7], clinAns[7], comments[7]));
            myList.Add(new zOBSOLETESwatScoreData("9. Complicated anticipatory grief (e.g., guilt, depression, etc.)",
                "PatientGroupLabel9", "ClinicianGroupLabel9", this, patientAns[8], clinAns[8], comments[8]));
            myList.Add(new zOBSOLETESwatScoreData("10. Awareness of prognosis", "PatientGroupLabel10",
                "ClinicianGroupLabel10", this, patientAns[9], clinAns[9], comments[9]));
            myList.Add(new zOBSOLETESwatScoreData(
                "11. Spirituality (e.g., higher purpose in life, sense of connection with all, etc.)",
                "PatientGroupLabel11", "ClinicianGroupLabel11", this, patientAns[10], clinAns[10], comments[10]));

            return myList.ToObservableCollection();
        }

        public void ReevaluateScore()
        {
            RaisePropertyChanged("PatientScore");
            RaisePropertyChanged("ClinicianScore");
        }

        public RelayCommand BradenScoreOK_Command { get; protected set; }
        public RelayCommand BradenScoreCancel_Command { get; protected set; }

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
                _zOBSOLETESwatScoreDatas = null;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        public void SetupCommands()
        {
            SwatScoreCommand = new RelayCommand(() =>
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
            });
            BradenScoreOK_Command = new RelayCommand(() =>
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if ((Encounter == null) || (EncounterData == null))
                {
                    return;
                }

                if (ValidatePopup() && (EncounterData.Validate()))
                {
                    if (!Encounter.EncounterData.Any(ed => ed.EncounterDataKey == EncounterData.EncounterDataKey))
                    {
                        Encounter.EncounterData.Add(EncounterData);
                    }

                    EncounterData.IntData = PatientScore;
                    EncounterData.Int2Data = ClinicianScore;

                    string pipeDelimitedPatientAnswers = "";
                    string pipeDelimitedClinicianAnswers = "";
                    List<string> nonSerializedComments = new List<string>();

                    foreach (var item in zOBSOLETESwatScoreDatas)
                    {
                        if (item.PatientAnswer1)
                        {
                            pipeDelimitedPatientAnswers += "1|";
                        }
                        else if (item.PatientAnswer2)
                        {
                            pipeDelimitedPatientAnswers += "2|";
                        }
                        else if (item.PatientAnswer3)
                        {
                            pipeDelimitedPatientAnswers += "3|";
                        }
                        else if (item.PatientAnswer4)
                        {
                            pipeDelimitedPatientAnswers += "4|";
                        }
                        else if (item.PatientAnswer5)
                        {
                            pipeDelimitedPatientAnswers += "5|";
                        }

                        if (item.ClinicianAnswer1)
                        {
                            pipeDelimitedClinicianAnswers += "1|";
                        }
                        else if (item.ClinicianAnswer2)
                        {
                            pipeDelimitedClinicianAnswers += "2|";
                        }
                        else if (item.ClinicianAnswer3)
                        {
                            pipeDelimitedClinicianAnswers += "3|";
                        }
                        else if (item.ClinicianAnswer4)
                        {
                            pipeDelimitedClinicianAnswers += "4|";
                        }
                        else if (item.ClinicianAnswer5)
                        {
                            pipeDelimitedClinicianAnswers += "5|";
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

                    EncounterData.TextData = pipeDelimitedPatientAnswers;
                    EncounterData.Text2Data = pipeDelimitedClinicianAnswers;
                    EncounterData.Text3Data = JSONSerializer.SerializeToJsonString(nonSerializedComments);

                    DynamicFormViewModel.PopupDataContext = null;
                    EncounterData.EndEditting();
                }
            });
            BradenScoreCancel_Command = new RelayCommand(() =>
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

        public int PatientScore
        {
            get
            {
                int score = 0;

                foreach (var item in zOBSOLETESwatScoreDatas)
                    if (item.PatientAnswer1)
                    {
                        score += 1;
                    }
                    else if (item.PatientAnswer2)
                    {
                        score += 2;
                    }
                    else if (item.PatientAnswer3)
                    {
                        score += 3;
                    }
                    else if (item.PatientAnswer4)
                    {
                        score += 4;
                    }
                    else if (item.PatientAnswer5)
                    {
                        score += 5;
                    }

                return score;
            }
        }

        public int ClinicianScore
        {
            get
            {
                int score = 0;

                foreach (var item in zOBSOLETESwatScoreDatas)
                    if (item.ClinicianAnswer1)
                    {
                        score += 1;
                    }
                    else if (item.ClinicianAnswer2)
                    {
                        score += 2;
                    }
                    else if (item.ClinicianAnswer3)
                    {
                        score += 3;
                    }
                    else if (item.ClinicianAnswer4)
                    {
                        score += 4;
                    }
                    else if (item.ClinicianAnswer5)
                    {
                        score += 5;
                    }

                return score;
            }
        }

        public RelayCommand SwatScoreCommand { get; set; }
        private string _PopupDataTemplate = "zOBSOLETESwatScorePopupDataTemplate";

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

        public zOBSOLETESwatScore(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SetupCommands();
        }

        public bool ValidatePopup()
        {
            bool AllValid = true;
            return AllValid; // Remove validation on SwatScore popup US 31793
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            _currentErrors.Clear();

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

            if (Required || (PatientScore != 0))
            {
                if (!ValidatePopup())
                {
                    _currentErrors.ForEach(e =>
                        {
                            e.Value.ForEach(error =>
                                {
                                    EncounterData.ValidationErrors.Add(new ValidationResult(error,
                                        new[] { "IntData" }));
                                    AllValid = false;
                                }
                            );
                        }
                    );
                }
                else
                {
                    if (Encounter.EncounterData.Contains(EncounterData) == false && EncounterData.IsNew)
                    {
                        Encounter.EncounterData.Add(EncounterData);
                    }
                }
            }

            return AllValid;
        }
    }

    public class zOBSOLETESwatScoreFactory
    {
        public static zOBSOLETESwatScore Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            zOBSOLETESwatScore qb = new zOBSOLETESwatScore(__FormSectionQuestionKey)
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
            EncounterData ed = vm.CurrentEncounter.EncounterData.Where(x =>
                x.EncounterKey == vm.CurrentEncounter.EncounterKey && x.SectionKey == formsection.Section.SectionKey &&
                x.QuestionGroupKey == qgkey && x.QuestionKey == q.QuestionKey).FirstOrDefault();
            if (ed == null)
            {
                ed = new EncounterData
                {
                    SectionKey = formsection.SectionKey.Value, QuestionGroupKey = qgkey, QuestionKey = q.QuestionKey,
                    BoolData = false
                };

                qb.EncounterData = ed;

                if (qb.IsNewEncounterOrSection &&
                    copyforward) // IsNewEncounterOrSection: why here and nowhere else??? because we don't add it to the encounter otherwise until FullValidation 
                {
                    qb.CopyForwardLastInstance();
                }

                // Add the row if something was copied forward.  Otherwise the OK of the popup will add the row.
                if (ed.IntData.HasValue || !String.IsNullOrEmpty(ed.TextData))
                {
                    qb.Encounter.EncounterData.Add(ed);
                }
            }
            else
            {
                qb.EncounterData = ed;
            }

            qb.EncounterData.PropertyChanged += qb.EncounterData_PropertyChanged;

            return qb;
        }
    }
}