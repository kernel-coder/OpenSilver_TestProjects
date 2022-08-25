#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionHospiceDischarge
    {
        public string DischargeReasonCode => CodeLookupCache.GetCodeFromKey(DischargeReason);

        public string DischargeReasonDesc => CodeLookupCache.GetCodeDescriptionFromKey(DischargeReason);

        public bool ShowRevokeReason
        {
            get
            {
                if (DischargeReasonCodeIsRevoked == false)
                {
                    return false;
                }

                if (RevokeReason != null && RevokeReason > 0)
                {
                    return true;
                }

                return Version >= 2 ? true : false;
            }
        }

        public bool ShowTransferReason
        {
            get
            {
                if (DischargeReasonCodeIsTransferred == false)
                {
                    return false;
                }

                if (TransferReason != null && TransferReason > 0)
                {
                    return true;
                }

                return Version >= 2 ? true : false;
            }
        }

        public string LocationOfDeathCode => CodeLookupCache.GetCodeFromKey(LocationOfDeath);

        public string HospiceTransferTypeCode => CodeLookupCache.GetCodeFromKey(HospiceTransferType);

        public string HospicePresentAtDeathCode => HospicePresentAtDeath == null ? "?" : (bool)HospicePresentAtDeath ? "Yes" : "No";

        public bool ShowExpiredYes
        {
            get
            {
                if (DischargeReasonCodeIsExpired == false)
                {
                    return false;
                }

                if (HospicePresentAtDeath == null || HospicePresentAtDeath == false)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowExpiredNo
        {
            get
            {
                if (DischargeReasonCodeIsExpired == false)
                {
                    return false;
                }

                if (HospicePresentAtDeath == null || HospicePresentAtDeath == true)
                {
                    return false;
                }

                return true;
            }
        }

        public bool ShowExpiredYesOrNo => ShowExpiredYes || ShowExpiredNo;

        private bool DischargeReasonCodeIsExpired => string.IsNullOrWhiteSpace(DischargeReasonCode)
            ? false
            : DischargeReasonCode.ToLower().Equals("expired");

        private bool DischargeReasonCodeIsRevoked => string.IsNullOrWhiteSpace(DischargeReasonCode)
            ? false
            : DischargeReasonCode.ToLower().Equals("revoked");

        private bool DischargeReasonCodeIsTransferred => string.IsNullOrWhiteSpace(DischargeReasonCode)
            ? false
            : DischargeReasonCode.ToLower().Equals("transferred");

        public bool DischargeReasonCodeIsAdministrative => string.IsNullOrWhiteSpace(DischargeReasonCode)
            ? false
            : DischargeReasonCode.ToLower().Equals("administrative");

        public int? ConvertedDischargeReason
        {
            get
            {
                int? clKey = null;
                var rCode = DischargeReasonCode;
                if (string.IsNullOrWhiteSpace(rCode))
                {
                    return clKey;
                }

                rCode = rCode.ToLower();
                if (rCode.Equals("expired"))
                {
                    var lCode = LocationOfDeathCode;
                    if (string.IsNullOrWhiteSpace(lCode) == false)
                    {
                        lCode = lCode.ToLower();
                        if (lCode.Equals("home"))
                        {
                            clKey = GetPATDISCHARGEREASON("HOSPDIED40");
                        }
                        else if (lCode.Equals("medicalfacility"))
                        {
                            clKey = GetPATDISCHARGEREASON("HOSPDIED41");
                        }
                        else if (lCode.Equals("unknown"))
                        {
                            clKey = GetPATDISCHARGEREASON("HOSPDIED42");
                        }
                    }
                }
                else if (rCode.Equals("revoked"))
                {
                    clKey = GetPATDISCHARGEREASON("HOSREVOKE");
                }
                else if (rCode.Equals("notterminal"))
                {
                    clKey = GetPATDISCHARGEREASON("01");
                }
                else if (rCode.Equals("moved"))
                {
                    clKey = GetPATDISCHARGEREASON("HOSMOVED");
                }
                else if (rCode.Equals("transferred"))
                {
                    var tCode = HospiceTransferTypeCode;
                    if (string.IsNullOrWhiteSpace(tCode) == false)
                    {
                        tCode = tCode.ToLower();
                        if (tCode.Equals("homehospice"))
                        {
                            clKey = GetPATDISCHARGEREASON("50");
                        }
                        else if (tCode.Equals("medical"))
                        {
                            clKey = GetPATDISCHARGEREASON("51");
                        }
                    }
                }
                else if (rCode.Equals("forcause"))
                {
                    clKey = GetPATDISCHARGEREASON("HOSCAUSE");
                }
                else if (rCode.Equals("administrative"))
                {
                    clKey = GetPATDISCHARGEREASON("HOSPADM");
                }

                return clKey ?? DischargeReason;
            }
        }

        partial void OnDischargeReasonChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (DischargeReasonCodeIsExpired == false)
            {
                OnHospicePresentAtDeathChanged();
            }

            if (DischargeReasonCodeIsRevoked == false)
            {
                RevokeReason = null;
            }

            if (DischargeReasonCodeIsTransferred == false)
            {
                TransferReason = null;
            }

            RaisePropertyChanged("DischargeReasonCode");
            RaisePropertyChanged("DischargeReasonDesc");
            RaisePropertyChanged("ShowRevokeReason");
            RaisePropertyChanged("ShowTransferReason");
        }

        partial void OnLocationOfDeathChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("LocationOfDeathCode");
        }

        partial void OnHospiceTransferTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospiceTransferTypeCode");
        }

        partial void OnHospicePresentAtDeathChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("HospicePresentAtDeathCode");
            RaisePropertyChanged("ShowExpiredYes");
            RaisePropertyChanged("ShowExpiredNo");
            RaisePropertyChanged("ShowExpiredYesOrNo");
        }

        public int? GetPATDISCHARGEREASON(string code)
        {
            return CodeLookupCache.GetKeyFromCode("PATDISCHARGEREASON", code);
        }
    }
}