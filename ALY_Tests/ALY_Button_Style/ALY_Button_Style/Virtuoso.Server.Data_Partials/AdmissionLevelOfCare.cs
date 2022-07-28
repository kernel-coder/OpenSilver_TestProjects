#region Usings

using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionLevelOfCare
    {
        private Encounter _currentEncounter;

        public Encounter CurrentEncounter
        {
            get { return _currentEncounter; }
            set
            {
                _currentEncounter = value;
                RaisePropertyChanged("CanFullEdit");
                RaisePropertyChanged("CanDelete");
            }
        }

        public override bool CanFullEdit => CurrentEncounter != null;

        public override bool CanDelete
        {
            get
            {
                // Can delete new items that were OKed 
                if (IsNew || AdmissionLevelOfCareKey <= 0)
                {
                    return IsOKed;
                }

                return CurrentEncounter != null;
            }
        }

        public string LevelOfCareCode => CodeLookupCache.GetCodeFromKey(LevelOfCare);

        public bool LevelOfCareIsRoutine
        {
            get
            {
                var locCode = CodeLookupCache.GetCodeFromKey(LevelOfCare);
                if (string.IsNullOrWhiteSpace(locCode))
                {
                    return true; // Assume routine if null
                }

                return locCode.ToLower() == "routine" ? true : false;
            }
        }

        public bool LevelOfCareLocationIsInpatientHospice
        {
            get
            {
                var lCode = CodeLookupCache.GetCodeFromKey(LevelOfCareLocation);
                if (string.IsNullOrWhiteSpace(lCode))
                {
                    return false;
                }

                return lCode.ToLower() == "hospice" || lCode.ToLower() == "certhospice" ? true : false;
            }
        }

        public string LevelOfCareDescription => CodeLookupCache.GetCodeDescriptionFromKey(LevelOfCare);

        public string LevelOfCareLocationCode => CodeLookupCache.GetCodeFromKey(LevelOfCareLocation);

        public string LevelOfCareLocationDescription => CodeLookupCache.GetCodeDescriptionFromKey(LevelOfCareLocation);

        public AdmissionLevelOfCare CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newLOC = (AdmissionLevelOfCare)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newLOC);
            if (newLOC.HistoryKey == null)
            {
                newLOC.HistoryKey = AdmissionLevelOfCareKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newLOC;
        }

        partial void OnAdmissionLevelOfCareKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
        }

        partial void OnAddedFromEncounterKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CanFullEdit");
            RaisePropertyChanged("CanDelete");
        }

        partial void OnLevelOfCareChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("LevelOfCareCode");
            RaisePropertyChanged("LevelOfCareDescription");
        }

        partial void OnLevelOfCareLocationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("LevelOfCareLocationCode");
            RaisePropertyChanged("LevelOfCareLocationDescription");
        }
    }
}