using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Virtuoso.Core.Controls
{
    public partial class NavigateCloseDialogWithRich : ChildWindow
    {
        public Button ButtonYes
        {
            get { return this.YesButton; }
        }

        public string ErrorMessageHeader
        {
            get { return (string)GetValue(ErrorMessageHeaderProperty); }
            set { SetValue(ErrorMessageHeaderProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageHeaderProperty =
            DependencyProperty.Register("ErrorMessageHeader", typeof(string), typeof(NavigateCloseDialogWithRich),
                null);

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage", typeof(string), typeof(NavigateCloseDialogWithRich), null);


        public string RichTextMessage
        {
            get { return (string)GetValue(RichTextMessageProperty); }
            set { SetValue(RichTextMessageProperty, value); }
        }

        public static readonly DependencyProperty RichTextMessageProperty =
            DependencyProperty.Register("RichTextMessage", typeof(string), typeof(NavigateCloseDialogWithRich), null);

        public NavigateCloseDialogWithRich()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(NavigateCloseDialog_Loaded);
        }

        void NavigateCloseDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ErrorMessageTextBox.ParagraphText))
                ErrorMessageTextBox.Visibility = System.Windows.Visibility.Collapsed;
            else
                ErrorMessageTextBox.Visibility = System.Windows.Visibility.Visible;

            if (string.IsNullOrEmpty(ErrorQuestionRichTextMessage.ParagraphText))
                ErrorQuestionRichTextMessage.Visibility = System.Windows.Visibility.Collapsed;
            else
                ErrorQuestionRichTextMessage.Visibility = System.Windows.Visibility.Visible;
        }

        public bool NoVisible
        {
            set
            {
                if (value) NoButton.Visibility = Visibility.Visible;
                else NoButton.Visibility = Visibility.Collapsed;
            }
        }

        public string OKLabel
        {
            set { YesButton.Content = value; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
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
}