using Virtuoso.Server.Data;

namespace Virtuoso.Core.ViewModel
{
    //Class functions as DataContext for enabling VIEW: AuthDetailSupplyOrEquipView
    public class AuthDetailSupplyOrEquipViewModel : AuthDetailViewModelBase
    {
        public AuthDetailSupplyOrEquipViewModel(AdmissionAuthorizationDetail detail)
            : base(VMTypeSelectorEnum.SupplyOrEquip)
        {
            //SelectedAuthType = authType;
            AuthDetailSelectedItem = detail;
        }

        AuthType SelectedAuthType { get; set; } //maybe refactor to base class
        public override void AuthTypeChanged(AuthType selectedAuthType)
        {
            base.AuthTypeChanged(selectedAuthType);
        }
    }
}
