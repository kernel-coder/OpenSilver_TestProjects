using System;
using System.Collections.Generic;
using System.Text;
using Virtuoso.Core.Controls;

namespace Virtuoso.Server.Data
{
    public sealed partial class AdmissionPainLocation : VirtuosoEntity
    {

        public AdmissionPainLocation()
        {
        }


        // Alternate constructor - use for creating objects without generating an offline identity
        public AdmissionPainLocation(bool skipOnCreatedAndOfflineIDGeneration)
        {

        }

        // Start AddedFromEncounterKey //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(19)]
        [System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        public System.Nullable<int> AddedFromEncounterKey
        {
            get
            {
                return this._addedFromEncounterKey;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._addedFromEncounterKey = value; return; }
                if (this._addedFromEncounterKey != value)
                {
                    this.RaiseDataMemberChanging("AddedFromEncounterKey");
                    this.ValidateProperty("AddedFromEncounterKey", value);
                    this._addedFromEncounterKey = value;
                    this.RaiseDataMemberChanged("AddedFromEncounterKey");
                }
            }
        }
        private System.Nullable<int> _addedFromEncounterKey;
        // End AddedFromEncounterKey //
        // Start AdmissionKey //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(2)]
        [System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        public int AdmissionKey
        {
            get
            {
                return this._admissionKey;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._admissionKey = value; return; }
                if (this._admissionKey != value)
                {
                    this.RaiseDataMemberChanging("AdmissionKey");
                    this.ValidateProperty("AdmissionKey", value);
                    this._admissionKey = value;
                    this.RaiseDataMemberChanged("AdmissionKey");
                }
            }
        }
        private int _admissionKey;
        // End AdmissionKey //
        // Start AdmissionPainLocationKey //
        [System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue = true)]
        [System.ComponentModel.DataAnnotations.KeyAttribute()]
        [ProtoBuf.ProtoMemberAttribute(1)]
        [System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        public int AdmissionPainLocationKey
        {
            get
            {
                return this._admissionPainLocationKey;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._admissionPainLocationKey = value; return; }
                if (this._admissionPainLocationKey != value)
                {
                    this.ValidateProperty("AdmissionPainLocationKey", value);
                    this._admissionPainLocationKey = value;
                    this.RaisePropertyChanged("AdmissionPainLocationKey");
                }
            }
        }
        private int _admissionPainLocationKey;
        // End AdmissionPainLocationKey //
        // Start DeletedBy //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(15)]
        public System.Nullable<System.Guid> DeletedBy
        {
            get
            {
                return this._deletedBy;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._deletedBy = value; return; }
                if (this._deletedBy != value)
                {
                    this.RaiseDataMemberChanging("DeletedBy");
                    this.ValidateProperty("DeletedBy", value);
                    this._deletedBy = value;
                    this.RaiseDataMemberChanged("DeletedBy");
                }
            }
        }
        private System.Nullable<System.Guid> _deletedBy;
        // End DeletedBy //
        // Start DeletedDate //
        public System.Nullable<System.DateTime> DeletedDate
        {
            get
            {
                return this._deletedDate;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._deletedDate = value; return; }
                if (this._deletedDate != value)
                {
                    this.RaiseDataMemberChanging("DeletedDate");
                    this.ValidateProperty("DeletedDate", value);
                    this._deletedDate = value;
                    this.RaiseDataMemberChanged("DeletedDate");
                }
            }
        }
        private System.Nullable<System.DateTime> _deletedDate;
        // End DeletedDate //
        // Start FirstIdentifiedDate //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Date First Identified")]
        [ProtoBuf.ProtoMemberAttribute(24)]
        public System.Nullable<System.DateTime> FirstIdentifiedDate
        {
            get
            {
                return this._firstIdentifiedDate;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._firstIdentifiedDate = value; return; }
                if (this._firstIdentifiedDate != value)
                {
                    this.RaiseDataMemberChanging("FirstIdentifiedDate");
                    this.ValidateProperty("FirstIdentifiedDate", value);
                    this._firstIdentifiedDate = value;
                    this.RaiseDataMemberChanged("FirstIdentifiedDate");
                }
            }
        }
        private System.Nullable<System.DateTime> _firstIdentifiedDate;
        // End FirstIdentifiedDate //
        // Start HistoryKey //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(4)]
        public System.Nullable<int> HistoryKey
        {
            get
            {
                return this._historyKey;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._historyKey = value; return; }
                if (this._historyKey != value)
                {
                    this.RaiseDataMemberChanging("HistoryKey");
                    this.ValidateProperty("HistoryKey", value);
                    this._historyKey = value;
                    this.RaiseDataMemberChanged("HistoryKey");
                }
            }
        }
        private System.Nullable<int> _historyKey;
        // End HistoryKey //
        // Start PainAggravating //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Aggravating Factors")]
        [ProtoBuf.ProtoMemberAttribute(12)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public string PainAggravating
        {
            get
            {
                return this._painAggravating;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painAggravating = value; return; }
                if (this._painAggravating != value)
                {
                    this.RaiseDataMemberChanging("PainAggravating");
                    this.ValidateProperty("PainAggravating", value);
                    this._painAggravating = value;
                    this.RaiseDataMemberChanged("PainAggravating");
                }
            }
        }
        private string _painAggravating;
        // End PainAggravating //
        // Start PainAlleviating //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Alleviating Factors")]
        [ProtoBuf.ProtoMemberAttribute(11)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public string PainAlleviating
        {
            get
            {
                return this._painAlleviating;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painAlleviating = value; return; }
                if (this._painAlleviating != value)
                {
                    this.RaiseDataMemberChanging("PainAlleviating");
                    this.ValidateProperty("PainAlleviating", value);
                    this._painAlleviating = value;
                    this.RaiseDataMemberChanged("PainAlleviating");
                }
            }
        }
        private string _painAlleviating;
        // End PainAlleviating //
        // Start PainComments //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Comments")]
        [ProtoBuf.ProtoMemberAttribute(20)]
        public string PainComments
        {
            get
            {
                return this._painComments;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painComments = value; return; }
                if (this._painComments != value)
                {
                    this.RaiseDataMemberChanging("PainComments");
                    this.ValidateProperty("PainComments", value);
                    this._painComments = value;
                    this.RaiseDataMemberChanged("PainComments");
                }
            }
        }
        private string _painComments;
        // End PainComments //
        // Start PainDuration //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Duration")]
        [ProtoBuf.ProtoMemberAttribute(5)]
        public string PainDuration
        {
            get
            {
                return this._painDuration;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painDuration = value; return; }
                if (this._painDuration != value)
                {
                    this.RaiseDataMemberChanging("PainDuration");
                    this.ValidateProperty("PainDuration", value);
                    this._painDuration = value;
                    this.RaiseDataMemberChanged("PainDuration");
                }
            }
        }
        private string _painDuration;
        // End PainDuration //
        // Start PainFrequency //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Frequency")]
        [ProtoBuf.ProtoMemberAttribute(9)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        [System.ComponentModel.DataAnnotations.StringLengthAttribute(60)]
        public string PainFrequency
        {
            get
            {
                return this._painFrequency;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painFrequency = value; return; }
                if (this._painFrequency != value)
                {
                    this.RaiseDataMemberChanging("PainFrequency");
                    this.ValidateProperty("PainFrequency", value);
                    this._painFrequency = value;
                    this.RaiseDataMemberChanged("PainFrequency");
                }
            }
        }
        private string _painFrequency;
        // End PainFrequency //
        // Start PainInterference //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Interference with Daily Activities")]
        [ProtoBuf.ProtoMemberAttribute(10)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public string PainInterference
        {
            get
            {
                return this._painInterference;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painInterference = value; return; }
                if (this._painInterference != value)
                {
                    this.RaiseDataMemberChanging("PainInterference");
                    this.ValidateProperty("PainInterference", value);
                    this._painInterference = value;
                    this.RaiseDataMemberChanged("PainInterference");
                }
            }
        }
        private string _painInterference;
        // End PainInterference //
        // Start PainLocation //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Location")]
        [ProtoBuf.ProtoMemberAttribute(7)]
        [System.ComponentModel.DataAnnotations.RangeAttribute(1, 9.22337203685478E+18, ErrorMessage = @"The Location field is required.")]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public int PainLocation
        {
            get
            {
                return this._painLocation;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painLocation = value; return; }
                if (this._painLocation != value)
                {
                    this.RaiseDataMemberChanging("PainLocation");
                    this.ValidateProperty("PainLocation", value);
                    this._painLocation = value;
                    this.RaiseDataMemberChanged("PainLocation");
                }
            }
        }
        private int _painLocation;
        // End PainLocation //
        // Start PainQuality //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Quality")]
        [ProtoBuf.ProtoMemberAttribute(8)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public string PainQuality
        {
            get
            {
                return this._painQuality;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painQuality = value; return; }
                if (this._painQuality != value)
                {
                    this.RaiseDataMemberChanging("PainQuality");
                    this.ValidateProperty("PainQuality", value);
                    this._painQuality = value;
                    this.RaiseDataMemberChanged("PainQuality");
                }
            }
        }
        private string _painQuality;
        // End PainQuality //
        // Start PainRadiates //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Does pain radiate?")]
        [ProtoBuf.ProtoMemberAttribute(21)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public System.Nullable<bool> PainRadiates
        {
            get
            {
                return this._painRadiates;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painRadiates = value; return; }
                if (this._painRadiates != value)
                {
                    this.RaiseDataMemberChanging("PainRadiates");
                    this.ValidateProperty("PainRadiates", value);
                    this._painRadiates = value;
                    this.RaiseDataMemberChanged("PainRadiates");
                }
            }
        }
        private System.Nullable<bool> _painRadiates;
        // End PainRadiates //
        // Start PainSite //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Site")]
        [ProtoBuf.ProtoMemberAttribute(6)]
        public int PainSite
        {
            get
            {
                return this._painSite;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painSite = value; return; }
                if (this._painSite != value)
                {
                    this.RaiseDataMemberChanging("PainSite");
                    this.ValidateProperty("PainSite", value);
                    this._painSite = value;
                    this.RaiseDataMemberChanged("PainSite");
                }
            }
        }
        private int _painSite;
        // End PainSite //
        // Start PainSymptoms //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Associated Symptoms")]
        [ProtoBuf.ProtoMemberAttribute(13)]
        [System.ComponentModel.DataAnnotations.RequiredAttribute()]
        public string PainSymptoms
        {
            get
            {
                return this._painSymptoms;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._painSymptoms = value; return; }
                if (this._painSymptoms != value)
                {
                    this.RaiseDataMemberChanging("PainSymptoms");
                    this.ValidateProperty("PainSymptoms", value);
                    this._painSymptoms = value;
                    this.RaiseDataMemberChanged("PainSymptoms");
                }
            }
        }
        private string _painSymptoms;
        // End PainSymptoms //
        // Start RadiatesToLocation //
        public System.Nullable<int> RadiatesToLocation
        {
            get
            {
                return this._radiatesToLocation;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._radiatesToLocation = value; return; }
                if (this._radiatesToLocation != value)
                {
                    this.RaiseDataMemberChanging("RadiatesToLocation");
                    this.ValidateProperty("RadiatesToLocation", value);
                    this._radiatesToLocation = value;
                    this.RaiseDataMemberChanged("RadiatesToLocation");
                }
            }
        }
        private System.Nullable<int> _radiatesToLocation;
        // End RadiatesToLocation //
        // Start Resolved //
        public bool Resolved
        {
            get
            {
                return this._resolved;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._resolved = value; return; }
                if (this._resolved != value)
                {
                    this.RaiseDataMemberChanging("Resolved");
                    this.ValidateProperty("Resolved", value);
                    this._resolved = value;
                    this.RaiseDataMemberChanged("Resolved");
                }
            }
        }
        private bool _resolved;
        // End Resolved //
        // Start ResolvedBy //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(27)]
        public System.Nullable<System.Guid> ResolvedBy
        {
            get
            {
                return this._resolvedBy;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._resolvedBy = value; return; }
                if (this._resolvedBy != value)
                {
                    this.RaiseDataMemberChanging("ResolvedBy");
                    this.ValidateProperty("ResolvedBy", value);
                    this._resolvedBy = value;
                    this.RaiseDataMemberChanged("ResolvedBy");
                }
            }
        }
        private System.Nullable<System.Guid> _resolvedBy;
        // End ResolvedBy //
        // Start ResolvedDate //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(26)]
        public System.Nullable<System.DateTime> ResolvedDate
        {
            get
            {
                return this._resolvedDate;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._resolvedDate = value; return; }
                if (this._resolvedDate != value)
                {
                    this.RaiseDataMemberChanging("ResolvedDate");
                    this.ValidateProperty("ResolvedDate", value);
                    this._resolvedDate = value;
                    this.RaiseDataMemberChanged("ResolvedDate");
                }
            }
        }
        private System.Nullable<System.DateTime> _resolvedDate;
        // End ResolvedDate //
        // Start ResolvedFromEncounterKey //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(28)]
        public System.Nullable<int> ResolvedFromEncounterKey
        {
            get
            {
                return this._resolvedFromEncounterKey;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._resolvedFromEncounterKey = value; return; }
                if (this._resolvedFromEncounterKey != value)
                {
                    this.RaiseDataMemberChanging("ResolvedFromEncounterKey");
                    this.ValidateProperty("ResolvedFromEncounterKey", value);
                    this._resolvedFromEncounterKey = value;
                    this.RaiseDataMemberChanged("ResolvedFromEncounterKey");
                }
            }
        }
        private System.Nullable<int> _resolvedFromEncounterKey;
        // End ResolvedFromEncounterKey //
        // Start Superceded //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(14)]
        public bool Superceded
        {
            get
            {
                return this._superceded;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._superceded = value; return; }
                if (this._superceded != value)
                {
                    this.RaiseDataMemberChanging("Superceded");
                    this.ValidateProperty("Superceded", value);
                    this._superceded = value;
                    this.RaiseDataMemberChanged("Superceded");
                }
            }
        }
        private bool _superceded;
        // End Superceded //
        // Start TenantID //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(3)]
        [System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        public int TenantID
        {
            get
            {
                return this._tenantID;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._tenantID = value; return; }
                if (this._tenantID != value)
                {
                    this.RaiseDataMemberChanging("TenantID");
                    this.ValidateProperty("TenantID", value);
                    this._tenantID = value;
                    this.RaiseDataMemberChanged("TenantID");
                }
            }
        }
        private int _tenantID;
        // End TenantID //
        // Start UpdatedBy //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(17)]
        public System.Guid UpdatedBy
        {
            get
            {
                return this._updatedBy;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._updatedBy = value; return; }
                if (this._updatedBy != value)
                {
                    this.RaiseDataMemberChanging("UpdatedBy");
                    this.ValidateProperty("UpdatedBy", value);
                    this._updatedBy = value;
                    this.RaiseDataMemberChanged("UpdatedBy");
                }
            }
        }
        private System.Guid _updatedBy;
        // End UpdatedBy //
        // Start UpdatedDate //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(18)]
        public System.DateTime UpdatedDate
        {
            get
            {
                return this._updatedDate;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._updatedDate = value; return; }
                if (this._updatedDate != value)
                {
                    this.RaiseDataMemberChanging("UpdatedDate");
                    this.ValidateProperty("UpdatedDate", value);
                    this._updatedDate = value;
                    this.RaiseDataMemberChanged("UpdatedDate");
                }
            }
        }
        private System.DateTime _updatedDate;
        // End UpdatedDate //
        // Start Version //
        [System.Runtime.Serialization.DataMemberAttribute()]
        [ProtoBuf.ProtoMemberAttribute(23)]
        public int Version
        {
            get
            {
                return this._version;
            }
            set
            {
                if (this.IsProtoDeserializing) { this._version = value; return; }
                if (this._version != value)
                {
                    this.RaiseDataMemberChanging("Version");
                    this.ValidateProperty("Version", value);
                    this._version = value;
                    this.RaiseDataMemberChanged("Version");
                    this.OnVersionChanged();
                }
            }
        }
        private int _version;



        public bool IsProtoDeserializing { get; set; }

        [ProtoBuf.ProtoBeforeDeserialization]
        public void OnProtoDeserializing()
        {
            this.IsProtoDeserializing = true;
        }

        [ProtoBuf.ProtoAfterDeserialization]
        public void OnProtoDeserialized()
        {
            this.IsProtoDeserializing = false;
        }
        public override object GetIdentity()
        {
            return this._admissionPainLocationKey;
        }
    }

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



        public bool ShowPainDuration => Version > 2;

        public AdmissionPainLocation CreateNewVersion()
        {
            // Create a new version and reset the old to the original pre-edit state and mark as superceded
            var newpain = (AdmissionPainLocation)Clone(this);
            return newpain;
        }

        void OnPainRadiatesChanged()
        {

        }

        void OnAdmissionPainLocationKeyChanged()
        {

        }

        void OnAddedFromEncounterKeyChanged()
        {

        }

        void OnVersionChanged()
        {

        }

        void OnFirstIdentifiedDateChanged()
        {

        }

        void OnResolvedChanged()
        {

        }
    }
}
