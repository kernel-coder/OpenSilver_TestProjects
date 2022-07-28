using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Framework;
using System.Windows.Media;

namespace Virtuoso.Core.Controls
{
#if OPENSILVER
    public class HtmlPresenterEx : CSHTML5.Native.Html.Controls.HtmlPresenter
    {
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(HtmlPresenterEx), new PropertyMetadata(false));

        public bool IsTabStop
        {
            get { return (bool)GetValue(IsTabStopProperty); }
            set { SetValue(IsTabStopProperty, value); }
        }
        public static readonly DependencyProperty IsTabStopProperty =
            DependencyProperty.Register(nameof(IsTabStop), typeof(bool), typeof(HtmlPresenterEx), new PropertyMetadata(true));

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }
        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(HtmlPresenterEx),
                new PropertyMetadata(TextWrapping.NoWrap));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }
        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(HtmlPresenterEx), new PropertyMetadata(new FontFamily("Portable User Interface")));

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(HtmlPresenterEx), new PropertyMetadata(11d));

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }
        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(HtmlPresenterEx), new PropertyMetadata(FontWeights.Normal));

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }
        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(HtmlPresenterEx), new PropertyMetadata(FontStyles.Normal));

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(HtmlPresenterEx), new PropertyMetadata(TextAlignment.Left));

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(HtmlPresenterEx), new PropertyMetadata(ScrollBarVisibility.Hidden));

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(HtmlPresenterEx), new PropertyMetadata(ScrollBarVisibility.Hidden));

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment), typeof(HtmlPresenterEx), new PropertyMetadata(HorizontalAlignment.Center));


        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.Register(nameof(VerticalContentAlignment), typeof(VerticalAlignment), typeof(HtmlPresenterEx), new PropertyMetadata(VerticalAlignment.Center));

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HtmlPresenterEx), new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(HtmlPresenterEx), new PropertyMetadata(null));

        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(HtmlPresenterEx),
                new PropertyMetadata(new Thickness()));
    }

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
            //try { this.Style = (Style)System.Windows.Application.Current.Resources["FakeCoreRichTextBoxStyle"]; }
            // catch { }            
#else
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreRichTextAreaStyle"]; }
            catch { }
#endif
            this.ErrorXamlMessage = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>vRichTextArea.Xaml parse error</Paragraph></Section>";
            this.IsHitTestVisible = false;
            this.IsReadOnly = true;
            this.IsTabStop = false;
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
                Html = paragraphText;
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
#if OPENSILVER
                this.Html = this.ErrorXamlMessage;
#else
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
}
