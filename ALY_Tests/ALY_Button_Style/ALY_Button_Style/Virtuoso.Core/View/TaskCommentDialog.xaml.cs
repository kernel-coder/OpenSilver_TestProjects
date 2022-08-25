using System.Windows;
using System.Windows.Controls;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class TaskCommentDialog : ChildWindow
    {
        public TaskCommentDialog(string pFullNameInformal, string pTaskComments)
        {
            InitializeComponent();
            this.Title = pFullNameInformal + " Comments";
            vRichTextAreaTaskComments.ParagraphText =
                ((string.IsNullOrWhiteSpace(pTaskComments)) ? "No comments found." : pTaskComments);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}