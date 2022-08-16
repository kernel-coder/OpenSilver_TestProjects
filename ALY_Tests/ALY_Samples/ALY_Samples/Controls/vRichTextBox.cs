#if OPENSILVER
using CSHTML5.Native.Html.Controls;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Virtuoso.Core.Framework;

namespace Virtuoso.Core.Controls
{
#if OPENSILVER
    public class vRichTextArea : HtmlPresenterEx
#else
    public class vRichTextArea : System.Windows.Controls.RichTextBox
#endif
    {
        public event EventHandler OnParagraphTextChanged;

        public string XamlError
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.vRichTextArea.XamlErrorProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.vRichTextArea.XamlErrorProperty, value); }
        }
        public static DependencyProperty XamlErrorProperty =
          DependencyProperty.Register("XamlError", typeof(string), typeof(Virtuoso.Core.Controls.vRichTextArea), null);

        public bool DisplayMessageBoxOnError
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.vRichTextArea.DisplayMessageBoxOnErrorProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.vRichTextArea.DisplayMessageBoxOnErrorProperty, value); }
        }
        public static DependencyProperty DisplayMessageBoxOnErrorProperty =
          DependencyProperty.Register("DisplayMessageBoxOnError", typeof(bool), typeof(Virtuoso.Core.Controls.vRichTextArea), null);

        public string ErrorXamlMessage { get; internal set; }
        public vRichTextArea(bool _displayMessageBoxOnError)
        {
            DisplayMessageBoxOnError = _displayMessageBoxOnError;
            init();
        }
        public vRichTextArea()
        {
            init();
        }
        void init()
        {
#if OPENSILVER
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreHtmlPresenterStyle"]; }
            catch { }
#else
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreRichTextAreaStyle"]; }
            catch { }
            this.IsReadOnly = true;
            this.IsTabStop = false;
#endif
            this.ErrorXamlMessage = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>vRichTextArea.Xaml parse error</Paragraph></Section>";
            this.IsHitTestVisible = false;
        }
        public static DependencyProperty ParagraphTextProperty =
          DependencyProperty.Register("ParagraphText", typeof(string), typeof(Virtuoso.Core.Controls.vRichTextArea),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.vRichTextArea)o).SetXamlFromParagraphText();
          }));

        public string ParagraphText
        {
            get { return ((string)(base.GetValue(Virtuoso.Core.Controls.vRichTextArea.ParagraphTextProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.vRichTextArea.ParagraphTextProperty, value); }
        }
        private void SetXamlFromParagraphText()
        {
            string paragraphText = ParagraphText;
            this.XamlError = string.Empty;
            try
            {
                //FYI - US 4131 - added xml:space="preserve"
#if OPENSILVER
                this.Html = this.ProcessHtml(paragraphText);
#else
                this.Xaml = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph xml:space=\"preserve\">" + paragraphText + "</Paragraph></Section>";
                ScrollViewer sv = this.Descendents().OfType<ScrollViewer>().FirstOrDefault();
                if (sv != null)
                {
                    this.Selection.Select(this.ContentStart, this.ContentStart); // This line of code makes sure that the text is scrolled to the top...
                }
#endif
            }
            catch (Exception xamlParseError)
            {
                this.XamlError = xamlParseError.Message;
#if !OPENSILVER
                this.Xaml = this.ErrorXamlMessage;
#endif
                if (DisplayMessageBoxOnError)
                    MessageBox.Show(String.Format("Error vRichTextArea.SetXamlFromParagraphText: Parsing Xaml: {0}.  Contact your system administrator.", paragraphText));
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnParagraphTextChanged != null) OnParagraphTextChanged(this, EventArgs.Empty);
            });
        }
    }
