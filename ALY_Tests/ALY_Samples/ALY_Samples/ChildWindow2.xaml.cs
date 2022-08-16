using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class ChildWindow2 : ChildWindow
    {
        public ChildWindow2()
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

