using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Virtuoso.Core.Controls
{
    public partial class HelpPopupDialog : ChildWindow
    {
        public HelpPopupDialog(Paragraph templateContent, string dialogTitle)
        {
            InitializeComponent();

            this.Title = dialogTitle;
            //contCtrl.Content = templateContent;

            //ds 06/03 Revisit Databinding this value to a "<Run/> element
            richTemplateContent.Blocks.Add(templateContent);
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }

    public static class RichTextHelper
    {
        #region "Rich Text Box Helper Functions"

        public static Span BulletLine(string content)
        {
            var plainContent = new Span();

            plainContent.Inlines.Add(BulletText(content));
            plainContent.Inlines.Add(NewLine());

            return plainContent;
        }


        public static Span BulletLineSub(string content)
        {
            var plainContent = new Span();

            plainContent.Inlines.Add(BulletSubText(content));
            plainContent.Inlines.Add(NewLine());

            return plainContent;
        }

        public static Span PlainLine(string content)
        {
            var plainContent = new Span();

            plainContent.Inlines.Add(PlainText(content));
            plainContent.Inlines.Add(NewLine());

            return plainContent;
        }

        public static Span BoldLine(string content)
        {
            var boldContent = new Span();

            boldContent.Inlines.Add(BoldText(content));
            boldContent.Inlines.Add(NewLine());

            return boldContent;
        }

        public static Run PlainText(string content)
        {
            var plainLineText = new Run { Text = content };
            return plainLineText;
        }

        public static Span BulletText(string content)
        {
            var bulletContent = new Span();
            var bulletDecoration = new Run { Text = "o	" };
            var bulletText = new Run { Text = content };
            bulletContent.Inlines.Add(bulletDecoration);
            bulletContent.Inlines.Add(bulletText);
            return bulletContent;
        }

        public static Run BulletSubText(string content)
        {
            var bulletSubText = new Run { Text = "     -	" + content };
            return bulletSubText;
        }

        public static Bold BoldText(string content)
        {
            var contentFormatting = new Bold();
            var contentText = new Run { Text = content };
            contentFormatting.Inlines.Add(contentText);


            return contentFormatting;
        }

        public static LineBreak NewLine()
        {
            return new LineBreak();
        }

        public static Hyperlink Hyperlink(string linkUri, string linkText)
        {
            var linkTitle = new Run { Text = linkText };


            var hypTest = new Hyperlink();
            hypTest.NavigateUri = new Uri(linkUri, UriKind.Absolute);
            hypTest.Inlines.Add(linkTitle);

            return hypTest;
        }

        #endregion
    }
}