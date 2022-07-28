#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Occasional;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class RiskAssessment
    {
        public bool Cloned { get; set; }

        public ICollectionView SortedRiskAssessmentLayout
        {
            get
            {
                var cvs = new CollectionViewSource();
                cvs.Source = RiskAssessmentLayout;
                cvs.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                cvs.View.MoveCurrentToFirst();

                return cvs.View;
            }
        }

        public RiskAssessment CreateNewVersion(bool supercede)
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newRisk = (RiskAssessment)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newRisk);
            RejectChanges();
            BeginEditting();
            if (supercede)
            {
                Superceded = true;
            }

            Cloned = true;
            EndEditting();
            return newRisk;
        }

        public RiskAssessment CloneRisk(out bool cloned, bool force = false)
        {
            if (!IsNew && !Cloned && EncounterRisk.Any() && (force || HasChanges ||
                                                             RiskAssessmentLayout.Where(fs => fs.HasChanges || fs.IsNew)
                                                                 .Any() || RiskRange.Where(fs =>
                                                                 fs.HasChanges || fs.IsNew).Any()))
            {
                var newRisk = CreateNewVersion(true);
                RiskAssessmentLayout.ForEach(ral =>
                {
                    if (ral.IsNew)
                    {
                        newRisk.RiskAssessmentLayout.Add(ral);
                    }
                    else
                    {
                        var newral = ral.CreateNewVersion();
                        newRisk.RiskAssessmentLayout.Add(newral);
                    }
                });

                RiskRange.ForEach(rr =>
                {
                    if (rr.IsNew)
                    {
                        newRisk.RiskRange.Add(rr);
                    }
                    else
                    {
                        var newrr = rr.CreateNewVersion();
                        newRisk.RiskRange.Add(newrr);
                    }
                });

                FormSection.ForEach(fs =>
                {
                    var newfs = fs.CreateNewVersion();
                    newRisk.FormSection.Add(newfs);
                });

                cloned = true;
                return newRisk;
            }

            cloned = false;
            return this;
        }

        public new RiskAssessment Clone()
        {
            var cloneRisk = (RiskAssessment)Clone(this);
            RiskAssessmentLayout.ForEach(ral =>
            {
                var cloneRal = (RiskAssessmentLayout)Clone(ral);
                cloneRisk.RiskAssessmentLayout.Add(cloneRal);
                if (ral.RiskQuestionKey != null)
                {
                    cloneRal.RiskQuestion = (RiskQuestion)Clone(ral.RiskQuestion);
                }

                if (ral.RiskGroupKey != null)
                {
                    var cloneRg = (RiskGroup)Clone(ral.RiskGroup);
                    cloneRal.RiskGroup = cloneRg;
                    ral.RiskGroup.RiskGroupQuestion.ForEach(rgq =>
                    {
                        var cloneRrq = (RiskGroupQuestion)Clone(rgq);
                        cloneRg.RiskGroupQuestion.Add(cloneRrq);
                        if (rgq.RiskQuestionKey != null)
                        {
                            cloneRrq.RiskQuestion = (RiskQuestion)Clone(rgq.RiskQuestion);
                        }
                    });
                }
            });

            RiskRange.ForEach(rr => { cloneRisk.RiskRange.Add((RiskRange)Clone(rr)); });

            return cloneRisk;
        }

        public RiskAssessment CopyRiskAssessment()
        {
            var newRisk = CreateNewVersion(false);
            RiskAssessmentLayout.ForEach(ral =>
            {
                if (ral.IsNew)
                {
                    newRisk.RiskAssessmentLayout.Add(ral);
                }
                else
                {
                    var newral = ral.CreateNewVersion();
                    newRisk.RiskAssessmentLayout.Add(newral);
                }
            });
            return newRisk;
        }
    }

    public partial class RiskAssessmentLayout
    {
        public RelayCommand<RiskAssessmentLayout> RiskLayoutValueChanged { get; set; }

        public IEncounterRisk CurrentEncounterRisk { get; set; }

        public EncounterRisk SaveCurrentEncounterRisk { get; set; }

        public EncounterRisk Save2CurrentEncounterRisk { get; set; }

        public RiskAssessmentLayout CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newRAL = (RiskAssessmentLayout)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newRAL);
            RejectChanges();
            BeginEditting();
            EndEditting();
            return newRAL;
        }
    }

    public partial class RiskGroup
    {
        public IEnumerable<RiskGroupQuestion> RiskGroupQuestionOrdered
        {
            get { return RiskGroupQuestion?.OrderBy(r => r.Sequence); }
        }

        public IEncounterRisk SubTotalRecord { get; set; }

        public ICollectionView SortedRiskGroupQuestion
        {
            get
            {
                var cvs = new CollectionViewSource();
                cvs.Source = RiskGroupQuestion;
                cvs.SortDescriptions.Add(new SortDescription("Sequence", ListSortDirection.Ascending));
                cvs.View.MoveCurrentToFirst();

                return cvs.View;
            }
        }
    }

    public partial class RiskGroupQuestion
    {
        public RelayCommand<RiskGroupQuestion> RiskGroupValueChanged { get; set; }

        public IEncounterRisk CurrentEncounterRisk { get; set; }

        public EncounterRisk SaveCurrentEncounterRisk { get; set; }

        public EncounterRisk Save2CurrentEncounterRisk { get; set; }
    }

    public partial class RiskRange
    {
        public RiskRange CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newRR = (RiskRange)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newRR);
            RejectChanges();
            BeginEditting();
            EndEditting();
            return newRR;
        }
    }
}