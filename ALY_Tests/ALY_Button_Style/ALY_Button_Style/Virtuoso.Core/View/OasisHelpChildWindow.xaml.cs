using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.IO;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using System.ComponentModel;
using System.Windows.Data;
using Virtuoso.Server.Data;
using Virtuoso.Client.Infrastructure;
using Virtuoso.Client.Core;

namespace Virtuoso.Core.View
{
    public class SingleQuestionHelpPOCO
    {
        public readonly string HTML;
        public readonly List<string> LinkableKeys;

        public SingleQuestionHelpPOCO(string html)
        {
            this.HTML = html;
            LinkableKeys = OasisHelpPOCO.GetLinkableIds(html);
        }
    }

    public class OasisHelpPOCO
    {
        internal enum FileStructureVersion
        {
            Unknown = 0,
            EarlyStructure = 1,
            FirstDivChange = 2,
            StrippedDown = 3
        }

        private FileStructureVersion _FileStructureVersion { get; set; }
        public string OasisHelpResourceURI { get; set; }
        private string _html { get; set; }
        private List<string> _linkableIDs { get; set; }
        private Dictionary<string, SingleQuestionHelpPOCO> _questions { get; set; }

        public OasisHelpPOCO(string html,
            string oasisHelpResourceURI) // External Constructor = call parse of html immediately.
            : this(oasisHelpResourceURI)
        {
            _FileStructureVersion = GetFileStructureVersion(html);
            ParseHtml(html);
        }

        private void ParseHtml(string Expression)
        {
            _html = Expression;

            if (_html != null && _html != string.Empty)
            {
                // Remove Encore specific best practices 
                _html = _html.Replace("class=\"EncoreTable\"", "class=\"EncoreTable\" style=\"display:none;\"");
                _html = _html.Replace("class=\"OasisVersion\"", "class=\"OasisVersion\" style=\"display:none;\"");

                _html = StripInternalLinksFromHtmlString(_html);

                _linkableIDs = GetLinkableIds(_html);

                _questions = ParseIntoQuestions(SetQuestionDelimiter(_html));
            }
        }

        private OasisHelpPOCO(string oasisHelpResourceURI)
        {
            this.OasisHelpResourceURI = oasisHelpResourceURI;
        }

        public string HTML
        {
            get { return this._html; }
        }

        public List<string> ItemLinks
        {
            get { return _linkableIDs; }
        }

        public int IndexOfKey(string key)
        {
            int i = 0;
            while (i < ItemLinks.Count)
            {
                if (ItemLinks[i].ToLower() == key.ToLower())
                {
                    return i;
                }

                i += 1;
            }

            return 0;
        }


        public Dictionary<string, SingleQuestionHelpPOCO> Questions
        {
            get { return _questions; }
        }

        private void AddQuestions(Dictionary<string, SingleQuestionHelpPOCO> dictionary, string html)
        {
            var question = new SingleQuestionHelpPOCO(html);
            foreach (var key in question.LinkableKeys)
            {
                dictionary.Add(key, question);
            }
        }

        private Dictionary<string, SingleQuestionHelpPOCO> ParseIntoQuestions(string questionDelimiter)
        {
            var result = new Dictionary<string, SingleQuestionHelpPOCO>();
            string middlePart;
            int currentLoc = 0; // Init to the 1st character location.
            int nextLoc = _html.IndexOf(questionDelimiter, currentLoc); // find the location of a questionDelimiter

            if (nextLoc > -1)
            {
                string
                    questionPrefix =
                        _html.Substring(0,
                            nextLoc -
                            1); // Grab characters before the 1st question to act as the question prefix.                                                

                currentLoc = nextLoc;
                nextLoc = _html.IndexOf(questionDelimiter,
                    nextLoc + 1); // find the location of the NEXT questionDelimiter after the 1st one...        
                while (nextLoc > -1)
                {
                    middlePart = _html.Substring(currentLoc, nextLoc - currentLoc);

                    AddQuestions(result, questionPrefix + middlePart + "</body></html>");

                    currentLoc = nextLoc;
                    nextLoc = _html.IndexOf(questionDelimiter,
                        nextLoc + 1); // find the location of the NEXT questionDelimiter after the 1st one...        
                }

                middlePart = _html.Substring(currentLoc, _html.Length - currentLoc);
                AddQuestions(result, questionPrefix + middlePart);
            }

            return result;
        }

