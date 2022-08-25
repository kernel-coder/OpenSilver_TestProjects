#region Usings

using System;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Discipline
    {
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

                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & homeHealthBit) > 0; // Is Valid for HomeHealth
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
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
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & hospiceBit) > 0; // Is Valid for Hospice
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
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
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
                return (tst & homeCareBit) > 0; // Is Valid for HomeCare
            }
            set
            {
                var homeHealthBit = (int)eServiceLineType.HomeHealth;
                var hospiceBit = (int)eServiceLineType.Hospice;
                var homeCareBit = (int)eServiceLineType.HomeCare;

                var isSet = value;
                var tst = ServiceLineTypeUseBits.HasValue ? ServiceLineTypeUseBits.Value : 0;
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

        public bool IsHomeHealth => IsValidForHomeHealth || IsValidForHomeCare;

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Code))
                {
                    return string.Format("Discipline {0}", Code.Trim());
                }

                return IsNew ? "New Discipline" : "Edit Discipline";
            }
        }

        public bool DisciplineIsAide => HCFACode == "F";

        public bool DisciplineIsSkilled
        {
            get
            {
                if (HCFACode == "A" || HCFACode == "B" || HCFACode == "C" || HCFACode == "D")
                {
                    return true;
                }

                return false;
            }
        }

        public bool DisciplineIsSN => HCFACode == "A" ? true : false;

        public string SupervisedServiceTypeLabel
        {
            get
            {
                var desc = "Assistant";
                switch (HCFACode)
                {
                    case "A":
                        desc = "LPN";
                        break;
                    case "B":
                        desc = "PTA";
                        break;
                    case "D":
                        desc = "OTA";
                        break;
                    case "F":
                        desc = "Aide";
                        break;
                }

                return desc;
            }
        }

        public string AssistantLabel
        {
            get
            {
                var label = string.Format("Using {0}'s", SupervisedServiceTypeLabel);
                if (SupervisedServiceTypeLabel == "Assistant")
                {
                    label = "Using Assistants";
                }

                return label;
            }
        }

        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Inactive)
            {
                InactiveDate = DateTime.UtcNow;
            }
            else
            {
                InactiveDate = null;
            }
        }

        partial void OnCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }

        partial void OnServiceLineTypeUseBitsChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            var hospiceBit = (int)eServiceLineType.Hospice;
            if (ServiceLineTypeUseBits == hospiceBit) // If the bits are ONLY Hospice
            {
                OASISBypass = true;
            }

            SharedBitChanges();
        }

        partial void OnHCFACodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (DisciplineIsAide)
            {
                SupportsAssistants = true;
            }

            RaisePropertyChanged("AssistantLabel");
            RaisePropertyChanged("IsDisciplineSupervisionRequired");
            RaisePropertyChanged("SupportsAssistants");
            RaisePropertyChanged("DisciplineIsAide");
        }

        public void SharedBitChanges()
        {
            RaisePropertyChanged("IsValidForHomeHealth");
            RaisePropertyChanged("IsValidForHospice");
            RaisePropertyChanged("IsValidForHomeCare");
            RaisePropertyChanged("IsHomeHealth");
            RaisePropertyChanged("ServiceLineTypeUseBits");
        }
    }
}