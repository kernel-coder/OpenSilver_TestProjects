#region Usings

using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class NonServiceType
    {
        public string EditName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    return string.Format("Non Service Type {0}", Description.Trim());
                }

                return IsNew ? "New Non Service Type" : "Edit Non Service Type";
            }
        }

        // Used to display * in SearchResultsView
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public bool IsValidForHomeHealth
        {
            get
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;

                var tst = ServiceLineTypeUseBits;
                return (tst & homeHealthBit) > 0; // Is Valid for HomeHealth
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits;
                if (isSet)
                {
                    tst = tst | homeHealthBit;
                }
                else
                {
                    tst = tst & (hospiceBit | homeCareBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public bool IsValidForHospice
        {
            get
            {
                var hospiceBit = (int)eServiceLineType.Hospice;
                var tst = ServiceLineTypeUseBits;
                return (tst & hospiceBit) > 0; // Is Valid for Hospice
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits;
                if (isSet)
                {
                    tst = tst | hospiceBit;
                }
                else
                {
                    tst = tst & (homeCareBit | homeHealthBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public bool IsValidForHomeCare
        {
            get
            {
                var homeCareBit = (int)eServiceLineType.HomeCare;
                var tst = ServiceLineTypeUseBits;
                return (tst & homeCareBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits;
                if (isSet)
                {
                    tst = tst | homeCareBit;
                }
                else
                {
                    tst = tst & (hospiceBit | homeHealthBit);
                }

                ServiceLineTypeUseBits = tst;
                SharedBitChanges();
            }
        }

        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");

        partial void OnDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("EditName");
        }

        partial void OnServiceLineTypeUseBitsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            SharedBitChanges();
        }

        public void SharedBitChanges()
        {
            RaisePropertyChanged("IsValidForHomeHealth");
            RaisePropertyChanged("IsValidForHospice");
            RaisePropertyChanged("IsValidForHomeCare");
            RaisePropertyChanged("ServiceLineTypeUseBits");
        }
    }
}