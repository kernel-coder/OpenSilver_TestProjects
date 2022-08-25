using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Virtuoso.Core.Controls
{
    public partial class NavigateCloseDialog : ChildWindow
    {
        public Button ButtonYes
        {
            get { return this.YesButton; }
        }

        public Button ButtonNo
        {
            get { return this.NoButton; }
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage", typeof(string), typeof(NavigateCloseDialog), null);

        public string ErrorQuestion
        {
            get { return (string)GetValue(ErrorQuestionProperty); }
            set { SetValue(ErrorQuestionProperty, value); }
        }

        public static readonly DependencyProperty ErrorQuestionProperty =
            DependencyProperty.Register("ErrorQuestion", typeof(string), typeof(NavigateCloseDialog), null);

        public NavigateCloseDialog()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(NavigateCloseDialog_Loaded);
        }

        void NavigateCloseDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.ErrorMessage))
                ErrorMessageTextBox.Visibility = System.Windows.Visibility.Collapsed;
            else
                ErrorMessageTextBox.Visibility = System.Windows.Visibility.Visible;

            if (string.IsNullOrEmpty(this.ErrorQuestion))
                ErrorQuestionTextBox.Visibility = System.Windows.Visibility.Collapsed;
            else
                ErrorQuestionTextBox.Visibility = System.Windows.Visibility.Visible;
        }

        public bool NoVisible
        {
            set
            {
                if (value) NoButton.Visibility = Visibility.Visible;
                else NoButton.Visibility = Visibility.Collapsed;
            }
        }

        public bool YesVisible
        {
            set
            {
                if (value) YesButton.Visibility = Visibility.Visible;
                else YesButton.Visibility = Visibility.Collapsed;
            }
        }

        public string NoLabel
        {
            set { NoButton.Content = value; }
        }

        public string YesLabel
        {
            set { YesButton.Content = value; }
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