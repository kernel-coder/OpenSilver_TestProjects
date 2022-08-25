#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class TeleMonitorCommentItem
    {
        public int Key { get; set; }
        public DateTimeOffset? ObserveDateTimeOffset { get; set; }
        public short SequenceNumber { get; set; }
        public string CommentText { get; set; }
    }

    public class TeleMonitoring : QuestionUI
    {
        public TeleMonitoring(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public void SetupTeleMonitoring()
        {
            // setup comments for encounter
            // includes non result related comments on batches with our encounter key (ie, Batch with only comments - no results)
            List<TeleMonitorCommentItem> commentList = new List<TeleMonitorCommentItem>();
            if ((Encounter != null) && (Encounter.TeleMonitorBatch != null))
            {
                foreach (TeleMonitorBatch tmb in Encounter.TeleMonitorBatch)
                {
                    foreach (TeleMonitorComment tmc in
                             tmb.TeleMonitorComment.Where(c => c.TeleMonitorResultKey == null))
                    {
                        if (commentList.FirstOrDefault(c => c.Key == tmc.TeleMonitorCommentKey) == null)
                        {
                            commentList.Add(new TeleMonitorCommentItem
                            {
                                Key = tmc.TeleMonitorCommentKey, ObserveDateTimeOffset = tmb.ObserveDateTimeOffset,
                                SequenceNumber = tmc.SequenceNumber, CommentText = tmc.CommentText
                            });
                        }
                    }
                }
            }

            // includes results whose batch includes comments without a result key (ie, Batch with results (with comments) and batch level comments)
            if ((Encounter != null) && (Encounter.TeleMonitorResult != null))
            {
                foreach (TeleMonitorResult tmr in Encounter.TeleMonitorResult)
                    if ((tmr.TeleMonitorBatch != null) && (tmr.TeleMonitorBatch.TeleMonitorComment != null))
                    {
                        foreach (TeleMonitorComment tmc in tmr.TeleMonitorBatch.TeleMonitorComment.Where(c =>
                                     c.TeleMonitorResultKey == null))
                            if (commentList.FirstOrDefault(c => c.Key == tmc.TeleMonitorCommentKey) == null)
                            {
                                commentList.Add(new TeleMonitorCommentItem
                                {
                                    Key = tmc.TeleMonitorCommentKey,
                                    ObserveDateTimeOffset = tmr.TeleMonitorBatch.ObserveDateTimeOffset,
                                    SequenceNumber = tmc.SequenceNumber, CommentText = tmc.CommentText
                                });
                            }
                    }
            }

            // append the comments in SequenceNumber order within ObserveDateTimeOffset
            string CR = char.ToString('\r');
            string comments = null;

            foreach (TeleMonitorCommentItem c in commentList
                         .OrderBy(c => c.ObserveDateTimeOffset)
                         .ThenBy(c => c.SequenceNumber))
            {
                comments = (string.IsNullOrWhiteSpace(comments)) ? c.CommentText : comments + CR + c.CommentText;
            }

            TeleMonitorCommentsForEncounter = (string.IsNullOrWhiteSpace(comments)) ? "None" : comments;
            // setup TeleMonitorResultList 
            TeleMonitorResultList = Encounter.TeleMonitorResult.OrderBy(r => r.ResultDateTimeOffset).ToList();
        }

        private string _TeleMonitorCommentsForEncounter;

        public string TeleMonitorCommentsForEncounter
        {
            get { return _TeleMonitorCommentsForEncounter; }
            set
            {
                _TeleMonitorCommentsForEncounter = value;
                this.RaisePropertyChangedLambda(p => p.TeleMonitorCommentsForEncounter);
            }
        }

        private List<TeleMonitorResult> _TeleMonitorResultList;

        public List<TeleMonitorResult> TeleMonitorResultList
        {
            get { return _TeleMonitorResultList; }
            set
            {
                _TeleMonitorResultList = value;
                this.RaisePropertyChangedLambda(p => p.TeleMonitorResultList);
            }
        }

        public override void ClearEntity()
        {
        }

        public override void Cleanup()
        {
            if (TeleMonitorResultList != null)
            {
                TeleMonitorResultList.Clear();
            }

            base.Cleanup();
        }

        public override bool CopyForwardLastInstance()
        {
            return false;
        }

        public override void CopyForwardfromEncounter(Encounter e)
        {
        }

        public override void BackupEntity(bool restore)
        {
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }
    }

    public class TeleMonitoringFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            TeleMonitoring tm = new TeleMonitoring(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
            };
            tm.SetupTeleMonitoring();
            return tm;
        }
    }
}