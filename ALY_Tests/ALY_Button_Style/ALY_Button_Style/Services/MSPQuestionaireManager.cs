#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Model;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IMSPQuestionaireManager : ICleanup
    {
        Question Question { get; set; }
        Section Section { get; set; }
        EncounterData EncounterData { get; set; }
        bool? HiddenOverride { get; set; }

        bool HideSectionOverride { get; set; }
    }

    public class MSPQuestionClass
    {
        public IMSPQuestionaireManager QuestionInterface;
        public EncounterData EncounterData { get; set; }
        public int Sequence { get; set; }
        public String QuestionID { get; set; }
        public String SectionID { get; set; }
        public int? NumToSkip { get; set; }
        public List<String> SkipValues { get; set; }
        public List<MSPSkipInfo> AdvancedSkipList { get; set; }
        public List<FormSectionQuestionAttribute> Attributes { get; set; }
    }

    public class MSPSkipInfo
    {
        public int Offset;
        public int NumToSkip;
        public string AnswerValue;
    }

    public class MSPQuestionaireManager
    {
        List<MSPQuestionClass> QuestionsToProcess = new List<MSPQuestionClass>();
        List<SectionUI> _sectionsToValidate = new List<SectionUI>();
        public List<SectionUI> SectionsToValidate => _sectionsToValidate;
        public Form AttatchedFormDef = null;

        public String StatusMessage
        {
            get
            {
                if (IsGHPPrimary30Day)
                {
                    return "GHP continues to Pay Primary during the 30-Month Coordination period";
                }

                if (IsGHPPrimary)
                {
                    return "GHP is Primary";
                }

                if (IsMedicareContinuePrimary)
                {
                    return "Medicare continues to pay primary";
                }

                if (IsMedicarePrimary)
                {
                    return "Medicare is Primary";
                }

                return "";
            }
        }

        private bool _MSPPopulated;

        public bool MSPPopulated
        {
            get { return _MSPPopulated; }
            set
            {
                if (_MSPPopulated != value)
                {
                    _MSPPopulated = value;
                    QuestionsToProcess.ForEach(q => q.QuestionInterface.HideSectionOverride = !MSPPopulated);
                }
            }
        }

        public void Cleanup()
        {
            if (QuestionsToProcess != null)
            {
                foreach (var q in QuestionsToProcess)
                    q.QuestionInterface.Cleanup();
                QuestionsToProcess.Clear();
                QuestionsToProcess = null;
            }

            if (_sectionsToValidate != null)
            {
                _sectionsToValidate.ForEach(s => s.Cleanup());
                _sectionsToValidate.Clear();
                _sectionsToValidate = null;
            }
        }

        public void RegisterQuestion(IMSPQuestionaireManager questionToRegister, int formSectionKey)
        {
            if (questionToRegister == null)
            {
                return;
            }

            if (QuestionsToProcess.Any(q => q.QuestionInterface == questionToRegister))
            {
                return;
            }

            MSPQuestionClass msp = new MSPQuestionClass();

            String QuestionSection = "";
            var _section = "";
            var _sectionrow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (_sectionrow != null)
            {
                _section = _sectionrow.FormSection.Sequence.ToString();
            }

            //if (_section != null) QuestionSection = _section.AttributeValue;
            QuestionSection = _section;

            String QuestionID = "";
            FormSectionQuestionAttribute _question = null;
            var _questionrow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (_questionrow != null)
            {
                _question = _questionrow.GetQuestionAttributeForName("QuestionID");
            }

            if (_question != null)
            {
                QuestionID = _question.AttributeValue;
            }

            var SeqRow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (SeqRow != null)
            {
                msp.Sequence = SeqRow.Sequence;
            }

            int? NumToSkip = null;
            FormSectionQuestionAttribute _numToSkipAttr = null;
            var _numToSkiprow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (_questionrow != null)
            {
                _numToSkipAttr = _numToSkiprow.GetQuestionAttributeForName("NumberToSkip");
            }

            if (_numToSkipAttr != null)
            {
                NumToSkip = Convert.ToInt32(_numToSkipAttr.AttributeValue);
            }

            string[] skipVals = null;
            FormSectionQuestionAttribute _SkipValuesAttr = null;
            var _SkipAttrsrow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (_questionrow != null)
            {
                _SkipValuesAttr = _SkipAttrsrow.GetQuestionAttributeForName("SkipValues");
            }

            if (_SkipValuesAttr != null)
            {
                skipVals = _SkipValuesAttr.AttributeValue.Split('|');
            }

            //
            string[] skipPairs = null;
            FormSectionQuestionAttribute _SkipPairsAttr = null;
            var _SkipPairsrow = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (_questionrow != null)
            {
                _SkipPairsAttr = _SkipPairsrow.GetQuestionAttributeForName("SkipNumberPairs");
            }

            if (_SkipPairsAttr != null)
            {
                skipPairs = _SkipPairsAttr.AttributeValue.Split('|');
            }

            //
            var formSectionQuestion = questionToRegister.Question.FormSectionQuestion
                .Where(fsq => fsq.FormSectionKey == formSectionKey).FirstOrDefault();
            if (formSectionQuestion != null)
            {
                msp.Attributes = formSectionQuestion.FormSectionQuestionAttribute.ToList();
            }

            msp.QuestionInterface = questionToRegister;
            msp.QuestionID = QuestionID;
            msp.SectionID = QuestionSection;
            msp.NumToSkip = NumToSkip;
            if (skipVals != null)
            {
                msp.SkipValues = new List<string>();
                msp.SkipValues = skipVals.ToList();
            }

            if (skipPairs != null)
            {
                //'1:1-7|2:0-8' - Answer:Offset-NumberToSkip|Answer:Offset-NumberToSkip (If the answer is 1, skip 1 question, then hide 7 questions|If the answer is 2, skip 0 questions, then hide 8)
                MSPSkipInfo advInfo;
                msp.AdvancedSkipList = new List<MSPSkipInfo>();
                foreach (var s in skipPairs)
                {
                    string[] sp = s.Split(':');
                    if (sp.Length == 2)
                    {
                        string[] row = sp[1].Split('-');
                        if (row.Length == 2)
                        {
                            advInfo = new MSPSkipInfo();
                            advInfo.AnswerValue = sp[0];
                            if (!Int32.TryParse(row[0], out advInfo.Offset))
                            {
                                continue;
                            }

                            if (!Int32.TryParse(row[1], out advInfo.NumToSkip))
                            {
                                continue;
                            }

                            msp.AdvancedSkipList.Add(advInfo);
                        }
                    }
                }
            }

            msp.EncounterData = questionToRegister.EncounterData;
            QuestionsToProcess.Add(msp);
        }

        public void UnRegisterQuestion(IMSPQuestionaireManager questionToUnRegister)
        {
            if (questionToUnRegister == null)
            {
                return;
            }

            var msp = QuestionsToProcess.FirstOrDefault(q => q.QuestionInterface == questionToUnRegister);
            if (msp != null)
            {
                QuestionsToProcess.Remove(msp);
            }
        }

        public void RegisterSection(SectionUI sectionToRegister)
        {
            if (sectionToRegister != null)
            {
                _sectionsToValidate.Add(sectionToRegister);
            }
        }

        public void UnRegisterSection(SectionUI sectionToUnRegister)
        {
            if (sectionToUnRegister == null)
            {
                return;
            }

            if (_sectionsToValidate.Contains(sectionToUnRegister))
            {
                _sectionsToValidate.Remove(sectionToUnRegister);
            }
        }

        public bool UpdateRegisteredQuestionsOnLoad(IMSPQuestionaireManager changedQuestion)
        {
            bool AllSuccessful = true;
            var WrappedQuestion = GetWrappedQuestion(changedQuestion);
            if (WrappedQuestion == null)
            {
                return AllSuccessful;
            }

            AllSuccessful = LoopOverQuestionsOnUpdate(WrappedQuestion);

            return AllSuccessful;
        }

        private MSPQuestionClass GetWrappedQuestion(IMSPQuestionaireManager changedQuestion)
        {
            return QuestionsToProcess.FirstOrDefault(q => q.QuestionInterface == changedQuestion);
        }

        private bool LoopOverQuestionsOnUpdate(MSPQuestionClass WrappedQuestion)
        {
            bool AllSuccessful = true;

            // If we can't skip from this question, continue on.
            if (!WrappedQuestion.Attributes.Any(at => at.AttributeName.ToUpper() == "SKIPAHEAD"))
            {
                return AllSuccessful;
            }

            if (WrappedQuestion.Attributes.FirstOrDefault(at => at.AttributeName.ToUpper() == "SKIPAHEAD")?.AttributeValue.ToUpper() == "FALSE")
            {
                return AllSuccessful;
            }

            foreach (var ctrl in QuestionsToProcess.Where(c =>
                         (c.SectionID == WrappedQuestion.SectionID && c.Sequence > WrappedQuestion.Sequence)))
                if (ctrl.SectionID != null)
                {
                    AllSuccessful = ProcessQuestion(ctrl, WrappedQuestion);
                }

            Messenger.Default.Send(true, "UpdateStatusMessage");
            return AllSuccessful;
        }

        private bool LoopOverQuestionsOnUpdateAdvanced(MSPQuestionClass WrappedQuestion)
        {
            bool AllSuccessful = true;
            // If we can't skip from this question, continue on.
            if (!WrappedQuestion.Attributes.Any(at => at.AttributeName.ToUpper() == "SKIPAHEAD"))
            {
                return AllSuccessful;
            }

            if (WrappedQuestion.Attributes.FirstOrDefault(at => at.AttributeName.ToUpper() == "SKIPAHEAD")?.AttributeValue.ToUpper() == "FALSE")
            {
                return AllSuccessful;
            }

            var qryQuestion = QuestionsToProcess.FirstOrDefault(q => q.QuestionID == WrappedQuestion.QuestionID && q.SectionID == WrappedQuestion.SectionID);
            if (qryQuestion == null)
            {
                return AllSuccessful;
            }

            foreach (var advitem in WrappedQuestion.AdvancedSkipList.Where(q =>
                         q.AnswerValue == qryQuestion.QuestionInterface.EncounterData.TextData))
            {
                // Process each question to determine what should show and what should hide.

                // Process each question to determine what should show and what should hide.
                // Only look at the rows within our 'group' identified by the start sequence of the source question and ending with the total in our group (NumToSkip Attribute)
                // Not to be confused with the number to skip for the answer and offset.  We need to deal with all the questions in our 'group' and hide the ones in the
                // skip range and show the ones outside the range.  nothing more, nothing less so it doesn't bleed into other questions.
                foreach (var ctrl in QuestionsToProcess.Where(c => (c.SectionID == WrappedQuestion.SectionID &&
                                                                    c.Sequence > WrappedQuestion.Sequence
                                                                    && ((WrappedQuestion.Sequence +
                                                                         WrappedQuestion.NumToSkip) >= c.Sequence))))
                    if (ctrl.SectionID != null)
                    {
                        ProcessSectionAndQuestionValue(ctrl, WrappedQuestion, advitem.AnswerValue, advitem.Offset,
                            advitem.NumToSkip);
                    }
            }

            Messenger.Default.Send(true, "UpdateStatusMessage");
            return AllSuccessful;
        }

        public bool UpdateRegisteredQuestions(IMSPQuestionaireManager changedQuestion)
        {
            bool AllSuccessful = true;
            var WrappedQuestion = GetWrappedQuestion(changedQuestion);
            if (WrappedQuestion == null)
            {
                return AllSuccessful;
            }

            if (WrappedQuestion.AdvancedSkipList == null || WrappedQuestion.AdvancedSkipList.Any() == false)
            {
                AllSuccessful = LoopOverQuestionsOnUpdate(WrappedQuestion);
            }
            else
            {
                AllSuccessful = LoopOverQuestionsOnUpdateAdvanced(WrappedQuestion);
            }

            AdvanceToDifferentSection(WrappedQuestion);

            Messenger.Default.Send(true, "UpdateStatusMessage");

            return AllSuccessful;
        }

        private bool ProcessQuestion(MSPQuestionClass questionToProcess, MSPQuestionClass SourceQuestion)
        {
            bool successful = true;

            // collapse everything in the section after the source
            if (SourceQuestion.NumToSkip == null && questionToProcess.SectionID == SourceQuestion.SectionID &&
                Convert.ToInt32(questionToProcess.Sequence) > SourceQuestion.Sequence)
            {
                ProcessSectionAndQuestion(questionToProcess, SourceQuestion);
            }
            //Collapse a set number of rows.
            else if (questionToProcess.SectionID == SourceQuestion.SectionID && Convert.ToInt32(questionToProcess
                                                                                 .Sequence) > SourceQuestion.Sequence
                                                                             && ((SourceQuestion.Sequence +
                                                                                     SourceQuestion.NumToSkip) >=
                                                                                 questionToProcess.Sequence)) //1
            {
                ProcessSectionAndQuestion(questionToProcess, SourceQuestion);
            }

            return successful;
        }

        private void ProcessSectionAndQuestion(MSPQuestionClass questionToProcess, MSPQuestionClass SourceQuestion)
        {
            if (SourceQuestion.SkipValues == null || SourceQuestion.SkipValues.Any() == false)
            {
                ProcessSectionAndQuestionNOTValue(questionToProcess, SourceQuestion, "1");
            }
            else
            {
                ProcessSectionAndQuestionValues(questionToProcess, SourceQuestion);
            }
        }

        private void ProcessSectionAndQuestionValue(MSPQuestionClass questionToProcess, MSPQuestionClass SourceQuestion,
            String valueParm, int OffsetParm, int NumToSkip)
        {
            // Set the rest of the questions in Section II hidden if Question 1 is answered no.
            var qryQuestion = QuestionsToProcess.FirstOrDefault(q => q.QuestionID == SourceQuestion.QuestionID && q.SectionID == SourceQuestion.SectionID);

            if (qryQuestion != null && qryQuestion.EncounterData.TextData != null &&
                qryQuestion.EncounterData.TextData == valueParm
                && questionToProcess.Sequence > (SourceQuestion.Sequence + OffsetParm) && questionToProcess.Sequence <=
                (SourceQuestion.Sequence + OffsetParm + NumToSkip))
            {
                SetHiddenValue(questionToProcess.QuestionInterface, true);
            }
            else
            {
                SetHiddenValue(questionToProcess.QuestionInterface, false);
            }
        }

        private void ProcessSectionAndQuestionNOTValue(MSPQuestionClass questionToProcess,
            MSPQuestionClass SourceQuestion, String valueParm)
        {
            // Set the rest of the questions in Section II hidden if Question 1 is answered no.
            var qryQuestion = QuestionsToProcess
                .FirstOrDefault(q => q.QuestionID == SourceQuestion.QuestionID && q.SectionID == SourceQuestion.SectionID);

            if (qryQuestion != null && qryQuestion.EncounterData.TextData != null &&
                qryQuestion.EncounterData.TextData != valueParm)
            {
                SetHiddenValue(questionToProcess.QuestionInterface, true);
            }
            else
            {
                SetHiddenValue(questionToProcess.QuestionInterface, false);
            }
        }

        private void ProcessSectionAndQuestionValues(MSPQuestionClass questionToProcess,
            MSPQuestionClass SourceQuestion)
        {
            // Set the rest of the questions in Section II hidden if Question 1 is answered no.
            var qryQuestion = QuestionsToProcess
                .FirstOrDefault(q => q.QuestionID == SourceQuestion.QuestionID && q.SectionID == SourceQuestion.SectionID);

            if (qryQuestion != null && qryQuestion.EncounterData.TextData != null &&
                SourceQuestion.SkipValues.Contains(qryQuestion.EncounterData.TextData))
            {
                SetHiddenValue(questionToProcess.QuestionInterface, true);
            }
            else
            {
                SetHiddenValue(questionToProcess.QuestionInterface, false);
            }
        }

        private void SetHiddenValue(IMSPQuestionaireManager ques, bool value)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { ques.HiddenOverride = value; });
        }

        private void AdvanceToDifferentSection(MSPQuestionClass changedQuestion)
        {

        }

        public bool IsMedicarePrimary
        {
            get
            {
                bool isPrimary = false;
                string q1ans = GetAnswerForSectionAndQuestion("4", 1);
                string q2ans = GetAnswerForSectionAndQuestion("4", 10);
                string q3ans = GetAnswerForSectionAndQuestion("4", 19);
                string q4ans = GetAnswerForSectionAndQuestion("4", 20);
                string q5ans = GetAnswerForSectionAndQuestion("4", 33);

                string q6ans = GetAnswerForSectionAndQuestion("5", 1);
                string q7ans = GetAnswerForSectionAndQuestion("5", 10);
                string q8ans = GetAnswerForSectionAndQuestion("5", 19);
                string q9ans = GetAnswerForSectionAndQuestion("5", 20);
                string q10ans = GetAnswerForSectionAndQuestion("5", 28);
                string q11ans = GetAnswerForSectionAndQuestion("5", 41);
                string q12ans = GetAnswerForSectionAndQuestion("5", 54);

                string q13ans = GetAnswerForSectionAndQuestion("6", 1);
                string q14ans = GetAnswerForSectionAndQuestion("6", 63);

                if ((q1ans == "0" || q1ans == "2") && (q2ans == "0" || q2ans == "2") && !AreAnySection1AND2QuestionsYes)
                {
                    isPrimary = true;
                }

                if (q3ans == "0" && (q1ans == "0" || q1ans == "2") && (q2ans == "0" || q2ans == "2"))
                {
                    isPrimary = true;
                }

                if (q3ans == "0" && !AreAnySection1AND2QuestionsYes)
                {
                    isPrimary = true;
                }

                if (q4ans == "0" && q5ans == "0" && !AreAnySection1AND2QuestionsYes)
                {
                    isPrimary = true;
                }

                if ((q6ans == "0" || q6ans == "2") && (q7ans == "0" || q7ans == "2") && q8ans == "0" && q9ans == "0" &&
                    !AreAnySection1AND2QuestionsYes)
                {
                    isPrimary = true;
                }

                if (q10ans == "0" && q11ans == "0" && q12ans == "0" && !AreAnySection1AND2QuestionsYes)
                {
                    isPrimary = true;
                }

                if (q13ans == "0" || q14ans == "0")
                {
                    isPrimary = true;
                }

                return isPrimary;
            }
        }

        public bool IsGHPPrimary
        {
            get
            {
                bool IsGHP = false;
                string q1ans = GetAnswerForSectionAndQuestion("4", 20);
                string q2ans = GetAnswerForSectionAndQuestion("4", 33);
                string q3ans = GetAnswerForSectionAndQuestion("5", 19);
                string q4ans = GetAnswerForSectionAndQuestion("5", 20);
                string q5ans = GetAnswerForSectionAndQuestion("5", 28);
                string q6ans = GetAnswerForSectionAndQuestion("5", 41);
                string q7ans = GetAnswerForSectionAndQuestion("5", 54);
                if (q1ans == "1" || q1ans == "2" || q1ans == "3" || q2ans == "1" || q3ans == "1" || q3ans == "2" ||
                    q3ans == "3"
                    || q4ans == "1" || q5ans == "1" || q6ans == "1" || q7ans == "1")
                {
                    IsGHP = true;
                }

                return IsGHP;
            }
        }

        public bool IsGHPPrimary30Day
        {
            get
            {
                bool IsPrimary = false;
                string q1ans = GetAnswerForSectionAndQuestion("6", 65);
                string q2ans = GetAnswerForSectionAndQuestion("6", 66);

                if (q1ans == "1" || q2ans == "1")
                {
                    IsPrimary = true;
                }

                return IsPrimary;
            }
        }

        public bool IsMedicareContinuePrimary
        {
            get
            {
                bool IsPrimary = false;
                string q1ans = GetAnswerForSectionAndQuestion("6", 66);

                if (q1ans == "0")
                {
                    IsPrimary = true;
                }

                return IsPrimary;
            }
        }

        public bool AreAnySection1AND2QuestionsYes
        {
            get
            {
                string q1ans = GetAnswerForSectionAndQuestion("1", 1);
                string q2ans = GetAnswerForSectionAndQuestion("1", 3);
                string q3ans = GetAnswerForSectionAndQuestion("1", 4);
                string q4ans = GetAnswerForSectionAndQuestion("1", 5);
                string q5ans = GetAnswerForSectionAndQuestion("2", 1);
                string q6ans = GetAnswerForSectionAndQuestion("2", 3);
                string q7ans = GetAnswerForSectionAndQuestion("2", 12);

                return q1ans == "1" || q2ans == "1" || q3ans == "1" || q4ans == "1" || q5ans == "1" || q6ans == "1"
                       || q7ans == "1";
            }
        }

        private string GetAnswerForSectionAndQuestion(string SectionIDParm, int SeqParm)
        {
            string quesanswer = "";
            var q1 = QuestionsToProcess.FirstOrDefault(q => q.SectionID == SectionIDParm && q.Sequence == SeqParm);
            if (q1 != null && q1.QuestionInterface != null && q1.QuestionInterface.EncounterData != null)
            {
                quesanswer = q1.QuestionInterface.EncounterData.TextData;
            }

            return quesanswer;
        }
    }
}