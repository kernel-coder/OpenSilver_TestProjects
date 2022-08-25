#region Usings

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PainScale : QuestionUI
    {
        private EncounterPain _EncounterPain;

        public EncounterPain EncounterPain
        {
            get { return _EncounterPain; }
            set
            {
                if (value != _EncounterPain)
                {
                    if (_EncounterPain != null)
                    {
                        _EncounterPain.PropertyChanged -= this_EncounterPainPropertyChanged;
                    }

                    _EncounterPain = value;
                    if (_EncounterPain != null)
                    {
                        _EncounterPain.PropertyChanged += this_EncounterPainPropertyChanged;
                    }
                }
            }
        }

        public EncounterPain BackupEncounterPain;

        private int previousscore = -1;
        private int previoustarget = -1;

        public RelayCommand PainFaces_Command { get; protected set; }
        public RelayCommand PainFacesOKButton_Click { get; protected set; }
        public RelayCommand PainFacesCancelButton_Click { get; protected set; }

        public RelayCommand PainPAINAD_Command { get; protected set; }
        public RelayCommand PainPAINADOKButton_Click { get; protected set; }
        public RelayCommand PainPAINADCancelButton_Click { get; protected set; }

        public RelayCommand PainFLACC_Command { get; protected set; }
        public RelayCommand PainFLACCOKButton_Click { get; protected set; }
        public RelayCommand PainFLACCCancelButton_Click { get; protected set; }

        public PainScale(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            ProcessGoals = new RelayCommand(() =>
            {
                string response = string.Empty;
                string subresponse = string.Empty;
                string reason = string.Empty;
                bool remove = true;
                int? keytouse = null;

                //we now collect the target value so pass it allow to the goal
                response = reason = CodeLookupCache.GetCodeFromKey(EncounterPain.TargetPain);

                int codekey = string.IsNullOrWhiteSpace(EncounterPain.PainScale)
                    ? 0
                    : Int32.Parse(EncounterPain.PainScale);
                string scale = CodeLookupCache.GetCodeFromKey("PAINSCALE", codekey);

                if (scale == "10" && EncounterPain.PainScore10.HasValue)
                {
                    subresponse = CodeLookupCache.GetCodeFromKey(EncounterPain.PainScore10);
                }
                else if (scale == "FLACC" && EncounterPain.PainScoreFLACC.HasValue)
                {
                    subresponse = EncounterPain.PainScoreFLACC.ToString();
                }
                else if (scale == "FACES" && EncounterPain.PainScoreFACES.HasValue)
                {
                    subresponse = CodeLookupCache.GetCodeFromKey(EncounterPain.PainScoreFACES);
                }
                else if (scale == "PAINAD" && EncounterPain.PainScorePAINAD.HasValue)
                {
                    subresponse = CodeLookupCache.GetCodeFromKey(EncounterPain.PainScorePAINAD);
                }

                if (!string.IsNullOrEmpty(subresponse) && !string.IsNullOrEmpty(response))
                {
                    remove = false;
                }

                if ((!string.IsNullOrWhiteSpace(subresponse)) && (!string.IsNullOrWhiteSpace(response)) &&
                    (Convert.ToInt32(subresponse) != previousscore) && (Convert.ToInt32(response) != previoustarget))
                {
                    GoalManager.UpdateGoals(this, response, subresponse, reason, remove, keytouse);
                    previousscore = Convert.ToInt32(subresponse);
                    previoustarget = Convert.ToInt32(response);
                }
            });

            SetupCommands();
        }

        private void this_EncounterPainPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName.Equals("PainScale")) ||
                (e.PropertyName.Equals("PainScore10")) ||
                (e.PropertyName.Equals("PainScoreFLACC")) ||
                (e.PropertyName.Equals("PainScorePAINAD")) ||
                (e.PropertyName.Equals("PainScoreFACES")))
            {
                int codekey = string.IsNullOrWhiteSpace(EncounterPain.PainScale)
                    ? 0
                    : Int32.Parse(EncounterPain.PainScale);
                string scale = CodeLookupCache.GetCodeFromKey("PAINSCALE", codekey);

                // Check if we just saved (the derived properties goto null)
                if ((EncounterPain.PainScale != null) &&
                    (EncounterPain.PainScore != null) &&
                    (EncounterPain.PainScore10 == null) &&
                    (EncounterPain.PainScoreFLACC == null) &&
                    (EncounterPain.PainScorePAINAD == null) &&
                    (EncounterPain.PainScoreFACES == null))
                {
                    bool hasNoScore = string.IsNullOrWhiteSpace(EncounterPain.PainScore);
                    switch (scale)
                    {
                        case "10":
                            EncounterPain.PainScore10 =
                                CodeLookupCache.GetKeyFromCode("PAIN10", EncounterPain.PainScore);
                            break;
                        case "FLACC":
                            EncounterPain.PainScoreFLACC =
                                hasNoScore ? null : (int?)Int32.Parse(EncounterPain.PainScore);
                            break;
                        case "PAINAD":
                            EncounterPain.PainScorePAINAD =
                                hasNoScore ? null : (int?)Int32.Parse(EncounterPain.PainScore);
                            break;
                        case "FACES":
                            EncounterPain.PainScoreFACES =
                                CodeLookupCache.GetKeyFromCode("PAINFACES", EncounterPain.PainScore);
                            break;
                    }
                }

                if (e.PropertyName.Equals("PainScale"))
                {
                    if ((scale == "FLACC") && (EncounterPain.PainScoreFLACC == null))
                    {
                        PainFLACCPopup();
                    }
                    else if ((scale == "PAINAD") && (EncounterPain.PainScorePAINAD == null))
                    {
                        PainPAINADPopup();
                    }
                    else if ((scale == "FACES") && (EncounterPain.PainScoreFACES == null))
                    {
                        PainFacesPopup();
                    }
                }
            }

            if (DynamicFormViewModel != null && DynamicFormViewModel.CurrentOasisManager != null)
            {
                // Create a new EncounterData row that is not attached to anything and not saved to DB
                // This is used to conform to the (Question, EncounterData) function signature
                var ed = new EncounterData
                    { TextData = CodeLookupCache.GetCodeDescriptionFromKey(EncounterPain.PainScore10) };
                DynamicFormViewModel.CurrentOasisManager.HISQuestionChanged(Question, ed);
            }
        }

        private bool _IsFACES;

        public bool IsFACES
        {
            get { return _IsFACES; }
            set
            {
                if (_IsFACES != value)
                {
                    _IsFACES = value;
                    this.RaisePropertyChangedLambda(p => p.IsFACES);
                }
            }
        }

        private bool _IsFLACC;

        public bool IsFLACC
        {
            get { return _IsFLACC; }
            set
            {
                if (_IsFLACC != value)
                {
                    _IsFLACC = value;
                    this.RaisePropertyChangedLambda(p => p.IsFLACC);
                }
            }
        }

        private bool _IsPAINAD;

        public bool IsPAINAD
        {
            get { return _IsPAINAD; }
            set
            {
                if (_IsPAINAD != value)
                {
                    _IsPAINAD = value;
                    this.RaisePropertyChangedLambda(p => p.IsPAINAD);
                }
            }
        }

        private int? _prevPainScoreFACES;
        private int? _prevPainScoreFLACC;
        private int? _prevPainScoreFLACCActivity;
        private int? _prevPainScoreFLACCConsole;
        private int? _prevPainScoreFLACCCry;
        private int? _prevPainScoreFLACCFace;
        private int? _prevPainScoreFLACCLegs;

        private int? _prevPainScorePAINAD;
        private int? _prevPainScorePAINADBreathing;
        private int? _prevPainScorePAINADCry;
        private int? _prevPainScorePAINADFace;
        private int? _prevPainScorePAINADActivity;
        private int? _prevPainScorePAINADConsole;

        private void SetupCommands()
        {
            PainFaces_Command = new RelayCommand(
                () => { PainFacesPopup(); });
            PainFacesOKButton_Click = new RelayCommand(
                () =>
                {
                    ClearOtherPainIgnoring("FACES");
                    IsFACES = false;
                    ProcessGoals.Execute(null);
                });
            PainFacesCancelButton_Click = new RelayCommand(
                () =>
                {
                    EncounterPain.PainScoreFACES = _prevPainScoreFACES;
                    IsFACES = false;
                });
            PainFLACC_Command = new RelayCommand(
                () => { PainFLACCPopup(); });
            PainFLACCOKButton_Click = new RelayCommand(
                () =>
                {
                    EncounterPain.PainScoreFLACC =
                        (int)EncounterPain.Activity +
                        (int)EncounterPain.Console +
                        (int)EncounterPain.Cry +
                        (int)EncounterPain.Face +
                        (int)EncounterPain.Legs;

                    ClearOtherPainIgnoring("FLACC");
                    IsFLACC = false;
                    ProcessGoals.Execute(null);
                });
            PainFLACCCancelButton_Click = new RelayCommand(
                () =>
                {
                    EncounterPain.PainScoreFLACC = _prevPainScoreFLACC;
                    EncounterPain.Activity = _prevPainScoreFLACCActivity;
                    EncounterPain.Console = _prevPainScoreFLACCConsole;
                    EncounterPain.Cry = _prevPainScoreFLACCCry;
                    EncounterPain.Face = _prevPainScoreFLACCFace;
                    EncounterPain.Legs = _prevPainScoreFLACCLegs;
                    IsFLACC = false;
                });
            PainPAINAD_Command = new RelayCommand(
                () => { PainPAINADPopup(); });

            PainPAINADOKButton_Click = new RelayCommand(
                () =>
                {
                    EncounterPain.PainScorePAINAD =
                        (int)EncounterPain.Breathing +
                        (int)EncounterPain.Cry +
                        (int)EncounterPain.Face +
                        (int)EncounterPain.Activity +
                        (int)EncounterPain.Console;

                    ClearOtherPainIgnoring("PAINAD");
                    IsPAINAD = false;
                    ProcessGoals.Execute(null);
                });
            PainPAINADCancelButton_Click = new RelayCommand(
                () =>
                {
                    EncounterPain.PainScorePAINAD = _prevPainScorePAINAD;
                    EncounterPain.Breathing = _prevPainScorePAINADBreathing;
                    EncounterPain.Cry = _prevPainScorePAINADCry;
                    EncounterPain.Face = _prevPainScorePAINADFace;
                    EncounterPain.Activity = _prevPainScorePAINADActivity;
                    EncounterPain.Console = _prevPainScorePAINADConsole;

                    IsPAINAD = false;
                });
        }

        public void PainFLACCPopup()
        {
            _prevPainScoreFLACC = EncounterPain.PainScoreFLACC;
            _prevPainScoreFLACCActivity = EncounterPain.Activity;
            _prevPainScoreFLACCConsole = EncounterPain.Console;
            _prevPainScoreFLACCCry = EncounterPain.Cry;
            _prevPainScoreFLACCFace = EncounterPain.Face;
            _prevPainScoreFLACCLegs = EncounterPain.Legs;
            if (EncounterPain.Activity == null)
            {
                EncounterPain.Activity = 0;
            }

            if (EncounterPain.Console == null)
            {
                EncounterPain.Console = 0;
            }

            if (EncounterPain.Cry == null)
            {
                EncounterPain.Cry = 0;
            }

            if (EncounterPain.Face == null)
            {
                EncounterPain.Face = 0;
            }

            if (EncounterPain.Legs == null)
            {
                EncounterPain.Legs = 0;
            }

            IsFLACC = true;
        }

        public void PainPAINADPopup()
        {
            _prevPainScorePAINAD = EncounterPain.PainScorePAINAD;
            _prevPainScorePAINADBreathing = EncounterPain.Breathing;
            _prevPainScorePAINADCry = EncounterPain.Cry;
            _prevPainScorePAINADFace = EncounterPain.Face;
            _prevPainScorePAINADActivity = EncounterPain.Activity;
            _prevPainScorePAINADConsole = EncounterPain.Console;
            if (EncounterPain.Breathing == null)
            {
                EncounterPain.Breathing = 0;
            }

            if (EncounterPain.Cry == null)
            {
                EncounterPain.Cry = 0;
            }

            if (EncounterPain.Face == null)
            {
                EncounterPain.Face = 0;
            }

            if (EncounterPain.Activity == null)
            {
                EncounterPain.Activity = 0;
            }

            if (EncounterPain.Console == null)
            {
                EncounterPain.Console = 0;
            }

            IsPAINAD = true;
        }

        public void PainFacesPopup()
        {
            _prevPainScoreFACES = EncounterPain.PainScoreFACES;
            IsFACES = true;
        }

        void CopyProperties(EncounterPain source)
        {
            EncounterPain.PainScore10 = source.PainScore10;
            EncounterPain.PainScoreFLACC = source.PainScoreFLACC;
            EncounterPain.PainScorePAINAD = source.PainScorePAINAD;
            EncounterPain.Activity = source.Activity;
            EncounterPain.Console = source.Console;
            EncounterPain.Cry = source.Cry;
            EncounterPain.Face = source.Face;
            EncounterPain.Legs = source.Legs;
            EncounterPain.Breathing = source.Breathing;
            EncounterPain.PainScoreFACES = source.PainScoreFACES;
            EncounterPain.PainScore = source.PainScore;
            EncounterPain.PainScale = source.PainScale;
            EncounterPain.TargetPain = source.TargetPain;
        }

        public override bool CopyForwardLastInstance()
        {
            foreach (var item in Admission.Encounter.OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
            {
                EncounterPain previous = item.EncounterPain.FirstOrDefault();
                if (previous != null)
                {
                    CopyProperties(previous);
                    return true;
                }
            }

            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
            EncounterPain previous = e.EncounterPain.FirstOrDefault();
            if (previous != null)
            {
                CopyProperties(previous);
            }
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                var previous = (EncounterPain)Clone(BackupEncounterPain);
                //need to copy so raise property changes gets called - can't just copy the entire object
                CopyProperties(previous);
            }
            else
            {
                BackupEncounterPain = (EncounterPain)Clone(EncounterPain);
            }
        }

        public override bool Validate(out string SubSections)
        {
            EncounterPain.IsValidating = true;
            SubSections = string.Empty;

            int codekey = string.IsNullOrWhiteSpace(EncounterPain.PainScale) ? 0 : Int32.Parse(EncounterPain.PainScale);
            string scale = CodeLookupCache.GetCodeFromKey("PAINSCALE", codekey);

            EncounterPain.ValidationErrors.Clear();

            if ((ConditionalRequired || Required) && (string.IsNullOrEmpty(EncounterPain.PainScale)) &&
                (Encounter.FullValidation))
            {
                EncounterPain.ValidationErrors.Add(new ValidationResult("The Pain Assessment field is required",
                    new[] { "PainScale" }));
                EncounterPain.IsValidating = false;
                return false;
            }

            if (!string.IsNullOrEmpty(EncounterPain.PainScale))
            {
                if ((scale == "10") && (EncounterPain.PainScore10 == null))
                {
                    EncounterPain.ValidationErrors.Add(new ValidationResult("A Pain Score is required",
                        new[] { "PainScore10" }));
                    EncounterPain.IsValidating = false;
                    return false;
                }

                if ((scale == "FLACC") && (EncounterPain.PainScoreFLACC == null))
                {
                    EncounterPain.ValidationErrors.Add(new ValidationResult("A FLACC Evalualtion is required",
                        new[] { "PainScale" }));
                    EncounterPain.IsValidating = false;
                    return false;
                }

                if ((scale == "PAINAD") && (EncounterPain.PainScorePAINAD == null))
                {
                    EncounterPain.ValidationErrors.Add(new ValidationResult("A PAINAD Evaluation is required",
                        new[] { "PainScale" }));
                    EncounterPain.IsValidating = false;
                    return false;
                }

                if ((scale == "FACES") && (EncounterPain.PainScoreFACES == null))
                {
                    EncounterPain.ValidationErrors.Add(new ValidationResult("A FACES Evalualtion is required",
                        new[] { "PainScale" }));
                    EncounterPain.IsValidating = false;
                    return false;
                }

                ClearOtherPainIgnoring(scale);

                switch (scale)
                {
                    case "10":
                        EncounterPain.PainScore =
                            CodeLookupCache.GetCodeFromKey("PAIN10", (int)EncounterPain.PainScore10);
                        break;
                    case "FLACC":
                        EncounterPain.PainScore = EncounterPain.PainScoreFLACC.ToString();
                        break;
                    case "PAINAD":
                        EncounterPain.PainScore = EncounterPain.PainScorePAINAD.ToString();
                        break;
                    case "FACES":
                        EncounterPain.PainScore =
                            CodeLookupCache.GetCodeFromKey("PAINFACES", (int)EncounterPain.PainScoreFACES);
                        break;
                    default:
                        EncounterPain.PainScore = null;
                        break;
                }

                if (EncounterPain.IsNew)
                {
                    Encounter.EncounterPain.Add(EncounterPain);
                }

                previousscore = -1;
                previoustarget = -1;

                EncounterPain.IsValidating = false;
                return true;
            }

            if ((EncounterPain.EntityState == EntityState.Modified) || (EncounterPain.EntityState == EntityState.New))
            {
                Encounter.EncounterPain.Remove(EncounterPain);
                EncounterPain = new EncounterPain { EncounterKey = EncounterPain.EncounterKey };
            }

            EncounterPain.IsValidating = false;
            return true;
        }

        private void ClearOtherPainIgnoring(string SkipType)
        {
            if (SkipType != "10")
            {
                EncounterPain.PainScore10 = null;
            }

            if (SkipType != "FLACC" && SkipType != "PAINAD")
            {
                EncounterPain.Activity = null;
                EncounterPain.Console = null;
                EncounterPain.Cry = null;
                EncounterPain.Face = null;
            }

            if (SkipType != "FLACC")
            {
                EncounterPain.PainScoreFLACC = null;
                EncounterPain.Legs = null;
            }

            if (SkipType != "PAINAD")
            {
                EncounterPain.PainScorePAINAD = null;
                EncounterPain.Breathing = null;
            }

            if (SkipType != "FACES")
            {
                EncounterPain.PainScoreFACES = null;
            }
        }

        public override void Cleanup()
        {
            if (_EncounterPain != null)
            {
                _EncounterPain.PropertyChanged -= this_EncounterPainPropertyChanged;
            }

            base.Cleanup();
        }
    }

    public class PainScaleFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PainScale ps = new PainScale(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };

            EncounterPain ep = vm.CurrentEncounter.EncounterPain
                .Where(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey).FirstOrDefault();
            if (ep == null)
            {
                ep = new EncounterPain { EncounterKey = vm.CurrentEncounter.EncounterKey };
                ps.EncounterPain = ep;

                if (ps.Encounter.IsNew && copyforward)
                {
                    ps.CopyForwardLastInstance();
                }
            }
            else
            {
                ps.EncounterPain = ep;
            }

            ep.AdmissionKey = vm.CurrentAdmission.AdmissionKey;

            int codekey = string.IsNullOrWhiteSpace(ep.PainScale) ? 0 : Int32.Parse(ep.PainScale);
            string scale = CodeLookupCache.GetCodeFromKey("PAINSCALE", codekey);

            switch (scale)
            {
                case "10":
                    ep.PainScore10 = CodeLookupCache.GetKeyFromCode("PAIN10", ep.PainScore);
                    break;
                case "FLACC":
                    ep.PainScoreFLACC =
                        string.IsNullOrWhiteSpace(ep.PainScale) ? null : (int?)Int32.Parse(ep.PainScore);
                    break;
                case "PAINAD":
                    ep.PainScorePAINAD =
                        string.IsNullOrWhiteSpace(ep.PainScale) ? null : (int?)Int32.Parse(ep.PainScore);
                    break;
                case "FACES":
                    ep.PainScoreFACES = CodeLookupCache.GetKeyFromCode("PAINFACES", ep.PainScore);
                    break;
            }

            return ps;
        }
    }
}