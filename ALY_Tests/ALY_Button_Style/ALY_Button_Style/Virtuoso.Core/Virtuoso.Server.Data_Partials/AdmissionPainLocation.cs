#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Occasional;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionPainLocation
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

        public override bool CanFullEdit
        {
            get
            {
                if (CurrentEncounter == null)
                {
                    // Not part of an encounter (regular patient maint) - can fully edit only new items
                    if (IsNew || AdmissionPainLocationKey <= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsNew || AdmissionPainLocationKey <= 0)
                {
                    return true;
                }

                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public override bool CanDelete
        {
            get
            {
                // Can delete new items that were OKed 
                if (IsNew || AdmissionPainLocationKey <= 0)
                {
                    return IsOKed;
                }

                if (CurrentEncounter == null)
                    // Not part of an encounter (regular patient maint) - can fully edit/delete only new items
                {
                    return CanFullEdit;
                }

                // Part of an encounter- can delete item that was added during this encounter
                return AddedFromEncounterKey == CurrentEncounter.EncounterKey ? true : false;
            }
        }

        public bool CanEditResolved
        {
            get
            {
                if (CurrentEncounter == null || Resolved == false || ResolvedFromEncounterKey == null)
                {
                    return true;
                }

                return CurrentEncounter.EncounterKey == ResolvedFromEncounterKey ? true : false;
            }
        }

        public string EditOrView => CanEditResolved ? "Edit" : "View";

        public bool PainInterferenceIsNullOrWhiteSpaceOrNone => string.IsNullOrWhiteSpace(PainInterference) ? true :
            PainInterference.ToLower().Equals("none") ? true : false;

        public bool PainFrequencyLess =>
            PainFrequency == null ? false : PainFrequency.ToLower().Equals("less") ? true : false;

        public bool PainFrequencyDaily =>
            PainFrequency == null ? false : PainFrequency.ToLower().Equals("daily") ? true : false;

        public bool PainFrequencyAll =>
            PainFrequency == null ? false : PainFrequency.ToLower().Equals("all") ? true : false;

        public vLabelForceRequired ForceRequiredFirstIdentifiedDate =>
            Version == 1 ? vLabelForceRequired.No : vLabelForceRequired.Yes;

        public bool ShowResolved => FirstIdentifiedDate == null ? false : true;

        public string ResolvedBlirb => ResolvedDate == null
            ? null
            : string.Format("On {0} by {1}", ((DateTime)ResolvedDate).ToShortDateString(),
                UserCache.Current.GetFormalNameFromUserId(ResolvedBy));

        public bool ShowPainDuration => Version > 2;

        public AdmissionPainLocation CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newpain = (AdmissionPainLocation)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newpain); //newpain.AdmissionPainLocationKey = 0;
            if (newpain.HistoryKey == null)
            {
                newpain.HistoryKey = AdmissionPainLocationKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newpain;
        }

        partial void OnPainRadiatesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (PainRadiates == true)
            {
                return;
            }

            RadiatesToLocation = null;
        }

        partial void OnAdmissionPainLocationKeyChanged()
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

        partial void OnVersionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ForceRequiredFirstIdentifiedDate");
        }

        partial void OnFirstIdentifiedDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (FirstIdentifiedDate == null)
            {
                Resolved = false;
            }

            RaisePropertyChanged("ShowResolved");
        }

        partial void OnResolvedChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Resolved)
            {
                ResolvedDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                ResolvedBy = WebContext.Current.User.MemberID;
                ResolvedFromEncounterKey = CurrentEncounter == null ? (int?)null : CurrentEncounter.EncounterKey;
            }
            else
            {
                ResolvedDate = null;
                ResolvedBy = null;
                ResolvedFromEncounterKey = null;
            }

            RaisePropertyChanged("ResolvedBlirb");
        }
    }
}