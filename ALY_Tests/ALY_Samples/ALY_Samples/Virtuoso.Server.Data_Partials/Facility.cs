#region Usings

using System;
using Virtuoso.Core.Cache;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Facility
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

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return string.Format("Facility {0}", Name.Trim());
                }

                return IsNew ? "New Facility" : "Edit Facility";
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

        public string TypeCode => CodeLookupCache.GetCodeFromKey(Type);

        public string TypeCodeDescription => CodeLookupCache.GetCodeDescriptionFromKey(Type);

        public string CMSCertificationNumberWrapper
        {
            get { return CMSCertificationNumber; }
            set
            {
                string tmpCMS = null;
                if (value != null)
                {
                    tmpCMS = value.Replace("-", "");
                    tmpCMS = tmpCMS.Substring(0, tmpCMS.Length <= 6 ? tmpCMS.Length : 6);
                }

                // ugly slight of hand to kick the bindings to get the converter to be executed.
                CMSCertificationNumber = "       ";
                CMSCertificationNumber = tmpCMS;
                RaisePropertyChanged("CMSCertificationNumberWrapper");
            }
        }

        partial void OnNameChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }

        partial void OnPatientID1LabelChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!IsInCancel)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(PatientID1Label))
            {
                PatientID1Label = PatientID2Label;
                PatientID2Label = PatientID3Label;
                PatientID3Label = null;
                RaisePropertyChanged("PatientID1Label");
                RaisePropertyChanged("PatientID2Label");
                RaisePropertyChanged("PatientID3Label");
            }
        }

        partial void OnPatientID2LabelChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!IsInCancel)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(PatientID2Label))
            {
                PatientID2Label = PatientID3Label;
                PatientID3Label = null;
                RaisePropertyChanged("PatientID2Label");
                RaisePropertyChanged("PatientID3Label");
            }
        }

        partial void OnPatientID3LabelChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (!IsInCancel)
            {
                return;
            }

            if (PatientID3Label == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(PatientID3Label))
            {
                PatientID3Label = null;
                RaisePropertyChanged("PatientID3Label");
            }
        }

        partial void OnCityChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
            RaisePropertyChanged("StateCodeCode");
        }

        partial void OnZipCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("CityStateZip");
        }

        partial void OnTypeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TypeCode");
            RaisePropertyChanged("TypeCodeDescription");
        }
    }

    public partial class FacilityBranch
    {
        private bool _isEnabled;
        private bool _isVisible;

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public bool IsEnabled
        {
            get { return _isEnabled; }

            set
            {
                _isEnabled = value;
                RaisePropertyChanged("IsEnabled");
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                RaisePropertyChanged("IsVisible");
            }
        }

        public string CityStateZip => FormatHelper.FormatCityStateZip(City, StateCodeCode, ZipCode);

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

        partial void OnStateCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("StateCodeCode");
        }
    }
}