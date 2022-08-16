#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OasisVersion
    {
        public bool UsingICD10
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SYS_CD))
                {
                    return false;
                }

                if (SYS_CD.ToUpper() != "OASIS")
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(VersionCD2))
                {
                    return false;
                }

                if (VersionCD2 == "02.00" || VersionCD2 == "2.10" || VersionCD2 == "2.11")
                {
                    return false;
                }

                // Assume OASIS 2.12
                return true;
            }
        }
    }

    public partial class OasisLayout
    {
        public bool IsType(OasisType type)
        {
            return (OasisType)Type == type;
        }

        public bool IsForRFA(string rfa)
        {
            if (string.IsNullOrWhiteSpace(RFAs))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rfa))
            {
                return false;
            }

            var rfas = "," + RFAs + ',';
            return rfas.Contains("," + rfa + ',') ? true : false;
        }
    }

    public partial class OasisAlert
    {
        public bool OasisAlertContainsOasisQuestionKey(int oasisQuestionKey)
        {
            if (OasisAlertQuestion == null)
            {
                return false;
            }

            var oaq = OasisAlertQuestion.Where(a => a.OasisQuestionKey == oasisQuestionKey).FirstOrDefault();
            return oaq == null ? false : true;
        }
    }

    public partial class OasisQuestion
    {
        public OasisLayout CachedOasisLayout { get; private set; }

        public string QuestionTextPrint
        {
            get
            {
                if (QuestionText == null)
                {
                    return null;
                }

                var p = QuestionText;
                p = p.Replace("<Bold>", "");
                p = p.Replace("</Bold>", "");
                p = p.Replace("<Underline>", "");
                p = p.Replace("</Underline>", "");
                p = p.Replace("<LineBreak />", "");
                p = p.Replace("<LineBreak/>", "");
                return p;
            }
        }

        public bool ShowQuestionText2 => string.IsNullOrWhiteSpace(QuestionText2) ? false : true;

        public bool ShowColumnHeaders
        {
            get
            {
                if (Column1HeaderText == null)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowColumn2
        {
            get
            {
                if (OasisAnswer == null)
                {
                    return false;
                }

                var showColumn2 = OasisAnswer.Where(p => p.SubQuestionColumn == 2).Any();
                return showColumn2;
            }
        }

        public string Column1HeaderText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Column1Header))
                {
                    return null;
                }

                return Column1Header.Trim();
            }
        }

        public string Column2HeaderText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Column2Header))
                {
                    return "<Bold>2.</Bold>";
                }

                return Column2Header.Trim();
            }
        }

        public bool IsType(OasisType type)
        {
            if (OasisLayout == null)
            {
                return false;
            }

            return (OasisType)OasisLayout.Type == type;
        }

        public void SetCachedOasisLayout(OasisLayout ol)
        {
            CachedOasisLayout = ol;
        }
    }

    public partial class OasisQuestionCodingRule
    {
        public bool IsDataTemplateCodingRulesOneColumn
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DataTemplate))
                {
                    return false;
                }

                return string.Equals(DataTemplate, "CodingRulesOneColumn", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsDataTemplateCodingRulesTwoColumn
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DataTemplate))
                {
                    return false;
                }

                return string.Equals(DataTemplate, "CodingRulesTwoColumn", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public partial class OasisAnswer
    {
        public string RFAs
        {
            get
            {
                if (CachedOasisLayout == null)
                {
                    return null;
                }

                return CachedOasisLayout.RFAs;
            }
        }

        public OasisLayout CachedOasisLayout { get; private set; }

        public string AnswerLabelBolded
        {
            get
            {
                var p = string.IsNullOrWhiteSpace(AnswerLabel) ? "" : "<Bold>" + AnswerLabel + "</Bold>";
                return p;
            }
        }

        public string AnswerTextShort
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AnswerText))
                {
                    return null;
                }

                if (AnswerText.Trim().ToLower().StartsWith("<bold>") == false)
                {
                    return AnswerText;
                }

                string[] delimiter = { "</Bold>", "</bold>", "</BOLD>" };
                var answerTextArray = AnswerText.Trim().Split(delimiter, StringSplitOptions.None);
                if (answerTextArray.Length == 0)
                {
                    return AnswerText;
                }

                if (string.IsNullOrWhiteSpace(answerTextArray[0]))
                {
                    return null;
                }

                return answerTextArray[0] + "</Bold>";
            }
        }

        public string SubQuestionLabelBolded
        {
            get
            {
                var p = string.IsNullOrWhiteSpace(SubQuestionLabel) ? "" : "<Bold>" + SubQuestionLabel + "</Bold>";
                return p;
            }
        }

        private string SubQuestionLabelMinusPeriod
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionLabel))
                {
                    return null;
                }

                return SubQuestionLabel.Replace(".", "");
            }
        }

        public string SubQuestionTextShortNotRich
        {
            get
            {
                var p = SubQuestionTextShort;
                if (string.IsNullOrWhiteSpace(p))
                {
                    return "";
                }

                p = p.Replace("<Bold>", "");
                p = p.Replace("</Bold>", "");
                p = p.Replace("<Underline>", "");
                p = p.Replace("</Underline>", "");
                p = p.Replace("<Italic>", "");
                p = p.Replace("</Italic>", "");
                p = p.Replace("<LineBreak />", "");
                p = p.Replace("<LineBreak/>", "");
                return p;
            }
        }

        public string SubQuestionTextShort
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionText))
                {
                    return null;
                }

                if (SubQuestionText.Trim().ToLower().StartsWith("<bold>") == false)
                {
                    return SubQuestionText;
                }

                string[] delimiter = { "</Bold>", "</bold>", "</BOLD>" };
                var answerTextArray = SubQuestionText.Trim().Split(delimiter, StringSplitOptions.None);
                if (answerTextArray.Length == 0)
                {
                    return SubQuestionText;
                }

                if (string.IsNullOrWhiteSpace(answerTextArray[0]))
                {
                    return null;
                }

                return answerTextArray[0] + "</Bold>";
            }
        }

        public string AnswerTextPrint
        {
            get
            {
                if (AnswerText == null)
                {
                    return null;
                }

                var p = AnswerText;
                p = p.Replace("<Bold>", "");
                p = p.Replace("</Bold>", "");
                p = p.Replace("<Underline>", "");
                p = p.Replace("</Underline>", "");
                p = p.Replace("<Italic>", "");
                p = p.Replace("</Italic>", "");
                p = p.Replace("<LineBreak />", "");
                p = p.Replace("<LineBreak/>", "");
                return p;
            }
        }

        public string AnswerText2Print
        {
            get
            {
                if (AnswerText2 == null)
                {
                    return null;
                }

                var p = AnswerText2;
                p = p.Replace("<Bold>", "");
                p = p.Replace("</Bold>", "");
                p = p.Replace("<Underline>", "");
                p = p.Replace("</Underline>", "");
                p = p.Replace("<Italic>", "");
                p = p.Replace("</Italic>", "");
                p = p.Replace("<LineBreak />", "");
                p = p.Replace("<LineBreak/>", "");
                return p;
            }
        }

        public bool IsDataTemplateSubQuestionDoubleRadioCombo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionDataTemplate))
                {
                    return false;
                }

                var r = string.Equals(SubQuestionDataTemplate, "SubQuestionDoubleRadioCombo",
                    StringComparison.OrdinalIgnoreCase);
                return r;
            }
        }

        public bool IsDataTemplateSubQuestionRadioCombo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionDataTemplate))
                {
                    return false;
                }

                var r = string.Equals(SubQuestionDataTemplate, "SubQuestionRadioCombo",
                    StringComparison.OrdinalIgnoreCase);
                return r;
            }
        }

        public bool IsDataTemplateSubQuestionDoubleRadioVertical
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionDataTemplate))
                {
                    return false;
                }

                var r = string.Equals(SubQuestionDataTemplate, "SubQuestionDoubleRadioVertical",
                    StringComparison.OrdinalIgnoreCase);
                return r;
            }
        }

        public bool IsDataTemplateSubQuestionRadioVertical
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubQuestionDataTemplate))
                {
                    return false;
                }

                var r = string.Equals(SubQuestionDataTemplate, "SubQuestionRadioVertical",
                    StringComparison.OrdinalIgnoreCase);
                return r;
            }
        }

        public bool IsType(OasisType type)
        {
            if (CachedOasisLayout == null)
            {
                return false;
            }

            return (OasisType)CachedOasisLayout.Type == type;
        }

        public void SetCachedOasisLayout(OasisLayout ol)
        {
            CachedOasisLayout = ol;
        }

        public string SubQuestionLabelAndTextShort(OasisManagerQuestion omq)
        {
            var p = string.IsNullOrWhiteSpace(SubQuestionLabelMinusPeriod)
                ? ""
                : SubQuestionLabelMinusPeriod + SubQuestionColumnText(omq) + ". ";
            p = p + SubQuestionTextShortNotRich;
            if (p.EndsWith(":"))
            {
                p = p.Substring(0, p.Length - 1);
            }

            return p;
        }

        public string SubQuestionColumnText(OasisManagerQuestion omq)
        {
            if (omq == null || omq.OasisQuestion == null || omq.OasisQuestion.ShowColumn2 == false ||
                SubQuestionColumn == null || SubQuestionLabel == null || SubQuestionLabel.StartsWith("RR") ||
                SubQuestionLabel.StartsWith("SS"))
            {
                return "";
            }

            return SubQuestionColumn.ToString().Trim();
        }
    }

    public partial class OasisHeader
    {
        public string CMSCertificationNumberWrapper
        {
            get { return CMSCertificationNumber; }
            set
            {
                string tmpCMS = null;
                if (value != null)
                {
                    tmpCMS = value.Replace("-", "");
                    tmpCMS = tmpCMS.Substring(0, tmpCMS.Length <= 6 ? tmpCMS.Length : 6);
                }

                // ugly slight of hand to kick the bindings to get the converter to be executed.
                CMSCertificationNumber = "       ";
                CMSCertificationNumber = tmpCMS;
                RaisePropertyChanged("CMSCertificationNumberWrapper");
            }
        }
    }
}