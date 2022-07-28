using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public partial class BillCodes
    {
        public string CodeTypeDescription
        {
            get
            {
                string description = "ZZZZZZZZZ";

                if (this.CodeLookup != null)
                {
                    description = this.CodeLookup.CodeDescription;
                }
                return description;
            }
        }
    }
}
