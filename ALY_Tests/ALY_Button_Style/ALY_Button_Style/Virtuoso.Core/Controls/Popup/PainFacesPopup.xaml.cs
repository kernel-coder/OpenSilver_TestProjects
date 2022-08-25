using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Virtuoso.Core.Services;
using Virtuoso.Core.Cache;

namespace Virtuoso.Core.Controls
{
    public partial class PainFacesPopup : UserControl
    {
        public PainFacesPopup()
        {
            InitializeComponent();
        }

        private void PainFace_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            painFace.SelectedKey = CodeLookupCache.GetKeyFromCode("PAINFACES", b.Tag.ToString());
        }
    }
}