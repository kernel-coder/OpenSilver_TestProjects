using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Virtuoso.Core.Controls
{
    public partial class UploadEncounterPopup : ChildWindow
    {
        public Button ButtonYes
        {
            get { return this.YesButton; }
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage", typeof(string), typeof(UploadEncounterPopup), null);

        public string ErrorQuestion
        {
            get { return (string)GetValue(ErrorQuestionProperty); }
            set { SetValue(ErrorQuestionProperty, value); }
        }

        private bool _Discard = false;

        public bool Discard
        {
            get { return _Discard; }
            set { _Discard = value; }
        }

        public static readonly DependencyProperty ErrorQuestionProperty =
            DependencyProperty.Register("ErrorQuestion", typeof(string), typeof(UploadEncounterPopup), null);

        public UploadEncounterPopup()
        {
            InitializeComponent();
            Discard = false;
            this.Loaded += new RoutedEventHandler(UploadEncounterPopup_Loaded);
        }

        void UploadEncounterPopup_Loaded(object sender, RoutedEventArgs e)
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

        public string OKLabel
        {
            set { YesButton.Content = value; }
        }

        public string CancelLabel
        {
            set { NoButton.Content = value; }
        }

        public double OKWidth
        {
            set { YesButton.Width = value; }
        }

        public double CancelWidth
        {
            set { NoButton.Width = value; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Discard = false;
            this.DialogResult = false;
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            Discard = true;
            this.DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Discard = false;
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