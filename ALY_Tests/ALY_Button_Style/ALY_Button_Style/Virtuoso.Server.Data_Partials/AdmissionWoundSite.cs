#region Usings

using System;
using System.Linq;
using System.Windows.Media;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Occasional;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionWoundSite
    {
        public enum SelectModeValue
        {
            NotShown,
            DisplaySelectedPartOnSilhouette,
            DisplayPartOnOptionsMenu
        }

        private Encounter _currentEncounter;
        private string _M1309orM1313ThumbNail;
        private int? _OasisStatus;

        private SelectModeValue selectMode = SelectModeValue.NotShown;

        public string PrintTemplateToUse => "WoundPopupV" + Version + "Print";

        public bool IsBrandNew
        {
            get
            {
                // IsBrandNew = IsFirstNew = (now added as part of a superceed operation)
                if (IsNew == false)
                {
                    return false;
                }

                if (HistoryKey != null)
                {
                    return false;
                }

                return true;
            }
        }

        public Color DrawColor => IsHealed ? Colors.DarkGray : Colors.Red;

        public bool IsMeasurementValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Length))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Width))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Depth))
                {
                    return false;
                }

                return true;
            }
        }

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
                    if (IsBrandNew)
                    {
                        return true;
                    }

                    return false;
                }

                // Part of an encounter- can edit new items and any item that was added during this encounter
                if (IsBrandNew)
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
                if (IsBrandNew)
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

        public string DrainageColorCode
        {
            get
            {
                if (DrainageColor == null)
                {
                    return null;
                }

                return CodeLookupCache.GetCodeFromKey((int)DrainageColor);
            }
        }

        public string PressureUlcerStageCode => CodeLookupCache.GetCodeFromKey(PressureUlcerStage);

        public int? WoundTissueType
        {
            get
            {
                if (Version == 1)
                {
                    return WoundTissueTypeV1;
                }

                return WoundTissueTypeV2;
            }
        }

        public string WoundTissueTypeCode
        {
            get
            {
                if (Version == 1)
                {
                    return CodeLookupCache.GetCodeFromKey(WoundTissueTypeV1);
                }

                return CodeLookupCache.GetCodeFromKey(WoundTissueTypeV2);
            }
        }

        public string WoundTissueTypeDescription
        {
            get
            {
                if (Version == 1)
                {
                    return CodeLookupCache.GetCodeDescriptionFromKey(WoundTissueTypeV1);
                }

                return CodeLookupCache.GetCodeDescriptionFromKey(WoundTissueTypeV2);
            }
        }

        public bool ShouldShowWoundSeverity =>
            Version != 1 && (IsTypeDiabeticUlcer || IsTypeVenousUlcer || IsTypeArterialUlcer);

        public string WoundStatusCode => CodeLookupCache.GetCodeFromKey(WoundStatus);

        public bool IsTypeOther
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WoundTypeCode))
                {
                    return true;
                }

                if (IsTypeBurn || IsTypeLaceration || IsTypePressureUlcer || IsTypeSurgicalWound || IsTypeStasisUlcer)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsTypeBurn => WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("burn");

        public bool IsTypeLaceration => WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("laceration");

        public bool IsTypePressureUlcer =>
            WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("pressureulcer");

        public bool IsTypePressureUlcerButNotD1orD2orD3
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("d.1") || c.Equals("d.2") || c.Equals("d.3") ? false : true;
            }
        }

        public bool IsTypeArterialUlcer =>
            WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("arterialulcer");

        public bool IsTypeVenousUlcer => WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("venousulcer");

        public bool IsTypeDiabeticUlcer => WoundTypeDescription == null
            ? false
            : WoundTypeDescription.ToLower().Equals("diabeticulcer") ||
              WoundTypeDescription.ToLower().Equals("diabetic ulcer");

        public bool IsTypeSurgicalWound =>
            WoundTypeCode == null ? false : WoundTypeCode.ToLower().Equals("surgicalwound");

        public bool IsTypeStasisUlcer => WoundTypeCode == null
            ? false
            : WoundTypeCode.ToLower().Equals("venousulcer") || WoundTypeCode.ToLower().Equals("arterialulcer");

        public bool IsObservable
        {
            get
            {
                var c = WoundStatusCode;
                return string.IsNullOrWhiteSpace(c) ? true : c.ToLower().Equals("observable") ? true : false;
            }
        }

        public bool IsWoundTissueTypeUnhealed => IsHealed ? false : true;

        public bool IsPressureUlcerStageI
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("1.") ? true : false;
            }
        }

        public bool IsPressureUlcerStageII
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("a.") ? true : false;
            }
        }

        public bool IsPressureUlcerStageIII
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("b.") ? true : false;
            }
        }

        public bool IsPressureUlcerStageIV
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("c.") ? true : false;
            }
        }

        public bool IsUnhealedStatisUlcerObservable
        {
            get
            {
                if (IsTypeStasisUlcer == false)
                {
                    return false;
                }

                if (IsHealed)
                {
                    return false;
                }

                return IsObservable;
            }
        }

        public bool IsUnhealedSurgicalWoundObservable
        {
            get
            {
                if (IsTypeSurgicalWound == false)
                {
                    return false;
                }

                if (IsHealed)
                {
                    return false;
                }

                return IsObservable;
            }
        }

        public bool IsUnhealedPressureUlcerStageIIorHigherObservable
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsHealed)
                {
                    return false;
                }

                if (IsObservable == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("1.") ? false : true;
            }
        }

        public bool IsUnhealingPressureUlcerStageIIorHigher
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c == null ? false : c.Equals("1.") ? false : true;
            }
        }

        public bool IsUnhealingPressureUlcerStageIIorHigherObservable
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                if (IsPressureUlcerObservable == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                if (c == null)
                {
                    return false;
                }

                return c.ToLower().Equals("d.1") ? false : c.Equals("1.") ? false : true;
            }
        }

        public bool IsUnhealingStageablePressureUlcerObservable
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                if (IsPressureUlcerObservable == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                if (c == null)
                {
                    return false;
                }

                if (c.ToLower().Equals("d.1"))
                {
                    return false;
                }

                if (c.ToLower().Equals("d.2"))
                {
                    return false;
                }

                if (c.ToLower().Equals("d.3"))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsUnhealingPressureUlcer
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                return PressureUlcerStageCode != null;
            }
        }

        public bool IsUnhealingPressureUlcerStageI
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("1.") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerStageII
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("a.") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerStageIII
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("b.") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerStageIV
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("c.") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerNSTG_DRSG
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("d.1") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerNSTG_CVRG
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("d.2") ? true : false);
            }
        }

        public bool IsUnhealingPressureUlcerNSTG_DEEP_TISUE
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                return c != null && (c.ToLower().Equals("d.3") ? true : false);
            }
        }

        public string M1309orM1313ThumbNail
        {
            get { return _M1309orM1313ThumbNail; }
            set
            {
                _M1309orM1313ThumbNail = value;
                RaisePropertyChanged("M1309orM1313ThumbNail");
            }
        }

        public bool IsUnhealingPressureUlcerM1310
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                if (IsWoundTissueTypeUnhealed == false)
                {
                    return false;
                }

                var c = PressureUlcerStageCode;
                if (c == null)
                {
                    return false;
                }

                c = c.ToLower();
                return c.Equals("b.") || c.Equals("c.") || c.Equals("d.2") ? true : false;
            }
        }

        public bool IsWoundTissueTypeGranulating
        {
            get
            {
                var c = WoundTissueTypeCode;
                return !string.IsNullOrWhiteSpace(c) && (c.ToLower().Contains("granulating") ? true : false);
            }
        }

        public bool IsPressureUlcerObservable
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                return IsObservable;
            }
        }

        public bool IsPressureUlcerUnobservable
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return false;
                }

                return IsObservable == false;
            }
        }

        public bool IsStasisUlcerObservable
        {
            get
            {
                if (IsTypeStasisUlcer == false)
                {
                    return false;
                }

                return IsObservable;
            }
        }

        public bool IsStasisUlcerUnobservable
        {
            get
            {
                if (IsTypeStasisUlcer == false)
                {
                    return false;
                }

                return IsObservable == false;
            }
        }

        public bool IsSurgicalWoundObservable
        {
            get
            {
                if (IsTypeSurgicalWound == false)
                {
                    return false;
                }

                return IsObservable;
            }
        }

        public bool IsSurgicalWoundUnobservable
        {
            get
            {
                if (IsTypeSurgicalWound == false)
                {
                    return false;
                }

                return IsObservable == false;
            }
        }

        public bool IsDrainageAmount => string.IsNullOrWhiteSpace(DrainageAmountCode) ? false :
            DrainageAmountCode.ToLower().Equals("none") ? false : true;

        public double SurfaceArea
        {
            get
            {
                if (string.IsNullOrEmpty(Width) || string.IsNullOrEmpty(Length))
                {
                    return 0;
                }

                var surfaceArea = 0.0;
                try
                {
                    surfaceArea = double.Parse(string.Format("{0:00.0}", double.Parse(Width))) *
                                  double.Parse(string.Format("{0:00.0}", double.Parse(Length)));
                }
                catch
                {
                }

                return surfaceArea;
            }
        }

        public int M1320sequence
        {
            get
            {
                CalculateOasisStatus();
                if (OasisStatus == null)
                {
                    return 5;
                }

                return (int)OasisStatus;
            }
        }

        public int M1324sequence
        {
            get
            {
                if (IsTypePressureUlcer == false)
                {
                    return 5;
                }

                var c = PressureUlcerStageCode;
                if (c == null)
                {
                    return 5;
                }

                c = c.ToLower();
                if (c.Equals("1."))
                {
                    return 1;
                }

                if (c.Equals("a."))
                {
                    return 2;
                }

                if (c.Equals("b."))
                {
                    return 3;
                }

                if (c.Equals("c."))
                {
                    return 4;
                }

                return 5;
            }
        }

        public int M1334sequence
        {
            get
            {
                CalculateOasisStatus();
                if (OasisStatus == null)
                {
                    return 0; // note - not all versions of OASIS support sequence 0 - this is trapped in the OasisManager
                }

                return (int)OasisStatus;
            }
        }

        public int M1342sequence => M1334sequence;

        public bool IsOther
        {
            get
            {
                if (WoundTypeCode == null)
                {
                    return false;
                }

                if (IsTypePressureUlcer || IsTypeStasisUlcer || IsTypeSurgicalWound)
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsHealed
        {
            get { return HealedDate == null || HealedDate == null ? false : true; }
            set
            {
                if (value == false && (HealedDate == null || HealedDate == null) == false)
                {
                    HealedDate = null;
                }
                else if (value && (HealedDate == null || HealedDate == null))
                {
                    if (CurrentEncounter == null)
                    {
                        HealedDate = DateTime.Today;
                    }
                    else if (CurrentEncounter.EncounterStartDate == null)
                    {
                        HealedDate = DateTime.Today;
                    }
                    else
                    {
                        HealedDate = CurrentEncounter.EncounterOrTaskStartDateAndTime.Value.Date;
                    }

                    MostProblematic = false;
                }
            }
        }

        public bool HasSiteSelected
        {
            get
            {
                var key = 0;

                if (Site > 0)
                {
                    key = Site;
                }

                return key > 0;
            }
        }

        public int WoundCountAtSite => 0;

        public string MultipleWoundsExistAtThisSiteMessage
        {
            get
            {
                if (WoundCountAtSite > 1)
                {
                    return "* Multiple Wounds Exist At This Site";
                }

                return string.Empty;
            }
        }

        public SelectModeValue SelectMode
        {
            get { return selectMode; }
            set
            {
                if (selectMode != value)
                {
                    selectMode = value;
                    UpdateAniManUI();
                }
            }
        }

        public bool DisplayingPartOnSilhouette => SelectMode == SelectModeValue.DisplaySelectedPartOnSilhouette;

        public bool AniManOptionSelectMode => SelectMode == SelectModeValue.DisplayPartOnOptionsMenu;

        public string WoundSiteDescription
        {
            get
            {
                var woundLocationKey = Site;
                var desc = WoundLocationCache.Current.GetWoundLocationDescriptionFromKey(woundLocationKey);
                return desc;
            }
        }

        public string WoundTypeCondencedDescription
        {
            get
            {
                if (IsTypeStasisUlcer)
                {
                    return "Stasis Ulcer";
                }

                if (IsTypePressureUlcer)
                {
                    return "Pressure Ulcer/Injury";
                }

                return WoundTypeCode == null ? "" : WoundTypeDescription;
            }
        }

        public int? PushSurfaceArea
        {
            get
            {
                if (string.IsNullOrEmpty(Width) || string.IsNullOrEmpty(Length))
                {
                    return null;
                }

                var surfaceArea = 0.0;
                try
                {
                    surfaceArea = double.Parse(string.Format("{0:00.0}", double.Parse(Width))) *
                                  double.Parse(string.Format("{0:00.0}", double.Parse(Length)));
                }
                catch
                {
                    return null;
                }

                if (surfaceArea == 0)
                {
                    return 0;
                }

                if (surfaceArea < 0.3)
                {
                    return 1;
                }

                if (surfaceArea <= 0.6)
                {
                    return 2;
                }

                if (surfaceArea <= 1.0)
                {
                    return 3;
                }

                if (surfaceArea <= 2.0)
                {
                    return 4;
                }

                if (surfaceArea <= 3.0)
                {
                    return 5;
                }

                if (surfaceArea <= 4.0)
                {
                    return 6;
                }

                if (surfaceArea <= 8.0)
                {
                    return 7;
                }

                if (surfaceArea <= 12.0)
                {
                    return 8;
                }

                if (surfaceArea <= 24.0)
                {
                    return 9;
                }

                return 10;
            }
        }

        public int? PushExudateAmount
        {
            get
            {
                if (string.IsNullOrEmpty(DrainageAmountCode))
                {
                    return null;
                }

                var c = DrainageAmountCode.ToLower();
                if (c.Equals("none"))
                {
                    return 0;
                }

                if (c.Equals("light"))
                {
                    return 1;
                }

                if (c.Equals("moderate"))
                {
                    return 2;
                }

                if (c.Equals("heavy"))
                {
                    return 3;
                }

                return null;
            }
        }

        public int? PushTissueType
        {
            get
            {
                if (string.IsNullOrEmpty(WoundTissueTypeCode))
                {
                    return null;
                }

                var c = WoundTissueTypeCode.ToLower();
                if (c.Equals("closed"))
                {
                    return 0;
                }

                if (c.Equals("epithelial"))
                {
                    return 1;
                }

                if (c.Contains("granulating"))
                {
                    if (WoundPercentEschar != null)
                    {
                        return 4;
                    }

                    if (WoundPercentSlough != null)
                    {
                        return 3;
                    }

                    return 2;
                }

                return null;
            }
        }

        public string PushScoreDescription
        {
            get
            {
                if (PushScore == null)
                {
                    return "";
                }

                return PushScore.ToString();
            }
        }

        public bool IsUnobservable
        {
            get
            {
                var c = WoundStatusCode;
                return string.IsNullOrWhiteSpace(c) ? false : c.ToLower().Equals("observable") ? false : true;
            }
            set
            {
                if (value)
                {
                    WoundStatus = CodeLookupCache.GetKeyFromCode("WOUNDSTATUS", "Unobservable");
                }
                else
                {
                    WoundStatus = CodeLookupCache.GetKeyFromCode("WOUNDSTATUS", "Observable");
                }
            }
        }

        public int? PercentAvascular { get; set; }

        public int? OasisStatus
        {
            get { return _OasisStatus; }
            set
            {
                _OasisStatus = value;
                RaisePropertyChanged("OasisStatus");
                RaisePropertyChanged("OasisStatusCode");
            }
        }

        public string OasisStatusCode
        {
            get
            {
                if (OasisStatus == null)
                {
                    return "Unknown";
                }

                if (OasisStatus == 1)
                {
                    return "Newly epithelialized";
                }

                if (OasisStatus == 2)
                {
                    return "Fully granulating";
                }

                if (OasisStatus == 3)
                {
                    return "Early/partial granulation";
                }

                if (OasisStatus == 4)
                {
                    return "Not Healing";
                }

                return "Unknown";
            }
        }

        public void RefreshRaiseChanged()
        {
            RaisePropertyChanged("Site");
        }

        public AdmissionWoundSite CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var _healedDate = HealedDate; // problem with IsHealed derived property resetting HealedDate - which resets HealedBy
            var _HealedBy = HealedBy;
            var newwound = (AdmissionWoundSite)Clone(this);
            newwound.HealedDate = _healedDate;
            newwound.HealedBy = _HealedBy;
            OfflineIDGenerator.Instance.SetKey(newwound);
            if (newwound.HistoryKey == null)
            {
                newwound.HistoryKey = AdmissionWoundSiteKey;
            }

            RejectChanges();
            BeginEditting();
            Superceded = true;
            EndEditting();
            return newwound;
        }

        partial void OnAdmissionWoundSiteKeyChanged()
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

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_STG2ThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerStageII == false)
            {
                return null; // not currently a stage 2
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Stage 2 PU"; // a new stage 2 since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return "Increased from Stage 1 to Stage 2 PU"; // increased in numerical stage
            }

            return null;
        }

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_STG3ThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerStageIII == false)
            {
                return null; // not currently a stage 3
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Stage 3 PU"; // a new stage 3 since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return "Increased from Stage 1 to Stage 3 PU"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageII)
            {
                return "Increased from Stage 2 to Stage 3 PU"; // increased in numerical stage
            }

            return null;
        }

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_STG4ThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerStageIV == false)
            {
                return null; // not currently a stage 4
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Stage 4 PU"; // a new stage 4 since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return "Increased from Stage 1 to Stage 4 PU"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageII)
            {
                return "Increased from  Stage 2 to Stage 4 PU"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageIII)
            {
                return "Increased from  Stage 3 to Stage 4 PU"; // increased in numerical stage
            }

            return null;
        }

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSGThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerNSTG_DRSG == false)
            {
                return null; // not currently an 'unstagable due to slough/eschar'
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Unstageable PU (due to non-removable dressing)"; // a new Unstageable since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return
                    "Changed from Stage 1 to Unstageable PU (due to non-removable dressing)"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageII)
            {
                return
                    "Changed from Stage 2 to Unstageable PU (due to non-removable dressing)"; // increased in numerical stage
            }

            return null;
        }

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRGThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerNSTG_CVRG == false)
            {
                return null; // not currently an 'unstagable due to slough/eschar'
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Unstageable PU (due to slough/eschar)"; // a new Unstageable since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return "Changed from Stage 1 to Unstageable PU (due to slough/eschar)"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageII)
            {
                return "Changed from Stage 2 to Unstageable PU (due to slough/eschar)"; // increased in numerical stage
            }

            return null;
        }

        public string M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUEThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            if (IsUnhealingPressureUlcerNSTG_DEEP_TISUE == false)
            {
                return null; // not currently an 'unstagable due to slough/eschar'
            }

            var ewsPrior = mostRecentSOCROCEncounter.EncounterWoundSite.Where(ew =>
                AdmissionWoundSiteKey > 0 && (ew.AdmissionWoundSiteKey == AdmissionWoundSiteKey ||
                                              ew.AdmissionWoundSiteKey == HistoryKey || HistoryKey != null &&
                                              ew.AdmissionWoundSite.HistoryKey == HistoryKey)).FirstOrDefault();
            if (ewsPrior == null)
            {
                return "New Unstageable PU (due to deep tissue injury)"; // a new Unstageable since last ROC/SOC
            }

            // Existed prior
            var awsPrior = ewsPrior.AdmissionWoundSite;
            if (awsPrior == null)
            {
                return null; // no AdmissionWoundSite attached (should never happen)
            }

            if (awsPrior.IsPressureUlcerStageI)
            {
                return
                    "Changed from Stage 1 to Unstageable PU (due to deep tissue injury)"; // increased in numerical stage
            }

            if (awsPrior.IsPressureUlcerStageII)
            {
                return
                    "Changed from Stage 2 to Unstageable PU (due to deep tissue injury)"; // increased in numerical stage
            }

            return null;
        }

        public string GetM1309orM1313ThumbNail(Encounter mostRecentSOCROCEncounter)
        {
            var thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_STG2ThumbNail(mostRecentSOCROCEncounter);
            if (string.IsNullOrWhiteSpace(thumbNail) == false)
            {
                return thumbNail;
            }

            thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_STG3ThumbNail(mostRecentSOCROCEncounter);
            if (string.IsNullOrWhiteSpace(thumbNail) == false)
            {
                return thumbNail;
            }

            thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_STG4ThumbNail(mostRecentSOCROCEncounter);
            if (string.IsNullOrWhiteSpace(thumbNail) == false)
            {
                return thumbNail;
            }

            thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DRSGThumbNail(mostRecentSOCROCEncounter);
            if (string.IsNullOrWhiteSpace(thumbNail) == false)
            {
                return thumbNail;
            }

            thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_CVRGThumbNail(mostRecentSOCROCEncounter);
            if (string.IsNullOrWhiteSpace(thumbNail) == false)
            {
                return thumbNail;
            }

            thumbNail = M1309orM1313_NBR_NEW_WRS_PRSULC_NSTG_DEEP_TISUEThumbNail(mostRecentSOCROCEncounter);
            return thumbNail;
        }

        partial void OnHealedDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if ((HealedDate == null || HealedDate == null) == false && HealedBy == null)
            {
                HealedBy = WebContext.Current.User.MemberID;
            }
            else if ((HealedDate == null || HealedDate == null) && HealedBy != null)
            {
                HealedBy = null;
            }

            RaisePropertyChanged("HealedBy");
            RaisePropertyChanged("IsHealed");
        }

        partial void OnLengthChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePushScore();
        }

        partial void OnWidthChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePushScore();
        }

        partial void OnDrainageAmountCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePushScore();
            RaisePropertyChanged("IsDrainageAmount");
            if (string.IsNullOrWhiteSpace(DrainageAmountCode))
            {
                DrainageAmountDescription = null;
                DrainageColor = null;
                DrainageOderCode = null;
                DrainageOderDescription = null;
            }
        }

        partial void OnWoundTissueTypeV1Changed()
        {
            if (IsDeserializing)
            {
                return;
            }

            OnWoundTissueTypeChanged();
        }

        partial void OnWoundTissueTypeV2Changed()
        {
            if (IsDeserializing)
            {
                return;
            }

            OnWoundTissueTypeChanged();
        }

        private void OnWoundTissueTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsWoundTissueTypeGranulating");
            CalculatePushScore();
            CalculateOasisStatus();
        }

        partial void OnWoundPercentEscharChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePercentAvascular();
            CalculatePushScore();
        }

        partial void OnWoundPercentGranulationChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePushScore();
            CalculateOasisStatus();
        }

        partial void OnWoundPercentSloughChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculatePercentAvascular();
            CalculatePushScore();
        }

        partial void OnWoundTypeCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            // Wound consult pulling incorrect information clear dependent columns when wound type changes
            if (IsTypeBurn == false)
            {
                BurnDegree = null;
            }

            if (IsTypePressureUlcer == false)
            {
                PressureUlcerStage = null;
            }

            if (IsTypeSurgicalWound == false)
            {
                WoundClosure = null;
            }

            if ((IsTypeStasisUlcer || IsTypePressureUlcer || IsTypeSurgicalWound) == false)
            {
                MostProblematic = false;
            }

            CalculateObservable();
            CalculatePushScore();
            RaisePropertyChanged("IsTypePressureUlcer");
            RaisePropertyChanged("IsTypePressureUlcerButNotD1orD2orD3");
            RaisePropertyChanged("WoundTypeCondencedDescription");
            RaisePropertyChanged("ShouldShowWoundSeverity");
        }

        partial void OnWoundTypeDescriptionChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("WoundTypeCondencedDescription");
            RaisePropertyChanged("ShouldShowWoundSeverity");
        }

        partial void OnTunnelingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Tunneling == false)
            {
                TunnelingDescription = null;
            }
        }

        partial void OnUnderminingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (Undermining == false)
            {
                UnderminingDescription = null;
            }
        }

        partial void OnWoundStatusChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (IsObservable == false)
            {
                WoundSeverity = null;
                MostProblematic = false;
                ContributingFactor = null;
                Length = null;
                Width = null;
                Depth = null;
                Undermining = false;
                UnderminingDescription = null;
                Tunneling = false;
                TunnelingDescription = null;
                WoundTissueTypeV1 = null;
                WoundTissueTypeV2 = null;
                WoundPercentGranulation = null;
                WoundPercentEschar = null;
                WoundPercentSlough = null;
                BurnDegree = null;
                WoundClosure = null;
                DrainageAmountCode = null;
                DrainageAmountDescription = null;
                DrainageColor = null;
                DrainageOderCode = null;
                DrainageOderDescription = null;
                WoundEdges = null;
                WoundEdgesCodes = null;
                SurroundingTissueCodes = null;
                SurroundingTissueDescription = null;
                SignsOfInfection = null;
                SignsOfInfectionCodes = null;
                Treatment = null;
            }

            CalculatePushScore();
            CalculateOasisStatus();
            RaisePropertyChanged("IsObservable");
            RaisePropertyChanged("IsUnobservable");
        }

        partial void OnPressureUlcerStageChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            //OASIS C2if (PressureUlcerStageCode == "1.") MostProblematic = false;
            RaisePropertyChanged("IsTypePressureUlcerButNotD1orD2orD3");
            CalculateObservable();
            CalculatePushScore();
        }

        partial void OnWoundEdgesCodesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculateOasisStatus();
        }

        partial void OnSignsOfInfectionCodesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            CalculateOasisStatus();
        }

        public void UpdateAniManUI()
        {
            RaisePropertyChanged("Site");
            RaisePropertyChanged("WoundCountAtSite");
            RaisePropertyChanged("MultipleWoundsExistAtThisSiteMessage");
            RaisePropertyChanged("WoundSiteDescription");
            RaisePropertyChanged("AniManOptionSelectMode");

            RaisePropertyChanged("DisplayingPartOnSilhouette");
            RaisePropertyChanged("AniManShown");
            RaisePropertyChanged("AniManLeftDetailShown");
            RaisePropertyChanged("AniManRightDetailShown");
            RaisePropertyChanged("WoundSiteCountMessage");
            RaisePropertyChanged("HasSiteSelected");
        }

        public void SetSite(int siteid)
        {
            if (siteid != Site)
            {
                Site = siteid;
                UpdateAniManUI();
            }
        }

        private void CalculatePushScore()
        {
            if (IsPressureUlcerObservable == false)
            {
                PushScore = null;
            }
            else if (PushSurfaceArea == null || PushExudateAmount == null || PushTissueType == null)
            {
                PushScore = null;
            }
            else
            {
                PushScore = PushSurfaceArea + PushExudateAmount + PushTissueType;
            }

            RaisePropertyChanged("PushScore");
            RaisePropertyChanged("PushScoreDescription");
        }

        private void CalculateObservable()
        {
            if (WoundStatus == null)
            {
                // So always - by default - the Unobservable checkbox is unchecked
                WoundStatus = CodeLookupCache.GetKeyFromCode("WOUNDSTATUS", "Observable");
            }

            RaisePropertyChanged("WoundStatus");
        }

        private void CalculatePercentAvascular()
        {
            PercentAvascular = null;

            //Version 2 because granulation/eschar/slough are available for all tissue types
            if (IsWoundTissueTypeGranulating || Version == 2) 
            {
                if (WoundPercentEschar != null && WoundPercentSlough != null)
                {
                    PercentAvascular = WoundPercentEschar + WoundPercentSlough;
                }
                else if (WoundPercentEschar != null)
                {
                    PercentAvascular = WoundPercentEschar;
                }
                else if (WoundPercentSlough != null)
                {
                    PercentAvascular = WoundPercentSlough;
                }
            }

            RaisePropertyChanged("PercentAvascular");
            CalculateOasisStatus();
        }

        private void CalculateOasisStatus()
        {
            if (IsObservable == false)
                // Unobservable, undetermined oasis status
            {
                OasisStatus = null;
            }

            // regardless of granulation or not, check for not healing (wound edges closed or signs of infection
            if (string.IsNullOrEmpty(WoundEdgesCodes) == false)
            {
                var c = WoundEdgesCodes.ToLower();
                if (c.Equals("closed"))
                {
                    OasisStatus = 4;
                    return;
                }
            }

            if (string.IsNullOrEmpty(SignsOfInfectionCodes) == false)
            {
                var c = SignsOfInfectionCodes.ToLower();
                if (c.Equals("none") == false)
                {
                    OasisStatus = 4;
                    return;
                }
            }

            // fell thru - wound edges are NOT closed (null, open or other) and no signs of infection
            // Check tissue type
            if (string.IsNullOrEmpty(WoundTissueTypeCode) == false)
            {
                var c = WoundTissueTypeCode.ToLower();
                if (c.Equals("closed") || c.Equals("epithelial"))
                {
                    // not granulating - assume newly epithelialized
                    OasisStatus = 1;
                    return;
                }

                // fell thru - its granulating check for not healing
                if (PercentAvascular != null)
                {
                    if (PercentAvascular >= 25)
                    {
                        OasisStatus = 4;
                        return;
                    }

                    if (PercentAvascular > 0)
                    {
                        OasisStatus = 3;
                        return;
                    }
                }

                // fell thru - no PercentAvascular (is null or 0)
                // check for early/partial granulation
                if (WoundPercentGranulation != null)
                {
                    if (WoundPercentGranulation >= 25)
                    {
                        OasisStatus = 3;
                        return;
                    }
                }

                // fell thru - assume fully granulating
                if (Version == 2 &&
                    c.Contains(
                        "earlygranulating")) //unless type for Version 2 wound is EarlyGranulating - 'Early/partial granulation'
                {
                    OasisStatus = 3;
                }
                else if
                    (Version == 2 &&
                     c.Contains("not healing")) //unless type for Version 2 wound is EarlyGranulating - 'Not Healing'
                {
                    OasisStatus = 4;
                }
                else
                {
                    OasisStatus = 2;
                }

                return;
            }

            // No wound tissue type, undetermined oasis status
            OasisStatus = null;
        }

        public void DoCalculates()
        {
            CalculateObservable();
            CalculatePercentAvascular();
            CalculatePushScore();
            CalculateOasisStatus();
        }
    }

    public partial class WoundPhoto
    {
        private int _clientSequence;

        public int ClientSequence
        {
            get { return _clientSequence; }
            set
            {
                _clientSequence = value;
                RaisePropertyChanged("ClientSequence");
            }
        }
    }
}