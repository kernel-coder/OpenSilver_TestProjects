#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Risk : QuestionUI
    {
        private RiskAssessment _RiskAssessment;

        public RiskAssessment RiskAssessment
        {
            get { return _RiskAssessment; }
            set
            {
                _RiskAssessment = value;
                RaisePropertyChanged("RiskAssessment");
            }
        }

        private IEnumerable<RiskAssessmentLayout> _RiskAssessmentLayout;

        public IEnumerable<RiskAssessmentLayout> RiskAssessmentLayout
        {
            get { return _RiskAssessmentLayout; }
            set
            {
                _RiskAssessmentLayout = value;
                RaisePropertyChanged("RiskAssessmentLayout");
            }
        }

        private bool _IsBereavementRiskAssessment;

        public bool IsBereavementRiskAssessment
        {
            get { return _IsBereavementRiskAssessment; }
            set
            {
                _IsBereavementRiskAssessment = value;
                RaisePropertyChanged("IsBereavementRiskAssessment");
            }
        }

        private Guid? _RiskForID;

        public Guid? RiskForID
        {
            get { return _RiskForID; }
            set
            {
                _RiskForID = value;
                RaisePropertyChanged("RiskForID");
            }
        }

        private EncounterRisk _TotalRecord;

        public EncounterRisk TotalRecord
        {
            get { return _TotalRecord; }
            set
            {
                _TotalRecord = value;
                RaisePropertyChanged("TotalRecord");
            }
        }

        private RiskRange _CurrentRiskRange;

        public RiskRange CurrentRiskRange
        {
            get { return _CurrentRiskRange; }
            set
            {
                _CurrentRiskRange = value;
                this.RaisePropertyChangedLambda(p => p.CurrentRiskRange);
            }
        }

        private string _ValidationMessage;

        public string ValidationMessage
        {
            get { return _ValidationMessage; }
            set
            {
                _ValidationMessage = value;
                RaisePropertyChanged("ValidationMessage");
            }
        }


        public RelayCommand<EncounterRisk> UpdateRiskTotalRecord { get; set; }

        public void EncounterRisk_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Score")
            {
                var s = sender as EncounterRisk;

                if (s != null && OasisManager != null)
                {
                    var ra = DynamicFormCache.GetRiskAssessmentByKey(s.RiskAssessmentKey);

                    if (ra.Label != null && ra.Label.ToLower() == "mahc-10 fall risk assessment tool")
                    {
                        OasisManager.RiskOasisMappingChanged(Question, s);
                    }
                }
            }
        }

        public Risk(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            UpdateRiskTotalRecord = new RelayCommand<EncounterRisk>(er =>
            {
                er.Score = RiskAssessmentLayout.Where(p => p.RiskQuestion != null)
                               .Sum(p => p.CurrentEncounterRisk.Score.Value) +
                           RiskAssessmentLayout.Where(p => p.RiskGroup != null).Sum(p =>
                               p.RiskGroup.RiskGroupQuestion.Sum(x => x.CurrentEncounterRisk.Score));

                foreach (var rg in RiskAssessmentLayout.Where(x =>
                             x.RiskGroup != null && x.RiskGroup.SubTotalRecord != null))
                    rg.RiskGroup.SubTotalRecord.Score =
                        rg.RiskGroup.RiskGroupQuestion.Sum(p => p.CurrentEncounterRisk.Score);

                CurrentRiskRange = DynamicFormCache.GetRiskRangeByKeyandScore(er.RiskAssessmentKey, er.Score.Value);
                if (CurrentRiskRange != null)
                {
                    er.RiskRangeKey = CurrentRiskRange.RiskRangeKey;
                }
            });
        }

        public override void Cleanup()
        {
            try
            {
                TotalRecord.PropertyChanged -= EncounterRisk_PropertyChanged;
            }
            catch (Exception)
            {
            }

            base.Cleanup();
        }

        // Need 2 levels of save incase - popup RiskAssessment is called from Re-Eval section
        EncounterRisk SaveTotalRecord = new EncounterRisk();
        EncounterRisk Save2TotalRecord = new EncounterRisk();

        public void SaveRiskAssessmentEncounterRisks()
        {
            SaveTotalRecord.CopyFrom(TotalRecord);

            foreach (var ral in RiskAssessmentLayout)
                if (ral.RiskQuestionKey.HasValue)
                {
                    ral.SaveCurrentEncounterRisk = new EncounterRisk();
                    if (ral.CurrentEncounterRisk != null)
                    {
                        ral.SaveCurrentEncounterRisk.CopyFrom(ral.CurrentEncounterRisk as EncounterRisk);
                    }
                }
                else if (ral.RiskGroupKey.HasValue)
                {
                    foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                    {
                        rgq.SaveCurrentEncounterRisk = new EncounterRisk();
                        if (rgq.CurrentEncounterRisk != null)
                        {
                            rgq.SaveCurrentEncounterRisk.CopyFrom(rgq.CurrentEncounterRisk as EncounterRisk);
                        }
                    }
                }
        }

        public void RestoreRiskAssessmentEncounterRisks()
        {
            TotalRecord.CopyFrom(SaveTotalRecord);

            foreach (var ral in RiskAssessmentLayout)
                if (ral.RiskQuestionKey.HasValue)
                {
                    if (ral.SaveCurrentEncounterRisk != null)
                    {
                        ral.CurrentEncounterRisk.CopyFrom(ral.SaveCurrentEncounterRisk);
                    }
                }
                else if (ral.RiskGroupKey.HasValue)
                {
                    foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                        if (rgq.SaveCurrentEncounterRisk != null)
                        {
                            rgq.CurrentEncounterRisk.CopyFrom(rgq.SaveCurrentEncounterRisk);
                        }
                }

            RaisePropertyChanged("RiskAssessmentLayout");
            RaisePropertyChanged("TotalRecord");
        }

        public void SaveRiskAssessmentEncounterRisks2()
        {
            Save2TotalRecord.CopyFrom(TotalRecord);
            if (TotalRecord != null) // to get the comment block scrollbar back to the top
            {
                string c = TotalRecord.Comment;
                TotalRecord.Comment = null;
                TotalRecord.Comment = c;
            }

            foreach (var ral in RiskAssessmentLayout)
                if (ral.RiskQuestionKey.HasValue)
                {
                    ral.Save2CurrentEncounterRisk = new EncounterRisk();
                    if (ral.CurrentEncounterRisk != null)
                    {
                        ral.Save2CurrentEncounterRisk.CopyFrom(ral.CurrentEncounterRisk as EncounterRisk);
                    }
                }
                else if (ral.RiskGroupKey.HasValue)
                {
                    foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                    {
                        rgq.Save2CurrentEncounterRisk = new EncounterRisk();
                        if (rgq.CurrentEncounterRisk != null)
                        {
                            rgq.Save2CurrentEncounterRisk.CopyFrom(rgq.CurrentEncounterRisk as EncounterRisk);
                        }
                    }
                }
        }

        public void RestoreRiskAssessmentEncounterRisks2()
        {
            ValidationMessage = null;
            TotalRecord.CopyFrom(Save2TotalRecord);

            foreach (var ral in RiskAssessmentLayout)
                if (ral.RiskQuestionKey.HasValue)
                {
                    if (ral.Save2CurrentEncounterRisk != null)
                    {
                        ral.CurrentEncounterRisk.CopyFrom(ral.Save2CurrentEncounterRisk);
                    }
                }
                else if (ral.RiskGroupKey.HasValue)
                {
                    foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                        if (rgq.Save2CurrentEncounterRisk != null)
                        {
                            rgq.CurrentEncounterRisk.CopyFrom(rgq.Save2CurrentEncounterRisk);
                        }
                }

            RaisePropertyChanged("RiskAssessmentLayout");
            RaisePropertyChanged("TotalRecord");
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
                RestoreRiskAssessmentEncounterRisks();
            }
            else
            {
                SaveRiskAssessmentEncounterRisks();
            }
        }

        public bool ValidateRequireResponseOnPopup()
        {
            ValidationMessage = null;
            if (isValid)
            {
                return true;
            }

            ValidationMessage = areSections
                ? "At least one response must be selected in each section"
                : "At least one response must be checked.";
            return false;
        }

        private bool areSections =>
            ((RiskAssessmentLayout == null) || (RiskAssessmentLayout.Any() == false)) ? false : true;

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            bool AllValid = true;
            if (Encounter.FullValidation && (Required || (ConditionalRequired && OasisManager != null &&
                                                          OasisManager.IsOasisActive &&
                                                          (!OasisManager.CurrentEncounterOasis.BypassFlag.HasValue ||
                                                           !OasisManager.CurrentEncounterOasis.BypassFlag.Value))))
            {
                AllValid = isValid;
            }

            if (AllValid && TotalRecord.IsNew && TotalRecord.Score.HasValue)
            {
                Encounter.EncounterRisk.Add(TotalRecord);

                foreach (var ral in RiskAssessmentLayout)
                    if (ral.RiskQuestionKey.HasValue)
                    {
                        Encounter.EncounterRisk.Add(ral.CurrentEncounterRisk as EncounterRisk);
                    }
                    else if (ral.RiskGroupKey.HasValue)
                    {
                        foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                            Encounter.EncounterRisk.Add(rgq.CurrentEncounterRisk as EncounterRisk);
                    }
            }

            return AllValid;
        }

        public bool isValid
        {
            get
            {
                if ((Hidden) || (Protected))
                {
                    return true;
                }

                bool ExclusiveSelected = (RiskAssessmentLayout.Where(p =>
                    p.RiskQuestion != null && p.Exclusive && p.CurrentEncounterRisk.IsSelected).Any());
                if (!ExclusiveSelected)
                {
                    if (RiskAssessmentLayout.Where(p => p.RiskQuestion != null && !p.Exclusive).Any())
                    {
                        if (!RiskAssessmentLayout
                                .Where(p => p.RiskQuestion != null && p.CurrentEncounterRisk.IsSelected).Any())
                        {
                            return false;
                        }
                    }

                    foreach (var rg in RiskAssessmentLayout.Where(p => p.RiskGroup != null))
                        if (!rg.RiskGroup.RiskGroupQuestion.Where(p => p.CurrentEncounterRisk.IsSelected).Any())
                        {
                            return false;
                        }
                }

                return true;
            }
        }
    }

    public class RiskFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterRisk mostRecentQER = null;

            EncounterRisk er = vm.CurrentEncounter.EncounterRisk.Where(x =>
                x.RiskForID == vm.RiskForID && x.IsTotal && x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                x.RiskAssessmentKey == formsection.RiskAssessmentKey && !x.RiskGroupKey.HasValue).FirstOrDefault();

            Encounter mostRecentEncounter = null;
            if (er == null)
            {
                er = new EncounterRisk
                {
                    RiskForID = vm.RiskForID, RiskAssessmentKey = formsection.RiskAssessmentKey.Value, IsTotal = true
                };

                foreach (Encounter ee in vm.CurrentAdmission.Encounter
                             .Where(e => e.EncounterStatus !=
                                         (int)EncounterStatusType
                                             .None) //Exclude Encounters that are place holders for Tasks that haven't been started yet
                             .Where(p => !p.IsNew).OrderByDescending(p => p.EncounterOrTaskStartDateAndTime))
                {
                    mostRecentQER = ee.EncounterRisk.Where(x =>
                            x.RiskForID == vm.RiskForID && x.IsTotal && x.EncounterKey == ee.EncounterKey &&
                            x.RiskAssessmentKey == formsection.RiskAssessmentKey && !x.RiskGroupKey.HasValue)
                        .FirstOrDefault();
                    if (mostRecentQER != null)
                    {
                        mostRecentEncounter = ee;
                        if (vm.IsBereavementRiskAssessment)
                        {
                            er.Comment = mostRecentQER.Comment;
                            er.CodeLookupKey = mostRecentQER.CodeLookupKey;
                            er.IsSelected = mostRecentQER.IsSelected;
                            er.RiskRangeKey = mostRecentQER.RiskRangeKey;
                            er.Score = mostRecentQER.Score;
                        }

                        break;
                    }
                }
            }

            RiskAssessment cachedRisk = DynamicFormCache.GetRiskAssessmentByKey(formsection.RiskAssessmentKey.Value);
            RiskAssessment clonedRisk = cachedRisk.Clone();
            IEnumerable<RiskAssessmentLayout> rals = (clonedRisk.RiskAssessmentLayout == null)
                ? null
                : clonedRisk.RiskAssessmentLayout.OrderBy(x => x.Sequence).ToList();

            bool requiredWhenOASIS = formsection.RequiredWhenOASIS;
            if ((vm != null) && (vm.CurrentEncounter != null) && (vm.CurrentEncounter.SYS_CDIsHospice))
            {
                requiredWhenOASIS = false; // override - forcing fnot required when HIS
            }

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Risk r = new Risk(__FormSectionQuestionKey)
            {
                IsBereavementRiskAssessment = vm.IsBereavementRiskAssessment,
                RiskForID = vm.RiskForID,
                RiskAssessment = clonedRisk,
                RiskAssessmentLayout = rals,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                TotalRecord = er,
                Required = formsection.Required,
                ConditionalRequired = requiredWhenOASIS,
                OasisManager = vm.CurrentOasisManager,
            };
            if ((r.IsBereavementRiskAssessment) && (er.IsNew) && (er.Score == null))
            {
                er.Score = 0;
            }

            if (((r.IsBereavementRiskAssessment) || (!er.IsNew)) && (er.Score != null))
            {
                r.CurrentRiskRange = DynamicFormCache.GetRiskRangeByKeyandScore(er.RiskAssessmentKey, er.Score.Value);
            }

            foreach (var ral in rals)
                if (ral.RiskQuestionKey.HasValue)
                {
                    ral.RiskLayoutValueChanged = new RelayCommand<RiskAssessmentLayout>(rq =>
                    {
                        if (rq.CurrentEncounterRisk.IsSelected)
                        {
                            rq.CurrentEncounterRisk.Score = rq.TrueValue;
                        }
                        else
                        {
                            rq.CurrentEncounterRisk.Score = rq.FalseValue;
                        }

                        if (rq.CurrentEncounterRisk.IsSelected && rq.Exclusive)
                        {
                            foreach (var item in r.RiskAssessmentLayout.Where(p =>
                                         p.RiskAssessmentLayoutKey != rq.RiskAssessmentLayoutKey))
                                if (item.RiskQuestionKey.HasValue)
                                {
                                    item.CurrentEncounterRisk.IsSelected = false;
                                    item.CurrentEncounterRisk.Score = item.FalseValue;
                                }
                                else if (item.RiskGroupKey.HasValue)
                                {
                                    foreach (var subitem in item.RiskGroup.RiskGroupQuestion)
                                    {
                                        subitem.CurrentEncounterRisk.IsSelected = false;
                                        subitem.CurrentEncounterRisk.Score = subitem.FalseValue;
                                    }
                                }
                        }
                        else if (rq.CurrentEncounterRisk.IsSelected && !rq.Exclusive)
                        {
                            foreach (var item in r.RiskAssessmentLayout.Where(p =>
                                         p.Exclusive && p.RiskAssessmentLayoutKey != rq.RiskAssessmentLayoutKey))
                                if (item.RiskQuestionKey.HasValue)
                                {
                                    item.CurrentEncounterRisk.IsSelected = false;
                                    item.CurrentEncounterRisk.Score = item.FalseValue;
                                }
                                else if (item.RiskGroupKey.HasValue)
                                {
                                    foreach (var subitem in item.RiskGroup.RiskGroupQuestion.Where(p => p.Exclusive))
                                    {
                                        subitem.CurrentEncounterRisk.IsSelected = false;
                                        subitem.CurrentEncounterRisk.Score = subitem.FalseValue;
                                    }
                                }
                        }


                        r.UpdateRiskTotalRecord.Execute(r.TotalRecord);
                    });

                    EncounterRisk qer = vm.CurrentEncounter.EncounterRisk.Where(x => x.RiskForID == vm.RiskForID &&
                        x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                        x.RiskAssessmentKey == ral.RiskAssessmentKey &&
                        x.RiskQuestionKey == ral.RiskQuestionKey).FirstOrDefault();
                    if (qer == null)
                    {
                        qer = new EncounterRisk
                        {
                            RiskForID = vm.RiskForID, RiskAssessmentKey = ral.RiskAssessmentKey,
                            RiskQuestionKey = ral.RiskQuestionKey, Score = ral.FalseValue
                        };
                        if (vm.IsBereavementRiskAssessment && (mostRecentEncounter != null))
                        {
                            mostRecentQER = mostRecentEncounter.EncounterRisk.Where(x => x.RiskForID == vm.RiskForID &&
                                x.EncounterKey == mostRecentEncounter.EncounterKey &&
                                x.RiskAssessmentKey == ral.RiskAssessmentKey &&
                                x.RiskQuestionKey == ral.RiskQuestionKey).FirstOrDefault();
                            if (mostRecentQER != null)
                            {
                                qer.Comment = mostRecentQER.Comment;
                                qer.CodeLookupKey = mostRecentQER.CodeLookupKey;
                                qer.IsSelected = mostRecentQER.IsSelected;
                                qer.RiskRangeKey = mostRecentQER.RiskRangeKey;
                                qer.Score = mostRecentQER.Score;
                            }
                        }
                    }

                    ral.CurrentEncounterRisk = qer;
                }
                else if (ral.RiskGroupKey.HasValue)
                {
                    foreach (var rgq in ral.RiskGroup.RiskGroupQuestion)
                    {
                        rgq.RiskGroupValueChanged = new RelayCommand<RiskGroupQuestion>(rq =>
                        {
                            if (rq.CurrentEncounterRisk.IsSelected)
                            {
                                rq.CurrentEncounterRisk.Score = rq.TrueValue;
                            }
                            else
                            {
                                rq.CurrentEncounterRisk.Score = rq.FalseValue;
                            }

                            if (rq.CurrentEncounterRisk.IsSelected && rq.Exclusive)
                            {
                                foreach (var item in rgq.RiskGroup.RiskGroupQuestion.Where(p =>
                                             p.RiskGroupQuestionKey != rq.RiskGroupQuestionKey))
                                {
                                    item.CurrentEncounterRisk.IsSelected = false;
                                    item.CurrentEncounterRisk.Score = item.FalseValue;
                                }
                            }
                            else if (rq.CurrentEncounterRisk.IsSelected && !rq.Exclusive)
                            {
                                foreach (var item in r.RiskAssessmentLayout.Where(p =>
                                             p.Exclusive && p.RiskQuestionKey.HasValue))
                                {
                                    item.CurrentEncounterRisk.IsSelected = false;
                                    item.CurrentEncounterRisk.Score = item.FalseValue;
                                }

                                foreach (var item in rgq.RiskGroup.RiskGroupQuestion.Where(p =>
                                             p.Exclusive && p.RiskGroupQuestionKey != rq.RiskGroupQuestionKey))
                                {
                                    item.CurrentEncounterRisk.IsSelected = false;
                                    item.CurrentEncounterRisk.Score = item.FalseValue;
                                }
                            }

                            r.UpdateRiskTotalRecord.Execute(r.TotalRecord);
                        });

                        EncounterRisk qer = vm.CurrentEncounter.EncounterRisk.Where(x => x.RiskForID == vm.RiskForID &&
                            x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                            x.RiskAssessmentKey == ral.RiskAssessmentKey &&
                            x.RiskGroupKey == rgq.RiskGroupKey &&
                            x.RiskQuestionKey == rgq.RiskQuestionKey).FirstOrDefault();
                        if (qer == null)
                        {
                            qer = new EncounterRisk
                            {
                                RiskForID = vm.RiskForID,
                                RiskAssessmentKey = ral.RiskAssessmentKey,
                                RiskGroupKey = rgq.RiskGroupKey,
                                RiskQuestionKey = rgq.RiskQuestionKey,
                                Score = rgq.FalseValue
                            };
                            if (vm.IsBereavementRiskAssessment && (mostRecentEncounter != null))
                            {
                                mostRecentQER = mostRecentEncounter.EncounterRisk.Where(x =>
                                    x.RiskForID == vm.RiskForID && x.EncounterKey == mostRecentEncounter.EncounterKey &&
                                    x.RiskAssessmentKey == ral.RiskAssessmentKey &&
                                    x.RiskGroupKey == rgq.RiskGroupKey &&
                                    x.RiskQuestionKey == rgq.RiskQuestionKey).FirstOrDefault();
                                if (mostRecentQER != null)
                                {
                                    qer.Comment = mostRecentQER.Comment;
                                    qer.CodeLookupKey = mostRecentQER.CodeLookupKey;
                                    qer.IsSelected = mostRecentQER.IsSelected;
                                    qer.RiskRangeKey = mostRecentQER.RiskRangeKey;
                                    qer.Score = mostRecentQER.Score;
                                }
                            }
                        }

                        rgq.CurrentEncounterRisk = qer;

                        EncounterRisk suber = vm.CurrentEncounter.EncounterRisk.Where(x =>
                            x.RiskForID == vm.RiskForID && x.IsTotal &&
                            x.EncounterKey == vm.CurrentEncounter.EncounterKey &&
                            x.RiskAssessmentKey == formsection.RiskAssessmentKey &&
                            x.RiskGroupKey == rgq.RiskGroupKey).FirstOrDefault();
                        if (suber == null)
                        {
                            suber = new EncounterRisk
                            {
                                RiskForID = vm.RiskForID,
                                RiskAssessmentKey = formsection.RiskAssessmentKey.Value,
                                RiskGroupKey = rgq.RiskGroupKey,
                                IsTotal = true
                            };
                            if (vm.IsBereavementRiskAssessment && (mostRecentEncounter != null))
                            {
                                mostRecentQER = mostRecentEncounter.EncounterRisk.Where(x =>
                                    x.RiskForID == vm.RiskForID && x.IsTotal &&
                                    x.EncounterKey == mostRecentEncounter.EncounterKey &&
                                    x.RiskAssessmentKey == formsection.RiskAssessmentKey &&
                                    x.RiskGroupKey == rgq.RiskGroupKey).FirstOrDefault();
                                if (mostRecentQER != null)
                                {
                                    suber.Comment = mostRecentQER.Comment;
                                    suber.CodeLookupKey = mostRecentQER.CodeLookupKey;
                                    suber.IsSelected = mostRecentQER.IsSelected;
                                    suber.RiskRangeKey = mostRecentQER.RiskRangeKey;
                                    suber.Score = mostRecentQER.Score;
                                }
                            }
                        }

                        rgq.RiskGroup.SubTotalRecord = suber;
                    }
                }

            if (r != null && r.TotalRecord != null)
            {
                // Nulling out empty score as the MAHC risk assessment has not been completed/used. Will set M1910 = no in RiskOasisMappingChanged
                if (r.TotalRecord.Score == 0 &&
                    vm.CurrentEncounter.EncounterRisk.Where(a => a.IsTotal == false && a.IsSelected).Any() == false)
                {
                    r.TotalRecord.Score = null;
                }

                r.TotalRecord.PropertyChanged += r.EncounterRisk_PropertyChanged;

                if (r.OasisManager != null && r.RiskAssessment != null)
                {
                    var ra = DynamicFormCache.GetRiskAssessmentByKey(r.RiskAssessment.RiskAssessmentKey);

                    if (ra.Label != null && ra.Label.ToLower() == "mahc-10 fall risk assessment tool")
                    {
                        r.OasisManager.RiskOasisMappingChanged(r.Question, r.TotalRecord);
                    }
                }
            }

            return r;
        }
    }
}