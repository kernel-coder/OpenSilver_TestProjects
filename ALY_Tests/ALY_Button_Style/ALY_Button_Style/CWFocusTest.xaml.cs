using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class CWFocusTest : ChildWindow
    {
        public CWFocusTest()
        {
            InitializeComponent();
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

