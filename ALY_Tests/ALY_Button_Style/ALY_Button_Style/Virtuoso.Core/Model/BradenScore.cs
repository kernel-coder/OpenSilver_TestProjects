#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class BradenScore : QuestionBase
    {
        private bool[] sensoryPerception = new bool[4];
        private bool[] moisture = new bool[4];
        private bool[] activity = new bool[4];
        private bool[] mobility = new bool[4];
        private bool[] nutrition = new bool[4];
        private bool[] friction = new bool[3];

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

            base.Cleanup();
        }

        public override void PreProcessing()
        {
            if ((EncounterData != null) && (EncounterData.IntData != null) && (EncounterData.IsNew) &&
                (OasisManager != null))
            {
                OasisManager.BradenScoreOasisMappingChanged(Question, EncounterData);
            }
        }

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

        private bool isTransferForm
        {
            get
            {
                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsTransfer == false))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsDischargeOrTransfer => (isDischargeForm || isTransferForm);

        public ObservableCollection<BradenScoreInfo> SensoryPerceptionList => sensoryPerceptionList;

        private ObservableCollection<BradenScoreInfo> sensoryPerceptionList =
            new ObservableCollection<BradenScoreInfo>();

        public ObservableCollection<BradenScoreInfo> MoistureList => moistureList;
        private ObservableCollection<BradenScoreInfo> moistureList = new ObservableCollection<BradenScoreInfo>();

        public ObservableCollection<BradenScoreInfo> ActivityList => activityList;
        private ObservableCollection<BradenScoreInfo> activityList = new ObservableCollection<BradenScoreInfo>();

        public BradenScoreInfo SelectedMoisture
        {
            get { return selectedMoisture; }
            set
            {
                selectedMoisture = value;
                RaisePropertyChanged("SelectedMoisture");
            }
        }

        private BradenScoreInfo selectedMoisture;

        public void SetupCommands()
        {
            BradenScoreCommand = new RelayCommand(() =>
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
                SetupData();
                DynamicFormViewModel.PopupDataContext = this;
            });
            BradenInfoCommand = new RelayCommand(() =>
            {
                BradenProtocolsByLevelOfRisk d = new BradenProtocolsByLevelOfRisk();
                d.Show();
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
                    int score = Score;
                    if (EncounterData.IntData != score)
                    {
                        if (!Encounter.EncounterData.Any(ed => ed.EncounterDataKey == EncounterData.EncounterDataKey))
                        {
                            Encounter.EncounterData.Add(EncounterData);
                        }

                        EncounterData.IntData = score;
                        EncounterData.TextData = GetScore(sensoryPerception) + "|"
                                                                             + GetScore(moisture) + "|"
                                                                             + GetScore(activity) + "|"
                                                                             + GetScore(mobility) + "|"
                                                                             + GetScore(nutrition) + "|"
                                                                             + GetScore(friction);
                    }

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
                SetupData();
            });
        }

        public void SetupData()
        {
            if ((EncounterData != null) && (!string.IsNullOrEmpty(EncounterData.TextData)))
            {
                string[] textData = EncounterData.TextData.Split('|');
                for (int i = 0; i < textData.Length; i++)
                {
                    bool[] myArray = null;
                    int myScore = 0;

                    switch (i)
                    {
                        case 0:
                            myArray = sensoryPerception;
                            break;
                        case 1:
                            myArray = moisture;
                            break;
                        case 2:
                            myArray = activity;
                            break;
                        case 3:
                            myArray = mobility;
                            break;
                        case 4:
                            myArray = nutrition;
                            break;
                        case 5:
                            myArray = friction;
                            break;
                    }

                    if (int.TryParse(textData[i], out myScore))
                    {
                        SetValue(myArray, myScore - 1, true);
                    }
                }
            }

            RaisePropertyChanged("SensoryPerception1");
            RaisePropertyChanged("SensoryPerception2");
            RaisePropertyChanged("SensoryPerception3");
            RaisePropertyChanged("SensoryPerception4");
            RaisePropertyChanged("Moisture1");
            RaisePropertyChanged("Moisture2");
            RaisePropertyChanged("Moisture3");
            RaisePropertyChanged("Moisture4");
            RaisePropertyChanged("Activity1");
            RaisePropertyChanged("Activity2");
            RaisePropertyChanged("Activity3");
            RaisePropertyChanged("Activity4");
            RaisePropertyChanged("Mobility1");
            RaisePropertyChanged("Mobility2");
            RaisePropertyChanged("Mobility3");
            RaisePropertyChanged("Mobility4");
            RaisePropertyChanged("Nutrition1");
            RaisePropertyChanged("Nutrition2");
            RaisePropertyChanged("Nutrition3");
            RaisePropertyChanged("Nutrition4");
            RaisePropertyChanged("Friction1");
            RaisePropertyChanged("Friction2");
            RaisePropertyChanged("Friction3");
        }

        public int Score
        {
            get
            {
                int score = 0;

                score = GetScore(sensoryPerception)
                        + GetScore(moisture)
                        + GetScore(activity)
                        + GetScore(mobility)
                        + GetScore(nutrition)
                        + GetScore(friction);

                return score;
            }
        }

        private int GetScore(bool[] array)
        {
            int score = 0;
            for (int i = 0; i < array.Length; i++)
                if (array[i])
                {
                    score = i + 1;
                    break;
                }

            return score;
        }

        public bool SensoryPerception1
        {
            get { return GetValue(sensoryPerception, 0); }
            set
            {
                SetValue(sensoryPerception, 0, value);
                RaisePropertyChanged("SensoryPerception1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("SensoryPerception1");
                }
            }
        }

        public bool SensoryPerception2
        {
            get { return GetValue(sensoryPerception, 1); }
            set
            {
                SetValue(sensoryPerception, 1, value);
                RaisePropertyChanged("SensoryPerception2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("SensoryPerception1");
                }
            }
        }

        public bool SensoryPerception3
        {
            get { return GetValue(sensoryPerception, 2); }
            set
            {
                SetValue(sensoryPerception, 2, value);
                RaisePropertyChanged("SensoryPerception3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("SensoryPerception1");
                }
            }
        }


        public bool SensoryPerception4
        {
            get { return GetValue(sensoryPerception, 3); }
            set
            {
                SetValue(sensoryPerception, 3, value);
                RaisePropertyChanged("SensoryPerception4");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("SensoryPerception1");
                }
            }
        }

        public bool Moisture1
        {
            get { return GetValue(moisture, 0); }
            set
            {
                SetValue(moisture, 0, value);
                RaisePropertyChanged("Moisture1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Moisture1");
                }
            }
        }

        public bool Moisture2
        {
            get { return GetValue(moisture, 1); }
            set
            {
                SetValue(moisture, 1, value);
                RaisePropertyChanged("Moisture2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Moisture1");
                }
            }
        }

        public bool Moisture3
        {
            get { return GetValue(moisture, 2); }
            set
            {
                SetValue(moisture, 2, value);
                RaisePropertyChanged("Moisture3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Moisture1");
                }
            }
        }

        public bool Moisture4
        {
            get { return GetValue(moisture, 3); }
            set
            {
                SetValue(moisture, 3, value);
                RaisePropertyChanged("Moisture4");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Moisture1");
                }
            }
        }

        public bool Activity1
        {
            get { return GetValue(activity, 0); }
            set
            {
                SetValue(activity, 0, value);
                RaisePropertyChanged("Activity1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Activity1");
                }
            }
        }

        public bool Activity2
        {
            get { return GetValue(activity, 1); }
            set
            {
                SetValue(activity, 1, value);
                RaisePropertyChanged("Activity2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Activity1");
                }
            }
        }

        public bool Activity3
        {
            get { return GetValue(activity, 2); }
            set
            {
                SetValue(activity, 2, value);
                RaisePropertyChanged("Activity3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Activity1");
                }
            }
        }

        public bool Activity4
        {
            get { return GetValue(activity, 3); }
            set
            {
                SetValue(activity, 3, value);
                RaisePropertyChanged("Activity4");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Activity1");
                }
            }
        }

        public bool Mobility1
        {
            get { return GetValue(mobility, 0); }
            set
            {
                SetValue(mobility, 0, value);
                RaisePropertyChanged("Mobility1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Mobility1");
                }
            }
        }

        public bool Mobility2
        {
            get { return GetValue(mobility, 1); }
            set
            {
                SetValue(mobility, 1, value);
                RaisePropertyChanged("Mobility2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Mobility1");
                }
            }
        }

        public bool Mobility3
        {
            get { return GetValue(mobility, 2); }
            set
            {
                SetValue(mobility, 2, value);
                RaisePropertyChanged("Mobility3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Mobility1");
                }
            }
        }

        public bool Mobility4
        {
            get { return GetValue(mobility, 3); }
            set
            {
                SetValue(mobility, 3, value);
                RaisePropertyChanged("Mobility4");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Mobility1");
                }
            }
        }

        public bool Nutrition1
        {
            get { return GetValue(nutrition, 0); }
            set
            {
                SetValue(nutrition, 0, value);
                RaisePropertyChanged("Nutrition1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Nutrition1");
                }
            }
        }

        public bool Nutrition2
        {
            get { return GetValue(nutrition, 1); }
            set
            {
                SetValue(nutrition, 1, value);
                RaisePropertyChanged("Nutrition2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Nutrition1");
                }
            }
        }

        public bool Nutrition3
        {
            get { return GetValue(nutrition, 2); }
            set
            {
                SetValue(nutrition, 2, value);
                RaisePropertyChanged("Nutrition3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Nutrition1");
                }
            }
        }

        public bool Nutrition4
        {
            get { return GetValue(nutrition, 3); }
            set
            {
                SetValue(nutrition, 3, value);
                RaisePropertyChanged("Nutrition4");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Nutrition1");
                }
            }
        }

        public bool Friction1
        {
            get { return GetValue(friction, 0); }
            set
            {
                SetValue(friction, 0, value);
                RaisePropertyChanged("Friction1");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Friction1");
                }
            }
        }

        public bool Friction2
        {
            get { return GetValue(friction, 1); }
            set
            {
                SetValue(friction, 1, value);
                RaisePropertyChanged("Friction2");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Friction1");
                }
            }
        }

        public bool Friction3
        {
            get { return GetValue(friction, 2); }
            set
            {
                SetValue(friction, 2, value);
                RaisePropertyChanged("Friction3");
                RaisePropertyChanged("Score");
                if (value)
                {
                    ClearErrorFromProperty("Friction1");
                }
            }
        }

        public bool GetValue(bool[] array, int index)
        {
            bool value = false;

            if (array.Length > index)
            {
                value = array[index];
            }

            return value;
        }

        public void SetValue(bool[] array, int index, bool value)
        {
            if (value)
            {
                // if we're setting the value to true, set the value of the the item that matches the index to true and everything else to false
                for (int i = 0; i < array.Length; i++) array[i] = (i == index);
            }
            else
            {
                // if we're setting the value to false, just set the value of the item that matches the index to false
                if (array.Length > index)
                {
                    array[index] = value;
                }
            }
        }

        public RelayCommand BradenInfoCommand { get; set; }
        public RelayCommand BradenScoreCommand { get; set; }
        private string _PopupDataTemplate = "BradenScorePopupDataTemplate";

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

        public BradenScore(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            SetupCommands();
        }

        private bool ValidatePopup()
        {
            bool AllValid = true;

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

            if (GetScore(sensoryPerception) == 0)
            {
                AllValid = false;
                AddErrorForProperty("SensoryPerception1", "A Sensory Preception value must be selected");
            }

            if (GetScore(moisture) == 0)
            {
                AllValid = false;
                AddErrorForProperty("Moisture1", "A Moisture value must be selected");
            }

            if (GetScore(activity) == 0)
            {
                AllValid = false;
                AddErrorForProperty("Activity1", "An Activity value must be selected");
            }

            if (GetScore(mobility) == 0)
            {
                AllValid = false;
                AddErrorForProperty("Mobility1", "A Mobility value must be selected");
            }

            if (GetScore(nutrition) == 0)
            {
                AllValid = false;
                AddErrorForProperty("Nutrition1", "A Nutrition value must be selected");
            }

            if (GetScore(friction) == 0)
            {
                AllValid = false;
                AddErrorForProperty("Friction1", "A Friction & Shear value must be selected");
            }

            return AllValid;
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

            if (IsDischargeOrTransfer)
            {
                return AllValid; // because cannot edit and already copied forward if need be
            }

            if (Required || (Score != 0))
            {
                if (!ValidatePopup())
                {
                    _currentErrors.ForEach(e =>
                    {
                        e.Value.ForEach(error =>
                        {
                            EncounterData.ValidationErrors.Add(new ValidationResult(error, new[] { "IntData" }));
                            AllValid = false;
                        });
                    });
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

        public class BradenScoreInfo : INotifyPropertyChanged
        {
            public int? Score
            {
                get { return score; }
                set
                {
                    score = value;
                    NotifyPropertyChanged("Score");
                }
            }

            private int? score;

            public string Header
            {
                get { return header; }
                set
                {
                    header = value;
                    NotifyPropertyChanged("Header");
                }
            }

            private string header;

            public string Text
            {
                get { return text; }
                set
                {
                    text = value;
                    NotifyPropertyChanged("Text");
                }
            }

            private string text;

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String property)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
                }
            }

            #endregion
        }
    }

    public class BradenScoreFactory
    {
        public static BradenScore Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            BradenScore qb = new BradenScore(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                //FormModel = m,
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
            // set EncounterData into factory (otherwise null reference exception against EncounterData in BackupEntity
            else
            {
                qb.EncounterData = ed;
            }

            qb.SetupData();
            qb.EncounterData.PropertyChanged += qb.EncounterData_PropertyChanged;

            return qb;
        }
    }
}