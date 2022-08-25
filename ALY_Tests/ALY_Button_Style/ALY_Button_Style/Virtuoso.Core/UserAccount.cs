#region Usings

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Virtuoso.Portable.Extensions;

#endregion

namespace Virtuoso.Services.Core.Model
{
    public partial class User
    {
        // set min value to avoid DateTime serialization exception
        [DataMember] public DateTime CacheDate { get; set; } = DateTime.MinValue.ToUniversalTime();

        public bool IsFakeAccount { get; set; }

        [Display(Name = "Display Name")]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(FriendlyName))
                {
                    return FriendlyName;
                }

                return FullName;
            }
        }

        [Display(Name = "Formal Name")]
        public string FullName => FormatHelper.FormatName(LastName, FirstName, MiddleName);

        public User Clone()
        {
            return (User)MemberwiseClone();
        }
    }
}