        private static FileStructureVersion GetFileStructureVersion(string html)
        {
            if (html.IndexOf("<div class=\"OasisQuestion\">") <= 0)
            {
                return FileStructureVersion.EarlyStructure;
            }
            else
            {
                if (html.IndexOf("OasisQuestionGrid") <= 0)
                {
                    return FileStructureVersion.FirstDivChange;
                }
                else
                {
                    return FileStructureVersion.StrippedDown;
                }
            }
        }

        private static string SetQuestionDelimiter(string html)
        {
            if (GetFileStructureVersion(html) == FileStructureVersion.EarlyStructure)
            {
                return "<div>";
            }
            else
            {
                return "<div class=\"OasisQuestion\">";
            }
        }

        private static string StripInternalLinksFromHtmlString(string html)
        {
            if (html == null)
            {
                return null;
            }

            var result = new System.Text.StringBuilder();

            int lastEnd = 0; // Init to the 1st character location.
            int poundloc =
                html.IndexOf("<a href=\"#",
                    lastEnd); // find the location of an internal link - hope that styles follow the href.
            while (poundloc > -1)
            {
                result.Append(html.Substring(lastEnd,
                    poundloc - lastEnd)); // Append characters to the left of the PREVIOUS occurence to the result.

                int closemarker =
                    html.IndexOf(">", poundloc) +
                    1; // Find the close of the <a href="# tag.  There could be styles, but no > characters. (Add one to make math below easier)
                lastEnd = html.IndexOf("</a>", poundloc); // Find the first </a> tag close after the internal link

                result.Append(html.Substring(closemarker,
                    lastEnd - closemarker)); // Append characters inside the <a href="# tag.       

                lastEnd += 4; // advance the index past the </a> tag                                  
                poundloc = html.IndexOf("<a href=\"#",
                    lastEnd); // find the location of the NEXT internal link - hope that styles follow the href.
            }

            result.Append(
                html.Substring(lastEnd)); // finally, Append characters after the LAST occurence to the result;

            return result.ToString();
        }

        public static List<string> GetLinkableIds(string html)
        {
            var result = new List<string>();

            if (html == null)
            {
                return null;
            }

            int lastEnd = 0; // Init to the 1st character location.
            int poundloc = html.IndexOf(" id=\"", lastEnd); // find the location of an id attribute
            while (poundloc > -1)
            {
                lastEnd = html.IndexOf("\"", poundloc + 6); // Find the close of the id attribute.

                string middlePart = html.Substring(poundloc + 5, lastEnd - (poundloc + 5)).Trim().ToUpper();
                int value = 0;
                var mnum = int.TryParse((middlePart + "      ").Substring(1, 4), out value);
                var gnum = int.TryParse((middlePart + "      ").Substring(2, 4), out value);
                if ((middlePart.Substring(0, 1) == "M" && mnum) || (middlePart.Substring(0, 1) == "J" && mnum) ||
                    (middlePart.Substring(0, 2) == "GG" && gnum))
                {
                    result.Add(middlePart); // Append characters inside the id if they match the correct pattern
                }

                lastEnd += 1; // advance the index past the </a> tag                                  
                poundloc = html.IndexOf(" id=\"", lastEnd); // find the location of the NEXT id attribute
            }

            return result;
        }


        internal OasisHelpPOCO Clone // Prevents re-parse - make sure all members are copied.
        {
            get
            {
                var result = new OasisHelpPOCO(this.OasisHelpResourceURI);
                result._html = string.Copy(this.HTML);

                result._FileStructureVersion = this._FileStructureVersion;
                result._linkableIDs = this._linkableIDs;
                result._questions = this._questions;

                return result;
            }
        }
    }

    public partial class OasisHelpChildWindow : ChildWindow
    {
        bool loaded = false;
        int questionIndex = -1;
        private string oasisHelpString = null;

        private static Dictionary<int, OasisHelpPOCO> htmlCache = new Dictionary<int, OasisHelpPOCO>();
        private OasisHelpPOCO currentHelp;

