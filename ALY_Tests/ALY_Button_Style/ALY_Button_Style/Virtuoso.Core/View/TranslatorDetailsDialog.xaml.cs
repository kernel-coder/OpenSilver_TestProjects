using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Converters;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.View
{
    public partial class TranslatorDetailsDialog : ChildWindow
    {
        public TranslatorDetailsDialog(int? language)
        {
            InitializeComponent();

            var converter = new CodeLookupDescriptionFromKeyConverter();
            var ret = converter.Convert(language, null, null, null);

            this.languageTxt.Text = (ret != null) ? ret.ToString() : "Unknown";
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}