#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class HospiceDisciplineDischarge
    {
        public string DisciplineDischargeReasonCode => CodeLookupCache.GetCodeFromKey(DisciplineDischargeReason);

        public int? ConvertedDisciplineDischargeReason
        {
            get
            {
                int? clKey = null;
                var rCode = DisciplineDischargeReasonCode;
                if (string.IsNullOrWhiteSpace(rCode))
                {
                    return clKey;
                }

                rCode = rCode.ToLower();
                if (rCode.Equals("disciplinegoalsmet"))
                {
                    clKey = GetPATDISCHARGEREASON("GOALS MET");
                }
                else if (rCode.Equals("patient/familyrequest"))
                {
                    clKey = GetPATDISCHARGEREASON("Request");
                }
                else if (rCode.Equals("refusedservices"))
                {
                    clKey = GetPATDISCHARGEREASON("RefusedSer");
                }
                else if (rCode.Equals("idtdecision"))
                {
                    clKey = GetPATDISCHARGEREASON("IDTDec");
                }

                return clKey ?? DisciplineDischargeReason;
            }
        }

        partial void OnDisciplineDischargeReasonChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("DisciplineDischargeReasonCode");
        }

        public int? GetPATDISCHARGEREASON(string code)
        {
            return CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", code);
        }
    }
}