        private OasisHelpPOCO GetHTML(int oasisVersionKey)
        {
            if (htmlCache.ContainsKey(oasisVersionKey))
            {
                return htmlCache[oasisVersionKey].Clone;
            }
            else
            {
                var _oasisHelpResourceURI = OasisCache.OasisHelpResourceURI(oasisVersionKey);
                if (_oasisHelpResourceURI == null)
                {
                    MessageBox.Show("OasisHelpChildWindow.ProcessHTML error: Oasis help document not found.");
                    return new OasisHelpPOCO(string.Empty, string.Empty);
                }

                var appFeatures = VirtuosoContainer.Current.GetInstance<IAppFeatures>();
                var stream = appFeatures.GetFileStream(_oasisHelpResourceURI);

                if (stream == null)
                {
                    MessageBox.Show("OasisHelpChildWindow.ProcessHTML error: " + _oasisHelpResourceURI + " not found.");
                    return new OasisHelpPOCO(string.Empty, _oasisHelpResourceURI);
                }

                string html = new StreamReader(stream).ReadToEnd();
                var result = new OasisHelpPOCO(html, _oasisHelpResourceURI);
                htmlCache.Add(oasisVersionKey, result);

                return result;
            }
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            ComboBox qcb = this.questionComboBox as ComboBox;
            if (qcb == null)
            {
                return;
            }

            qcb.SelectedIndex = questionIndex;
        }

        public OasisHelpChildWindow(OasisManagerQuestion question)
        {
            InitializeComponent();
            loaded = ProcessHTML(question);
        }


        private bool ProcessHTML(OasisManagerQuestion oasisManagerQuestion)
        {
            //DS 13022 11/11/14
            int oasisVersionKey = oasisManagerQuestion.OasisManager.OasisVersionKey; // Numeric value, such as 14.

            currentHelp = GetHTML(oasisVersionKey);

            string oasisHelpResourceURI = currentHelp.OasisHelpResourceURI;

            if (currentHelp.HTML == null || currentHelp.HTML == string.Empty)
            {
                //DS 13022 11/11/14 MessageBox.Show("OasisHelpChildWindow.ProcessHTML error: /Virtuoso;component/Assets/Resources/Oasis_02.00_Chapter_3.htm is corrupt.");
                MessageBox.Show("[000] OasisHelpChildWindow.ProcessHTML error: " + oasisHelpResourceURI +
                                " not found.");
                return false;
            }

            ComboBox qcb = this.questionComboBox as ComboBox;
            if (qcb != null)
            {
                qcb.ItemsSource = currentHelp.ItemLinks;
            }

            if (currentHelp.ItemLinks.Count < 2 || currentHelp.ItemLinks.Count != currentHelp.Questions.Count)
            {
                MessageBox.Show(
                    "[001] OasisHelpChildWindow.ProcessHTML error: " + oasisHelpResourceURI + " is corrupt.");
                return false;
            }

            var k = oasisManagerQuestion.OasisQuestion.Question.Trim().ToLower();
            questionIndex = currentHelp.IndexOfKey(k);

            return true;
        }

        private void prevQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            BumpQuestion(-1);
        }

        private void nextQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            BumpQuestion(+1);
        }

        private int BoundedIndex(int questionIndex)
        {
            if (questionIndex < 0)
            {
                questionIndex = 0;
            }
            else
            {
                if (questionIndex >= currentHelp.Questions.Count)
                {
                    questionIndex = currentHelp.Questions.Count - 1;
                }
            }

            return questionIndex;
        }

        private void BumpQuestion(int bump)
        {
            ComboBox qcb = this.questionComboBox as ComboBox;
            if (qcb == null)
            {
                return;
            }

            qcb.SelectedIndex = BoundedIndex(qcb.SelectedIndex + bump);
        }

        private void questionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PositionToQuestion();
        }

        private void questionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            PositionToQuestion();
        }

        private void PositionToQuestion()
        {
            WebBrowser w = this.webBrowser as WebBrowser;
            ComboBox qcb = this.questionComboBox as ComboBox;

            if ((qcb == null) || (!loaded) || (currentHelp == null) || (w == null))
            {
                return;
            }

            questionIndex = BoundedIndex(qcb.SelectedIndex);
            string key = currentHelp.ItemLinks[questionIndex];
            var help = currentHelp.Questions[key];

            oasisHelpString = help.HTML;

            if (string.IsNullOrWhiteSpace(oasisHelpString))
            {
                return;
            }

            try
            {
                w.NavigateToString(oasisHelpString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                MessageBox.Show("OASIS Help navigation error:  Closing help window.  Note, you can relaunch help.");
                this.DialogResult = false;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
            }
        }
    }
}