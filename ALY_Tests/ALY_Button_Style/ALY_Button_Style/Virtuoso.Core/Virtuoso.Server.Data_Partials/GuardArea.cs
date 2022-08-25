#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class GuardArea
    {
        public string State
        {
            get
            {
                var state = CodeLookupCache.GetCodeFromKey(StateCode);
                if (state == null)
                {
                    state = "";
                }

                return state;
            }
        }

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(GuardAreaID))
                {
                    return string.Format("Guard Area {0}", GuardAreaID.Trim());
                }

                return IsNew ? "New Guard Area" : "Edit Guard Area";
            }
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("State");
            RaisePropertyChanged("GuardAreaID");
        }


        partial void OnGuardAreaIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }
    }
}