#if OPENSILVER
    public class HtmlPresenterEx : HtmlPresenter
    {
        public bool IsTabStop
        {
            get { return (bool)GetValue(IsTabStopProperty); }
            set { SetValue(IsTabStopProperty, value); }
        }

        public static readonly DependencyProperty IsTabStopProperty =
            DependencyProperty.Register("IsTabStop", typeof(bool), typeof(HtmlPresenterEx), new PropertyMetadata(false));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(HtmlPresenterEx), new PropertyMetadata(false));

        public SolidColorBrush Foreground
        {
            get { return (SolidColorBrush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register("Foreground", typeof(SolidColorBrush), typeof(HtmlPresenterEx), new PropertyMetadata(null));

        public SolidColorBrush Background
        {
            get { return (SolidColorBrush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(SolidColorBrush), typeof(HtmlPresenterEx), new PropertyMetadata(null));

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(HtmlPresenterEx), new PropertyMetadata(0.0));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(HtmlPresenterEx), new PropertyMetadata(null));

        public HorizontalAlignment HorizontalContentAlignment { get; set; }
        public TextWrapping TextWrapping { get; set; }
        public TextAlignment TextAlignment { get; set; }
        public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }
        public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
        public FontWeight FontWeight { get; set; }
        public Thickness Padding { get; set; }

        private string GetHtmlStyleFromStyleDictionary()
        {
            string htmlStyle = "";
            if (_styleDictionary.Any()) htmlStyle = "style=";
            foreach (var item in _styleDictionary)
            {
                htmlStyle = $"{htmlStyle}{item.Key}:{item.Value};";
            }
            return htmlStyle;
        }

        private IEnumerable<string> GetTagsList(string originalText)
        {
            var expression = "<(“[^”]*”|'[^’]*’|[^'”>])*>";
            var regex = new Regex(expression);
            var result = regex.Matches(originalText).Cast<Match>().Select(u => u.Value);
            return result;
        }

        private void SetPropertiesFromControlStyle()
        {
            Style currentStyle = this.Style as Style;

            foreach (Setter setter in currentStyle.Setters)
            {
                //setting properties only if they are not already set by control using inline properties
                if (setter.Property == HtmlPresenterEx.ForegroundProperty && this.Foreground == null)
                    AddPropertyToStyleDictionary("color", setter.Value.ToString());
                else if (setter.Property == HtmlPresenterEx.FontSizeProperty && this.FontSize == 0)
                    AddPropertyToStyleDictionary("font-size", this.FontSize);
                else if (setter.Property == HtmlPresenterEx.FontFamilyProperty && this.FontFamily == null)
                    AddPropertyToStyleDictionary("font-family", this.FontFamily);
            }
        }

        private void AddPropertyToStyleDictionary(string property, object value)
        {
            if (string.IsNullOrWhiteSpace(value.ToString())) return;
            if (_styleDictionary.ContainsKey(property))
            {
                _styleDictionary[property] = value.ToString();
            }
            else
            {
                _styleDictionary.Add(property, value.ToString());
            }
        }

        Dictionary<string, string> _styleDictionary = new Dictionary<string, string>();
        public string ProcessHtml(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText)) return "";
            var tagsList = GetTagsList(originalText);

            if (!_styleDictionary.Any()) //if once style is determine then it won't get processed again for this instance of rich text 
            {
                //by this point, if there are inline styles of properties they will be set to dependency properties
                string foreground = this.Foreground.ToString();
                if (foreground.StartsWith("#")) foreground = HexToColor(foreground);

                AddPropertyToStyleDictionary("color", foreground);
                AddPropertyToStyleDictionary("font-size", this.FontSize.ToString() + "px");
                AddPropertyToStyleDictionary("font-family", this.FontFamily.ToString());

                SetPropertiesFromControlStyle();
                ReadInlinePropertiesFromTags(tagsList);
            }
            string htmlStyle = GetHtmlStyleFromStyleDictionary();
            StringBuilder plainText = new StringBuilder(originalText);

            foreach (var tag in tagsList)
            {
                //notice how we are not closing any tag like <Bold, to handle the situation of inline styles in these tags
                if (tag.StartsWith("<Bold")) plainText.Replace(tag, $"<b {htmlStyle}>");
                if (tag.StartsWith("<Underline")) plainText.Replace(tag, $"<u {htmlStyle}>");
                if (tag.StartsWith("<Italic")) plainText.Replace(tag, $"<i {htmlStyle}>");
                if (tag.StartsWith("<Run")) plainText.Replace(tag, $"<span {htmlStyle}>");
            }
            //all closing tags here, for them simple string replacement will work
            plainText.Replace("</Bold>", $"</b>");
            plainText.Replace("</Underline>", "</u>");
            plainText.Replace("</Italic>", "</i>");
            plainText.Replace("</Run>", "</span>");
            plainText.Replace("<LineBreak/>", "<br/>");

            var result = plainText.ToString();
            return result;
        }
        private void ReadInlinePropertiesFromTags(IEnumerable<string> tags)
        {
            var expression = "(\\w+)=(\"[^<>\"]*\"|'[^<>']*'|\\w+)";
            foreach (var tag in tags)
            {
                var matches = Regex.Matches(tag, expression);
                foreach (var match in matches)
                {
                    var values = match.ToString().Split('=');
                    if (values.Count() == 2)
                    {

                        if (values.First() == "Foreground")
                        {
                            var colorText = values.Last().ToString();
                            if (colorText.Length >= 2 && (colorText[0] == '\'' || colorText[0] == '"') && colorText[0] == colorText[colorText.Length - 1])
                            {
                                string colorName = colorText.Substring(1, colorText.Length - 2);
                                if (colorName.StartsWith("#"))
                                    AddPropertyToStyleDictionary("color", HexToColor(colorName));
                                else
                                    AddPropertyToStyleDictionary("color", colorName);
                            }
                            else
                                AddPropertyToStyleDictionary("color", colorText);
                        }
                        else if (values.First() == "FontSize")
                        {
                            string fontSize = values.Last().ToString();
                            if ((fontSize[0] == '\'' || fontSize[0] == '"') && fontSize[0] == fontSize[fontSize.Length - 1])
                            {
                                fontSize = fontSize.Substring(1, fontSize.Length - 2);
                            }
                            AddPropertyToStyleDictionary("font-size", fontSize + "px");
                        }
                    }
                }
            }
        }

        public static string HexToColor(string colorCode)
        {
            if (string.IsNullOrWhiteSpace(colorCode)) return "";
            colorCode = colorCode.TrimStart('#');

            byte a = (byte)int.Parse(colorCode.Substring(0, 2), NumberStyles.HexNumber);
            byte r = (byte)int.Parse(colorCode.Substring(2, 2), NumberStyles.HexNumber);
            byte g = (byte)int.Parse(colorCode.Substring(4, 2), NumberStyles.HexNumber);
            byte b = (byte)int.Parse(colorCode.Substring(6, 2), NumberStyles.HexNumber);
            return $"rgba({r},{g},{b},{a})";
        }
    }
#else
    //this is a dummy class to resolve the issue of missing type in silverlight version
    //missing type exception is raised from style for HTMLPresenterEx from CoreStyles.xaml
    public class HtmlPresenterEx : TextBox
    {
    }
#endif
}