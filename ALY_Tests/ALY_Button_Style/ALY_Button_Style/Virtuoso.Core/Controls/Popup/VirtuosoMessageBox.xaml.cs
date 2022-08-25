using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.Controls
{
    public partial class VirtuosoMessageBox : ChildWindow
    {
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(VirtuosoMessageBox), null);

        public VirtuosoMessageBox(string message)
        {
            InitializeComponent();

            this.Message = message;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}