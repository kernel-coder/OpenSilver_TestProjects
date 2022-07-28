using System;
using System.Collections.Generic;
using System.Text;

namespace Virtuoso.Server.Data
{
	public class ValidationTestEntity : VirtuosoEntity
	{

		[System.ComponentModel.DataAnnotations.RequiredAttribute()]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		[System.ComponentModel.DataAnnotations.StringLengthAttribute(40)]
		public string FirstName
		{
			get
			{
				return this._firstName;
			}
			set
			{
				if (this._firstName != value)
				{
					//this.OnFirstNameChanging(value);
					this.RaiseDataMemberChanging("FirstName");
					this.ValidateProperty("FirstName", value);
					this._firstName = value;
					this.RaiseDataMemberChanged("FirstName");
					//this.OnFirstNameChanged();
				}
			}
		}
		private string _firstName;


		public string NotRequiredName
		{
			get
			{
				return this._notRequiredName;
			}
			set
			{
				if (this._notRequiredName != value)
				{
					//this.OnFriendlyNameChanging(value);
					this.RaiseDataMemberChanging("NotRequiredName");
					this.ValidateProperty("NotRequiredName", value);
					this._notRequiredName = value;
					this.RaiseDataMemberChanged("NotRequiredName");
					//this.OnFriendlyNameChanged();
				}
			}
		}
		private string _notRequiredName;


		[System.ComponentModel.DataAnnotations.RequiredAttribute()]
		[System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
		[System.ComponentModel.DataAnnotations.StringLengthAttribute(40)]
		public string FriendlyName
		{
			get
			{
				return this._friendlyName;
			}
			set
			{
				if (this._friendlyName != value)
				{
					//this.OnFriendlyNameChanging(value);
					this.RaiseDataMemberChanging("FriendlyName");
					this.ValidateProperty("FriendlyName", value);
					this._friendlyName = value;
					this.RaiseDataMemberChanged("FriendlyName");
					//this.OnFriendlyNameChanged();
				}
			}
		}
		private string _friendlyName;


		
	}

}
