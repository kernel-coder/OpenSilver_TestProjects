using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class InformationButtonDialog : ChildWindow
    {
        public InformationButtonDialog(string Title, string innerText)
        {
            InitializeComponent();
            this.InnerText.ParagraphText = innerText;
            this.Title = Title;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}