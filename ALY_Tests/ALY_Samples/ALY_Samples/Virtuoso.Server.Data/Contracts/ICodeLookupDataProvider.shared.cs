using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public interface ICodeLookupDataProvider
    {
        CodeLookup GetCodeLookupFromCode(string code, int tenantID);
        CodeLookup GetCodeLookupFromKey(int? key);
        string GetCodeLookupCodeFromKey(int? key);
        string GetCodeLookupCodeDescriptionFromKey(int? key);
        IEnumerable<CodeLookup> GetCodeLookupsFromType(string codeType);
    }
}
