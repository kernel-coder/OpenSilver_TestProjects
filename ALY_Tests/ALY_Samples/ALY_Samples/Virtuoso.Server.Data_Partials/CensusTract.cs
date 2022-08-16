#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class CensusTract
    {
        public string State => CodeLookupCache.GetCodeFromKey(StateCode);

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CensusTractID))
                {
                    return string.Format("Census Tract ID: {0}", CensusTractID.Trim());
                }

                return IsNew ? "New Census Tract" : "Edit Census Tract";
            }
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("State");
            RaisePropertyChanged("CensusTractID");
        }

        partial void OnCensusTractIDChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}-{2}", CensusTractID, State, County);
        }
    }
}