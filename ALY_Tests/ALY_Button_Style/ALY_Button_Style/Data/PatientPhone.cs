using System;
using System.Collections.Generic;
using System.Text;

namespace Virtuoso.Server.Data
{
	public sealed partial class PatientPhone : VirtuosoEntity
	{

		public PatientPhone()
		{
			//this.OnCreated();
			//Virtuoso.Core.Occasional.OfflineIDGenerator.Instance.SetKey(this);
		}


		// Alternate constructor - use for creating objects without generating an offline identity
		public PatientPhone(bool skipOnCreatedAndOfflineIDGeneration)
		{

		}

		// Start EffectiveFromDate //
		//[System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(Virtuoso.Validation.DateValidations), @"DateTimeValid")]
		[System.Runtime.Serialization.DataMemberAttribute()]
		[ProtoBuf.ProtoMemberAttribute(9)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public System.Nullable<System.DateTime> EffectiveFromDate
		{
			get
			{
				return this._effectiveFromDate;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._effectiveFromDate = value; return; }
				if (this._effectiveFromDate != value)
				{
					//this.OnEffectiveFromDateChanging(value);
					this.RaiseDataMemberChanging("EffectiveFromDate");
					this.ValidateProperty("EffectiveFromDate", value);
					this._effectiveFromDate = value;
					this.RaiseDataMemberChanged("EffectiveFromDate");
					//this.OnEffectiveFromDateChanged();
				}
			}
		}
		private System.Nullable<System.DateTime> _effectiveFromDate;
		// End EffectiveFromDate //
		// Start Extension //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Extension")]
		[ProtoBuf.ProtoMemberAttribute(7)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		[System.ComponentModel.DataAnnotations.StringLengthAttribute(6)]
		public string Extension
		{
			get
			{
				return this._extension;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._extension = value; return; }
				if (this._extension != value)
				{
					//this.OnExtensionChanging(value);
					this.RaiseDataMemberChanging("Extension");
					this.ValidateProperty("Extension", value);
					this._extension = value;
					this.RaiseDataMemberChanged("Extension");
					//this.OnExtensionChanged();
				}
			}
		}
		private string _extension;
		// End Extension //
		// Start HistoryKey //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[ProtoBuf.ProtoMemberAttribute(4)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public System.Nullable<int> HistoryKey
		{
			get
			{
				return this._historyKey;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._historyKey = value; return; }
				if (this._historyKey != value)
				{
					//this.OnHistoryKeyChanging(value);
					this.RaiseDataMemberChanging("HistoryKey");
					this.ValidateProperty("HistoryKey", value);
					this._historyKey = value;
					this.RaiseDataMemberChanged("HistoryKey");
					//this.OnHistoryKeyChanged();
				}
			}
		}
		private System.Nullable<int> _historyKey;
		// End HistoryKey //
		// Start Inactive //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Inactive")]
		[ProtoBuf.ProtoMemberAttribute(12)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public bool Inactive
		{
			get
			{
				return this._inactive;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._inactive = value; return; }
				if (this._inactive != value)
				{
					//this.OnInactiveChanging(value);
					this.RaiseDataMemberChanging("Inactive");
					this.ValidateProperty("Inactive", value);
					this._inactive = value;
					this.RaiseDataMemberChanged("Inactive");
					//this.OnInactiveChanged();
				}
			}
		}
		private bool _inactive;
		// End Inactive //
		// Start InactiveDate //
		//[System.ComponentModel.DataAnnotations.CustomValidationAttribute(typeof(Virtuoso.Validation.DateValidations), @"DateTimeValid")]
		[System.Runtime.Serialization.DataMemberAttribute()]
		[ProtoBuf.ProtoMemberAttribute(13)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public System.Nullable<System.DateTime> InactiveDate
		{
			get
			{
				return this._inactiveDate;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._inactiveDate = value; return; }
				if (this._inactiveDate != value)
				{
					//this.OnInactiveDateChanging(value);
					this.RaiseDataMemberChanging("InactiveDate");
					this.ValidateProperty("InactiveDate", value);
					this._inactiveDate = value;
					this.RaiseDataMemberChanged("InactiveDate");
					//this.OnInactiveDateChanged();
				}
			}
		}
		private System.Nullable<System.DateTime> _inactiveDate;
		// End InactiveDate //
		// Start Main //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Primary")]
		[ProtoBuf.ProtoMemberAttribute(5)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public bool Main
		{
			get
			{
				return this._main;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._main = value; return; }
				if (this._main != value)
				{
					//this.OnMainChanging(value);
					this.RaiseDataMemberChanging("Main");
					this.ValidateProperty("Main", value);
					this._main = value;
					this.RaiseDataMemberChanged("Main");
					//this.OnMainChanged();
				}
			}
		}
		private bool _main;
		// End Main //
		// Start Number //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Phone Number")]
		[ProtoBuf.ProtoMemberAttribute(6)]
		[System.ComponentModel.DataAnnotations.RegularExpressionAttribute(@"^\d{7}$|^\d{10}$", ErrorMessage = @"Invalid phone number format, must be 999.9999 or 999.999.9999")]
		[System.ComponentModel.DataAnnotations.RequiredAttribute()]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		[System.ComponentModel.DataAnnotations.StringLengthAttribute(10)]
		public string Number
		{
			get
			{
				return this._number;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._number = value; return; }
				if (this._number != value)
				{
					//this.OnNumberChanging(value);
					this.RaiseDataMemberChanging("Number");
					this.ValidateProperty("Number", value);
					this._number = value;
					this.RaiseDataMemberChanged("Number");
					//this.OnNumberChanged();
				}
			}
		}
		private string _number;
		// End Number //
		// Start PatientKey //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[ProtoBuf.ProtoMemberAttribute(2)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public int PatientKey
		{
			get
			{
				return this._patientKey;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._patientKey = value; return; }
				if (this._patientKey != value)
				{
					//this.OnPatientKeyChanging(value);
					this.RaiseDataMemberChanging("PatientKey");
					this.ValidateProperty("PatientKey", value);
					this._patientKey = value;
					this.RaiseDataMemberChanged("PatientKey");
					//this.OnPatientKeyChanged();
				}
			}
		}
		private int _patientKey;
		// End PatientKey //
		// Start PatientPhoneKey //
		[System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue = true)]
		[System.ComponentModel.DataAnnotations.KeyAttribute()]
		[ProtoBuf.ProtoMemberAttribute(1)]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public int PatientPhoneKey
		{
			get
			{
				return this._patientPhoneKey;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._patientPhoneKey = value; return; }
				if (this._patientPhoneKey != value)
				{
					//this.OnPatientPhoneKeyChanging(value);
					this.ValidateProperty("PatientPhoneKey", value);
					this._patientPhoneKey = value;
					this.RaisePropertyChanged("PatientPhoneKey");
					//this.OnPatientPhoneKeyChanged();
				}
			}
		}
		private int _patientPhoneKey;

		// End TenantID //
		// Start Type //
		[System.Runtime.Serialization.DataMemberAttribute()]
		[System.ComponentModel.DataAnnotations.DisplayAttribute(Name = @"Type")]
		[ProtoBuf.ProtoMemberAttribute(8)]
		[System.ComponentModel.DataAnnotations.RequiredAttribute()]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		public System.Nullable<int> Type
		{
			get
			{
				return this._type;
			}
			set
			{
				//if (this.IsProtoDeserializing) { this._type = value; return; }
				if (this._type != value)
				{
					//this.OnTypeChanging(value);
					this.RaiseDataMemberChanging("Type");
					this.ValidateProperty("Type", value);
					this._type = value;
					this.RaiseDataMemberChanged("Type");
					//this.OnTypeChanged();
				}
			}
		}
		private System.Nullable<int> _type;
	}

	public partial class PatientPhone
	{
		public int PhoneTypePriority
		{
			get
			{
				var code = "cell";
				if (string.IsNullOrWhiteSpace(code))
				{
					return 5;
				}

				code = code.ToLower();
				if (code == "cell")
				{
					return 1;
				}

				if (code == "home")
				{
					return 2;
				}

				if (code == "work")
				{
					return 3;
				}

				return 4;
			}
		}


		public string PhoneNumber
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Number))
				{
					return null;
				}

				return Number;

			}
		}
	}

}
