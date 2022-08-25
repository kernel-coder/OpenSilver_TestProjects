using Virtuoso.Client.Core;
using Virtuoso.Core.View;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Core.View
{
    public partial class HavenErrorWindow : PageBase
    {
        public HavenErrorWindow()
        {
            InitializeComponent();
            this.DataContext = VirtuosoContainer.Current.GetExport<HavenErrorWindowViewModel>().Value;
        }
    }
}