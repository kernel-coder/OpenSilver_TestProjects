#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Server.Data
{
    public enum ComfortPackTimePoint
    {
        Current = 0,
        Future = 1,
        Removed = 2
    }

    public partial class ComfortPack
    {
        private DateTimeOffset? _LastApplyDateTime;

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ComfortPackCode))
                {
                    return string.Format("Comfort Pack {0}", ComfortPackCode.Trim());
                }

                return IsNew ? "New Comfort Pack" : "Edit Comfort Pack";
            }
        }

        public string InactiveBlirb
        {
            get
            {
                if (Inactive == false)
                {
                    return null;
                }

                var UserName = InactiveBy != null ? UserCache.Current.GetFormalNameFromUserId(InactiveBy) : "?";
                var DateFormatted = InactiveDate.HasValue ? InactiveDate.Value.ToString("MM/dd/yyyy") : "?";
                return "Inactivated By " + UserName + " on " + DateFormatted;
            }
        }

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

        public string HomeHealthPrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "2");

        public string HospicePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "4");

        public string HomeCarePrompt => CodeLookupCache.GetDescriptionFromCode("ServiceLineType", "8");

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

        public bool HasMedications
        {
            get
            {
                if (ComfortPackMedication == null || ComfortPackMedication.Count == 0)
                {
                    return false;
                }

                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                var hasMedications = ComfortPackMedication.Where(p =>
                    p.HistoryKey == null &&
                    (p.EffectiveFromDate.HasValue == false || p.EffectiveFromDate <= today) &&
                    (p.EffectiveThruDate.HasValue == false || p.EffectiveThruDate > today) &&
                    (p.ObsoleteDate.HasValue == false || p.ObsoleteDate > today)).Any();
                return hasMedications;
            }
        }

        public DateTimeOffset? LastApplyDateTime
        {
            get { return _LastApplyDateTime; }
            set
            {
                _LastApplyDateTime = value;
                RaisePropertyChanged("LastApplyDateTime");
            }
        }

        public string LastApplyDateTimeString => LastApplyDateTime == null
            ? null
            : Convert.ToDateTime(((DateTimeOffset)LastApplyDateTime).Date).ToShortDateString();

        partial void OnInactiveChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            InactiveDate = Inactive ? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified) : (DateTime?)null;
            InactiveBy = Inactive ? WebContext.Current.User.MemberID : (Guid?)null;
            RaisePropertyChanged("InactiveBlirb");
            RaisePropertyChanged("InactiveIndicator");
            RaisePropertyChanged("IsInactiveIndicator");
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
            RaisePropertyChanged("IsHomeHealth");
            RaisePropertyChanged("ServiceLineTypeUseBits");
        }
    }

    public partial class ComfortPackDiscipline
    {
        private Discipline _MyDiscipline;

        public Discipline MyDiscipline
        {
            get
            {
                if (_MyDiscipline != null)
                {
                    return _MyDiscipline;
                }

                _MyDiscipline = DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                return _MyDiscipline;
            }
        }

        public string DisciplineDescription => MyDiscipline?.Description;

        partial void OnDisciplineKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            _MyDiscipline = null;
            RaisePropertyChanged("MyDiscipline");
            RaisePropertyChanged("DisciplineDescription");
        }
    }

    public partial class ComfortPackMedication
    {
        private bool _CanSelect;
        private DateTime? _MedicationStartDate;
        private bool _Selected;

        public ComfortPackTimePoint TimePoint
        {
            get
            {
                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                var from = EffectiveFromDate?.Date;
                var thru = EffectiveThruDate?.Date;
                if (from != null && from > today)
                {
                    return ComfortPackTimePoint.Future;
                }

                if (thru != null && thru < today)
                {
                    return ComfortPackTimePoint.Removed;
                }

                return ComfortPackTimePoint.Current;
            }
        }

        public string TimePointDescription
        {
            get
            {
                if (TimePoint == ComfortPackTimePoint.Future)
                {
                    return "(future)";
                }

                if (TimePoint == ComfortPackTimePoint.Removed)
                {
                    return "(removed)";
                }

                return null;
            }
        }

        public string MedicationThumbNail => MedicationName;

        public string MedicationDosageAmountDescription => DosageFormat(MedicationDosageAmount);

        public string MedicationDosageAmountToDescription => DosageFormat(MedicationDosageAmountTo);

        public bool IsOverTheCounter => MedicationRXType == 2 ? true : false;

        public bool IsMediSpanMedication => MediSpanMedicationKey != null;

        public List<CodeLookup> ValidAdministrationRoutes
        {
            get
            {
                if (MedicationRoute.HasValue == false)
                {
                    return new List<CodeLookup>();
                }

                var children = CodeLookupCache.GetChildrenFromKey(MedicationRoute.Value).Where(p => p.Inactive == false)
                    .ToList();

                if (AdministrationRouteKey.HasValue)
                {
                    var me = CodeLookupCache.GetCodeLookupFromKey(AdministrationRouteKey.Value);
                    if (me != null && children.Any(a => a.CodeLookupKey == me.CodeLookupKey) == false)
                    {
                        children.Add(me);
                    }
                }

                return children;
            }
        }

        public string AllowDuplicatesYN => AllowDuplicates ? "Y" : "N";

        public string WouldCauseDuplicateIndicator => CanSelect ? "" : "*";

        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                RaisePropertyChanged("Selected");
            }
        }

        public bool CanSelect
        {
            get { return _CanSelect; }
            set
            {
                _CanSelect = value;
                RaisePropertyChanged("CanSelect");
                RaisePropertyChanged("WouldCauseDuplicateIndicator");
            }
        }

        public DateTime? MedicationStartDate
        {
            get { return _MedicationStartDate; }
            set
            {
                _MedicationStartDate = value;
                RaisePropertyChanged("MedicationStartDate");
            }
        }

        partial void OnEffectiveFromDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TimePoint");
            RaisePropertyChanged("TimePointDescription");
        }

        partial void OnEffectiveThruDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TimePoint");
            RaisePropertyChanged("TimePointDescription");
        }

        partial void OnMediSpanMedicationKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("IsMediSpanMedication");
        }

        partial void OnMedicationNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationThumbNail");
        }

        partial void OnMedicationDosageAmountChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDosageAmountDescription");
        }

        partial void OnMedicationDosageAmountToChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("MedicationDosageAmountToDescription");
        }

        private string DosageFormat(string doseage)
        {
            if (string.IsNullOrWhiteSpace(doseage))
            {
                return null;
            }

            double d = 0;
            try
            {
                d = Convert.ToDouble(doseage);
            }
            catch
            {
                d = 0;
            }

            // TODO Equality comparison of floating point numbers. Possible loss of precision while rounding values
            // if (Math.Abs(d - 0.5) < TOLERANCE)
            if (d == 0.5)
            {
                return "(one-half)";
            }

            // TODO Equality comparison of floating point numbers. Possible loss of precision while rounding values
            // if (Math.Abs(d - 0.25) < TOLERANCE)
            if (d == 0.25)
            {
                return "(one-quarter)";
            }

            return null;
        }

        partial void OnAsNeededChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (AsNeeded == false)
            {
                AsNeededFor = null;
            }
        }

        partial void OnMedicationDosageVaryingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (MedicationDosageVarying == false)
            {
                MedicationDosageAmountTo = null;
            }
        }

        partial void OnMedicationRouteChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            var routeCodeDescription = MedicationRoute.HasValue
                ? CodeLookupCache.GetCodeDescriptionFromKey(MedicationRoute.Value)
                : null;
            if (routeCodeDescription != null)
            {
                var administrationRouteCL = CodeLookupCache.GetCodeLookupsFromType("MedRouteAdminMethod")
                    .Where(w => w.CodeDescription == routeCodeDescription).FirstOrDefault();
                RaisePropertyChanged("ValidAdministrationRoutes");
                if (administrationRouteCL != null)
                {
                    AdministrationRouteKey = administrationRouteCL.CodeLookupKey;
                }
            }
        }

        partial void OnAllowDuplicatesChanged()
        {
            RaisePropertyChanged("AllowDuplicatesYN");
        }
    }

    public partial class ComfortPackSupply
    {
        private Supply _MySupply;

        public Supply MySupply
        {
            get
            {
                if (_MySupply != null)
                {
                    return _MySupply;
                }

                _MySupply = SupplyCache.GetSupplyFromKey(SupplyKey);
                return _MySupply;
            }
        }

        public string SupplyDescription1 => MySupply?.Description1;

        public ComfortPackTimePoint TimePoint
        {
            get
            {
                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                var from = EffectiveFromDate?.Date;
                var thru = EffectiveThruDate?.Date;
                if (from != null && from > today)
                {
                    return ComfortPackTimePoint.Future;
                }

                if (thru != null && thru < today)
                {
                    return ComfortPackTimePoint.Removed;
                }

                return ComfortPackTimePoint.Current;
            }
        }

        public string TimePointDescription
        {
            get
            {
                if (TimePoint == ComfortPackTimePoint.Future)
                {
                    return "(future)";
                }

                if (TimePoint == ComfortPackTimePoint.Removed)
                {
                    return "(removed)";
                }

                return null;
            }
        }

        public string EffectiveFromDateNote
        {
            get
            {
                if (MySupply?.EffectiveFrom == null)
                {
                    return null;
                }

                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                var from = EffectiveFromDate?.Date ?? DateTime.MinValue;
                var thru = EffectiveThruDate?.Date ?? DateTime.MaxValue;
                var supplyFrom = MySupply.EffectiveFrom == null ? DateTime.MinValue : MySupply.EffectiveFrom.Date;
                if (supplyFrom > today && (supplyFrom > from || supplyFrom > thru))
                {
                    return "Note: This supply does not take effect until " + supplyFrom.ToShortDateString();
                }

                return null;
            }
        }

        public string EffectiveThruDateNote
        {
            get
            {
                if (MySupply?.EffectiveThru == null)
                {
                    return null;
                }

                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date;
                var from = EffectiveFromDate?.Date ?? DateTime.MinValue;
                var thru = EffectiveThruDate?.Date ?? DateTime.MaxValue;
                var supplyThru = MySupply.EffectiveThru == null
                    ? DateTime.MaxValue
                    : ((DateTime)MySupply.EffectiveThru).Date;
                if (from > today && supplyThru < from || thru > today && supplyThru < thru)
                {
                    return "Note: This supply in only in effect thru " + supplyThru.ToShortDateString();
                }

                return null;
            }
        }

        partial void OnSupplyKeyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            _MySupply = null;
            RaisePropertyChanged("MySupply");
            RaisePropertyChanged("SupplyDescription1");
            RaisePropertyChanged("EffectiveFromDateNote");
            RaisePropertyChanged("EffectiveThruDateNote");
        }

        partial void OnEffectiveFromDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TimePoint");
            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("EffectiveFromDateNote");
            RaisePropertyChanged("EffectiveThruDateNote");
        }

        partial void OnEffectiveThruDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TimePoint");
            RaisePropertyChanged("TimePointDescription");
            RaisePropertyChanged("EffectiveFromDateNote");
            RaisePropertyChanged("EffectiveThruDateNote");
        }
    }
}