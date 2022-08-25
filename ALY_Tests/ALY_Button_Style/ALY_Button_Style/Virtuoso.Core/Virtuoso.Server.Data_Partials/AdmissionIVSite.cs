#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionIVSite
    {
        private Encounter _currentEncounter;

        public string InfiltrationPhlebitisScaleURL =>
            "http://atitesting.com/ati_next_gen/skillsmodules/content/iv-therapy/equipment/assessing-iv.html";

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
                    if (IsNew || AdmissionIVSiteKey <= 0)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsNew || AdmissionIVSiteKey <= 0)
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
                if (IsNew || AdmissionIVSiteKey <= 0)
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

        public string IVInfiltrationScaleCode
        {
            get
            {
                if (IVInfiltrationScale == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey((int)IVInfiltrationScale);
            }
        }

        public string IVPhlebitisScaleCode
        {
            get
            {
                if (IVPhlebitisScale == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey(IVPhlebitisScale);
            }
        }

        public string IVThumbNail
        {
            get
            {
                var ivThumbNail = string.Format("{0} - {1},  {2}{3}{4}",
                    Number.ToString(),
                    string.IsNullOrWhiteSpace(IVTypeDescription) ? "Type ?" : IVTypeDescription.Trim(),
                    string.IsNullOrWhiteSpace(IVLocationDescription) ? "Location ?" : IVLocationDescription.Trim(),
                    IVInsertionChangeDate == null || IVInsertionChangeDate == DateTime.MinValue
                        ? ""
                        : ",  Inserted/Changed on " + ((DateTime)IVInsertionChangeDate).ToShortDateString(),
                    IVDiscontinueDate == null || IVDiscontinueDate == DateTime.MinValue
                        ? ""
                        : ",  Discontinued on " + ((DateTime)IVDiscontinueDate).ToShortDateString());
                return ivThumbNail;
            }
        }

        public AdmissionIVSite CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newwound = (AdmissionIVSite)Clone(this);
            OfflineIDGenerator.Instance.SetKey(newwound);
            if (newwound.HistoryKey == null)
            {
                newwound.HistoryKey = AdmissionIVSiteKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newwound;
        }

        partial void OnAdmissionIVSiteKeyChanged()
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

        partial void OnDeletedDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if ((DeletedDate == null || DeletedDate == null) == false && DeletedBy == null)
            {
                DeletedBy = WebContext.Current.User.MemberID;
            }
            else if ((DeletedDate == null || DeletedDate == null) && DeletedBy != null)
            {
                DeletedBy = null;
            }

            RaisePropertyChanged("DeletedBy");
        }

        partial void OnNumberChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IVThumbNail");
        }

        partial void OnIVTypeDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IVThumbNail");
        }

        partial void OnIVLocationDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IVThumbNail");
        }

        partial void OnIVInsertionChangeDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IVThumbNail");
        }

        partial void OnIVDiscontinueDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IVThumbNail");
        }
    }
}