#region Usings

using System.Collections.Generic;
using System.ComponentModel.Composition;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class HavenErrorItem
    {
        public string ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public string ErrorType { get; set; }
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class HavenErrorWindowViewModel : ViewModelBase, INavigateClose
    {
        private List<HavenErrorItem> _HavenErrorList;

        public List<HavenErrorItem> HavenErrorList
        {
            get { return _HavenErrorList; }
            set
            {
                _HavenErrorList = value;
                this.RaisePropertyChangedLambda(p => p.HavenErrorList);
            }
        }

        public void NavigateClose()
        {
            NavigateBack();
        }
    }
}