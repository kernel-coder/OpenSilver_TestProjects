#region Usings

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

#endregion

namespace Virtuoso.Core.Utility
{
    public class ValidationResults : List<ValidationResult>
    {
        public void AddSingle(ValidationResult vr)
        {
            var found = this.Any(q =>
                (q.ErrorMessage == vr.ErrorMessage) && (vr.MemberNames.All(mn => q.MemberNames.Any(w => w == mn))));
            if (!found)
            {
                Add(vr);
            }
        }
    }
}