using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class ComboItemsTestDlg : ChildWindow
    {
        public ComboItemsTestDlg()
        {
            InitializeComponent();

            for (int i = 0; i < 30; i++)
            {
                searchItemComboBox.Items.Add($"Item {i+1}");

            }

            //for (int i = 0; i < 30; i++)
            //{
            //    comboMulti.Items.Add($"Item {i + 1}");
            //}
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

