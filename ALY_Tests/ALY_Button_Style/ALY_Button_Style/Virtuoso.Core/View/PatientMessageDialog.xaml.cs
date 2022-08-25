using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class PatientMessageDialog : ChildWindow
    {
        public PatientMessageDialog(string pFullNameInformal, string pPatientMessages)
        {
            InitializeComponent();
            this.Title = pFullNameInformal + " Messages";
            vRichTextAreaPatientMessages.ParagraphText = ((string.IsNullOrWhiteSpace(pPatientMessages))
                ? "No patient messages found."
                : pPatientMessages);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}