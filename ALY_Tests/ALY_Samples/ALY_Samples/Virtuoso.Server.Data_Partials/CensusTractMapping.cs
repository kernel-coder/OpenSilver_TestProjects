#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class CensusTractMapping
    {
        private string _CensusTractID;

        public string CensusTractID
        {
            get
            {
                if (string.IsNullOrEmpty(_CensusTractID))
                {
                    var ct = CensusTractCache.GetCensusTract(CensusTractKey);
                    if (ct != null && ct.CensusTractID != null)
                    {
                        _CensusTractID = ct.CensusTractID;
                    }

                    return _CensusTractID;
                }

                return _CensusTractID;
            }
        }

        partial void OnCensusTractKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            _CensusTractID = string.Empty;
            RaisePropertyChanged("CensusTractID");
        }

        public override string ToString()
        {
            if (CensusTract == null)
            {
                return CensusTractMappingKey.ToString();
            }

            return CensusTract.CensusTractID;
        }
    }
}