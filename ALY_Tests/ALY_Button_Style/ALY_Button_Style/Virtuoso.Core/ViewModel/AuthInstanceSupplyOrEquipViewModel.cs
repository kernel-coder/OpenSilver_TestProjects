#region Usings

using System.Linq;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    //Class functions as DataContext for enabling VIEW: AuthDetailSupplyOrEquipView
    public class AuthInstanceSupplyOrEquipViewModel : AuthInstanceViewModelBase
    {
        public AuthInstanceSupplyOrEquipViewModel(AdmissionAuthorizationInstance instance, AuthMode mode)
            : base(VMTypeSelectorEnum.SupplyOrEquip, mode)
        {
            AuthInstanceSelectedItem = instance;
        }

        public override void CreateAuthorizationDetail(AdmissionAuthorizationInstance instance)
        {
            //TODO: figure out what to do when have no distribution...
            if (Mode == AuthMode.ADD)
            {
                //Is ADD - so modify the existing AdmissionAuthorizationDetail row?
                //Create AdmissionAuthorizationDetail as copy of AdmissionAuthorizationInstance instance
                var newDetail = new AdmissionAuthorizationDetail();
                Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, newDetail);
                instance.AdmissionAuthorizationDetail.Add(newDetail);
            }
            else
            {
                var updatedDetail = instance.AdmissionAuthorizationDetail.First(); //Will we always have 1 (and only 1)?
                Portable.Utility.DynamicCopy.CopyDataMemberProperties(instance, updatedDetail);
            }
        }
    }
}